// 
//  VirtualEarthTileLayer.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
// Copyright (c) 2017 Jonathan Derrough
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Xml.Serialization;
using System.Globalization;

using Microsoft.MapPoint;

using UnityEngine;

using UnitySlippyMap.Helpers;
using UnityEngine.Networking;
using System.Collections;

namespace UnitySlippyMap.Layers
{
	/// <summary>
	/// A class representing a VirtualEarth tile layer.
	/// See <see cref="http://msdn.microsoft.com/en-us/library/ff701712.aspx" />.
	/// See <see cref="http://msdn.microsoft.com/en-us/library/ff701716.aspx" />.
	/// </summary>
	public class VirtualEarthTileLayerBehaviour : WebTileLayerBehaviour
	{

    #region Private members & properties

		/// <summary>
		/// Set it to true to notify the VirtualEarthTileLayer to reload the metadata.
		/// </summary>
		private bool hostnameChanged = false;

		/// <summary>
		/// The hostname.
		/// </summary>
		private string hostname = "dev.virtualearth.net";

		/// <summary>
		/// Gets or sets the hostname.
		/// </summary>
		/// <value>The hostname.</value>
		public string Hostname {
			get { return hostname; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (value == String.Empty)
					throw new ArgumentException ("value cannot be empty");
				hostnameChanged = true;
				hostname = value;
			}
		}

		/// <summary>
		/// Set it if you are using a proxy (mandatory with Unity3D webplayer since http://dev.virtualearth.net/crossdomain.xml is not supported for some reason).
		/// </summary>
		private string proxyURL = null;

		/// <summary>
		/// Gets or sets the proxy URL.
		/// </summary>
		/// <value>The proxy URL.</value>
		public string ProxyURL { get { return proxyURL; } set { proxyURL = value; } }

		/// <summary>
		/// Set it to true to notify the VirtualEarthTileLayer to reload the metadata.
		/// </summary>
		private bool metadataRequestURIChanged = false;

		/// <summary>
		/// The request URI for the metada.
		/// </summary>
		private string metadataRequestURI = "/REST/V1/Imagery/Metadata/Road?mapVersion=v1&output=xml&key=";

		/// <summary>
		/// Gets or sets the metadata URL.
		/// </summary>
		/// <value>The metadata UR.</value>
		public string MetadataURL {
			get { return metadataRequestURI; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (value == String.Empty)
					throw new ArgumentException ("value cannot be empty");
				metadataRequestURIChanged = true;
				metadataRequestURI = value;
			} 
		}

		/// <summary>
		/// Set it to true to notify the VirtualEarthTileLayer to reload the metadata.
		/// </summary>
		private bool keyChanged = false;

		/// <summary>
		/// The authentication key to VirtualEarth service.
		/// </summary>
		private string key = String.Empty;

		/// <summary>
		/// Gets or sets the key.
		/// </summary>
		/// <value>The authentication key to VirtualEarth service.</value>
		public string Key {
			get { return key; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (value == String.Empty)
					throw new ArgumentException ("value cannot be empty");
				keyChanged = true;
				key = value; 
			} 
		}

		/// <summary>
		/// The loader.
		/// </summary>
		private Coroutine loaderCoroutine;

        /// <summary>
        /// Set to true when the VirtualEarthTileLayer is parsing the metadata.
        /// </summary>
        private bool isParsingMetadata = false;

    #endregion

    #region MonoBehaviour implementation

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Awake().
		/// </summary>
		private new void Awake ()
		{
			base.Awake ();
			minZoom = 1;
			maxZoom = 23;
		}

        /// <summary>
        /// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
        /// </summary>
        private void Update()
        {
            if ((hostnameChanged || keyChanged || metadataRequestURIChanged) && loaderCoroutine == null)
            {
                if (metadataRequestURI != null && metadataRequestURI != String.Empty && key != null && key != String.Empty)
                {
                    loaderCoroutine = StartCoroutine(GetMap());
                }
                else
                {
                    Debug.LogError("No base url has been set!");
                }
            }
        }

