// 
//  MBTilesLayer.cs
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

using UnityEngine;

namespace UnitySlippyMap.Layers
{

// <summary>
// An class representing an MBTiles tile layer.
// </summary>
	public class MBTilesLayer : DBTileLayer
	{
	#region Private members & properties

		/// <summary>
		/// The path to the MBTiles file to query. Changing the property triggers the loading.
		/// </summary>
		private string filepath;

		/// <summary>
		/// Gets or sets the filepath.
		/// </summary>
		/// <value>The path to the MBTiles file to query. Changing the property triggers the loading.</value>
		public string Filepath {
			get { return filepath; }
			set {
				filepath = value;
				if (filepath != null && filepath != String.Empty)
					Open ();
				else {
					Close ();
					throw new ArgumentException ("filepath must not be null or empty");
				}
			}
		}
	
		/// <summary>
		/// The bounds of the layer.
		/// </summary>
		private Rect bounds;

		/// <summary>
		/// Gets the bounds.
		/// </summary>
		/// <value>The bounds of the layer.</value>
		public Rect Bounds { get { return bounds; } }
	
		/// <summary>
		/// The center coordinates of the layer.
		/// </summary>
		private Vector3 center;

		/// <summary>
		/// Gets the center.
		/// </summary>
		/// <value>The center coordinates of the layer.</value>
		public Vector3 Center { get { return center; } }
	
		/// <summary>
		/// The name of the layer.
		/// </summary>
		private string _name;

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name of the layer.</value>
		public string Name { get { return _name; } }
	
		/// <summary>
		/// The description of the layer.
		/// </summary>
		private string description;

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>The description of the layer.</value>
		public string Description { get { return description; } }
	
		/// <summary>
		/// The attribution of the layer.
		/// </summary>
		private string attribution;

		/// <summary>
		/// Gets the attribution.
		/// </summary>
		/// <value>The attribution of the layer.</value>
		public string Attribution { get { return attribution; } }
	
		/// <summary>
		/// The template of the layer.
		/// </summary>
		private string template;

		/// <summary>
		/// Gets the template.
		/// </summary>
		/// <value>The template of the layer.</value>
		public string Template { get { return template; } }
	
		/// <summary>
		/// The MBTiles version of the database.
		/// </summary>
		private string version;

		/// <summary>
		/// Gets the version.
		/// </summary>
		/// <value>The MBTiles version of the database.</value>
		public string Version { get { return version; } }	
	
		/// <summary>
		/// The SQLite database.
		/// </summary>
		private SqliteDatabase db;
	
	#endregion

	#region Private methods
	
		/// <summary>
		/// The metadata row name looked for.
		/// </summary>
		private static string metadataRowNameLookedFor;

		/// <summary>
		/// The metadatas match predicate.
		/// </summary>
		/// <returns><c>true</c>, if metadatas matched, <c>false</c> otherwise.</returns>
		/// <param name="row">Row.</param>
		private static bool metadataMatchPredicate (DataRow row)
		{
			if ((row ["name"] as string) == metadataRowNameLookedFor)
				return true;
			return false;
		}
	
		/// <summary>
		/// Opens the MBTiles database file located at Filepath.
		/// </summary>
		private void Open ()
		{
			if (db != null)
				db.Close ();
		
			db = new SqliteDatabase ();
			db.Open (filepath);
		
			DataTable dt = db.ExecuteQuery ("SELECT * FROM metadata");

#if DEBUG_LOG
		string dbg = String.Empty;
		foreach (DataRow dbgRow in dt.Rows)
		{
			foreach (string col in dt.Columns)
			{
				dbg += "\t" + dbgRow[col];
			}
			dbg += "\n";
		}
		Debug.Log("DEBUG: MBTilesLayer.Update: metadata:\n" + dbg);
#endif
		
			metadataRowNameLookedFor = "version";
			DataRow row = dt.Rows.Find (metadataMatchPredicate);
			if (row == null) {
				db.Close ();
				throw new SqliteException ("missing 'version' in metadata");
			}
		
			version = row ["value"] as string;
			switch (version) {
			case "1.0.0":
				metadataRowNameLookedFor = "bounds";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					string[] tokens = (row ["value"] as string).Split (new Char[] { ',' });
					bounds = new Rect (Single.Parse (tokens [0]), Single.Parse (tokens [1]), Single.Parse (tokens [2]), Single.Parse (tokens [3]));
				}

				metadataRowNameLookedFor = "center";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					string[] tokens = (row ["value"] as string).Split (new Char[] { ',' });
					center = new Vector3 (Single.Parse (tokens [0]), Single.Parse (tokens [1]), Single.Parse (tokens [2]));
				}

