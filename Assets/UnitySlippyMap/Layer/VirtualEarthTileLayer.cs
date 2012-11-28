// 
//  VirtualEarthTileLayer.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//#define DEBUG_LOG

using System;
using System.IO;
using System.Xml.Serialization;

using UnityEngine;

using UnitySlippyMap;

using System.Globalization;
using Microsoft.MapPoint;

// <summary>
// A class representing a VirtualEarth tile layer.
// </summary>
public class VirtualEarthTileLayer : TileLayer
{
    // http://msdn.microsoft.com/en-us/library/ff701712.aspx
    // http://msdn.microsoft.com/en-us/library/ff701716.aspx

    // TODO: summaries, arguments safeguards, subdomain rotations

    #region Private members & properties

    private bool            metadataURLChanged = false;
    private string          metadataURL = "http://dev.virtualearth.net/REST/V1/Imagery/Metadata/Road?mapVersion=v1&output=xml&key=";
    public string           MetadataURL { get { return metadataURL; } set { metadataURL = value; } }

    private bool            keyChanged = false;
    private string          key = String.Empty;
    public string           Key { get { return key; } set { keyChanged = true; key = value; } }

    private WWW             loader;

    private bool            isParsingMetadata = false;

    #endregion

    #region MonoBehaviour implementation

    private new void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if ((keyChanged || metadataURLChanged) && loader == null)
        {
#if DEBUG_LOG
            Debug.Log("DEBUG: VirtualEarthTileLayer.Update: launching metadata request on: " + metadataURL + key);
#endif

            // FIXME: assert the properties
            if (metadataURL != null && metadataURL != String.Empty
                && key != null && key != String.Empty)
                loader = new WWW(metadataURL + key);
            else
                loader = null;

            keyChanged = false;
            metadataURLChanged = false;
            isReadyToBeQueried = false;
        }
        else if (loader != null && loader.isDone)
        {
            if (loader.error != null || loader.text.Contains("404 Not Found"))
            {
#if DEBUG_LOG
				Debug.LogError("ERROR: VirtualEarthTileLayer.Update: loader [" + loader.url + "] error: " + loader.error + "(" + loader.text + ")");
#endif
                loader = null;
                return;
            }
            else
            {
                if (isParsingMetadata == false)
                {
#if DEBUG_LOG
                    Debug.Log("DEBUG: VirtualEarthTileLayer.Update: metadata response:\n" + loader.text);
#endif

                    byte[] bytes = loader.bytes;

                    isParsingMetadata = true;

                    UnityThreadHelper.TaskDistributor.Dispatch(() =>
                    {
                        UnitySlippyMap.VirtualEarth.Metadata metadata = null;
                        try
                        {
                            XmlSerializer xs = new XmlSerializer(typeof(UnitySlippyMap.VirtualEarth.Metadata), "http://schemas.microsoft.com/search/local/ws/rest/v1");
                            metadata = xs.Deserialize(new MemoryStream(bytes)) as UnitySlippyMap.VirtualEarth.Metadata;
                        }
                        catch (
                            Exception
#if DEBUG_LOG
                             e
#endif
                            )
                        {
#if DEBUG_LOG
                            Debug.LogError("ERROR: VirtualEarthTileLayer.Update: metadata deserialization exception:\n" + e.Source + " : " + e.InnerException + "\n" + e.Message + "\n" + e.StackTrace);
#endif
                            return;
                        }

                        UnityThreadHelper.Dispatcher.Dispatch(() =>
                        {
#if DEBUG_LOG
                            Debug.Log("DEBUG: VirtualEarthTileLayer.Update: ImageUrl: " + (metadata.ResourceSets[0].Resources[0] as UnitySlippyMap.VirtualEarth.ImageryMetadata).ImageUrl);
#endif

                            baseURL = (metadata.ResourceSets[0].Resources[0] as UnitySlippyMap.VirtualEarth.ImageryMetadata).ImageUrl.Replace("{culture}", CultureInfo.CurrentCulture.ToString());

                            isReadyToBeQueried = true;

                            loader = null;

                            isParsingMetadata = false;

                            if (needsToBeUpdatedWhenReady)
                            {
                                UpdateContent();
                                needsToBeUpdatedWhenReady = false;
                            }
                        });
                    });
                }
            }
        }
    }

    #endregion

    #region TileLayer implementation

    protected override void GetTileCountPerAxis(out int tileCountOnX, out int tileCountOnY)
    {
        tileCountOnX = tileCountOnY = (int)Mathf.Pow(2, Map.RoundedZoom);
    }

    protected override void GetCenterTile(out int tileX, out int tileY, out float offsetX, out float offsetZ)
    {
        int[] tileCoordinates = GeoHelpers.WGS84ToTile(Map.CenterWGS84[0], Map.CenterWGS84[1], Map.RoundedZoom);
        double[] centerTile = GeoHelpers.TileToWGS84(tileCoordinates[0], tileCoordinates[1], Map.RoundedZoom);
        double[] centerTileMeters = Map.WGS84ToEPSG900913Transform.Transform(centerTile); //GeoHelpers.WGS84ToMeters(centerTile[0], centerTile[1]);

        tileX = tileCoordinates[0];
        tileY = tileCoordinates[1];
        offsetX = Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913[0] - centerTileMeters[0]) * Map.RoundedScaleMultiplier;
        offsetZ = -Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913[1] - centerTileMeters[1]) * Map.RoundedScaleMultiplier;
    }

    protected override bool GetNeighbourTile(int tileX, int tileY, float offsetX, float offsetZ, int tileCountOnX, int tileCountOnY, NeighbourTileDirection dir, out int nTileX, out int nTileY, out float nOffsetX, out float nOffsetZ)
    {
        bool ret = false;
        nTileX = 0;
        nTileY = 0;
        nOffsetX = 0.0f;
        nOffsetZ = 0.0f;

        switch (dir)
        {
            case NeighbourTileDirection.South:
                if ((tileY + 1) < tileCountOnY)
                {
                    nTileX = tileX;
                    nTileY = tileY + 1;
                    nOffsetX = offsetX;
                    nOffsetZ = offsetZ - Map.RoundedHalfMapScale;
                    ret = true;
                }
                break;

            case NeighbourTileDirection.North:
                if (tileY > 0)
                {
                    nTileX = tileX;
                    nTileY = tileY - 1;
                    nOffsetX = offsetX;
                    nOffsetZ = offsetZ + Map.RoundedHalfMapScale;
                    ret = true;
                }
                break;

            case NeighbourTileDirection.East:
                if ((tileX + 1) < tileCountOnX)
                {
                    nTileX = tileX + 1;
                    nTileY = tileY;
                    nOffsetX = offsetX + Map.RoundedHalfMapScale;
                    nOffsetZ = offsetZ;
                    ret = true;
                }
                break;

            case NeighbourTileDirection.West:
                if (tileX > 0)
                {
                    nTileX = tileX - 1;
                    nTileY = tileY;
                    nOffsetX = offsetX - Map.RoundedHalfMapScale;
                    nOffsetZ = offsetZ;
                    ret = true;
                }
                break;
        }


        return ret;
    }

    protected override string GetTileURL(int tileX, int tileY, int roundedZoom)
    {
        string quadKey = TileSystem.TileXYToQuadKey(tileX, tileY, roundedZoom);
        return baseURL.Replace("{quadkey}", quadKey).Replace("{subdomain}", "t0");
    }
    #endregion
}

