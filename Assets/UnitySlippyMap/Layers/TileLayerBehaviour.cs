// 
//  TileLayer.cs
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
using System.Collections.Generic;

using UnityEngine;

using UnitySlippyMap.Map;

namespace UnitySlippyMap.Layers
{

	/// <summary>
	/// An abstract class representing a tile layer.
	/// One can derive from it to leverage specific or custom tile services.
	/// </summary>
	public abstract class TileLayerBehaviour : LayerBehaviour
	{
	#region Protected members & properties

		/// <summary>
		/// The tile cache size limit.
		/// </summary>
		protected int tileCacheSizeLimit = 100;

		/// <summary>
		/// Gets or sets the tile cache size limit.
		/// </summary>
		/// <value>The tile cache size limit.</value>
		public int TileCacheSizeLimit {
			get { return tileCacheSizeLimit; }
			set { tileCacheSizeLimit = value; }
		}

		//public int									TileSize = 256;

		/// <summary>
		/// The shared tile template
		/// </summary>
		protected static TileBehaviour tileTemplate;

		/// <summary>
		/// The tile template use count.
		/// </summary>
		protected static int tileTemplateUseCount = 0;

		/// <summary>
		/// The tiles.
		/// </summary>
		protected Dictionary<string, TileBehaviour> tiles = new Dictionary<string, TileBehaviour> ();

		/// <summary>
		/// The tile cache.
		/// </summary>
		protected List<TileBehaviour> tileCache = new List<TileBehaviour> ();

		/// <summary>
		/// The visited tiles.
		/// </summary>
		protected List<string> visitedTiles = new List<string> ();

		/// <summary>
		/// The is ready to be queried flag.
		/// </summary>
		protected bool isReadyToBeQueried = false;

		/// <summary>
		/// The needs to be updated when ready flag.
		/// </summary>
		protected bool needsToBeUpdatedWhenReady = false;
    
	
		/// <summary>
		/// A enumeration of the tile directions.
		/// </summary>
		protected enum NeighbourTileDirection
		{
			North,
			South,
			East,
			West
		}
	
	#endregion
	
	#region MonoBehaviour implementation
	
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Awake().
		/// </summary>
		protected void Awake ()
		{
			// create the tile template if needed
			if (tileTemplate == null) {
				tileTemplate = TileBehaviour.CreateTileTemplate ();
				tileTemplate.hideFlags = HideFlags.HideAndDontSave;
				tileTemplate.GetComponent<Renderer>().enabled = false;
			}
			++tileTemplateUseCount;
		}

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Start().
		/// </summary>
		private void Start ()
		{		
			if (tileTemplate.transform.localScale.x != Map.RoundedHalfMapScale)
				tileTemplate.transform.localScale = new Vector3 (Map.RoundedHalfMapScale, 1.0f, Map.RoundedHalfMapScale);
		}

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.OnDestroy().
		/// </summary>
		private void OnDestroy ()
		{
			--tileTemplateUseCount;
		
			// destroy the tile template if nobody is using anymore
			if (tileTemplate != null && tileTemplateUseCount == 0)
				DestroyImmediate (tileTemplate);
		}
	
	#endregion
	
	#region Layer implementation
	
		/// <summary>
		/// Updates the content. See <see cref="UnitySlippyMap.Layers.Layer.UpdateContent"/>.
		/// </summary>
		public override void UpdateContent ()
		{
			if (tileTemplate.transform.localScale.x != Map.RoundedHalfMapScale)
				tileTemplate.transform.localScale = new Vector3 (Map.RoundedHalfMapScale, 1.0f, Map.RoundedHalfMapScale);

			if (Map.CurrentCamera != null && isReadyToBeQueried) {
				Plane[] frustum = GeometryUtility.CalculateFrustumPlanes (Map.CurrentCamera);

				visitedTiles.Clear ();

				UpdateTiles (frustum);

				CleanUpTiles (frustum, Map.RoundedZoom);
			} else
				needsToBeUpdatedWhenReady = true;
		
			// move the tiles by the map's root translation
			Vector3 displacement = Map.gameObject.transform.position;
			if (displacement != Vector3.zero) {
				foreach (KeyValuePair<string, TileBehaviour> tile in tiles) {
					tile.Value.transform.position += displacement;
				}
			}
		}
	
	#endregion
	
	#region Protected methods
	
		/// <summary>
		/// The tile address looked for.
		/// </summary>
		protected static string	tileAddressLookedFor;

