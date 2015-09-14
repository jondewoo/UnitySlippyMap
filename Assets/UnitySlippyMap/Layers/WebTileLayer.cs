// 
//  WebTileLayer.cs
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

namespace UnitySlippyMap.Layers
{

	/// <summary>
	/// An abstract class representing a web tile layer.
	/// One can derive from it to leverage specific or custom tile services.
	/// </summary>
	public abstract class WebTileLayer : TileLayer
	{
	#region Protected members & properties

		/// <summary>
		/// The base URL.
		/// </summary>
		protected string baseURL;

		/// <summary>
		/// Gets or sets the base URL.
		/// </summary>
		/// <value>The base URL.</value>
		public string BaseURL {
			get { return baseURL; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				baseURL = value;
			}
		}
	
	#endregion
	
    #region TileLayer implementation
	
		/// <summary>
		/// Requests the tile's texture and assign it. See <see cref="UnitySlippyMap.Layers.TileLayer.RequestTile"/>.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tile">Tile.</param>
		protected override void RequestTile (int tileX, int tileY, int roundedZoom, Tile tile)
		{
			TileDownloader.Instance.Get (GetTileURL (tileX, tileY, roundedZoom), tile);
		}

		/// <summary>
		/// Cancels the request for the tile's texture. See <see cref="UnitySlippyMap.Layers.TileLayer.CancelTileRequest"/>.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <returns><c>true</c> if this instance cancel tile request the specified tileX tileY roundedZoom; otherwise, <c>false</c>.</returns>
		protected override void CancelTileRequest (int tileX, int tileY, int roundedZoom)
		{
			TileDownloader.Instance.Cancel (GetTileURL (tileX, tileY, roundedZoom));
		}
	
	#endregion
	
	#region WebTileLayer interface
	
		/// <summary>
		/// Gets the tile URL. See <see cref="UnitySlippyMap.Layers.WebTileLayer.GetTileURL"/>.
		/// </summary>
		/// <returns>The tile URL.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		protected abstract string GetTileURL (int tileX, int tileY, int roundedZoom);

	#endregion
	}

}