				metadataRowNameLookedFor = "minzoom";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					minZoom = Single.Parse (row ["value"] as string);
				}

				metadataRowNameLookedFor = "maxzoom";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					maxZoom = Single.Parse (row ["value"] as string);
				}

				metadataRowNameLookedFor = "name";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					_name = row ["value"] as string;
				}

				metadataRowNameLookedFor = "description";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					description = row ["value"] as string;
				}

				metadataRowNameLookedFor = "attribution";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					attribution = row ["value"] as string;
				}

				metadataRowNameLookedFor = "template";
				row = dt.Rows.Find (metadataMatchPredicate);
				if (row != null) {
					template = row ["value"] as string;
				}
			
				break;
			default:
				throw new SqliteException ("unsupported SQLite version: " + version);
			}
		
			isReadyToBeQueried = true;
		}
	
		/// <summary>
		/// Closes the MBTiles database file.
		/// </summary>
		private void Close ()
		{
			isReadyToBeQueried = false;
			if (db != null) {
				db.Close ();
				db = null;
			}
		}
	
	#endregion
	
	#region MonoBehaviour implementation
	
		/// <summary>
		/// Update this instance.
		/// </summary>
		private void Update ()
		{
		}
	
	#endregion
	
    #region TileLayer implementation

		/// <summary>
		/// Gets the tile count per axis.
		/// </summary>
		/// <param name="tileCountOnX">Tile count on x.</param>
		/// <param name="tileCountOnY">Tile count on y.</param>
		protected override void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY)
		{
			tileCountOnX = tileCountOnY = (int)Mathf.Pow (2, Map.RoundedZoom);
		}
	
		/// <summary>
		/// Gets the center tile.
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
			tileY = tileCountOnY - (tileCoordinates [1] + 1);
			offsetX = Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [0] - centerTileMeters [0]) * Map.RoundedScaleMultiplier;
			offsetZ = -Map.RoundedHalfMapScale / 2.0f - (float)(Map.CenterEPSG900913 [1] - centerTileMeters [1]) * Map.RoundedScaleMultiplier;
		}
	
		/// <summary>
		/// Gets the neighbour tile.
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
				if (tileY > 0) {
					nTileX = tileX;
					nTileY = tileY - 1;
					nOffsetX = offsetX;
					nOffsetZ = offsetZ - Map.RoundedHalfMapScale;
					ret = true;
				}
				break;
			
			case NeighbourTileDirection.North:
				if ((tileY + 1) < tileCountOnY) {
					nTileX = tileX;
					nTileY = tileY + 1;
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
	
		/// <summary>
		/// Requests the tile's texture and assign it.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tile">Tile.</param>
		protected override void RequestTile (int tileX, int tileY, int roundedZoom, Tile tile)
		{
			if (db == null) {
				throw new NullReferenceException ("db");
			}
		
			DataTable dt = db.ExecuteQuery ("SELECT tile_data FROM tiles WHERE zoom_level=" + roundedZoom + " AND tile_column=" + tileX + " AND tile_row=" + tileY);
			if (dt.Rows.Count == 0) {
#if DEBUG_LOG
			Debug.LogWarning("WARNING: no rows in MBTiles db for tile: " + tileX + "," + tileY + "," + roundedZoom);
#endif
				return;
			}
		
			Texture2D tex = new Texture2D ((int)Map.TileResolution, (int)Map.TileResolution);
			if (tex.LoadImage ((byte[])dt.Rows [0] ["tile_data"]))
				tile.SetTexture (tex);
			else {
#if DEBUG_LOG
			Debug.LogError("ERROR: MBTilesLayer.RequestTile: couldn't load image for: " + tileX + "," + tileY + "," + roundedZoom);
#endif
			}
		}
	
		/// <summary>
		/// Cancels the request for the tile's texture.
		/// </summary>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <returns><c>true</c> if this instance cancel tile request the specified tileX tileY roundedZoom; otherwise, <c>false</c>.</returns>
		protected override void CancelTileRequest (int tileX, int tileY, int roundedZoom)
		{
			if (db == null) // TODO: exception
				return;
		}
	
	#endregion
	}

}