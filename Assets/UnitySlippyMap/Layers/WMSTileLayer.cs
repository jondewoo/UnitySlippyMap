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

using UnityEngine;

using ProjNet.CoordinateSystems;
using System.Xml;

namespace UnitySlippyMap.Layers
{

	/// <summary>
	/// A class representing a Web Mapping Service tile layer.
	/// </summary>
	public class WMSTileLayer : WebTileLayer
	{
	#region Private members & properties

		/// <summary>
		/// Gets or sets the base URL.
		/// </summary>
		/// <value>The base URL.</value>
		public new string BaseURL {
			get { return baseURL; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (value == String.Empty)
					throw new Exception ("value cannot be empty");
				baseURLChanged = true;
				baseURL = value;
			}
		}

		/// <summary>
		/// The coma separated list of layers to be requested.
		/// </summary>
		private string layers = String.Empty;

		public string               Layers {
			get { return layers; }
			set {
				layers = value;
				if (layers == null)
					layers = String.Empty;
				else {
					CheckLayers ();
				}
			}
		}

		/// <summary>
		/// The Spatial Reference System of the layer.
		/// </summary>
		private ICoordinateSystem srs = GeographicCoordinateSystem.WGS84;

		/// <summary>
		/// Gets or sets the SRS.
		/// </summary>
		/// <value>The Spatial Reference System of the layer.</value>
		public ICoordinateSystem SRS {
			get { return srs; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				srs = value;
				srsName = srs.Authority + ":" + srs.AuthorityCode;
				CheckSRS ();
			}
		}

		/// <summary>
		/// The name of the srs.
		/// </summary>
		private string srsName = "EPSG:4326";

		/// <summary>
		/// Gets the name of the SRS.
		/// </summary>
		/// <value>The name of the SRS.</value>
		public string SRSName { get { return srsName; } }
    
		/// <summary>
		/// The image format to request.
		/// </summary>
		private string format = "image/png";

		/// <summary>
		/// Gets or sets the format.
		/// </summary>
		/// <value>The format.</value>
		public string Format {
			get { return format; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				format = value; 
			}
		}
	
		/// <summary>
		/// Set it to true to notify the WMSTileLayer to reload the capabilities.
		/// </summary>
		private bool baseURLChanged = false;

		/// <summary>
		/// The loader.
		/// </summary>
		private WWW loader;

		/// <summary>
		/// Set to true when the WMSTileLayer is parsing the capabilities.
		/// </summary>
		private bool isParsingGetCapabilities = false;
    
		/// <summary>
		/// The WMS capabilities.
		/// </summary>
		private UnitySlippyMap.WMS.WMT_MS_Capabilities  capabilities;

		/// <summary>
		/// Gets the capabilities.
		/// </summary>
		/// <value>The capabilities.</value>
		public UnitySlippyMap.WMS.WMT_MS_Capabilities Capabilities { get { return capabilities; } }

    #endregion