		/// <summary>
		/// Visited tiles match predicate.
		/// </summary>
		/// <returns><c>true</c>, if tile address matched, <c>false</c> otherwise.</returns>
		/// <param name="tileAddress">Tile address.</param>
		protected static bool visitedTilesMatchPredicate (string tileAddress)
		{
			if (tileAddress == tileAddressLookedFor)
				return true;
			return false;
		}
	
	#endregion
		
	#region Private methods

		/// <summary>
		/// Checks if a tile is fully visible
		/// </summary>
		/// <returns><c>true</c>, if the tile exists, <c>false</c> otherwise.</returns>
		/// <param name="tileRoundedZoom">Tile rounded zoom.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		private bool CheckTileExistence (int tileRoundedZoom, int tileX, int tileY)
		{
			string key = TileBehaviour.GetTileKey (tileRoundedZoom, tileX, tileY);
			if (!tiles.ContainsKey (key))
				return true; // the tile is out of the frustum
			TileBehaviour tile = tiles [key];
			Renderer r = tile.GetComponent<Renderer>();
			return r.enabled && r.material.mainTexture != null && !tile.Showing;
		}

		/// <summary>
		/// Checks if a tile is covered by other tiles with a smaller rounded zoom 
		/// </summary>
		/// <returns><c>true</c>, if tile out existence was checked, <c>false</c> otherwise.</returns>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tileRoundedZoom">Tile rounded zoom.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		private bool CheckTileOutExistence (int roundedZoom, int tileRoundedZoom, int tileX, int tileY)
		{
			if (roundedZoom == tileRoundedZoom)
				return CheckTileExistence (tileRoundedZoom, tileX, tileY);
			return CheckTileOutExistence (roundedZoom, tileRoundedZoom - 1, tileX / 2, tileY / 2); 
		}