		private IEnumerator GetMap()
		{
            var url = "http://" + hostname + "/" + metadataRequestURI + key;
			if (proxyURL != null)
			{
				url = (proxyURL.StartsWith("http://") ? "" : "http://") + proxyURL + (proxyURL.EndsWith("?") ? "" : "?") + "url=" + UnityWebRequest.EscapeURL(url);
			}

            using (var www = UnityWebRequest.Get(url))
			{
				yield return www.SendWebRequest();

                hostnameChanged = false;
                keyChanged = false;
                metadataRequestURIChanged = false;
                isReadyToBeQueried = false;

                if (www.error != null || www.responseCode == 404)
                {
#if DEBUG_LOG
					Debug.LogError ("ERROR: VirtualEarthTileLayer.Update: loader [" + www.url + "] error: " + www.error);// + "(" + loader.text + ")");
#endif
                    loaderCoroutine = null;
                }
                else
                {
                    if (isParsingMetadata == false)
                    {
#if DEBUG_LOG
						Debug.Log ("DEBUG: VirtualEarthTileLayer.Update: metadata response:\n" + www.downloadHandler.text);
#endif

                        byte[] bytes = www.downloadHandler.data;

                        isParsingMetadata = true;

                        UnityThreadHelper.CreateThread(() =>
                        {
                            UnitySlippyMap.VirtualEarth.Metadata metadata = null;
                            try
                            {
                                XmlSerializer xs = new XmlSerializer(typeof(UnitySlippyMap.VirtualEarth.Metadata), "http://schemas.microsoft.com/search/local/ws/rest/v1");
                                metadata = xs.Deserialize(new MemoryStream(bytes)) as UnitySlippyMap.VirtualEarth.Metadata;

                                baseURL = (metadata.ResourceSets[0].Resources[0] as UnitySlippyMap.VirtualEarth.ImageryMetadata).ImageUrl.Replace("{culture}", CultureInfo.CurrentCulture.ToString());
                            }
                            catch (
                            Exception
#if DEBUG_LOG
                             e
#endif
                            )
                            {
#if DEBUG_LOG
								Debug.LogError ("ERROR: VirtualEarthTileLayer.Update: metadata deserialization exception:\n" + e.Source + " : " + e.InnerException + "\n" + e.Message + "\n" + e.StackTrace);
#endif
                            }

                            UnityThreadHelper.Dispatcher.Dispatch(() =>
                            {
#if DEBUG_LOG
								Debug.Log ("DEBUG: VirtualEarthTileLayer.Update: ImageUrl: " + (metadata.ResourceSets [0].Resources [0] as UnitySlippyMap.VirtualEarth.ImageryMetadata).ImageUrl);
#endif

                                isReadyToBeQueried = true;

                                loaderCoroutine = null;

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

		/// <summary>
		/// Gets the numbers of tiles on each axis in respect to the map's zoom level. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetTileCountPerAxis"/>.
		/// </summary>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		protected override void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY)
		{
			tileCountOnX = tileCountOnY = (int)Mathf.Pow (2, Map.RoundedZoom);
		}

		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the tile under the center of the map. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetCenterTile"/>.
		/// </summary>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="offsetX">Offset x.</param>
		/// <param name="offsetZ">Offset z.</param>
		protected override void GetCenterTile (int tileCountOnX, int tileCountOnY, out int tileX, out int tileY, out float offsetX, out float offsetZ)
		{
			int[] tileCoordinates = GeoHelpers.WGS84ToTile (Map.CenterWGS84 [0], Map.CenterWGS84 [1], Map.RoundedZoom);
			double[] centerTile = GeoHelpers.TileToWGS84 (tileCoordinates [0], tileCoordinates [1], Map.RoundedZoom);
			double[] centerTileMeters = Map.WGS84ToEPSG900913Transform.Transform (centerTile);

			tileX = tileCoordinates [0];
			tileY = tileCoordinates [1];
			offsetX = Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [0] - centerTileMeters [0]) * Map.RoundedScaleMultiplier;
			offsetZ = -Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [1] - centerTileMeters [1]) * Map.RoundedScaleMultiplier;
		}

		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the neighbour tile in the specified direction. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetNeighbourTile"/>.
		/// </summary>
		/// <returns><c>true</c>, if neighbour tile was gotten, <c>false</c> otherwise.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="offsetX">Offset x.</param>
		/// <param name="offsetZ">Offset z.</param>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		/// <param name="dir">Dir.</param>
		/// <param name="nTileX">N tile x.</param>
		/// <param name="nTileY">N tile y.</param>
		/// <param name="nOffsetX">N offset x.</param>
		/// <param name="nOffsetZ">N offset z.</param>
		protected override bool GetNeighbourTile (int tileX, int tileY, float offsetX, float offsetZ, int tileCountOnX, int tileCountOnY, NeighbourTileDirection dir, out int nTileX, out int nTileY, out float nOffsetX, out float nOffsetZ)
		{
			bool ret = false;
			nTileX = 0;
			nTileY = 0;
			nOffsetX = 0.0f;
			nOffsetZ = 0.0f;

			switch (dir) {
			case NeighbourTileDirection.South:
				if ((tileY + 1) < tileCountOnY) {
					nTileX = tileX;
					nTileY = tileY + 1;
					nOffsetX = offsetX;
					nOffsetZ = offsetZ - Map.RoundedHalfMapScale;
					ret = true;
				}
				break;

			case NeighbourTileDirection.North:
				if (tileY > 0) {
					nTileX = tileX;
					nTileY = tileY - 1;
					nOffsetX = offsetX;
					nOffsetZ = offsetZ + Map.RoundedHalfMapScale;
					ret = true;
				}
				break;

			case NeighbourTileDirection.East:
				nTileX = tileX + 1;
				nTileY = tileY;
				nOffsetX = offsetX + Map.RoundedHalfMapScale;
				nOffsetZ = offsetZ;
				ret = true;
				break;

			case NeighbourTileDirection.West:
				nTileX = tileX - 1;
				nTileY = tileY;
				nOffsetX = offsetX - Map.RoundedHalfMapScale;
				nOffsetZ = offsetZ;
				ret = true;
				break;
			}


			return ret;
		}
	
	#endregion
	
	#region WebTileLayer implementation
	
		/// <summary>
		/// Gets the tile URL. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetTileURL"/>.
		/// </summary>
		/// <returns>The tile URL.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		protected override string GetTileURL (int tileX, int tileY, int roundedZoom)
		{
			string quadKey = TileSystem.TileXYToQuadKey (tileX, tileY, roundedZoom);
			string url = baseURL.Replace ("{quadkey}", quadKey).Replace ("{subdomain}", "t0");
			if (proxyURL != null)
			{
				url = (proxyURL.StartsWith("http://") ? "" : "http://") + proxyURL + (proxyURL.EndsWith("?") ? "" : "?") + "key=" + key + "&url=" + UnityWebRequest.EscapeURL(url);
			}
			return url;
		}
	
    #endregion
	}

}