    #region MonoBehaviour implementation

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// </summary>
		private void Update ()
		{
			if (baseURLChanged && loader == null) {
#if DEBUG_LOG
				Debug.Log ("DEBUG: WMSTileLayer.Update: launching GetCapabilities on: " + baseURL);
#endif

				if (baseURL != null && baseURL != String.Empty)
					loader = new WWW (baseURL + (baseURL.EndsWith ("?") ? "" : "?") + "SERVICE=WMS&REQUEST=GetCapabilities&VERSION=1.1.1");
				else
					loader = null;

				baseURLChanged = false;
				isReadyToBeQueried = false;
			} else if (loader != null && loader.isDone) {
				if (loader.error != null || loader.text.Contains ("404 Not Found")) {
#if DEBUG_LOG
					Debug.LogError ("ERROR: WMSTileLayer.Update: loader [" + loader.url + "] error: " + loader.error + "(" + loader.text + ")");
#endif
					loader = null;
					return;
				} else {
					if (isParsingGetCapabilities == false) {
#if DEBUG_LOG
						Debug.Log ("DEBUG: WMSTileLayer.Update: GetCapabilities response:\n" + loader.text);
#endif

						byte[] bytes = loader.bytes;

						isParsingGetCapabilities = true;

						UnityThreadHelper.CreateThread (() =>
						{
							capabilities = null;
							try {
								XmlSerializer xs = new XmlSerializer (typeof(UnitySlippyMap.WMS.WMT_MS_Capabilities));
								using (XmlReader xr = XmlReader.Create(new MemoryStream(bytes),
							new XmlReaderSettings {
								ProhibitDtd = false
#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WEBPLAYER
								, XmlResolver = null
#endif
							})) {
									capabilities = xs.Deserialize (xr/*new MemoryStream(bytes)*/) as UnitySlippyMap.WMS.WMT_MS_Capabilities;
								}
							} catch (Exception
#if DEBUG_LOG
							e
#endif
							) {
#if DEBUG_LOG
								Debug.LogError ("ERROR: WMSTileLayer.Update: GetCapabilities deserialization exception:\n" + e.Source + " : " + e.InnerException + "\n" + e.Message + "\n" + e.StackTrace);
#endif
							}

#if DEBUG_LOG
							Debug.Log (String.Format (
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
#endif

							CheckLayers ();
							CheckSRS ();

							UnityThreadHelper.Dispatcher.Dispatch (() =>
							{
#if DEBUG_LOG
								if (capabilities != null) {
									string layers = String.Empty;
									foreach (UnitySlippyMap.WMS.Layer layer in capabilities.Capability.Layer.Layers) {
										layers += "'" + layer.Name + "': " + layer.Abstract + "\n";
									}
	
									Debug.Log ("DEBUG: WMSTileLayer.Update: layers: " + capabilities.Capability.Layer.Layers.Count + "\n" + layers);
								}
#endif

								isReadyToBeQueried = true;

								loader = null;

								isParsingGetCapabilities = false;

								if (needsToBeUpdatedWhenReady) {
									UpdateContent ();
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
		/// Gets the numbers of tiles on each axis in respect to the map's zoom level. See <see cref="UnitySlippyMap.Layers.TileLayer.GetTileCountPerAxis"/>.
		/// </summary>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		protected override void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY)
		{
			tileCountOnX = tileCountOnY = (int)Mathf.Pow (2, Map.RoundedZoom);
		}
	
		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the tile under the center of the map. See <see cref="UnitySlippyMap.Layers.TileLayer.GetCenterTile"/>.
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
			double[] centerTileMeters = Map.WGS84ToEPSG900913Transform.Transform (centerTile); //GeoHelpers.WGS84ToMeters(centerTile[0], centerTile[1]);

			tileX = tileCoordinates [0];
			tileY = tileCoordinates [1];
			offsetX = Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [0] - centerTileMeters [0]) * Map.RoundedScaleMultiplier;
			offsetZ = -Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [1] - centerTileMeters [1]) * Map.RoundedScaleMultiplier;
		}
	
		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the neighbour tile in the specified direction. See <see cref="UnitySlippyMap.Layers.TileLayer.GetNeighbourTile"/>.
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
		/// Gets the tile URL. See <see cref="UnitySlippyMap.Layers.WebTileLayer.GetTileURL"/>.
		/// </summary>
		/// <returns>The tile UR.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		protected override string GetTileURL (int tileX, int tileY, int roundedZoom)
		{
			double[] tile = GeoHelpers.TileToWGS84 (tileX, tileY, roundedZoom);
			double[] tileMeters = Map.WGS84ToEPSG900913Transform.Transform (tile); //GeoHelpers.WGS84ToMeters(tile[0], tile[1]);
			float tileSize = Map.TileResolution * Map.RoundedMetersPerPixel;
			double[] min = Map.EPSG900913ToWGS84Transform.Transform (new double[2] {
				tileMeters [0],
				tileMeters [1] - tileSize
			}); //GeoHelpers.MetersToWGS84(xmin, ymin);
			double[] max = Map.EPSG900913ToWGS84Transform.Transform (new double[2] {
				tileMeters [0] + tileSize,
				tileMeters [1]
			}); //GeoHelpers.MetersToWGS84(xmax, ymax);
			return baseURL + (baseURL.EndsWith ("?") ? "" : "?") + "SERVICE=WMS&REQUEST=GetMap&VERSION=1.1.1&LAYERS=" + layers + "&STYLES=&SRS=" + srsName + "&BBOX=" + min [0] + "," + min [1] + "," + max [0] + "," + max [1] + "&WIDTH=" + Map.TileResolution + "&HEIGHT=" + Map.TileResolution + "&FORMAT=" + format;
		}
	#endregion

    #region WMSTileLayer implementation

		/// <summary>
		/// Throws an exception if the layers' list is invalid.
		/// </summary>
		private void CheckLayers ()
		{
			if (capabilities == null
				|| capabilities.Capability == null
				|| capabilities.Capability.Layer == null
				|| capabilities.Capability.Layer.Layers == null)
				return;

			// check if the layers exist
			string[] layersArray = layers.Split (new Char[] { ',' });
			foreach (string layersArrayItem in layersArray) {
				bool exists = false;
				foreach (UnitySlippyMap.WMS.Layer layer in capabilities.Capability.Layer.Layers) {
					if (layersArrayItem == layer.Name) {
						exists = true;
						break;
					}
				}
				if (exists == false) {
#if DEBUG_LOG
					Debug.LogError ("layer '" + layersArrayItem + "' doesn't exist");
#endif
					throw new ArgumentException ("layer '" + layersArrayItem + "' doesn't exist");
				}
			}
		}

		/// <summary>
		/// Throws an exception if the SRS is invalid.
		/// </summary>
		private void CheckSRS ()
		{
			if (capabilities == null
				|| capabilities.Capability == null
				|| capabilities.Capability.Layer == null
				|| capabilities.Capability.Layer.SRS == null)
				return;

			// check if the srs is supported
			bool exists = false;
			foreach (string supportedSRS in capabilities.Capability.Layer.SRS) {
				if (supportedSRS == srsName) {
					exists = true;
					break;
				}
			}
			if (exists == false)
				throw new ArgumentException ("SRS '" + srsName + "' isn't supported");
		}

    #endregion
	}

}