		/// <summary>
		/// Checks if a tile is covered by other tiles with a upper rounded zoom
		/// </summary>
		/// <returns><c>true</c>, if tile in existence was checked, <c>false</c> otherwise.</returns>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tileRoundedZoom">Tile rounded zoom.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		private bool CheckTileInExistence (int roundedZoom, int tileRoundedZoom, int tileX, int tileY)
		{
			if (roundedZoom == tileRoundedZoom)
				return CheckTileExistence (tileRoundedZoom, tileX, tileY);
			int currentRoundedZoom = tileRoundedZoom + 1;
			int currentTileX = tileX * 2;
			int currentTileY = tileY * 2;
			return CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX, currentTileY)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX + 1, currentTileY)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX, currentTileY + 1)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX + 1, currentTileY + 1);
		}

		/// <summary>
		/// Removes the tiles outside of the camera frustum and zoom level.
		/// </summary>
		/// <param name="frustum">Frustum.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		private void CleanUpTiles (Plane[] frustum, int roundedZoom)
		{
			List<string> tilesToRemove = new List<string> ();
			foreach (KeyValuePair<string, TileBehaviour> pair in tiles) {
				TileBehaviour tile = pair.Value;
				string tileKey = pair.Key;

				string[] tileAddressTokens = tileKey.Split ('_');
				int tileRoundedZoom = Int32.Parse (tileAddressTokens [0]);
				int tileX = Int32.Parse (tileAddressTokens [1]);
				int tileY = Int32.Parse (tileAddressTokens [2]);

				int roundedZoomDif = tileRoundedZoom - roundedZoom;
				bool inFrustum = GeometryUtility.TestPlanesAABB (frustum, tile.GetComponent<Collider>().bounds);

				if (!inFrustum || roundedZoomDif != 0) {
					CancelTileRequest (tileX, tileY, tileRoundedZoom);

					if (!inFrustum
						|| (roundedZoomDif > 0 && CheckTileOutExistence (roundedZoom, tileRoundedZoom, tileX, tileY))
						|| (roundedZoomDif < 0 && CheckTileInExistence (roundedZoom, tileRoundedZoom, tileX, tileY))) {
						tilesToRemove.Add (tileKey);
					}
				}
			}

			foreach (string tileAddress in tilesToRemove) {
				TileBehaviour tile = tiles [tileAddress];

				Renderer renderer = tile.GetComponent<Renderer>();
				if (renderer != null) {
					GameObject.DestroyImmediate (renderer.material.mainTexture);
					//TextureAtlasManager.Instance.RemoveTexture(pair.Value.TextureId);
					renderer.material.mainTexture = null;

					renderer.enabled = false;
				}

#if DEBUG_LOG
			Debug.Log("DEBUG: remove tile: " + pair.Key);
#endif

				tiles.Remove (tileAddress);
				tileCache.Add (tile);
			}
		}

		/// <summary>
		/// Updates the tiles in respect to the camera frustum and the map's zoom level.
		/// </summary>
		/// <param name="frustum">Frustum.</param>
		private void UpdateTiles (Plane[] frustum)
		{
			int tileX, tileY;
			int tileCountOnX, tileCountOnY;
			float offsetX, offsetZ;
		
			GetTileCountPerAxis (out tileCountOnX, out tileCountOnY);
			GetCenterTile (tileCountOnX, tileCountOnY, out tileX, out tileY, out offsetX, out offsetZ);
			GrowTiles (frustum, tileX, tileY, tileCountOnX, tileCountOnY, offsetX, offsetZ);
		}

		/// <summary>
		/// Grows the tiles recursively starting from the map's center in all four directions.
		/// </summary>
		/// <param name="frustum">Frustum.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		/// <param name="offsetX">Offset x.</param>
		/// <param name="offsetZ">Offset z.</param>
		void GrowTiles (Plane[] frustum, int tileX, int tileY, int tileCountOnX, int tileCountOnY, float offsetX, float offsetZ)
		{
			tileTemplate.transform.position = new Vector3 (offsetX, tileTemplate.transform.position.y, offsetZ);
			if (GeometryUtility.TestPlanesAABB (frustum, tileTemplate.GetComponent<Collider>().bounds) == true) {
				if (tileX < 0)
					tileX += tileCountOnX;
				else if (tileX >= tileCountOnX)
					tileX -= tileCountOnX;

				string tileAddress = TileBehaviour.GetTileKey (Map.RoundedZoom, tileX, tileY);
				//Debug.Log("DEBUG: tile address: " + tileAddress);
				if (tiles.ContainsKey (tileAddress) == false) {
					TileBehaviour tile = null;
					if (tileCache.Count > 0) {
						tile = tileCache [0];
						tileCache.Remove (tile);
						tile.transform.position = tileTemplate.transform.position;
						tile.transform.localScale = new Vector3 (Map.RoundedHalfMapScale, 1.0f, Map.RoundedHalfMapScale);
						//tile.gameObject.active = this.gameObject.active;
					} else {
						tile = (GameObject.Instantiate (tileTemplate.gameObject) as GameObject).GetComponent<TileBehaviour> ();
						tile.transform.parent = this.gameObject.transform;
					}
				
					tile.name = "tile_" + tileAddress;
					tiles.Add (tileAddress, tile);
				
					RequestTile (tileX, tileY, Map.RoundedZoom, tile);
				}
			
				tileAddressLookedFor = tileAddress;
				if (visitedTiles.Exists (visitedTilesMatchPredicate) == false) {
					visitedTiles.Add (tileAddress);

					// grow tiles in the four directions without getting outside of the coordinate range of the zoom level
					int nTileX, nTileY;
					float nOffsetX, nOffsetZ;

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.South, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.North, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.East, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.West, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);
				}
			}
		}
	
	#endregion
	
	#region TileLayer interface
	
		/// <summary>
		/// Gets the numbers of tiles on each axis in respect to the map's zoom level.
		/// </summary>
		protected abstract void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY);

		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the tile under the center of the map.
		/// </summary>
		protected abstract void GetCenterTile (int tileCountOnX, int tileCountOnY, out int tileX, out int tileY, out float offsetX, out float offsetZ);

		/// <summary>
		/// Gets the tile coordinates and offsets to the origin for the neighbour tile in the specified direction.
		/// </summary>
		protected abstract bool GetNeighbourTile (int tileX, int tileY, float offsetX, float offsetY, int tileCountOnX, int tileCountOnY, NeighbourTileDirection dir, out int nTileX, out int nTileY, out float nOffsetX, out float nOffsetZ);
	
		/// <summary>
		/// Requests the tile's texture and assign it.
		/// </summary>
		/// <param name='tileX'>
		/// Tile x.
		/// </param>
		/// <param name='tileY'>
		/// Tile y.
		/// </param>
		/// <param name='roundedZoom'>
		/// Rounded zoom.
		/// </param>
		/// <param name='tile'>
		/// Tile.
		/// </param>
		protected abstract void RequestTile (int tileX, int tileY, int roundedZoom, TileBehaviour tile);
	
		/// <summary>
		/// Cancels the request for the tile's texture.
		/// </summary>
		/// <param name='tileX'>
		/// Tile x.
		/// </param>
		/// <param name='tileY'>
		/// Tile y.
		/// </param>
		/// <param name='roundedZoom'>
		/// Rounded zoom.
		/// </param>
		protected abstract void CancelTileRequest (int tileX, int tileY, int roundedZoom);
	
	#endregion
	
	}

}