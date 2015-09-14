// 
//  OSMTileLayer.cs
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
using System;

using UnityEngine;

using System.IO;

namespace UnitySlippyMap.Layers
{

	/// <summary>
	/// A class representing an Open Street Map tile layer.
	/// </summary>
	public class OSMTileLayer : WebTileLayer
	{
	#region Private members & properties
	
		/// <summary>
		/// The format for the URL parameters as in String.Format().
		/// </summary>
		private string urlParametersFormat = "{0}/{1}/{2}";

		/// <summary>
		/// Gets or sets the URL parameters format.
		/// </summary>
		/// <value>The format for the URL parameters as in String.Format().</value>
		public string URLParametersFormat {
			get { return urlParametersFormat; } 
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (value == String.Empty)
					throw new ArgumentException ("value cannot be empty");
				urlParametersFormat = value; 
			}
		}
	
		/// <summary>
		/// The extension of the tile files.
		/// </summary>
		private string tileImageExtension = ".png";

		/// <summary>
		/// Gets or sets the tile image extension.
		/// </summary>
		/// <value>The extension of the tile files.</value>
		public string TileImageExtension {
			get { return tileImageExtension; }
			set { tileImageExtension = value;
				if (tileImageExtension == null)
					tileImageExtension = String.Empty; }
		}
	
	#endregion

    #region OSMTileLayer implementation

		/// <summary>
		/// Initializes a new instance of the <see cref="UnitySlippyMap.Layers.OSMTileLayer"/> class.
		/// </summary>
		public OSMTileLayer ()
		{
			isReadyToBeQueried = true;
		}

    #endregion

    #region MonoBehaviour implementation
    
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Awake().
		/// </summary>
		private new void Awake ()
		{
			base.Awake ();
			minZoom = 1;
			maxZoom = 18;
		}

    #endregion

    #region TileLayer implementation

		/// <summary>
		/// Gets the tile count per axis. See <see cref="UnitySlippyMap.Layers.TileLayer.GetTileCountPerAxis"/>.
		/// </summary>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		protected override void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY)
		{
			tileCountOnX = tileCountOnY = (int)Mathf.Pow (2, Map.RoundedZoom);
		}
	
		/// <summary>
		/// Gets the center tile. See <see cref="UnitySlippyMap.Layers.TileLayer.GetCenterTile"/>.
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
		/// Gets a neighbour tile. See <see cref="UnitySlippyMap.Layers.TileLayer.GetNeighbourTile"/>.
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
		/// Gets a tile URL. See <see cref="UnitySlippyMap.Layers.WebTileLayer.GetTileURL"/>.
		/// </summary>
		/// <returns>The tile URL.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		protected override string GetTileURL (int tileX, int tileY, int roundedZoom)
		{
			return String.Format (Path.Combine (BaseURL, URLParametersFormat).Replace ("\\", "/") + TileImageExtension, roundedZoom, tileX, tileY);
		}

	#endregion
	}

}