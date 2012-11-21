// 
//  WMSTileLayer.cs
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

#define DEBUG_LOG

using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

using UnityEngine;

using UnitySlippyMap;

// <summary>
// A class representing a Web Mapping Service tile layer.
// </summary>
public class WMSTileLayer : TileLayer
{
	#region Private members & properties

	public new string		BaseURL { get { return baseURL; } set { baseURLChanged = true; baseURL = value; } }

	private List<string>	layers = new List<string>();
	public List<string>		Layers
	{
		get { return layers; }
		set
		{
			layers = value;
			
			layersString = String.Empty;
			if (layers != null)
			{
				for (int i = 0; i < layers.Count; ++i)
				{
					if (layers[i] != null && layers[i].Length != 0)
					{
						if (layersString.Length != 0)
							layersString += ",";
						layersString += layers[i];
					}
				}
			}
		}
	}
	
	private string			layersString = String.Empty;
	
	private string			crs = "EPSG:4326";
	public string			CRS { get { return crs; } } // FIXME: test conversions again to enable wide crs support
	
	private string			format = "image/png";
	public string			Format { get { return format; } set { format = value; } }
	
	private bool			baseURLChanged = false;
	private WWW				loader;
	
	#endregion
	
	#region MonoBehaviour implementation
	
	private void Update()
	{
		if (baseURLChanged && loader == null)
		{
			if (baseURL != null && baseURL != String.Empty)
				loader = new WWW(baseURL + (baseURL.EndsWith("?") ? "" : "?") + "SERVICE=WMS&REQUEST=GetCapabilities&VERSION=1.1.1");
			else
				loader = null;
			baseURLChanged = false;
		}
		else if (loader != null && loader.isDone)
		{
			if (loader.error != null || loader.text.Contains("404 Not Found"))
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: WMSTileLayer.Update: loader [" + loader.url + "] error: " + loader.error + "(" + loader.text + ")");
#endif
				loader = null;
				return ;
			}
			else
			{
#if DEBUG_LOG
				Debug.Log("DEBUG: get capabilities:\n" + loader.text);
#endif
				XmlSerializer xs = new XmlSerializer(typeof (UnitySlippyMap.WMS.WMT_MS_Capabilities));
				UnitySlippyMap.WMS.WMT_MS_Capabilities capabilities = xs.Deserialize(new MemoryStream(loader.bytes)) as UnitySlippyMap.WMS.WMT_MS_Capabilities;
				
				Debug.Log(String.Format(
					"DEBUG: capabilities:\nversion: {0}\n" +
					"\tService:\n\t\tName: {1}\n\t\tTitle: {2}\n\t\tAbstract: {3}\n\t\tOnlineResource: {4}\n" + 
					"\t\tContactInformation:\n" +
					"\t\t\tContactAddress:\n\t\t\t\tAddressType: {5}\n\t\t\t\tAddress: {6}\n\t\t\t\tCity: {7}\n\t\t\t\tStateOrProvince: {8}\n\t\t\t\tPostCode: {9}\n\t\t\t\tCountry: {10}\n" +
					"\t\t\tContactElectronicMailAddress: {11}\n" +
					"\t\tFees: {12}\n",
					capabilities.version,
					capabilities.Service.Name,
					capabilities.Service.Title,
					capabilities.Service.Abstract,
					capabilities.Service.OnlineResource.href,
					capabilities.Service.ContactInformation.ContactAddress.AddressType,
					capabilities.Service.ContactInformation.ContactAddress.Address,
					capabilities.Service.ContactInformation.ContactAddress.City,
					capabilities.Service.ContactInformation.ContactAddress.StateOrProvince,
					capabilities.Service.ContactInformation.ContactAddress.PostCode,
					capabilities.Service.ContactInformation.ContactAddress.Country,
					capabilities.Service.ContactInformation.ContactElectronicMailAddress,
					capabilities.Service.Fees
					));

				string layers = String.Empty;
				foreach (UnitySlippyMap.WMS.Layer layer in capabilities.Capability.Layer.Layers)
				{
					layers += layer.Name + " " + layer.Abstract + "\n";
				}
				Debug.Log("DEBUG: layers: " + capabilities.Capability.Layer.Layers.Count + "\n" + layers);
				
				loader = null;
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
		double[] centerTileMeters = GeoHelpers.WGS84ToMeters(centerTile[0], centerTile[1]);

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
			break ;
			
		case NeighbourTileDirection.North:
			if (tileY > 0)
			{
	 			nTileX = tileX;
				nTileY = tileY - 1;
				nOffsetX = offsetX;
                nOffsetZ = offsetZ + Map.RoundedHalfMapScale;
                ret = true;
			}
			break ;
			
		case NeighbourTileDirection.East:
			if ((tileX + 1) < tileCountOnX)
			{
	 			nTileX = tileX + 1;
				nTileY = tileY;
                nOffsetX = offsetX + Map.RoundedHalfMapScale;
                nOffsetZ = offsetZ;
				ret = true;
			}
			break ;
			
		case NeighbourTileDirection.West:
			if (tileX > 0)
			{
	 			nTileX = tileX - 1;
				nTileY = tileY;
                nOffsetX = offsetX - Map.RoundedHalfMapScale;
                nOffsetZ = offsetZ;
				ret = true;
			}
			break ;
		}
		

		return ret;
	}
	
	protected override string GetTileURL(int tileX, int tileY, int roundedZoom)
	{
		double[] tile = GeoHelpers.TileToWGS84(tileX, tileY, roundedZoom);
		float halfTileSize = Map.RoundedHalfMapScale / 2.0f;
		double xmin = tile[0] - halfTileSize;
		double ymin = tile[1] - halfTileSize;
		double xmax = tile[0] + halfTileSize;
		double ymax = tile[1] + halfTileSize;
		return baseURL + (baseURL.EndsWith("?") ? "" : "?") + "SERVICE=WMS&REQUEST=GetMap&VERSION=1.1.1&LAYERS=" + layersString + "&STYLES=&CRS=" + crs + "&BBOX=" + xmin + "," + ymin + "," + xmax + "," + ymax + "&WIDTH=" + Map.TileResolution + "&HEIGHT=" + Map.TileResolution + "&FORMAT=" + format;
	}
	#endregion
}

