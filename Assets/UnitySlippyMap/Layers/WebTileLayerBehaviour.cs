// 
//  WebTileLayer.cs
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

using UnitySlippyMap.Map;

namespace UnitySlippyMap.Layers
{
	/// <summary>
	/// An abstract class representing a web tile layer.
	/// One can derive from it to leverage specific or custom tile services.
	/// </summary>
	public abstract class WebTileLayerBehaviour : TileLayerBehaviour
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
		/// Requests the tile's texture and assign it. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.RequestTile"/>.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tile">Tile.</param>
		protected override void RequestTile (int tileX, int tileY, int roundedZoom, TileBehaviour tile)
		{
			TileDownloaderBehaviour.Instance.Get (GetTileURL (tileX, tileY, roundedZoom), tile);
		}

		/// <summary>
		/// Cancels the request for the tile's texture. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.CancelTileRequest"/>.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <returns><c>true</c> if this instance cancel tile request the specified tileX tileY roundedZoom; otherwise, <c>false</c>.</returns>
		protected override void CancelTileRequest (int tileX, int tileY, int roundedZoom)
		{
			TileDownloaderBehaviour.Instance.Cancel (GetTileURL (tileX, tileY, roundedZoom));
		}
	
	#endregion
	
	#region WebTileLayer interface
	
		/// <summary>
		/// Gets the tile URL. See <see cref="UnitySlippyMap.Layers.TileLayerBehaviour.GetTileURL"/>.
		/// </summary>
		/// <returns>The tile URL.</returns>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		protected abstract string GetTileURL (int tileX, int tileY, int roundedZoom);

	#endregion
	}

}