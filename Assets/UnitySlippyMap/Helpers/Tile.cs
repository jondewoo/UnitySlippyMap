// 
//  Tile.cs
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

using UnityEngine;
using System;

// FIXME: not sure the use of a namespace is appropriate
namespace UnitySlippyMap
{
	// <summary>
	// Helper class ported mostly from: http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/
	// </summary>
	// TODO: refactor the whole thing
	public static class Tile
	{
		public static double	OriginShift = 2.0 * Math.PI * 6378137.0 / 2.0;
		public static float		MetersPerInch = 2.54f / 100.0f;
		public static double	EarthCircumference = 6378137.0 * Math.PI * 2.0;
		
		// <summary>
		// Converts WGS84 LatLon coordinates to OSM tile coordinates.
		// </summary>
		public static int[] WGS84ToTile(double lon, double lat, int zoom)
		{
			int[] p = new int[2];
			p[0] = (int)((lon + 180.0) / 360.0 * (1 << zoom));
			p[1] = (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 
				1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
		 
			return p;
		}
		
		// <summary>
		// Converts OSM tile coordinates to WGS84 LatLon coordinates (upper left corner of the tile).
		// </summary>
		public static double[] TileToWGS84(int tile_x, int tile_y, int zoom) 
		{
			double[] p = new double[2];
			double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));
		 
			p[0] = ((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
			p[1] = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
		 
			return p;
		}
		
		// <summary>
		// Converts WGS84 coordinates to EPSG 900913.
		// </summary>
		public static double[] WGS84ToMeters(double lon, double lat)
		{
			double[] p = new double[2];
			p[0] = lon * OriginShift / 180.0;
        	p[1] = Math.Log( Math.Tan((90.0 + lat) * Math.PI / 360.0 )) / (Math.PI / 180.0);

        	p[1] = p[1] * OriginShift / 180.0;
			
			return p;
		}
		
		// <summary>
		// Converts EPSG 900913 coordinates to WGS84.
		// </summary>
		public static double[] MetersToWGS84(double x, double y)
		{
			double[] p = new double[2];
			p[0] = (x / OriginShift) * 180.0;
        	p[1] = (y / OriginShift) * 180.0;

        	p[1] = 180 / Math.PI * (2.0 * Math.Atan( Math.Exp( p[1] * Math.PI / 180.0)) - Math.PI / 2.0);
			
			return p;
		}

		/*
		public static double[] WGS84OffsetToTileUpperLeftCorner(double lon, double lat, int zoom)
		{
			int[] tile = WGS84ToTile(lon, lat, zoom);
			double[] wgs84 = TileToWGS84(tile[0], tile[1], zoom);
			double[] offset = new double[2];
			
			offset[0] = lon - wgs84[0];
			offset[1] = lat - wgs84[1];
			
			return offset;
		}
		*/
		
		// <summary>
		// Returns the numbers of meters per pixel in respect to the latitude and zoom level of the map.
		// </summary>
		public static float MetersPerPixel(float latitude, float zoomLevel)
		{
		    double realLengthInMeters = EarthCircumference * Math.Cos (Mathf.Deg2Rad * latitude);
			return (float)(realLengthInMeters / Math.Pow(2.0, zoomLevel + 8));
		}
		
		/*
		public static double[] MetersToPixels(float x, float y, float zoomLevel)
		{
			double res = Resolution(zoomLevel);
			double[] p = new double[2];
			double OriginShift = 2.0 * Math.PI * 6378137.0 / 2.0;
			p[0] = (x + OriginShift) / res;
			p[1] = (y + OriginShift) / res;
			return p;
		}
		
		public static int[] PixelsToTMS(float px, float py)
		{
			int[] t = new int[2];
			t[0] = (int)( Mathf.Ceil( px / 256 ) - 1 );
        	t[1] = (int)( Mathf.Ceil( py / 256 ) - 1 );
			return t;
		}
		
		public static int[] MetersToTMS(float x, float y, float zoomLevel)
		{
			double[] p = MetersToPixels(x, y, zoomLevel);
			return PixelsToTMS((float)p[0], (float)p[1]);
		}
		
		public static int[] TMSToTile(int tx, int ty, float zoomLevel)
		{
			return new int[] { tx, (int)(Math.Pow(2, zoomLevel - 1) - ty) };
		}
		
		public static double Resolution(float zoomLevel)
		{
			return (2.0 * Math.PI * 6378137.0) / (256 * Math.Pow(2, (double)zoomLevel));
		}
		*/
		//
		
		// <summary>
		// Returns the Open Street Map zoom level in respect to the map scale, latitude, tile size and resolution.
		// </summary>
		public static float MapScaleToOsmZoomLevel(float mapScale, float latitude, float tileSize, float ppi)
		{
		    double realLengthInMeters = EarthCircumference * Math.Cos (Mathf.Deg2Rad * latitude);
		    double zoomLevelExp = (realLengthInMeters * ppi) / (tileSize * MetersPerInch * mapScale);
		
		    return (float) Math.Log(zoomLevelExp, 2.0);
		}
		
		// <summary>
		// Returns the map scale in respect to the Open Street Map zoom level, latitude, tile size and resolution.
		// </summary>
		public static float OsmZoomLevelToMapScale(float zoomLevel, float latitude, float tileSize, float ppi)
		{
		    double realLengthInMeters = EarthCircumference * Math.Cos (Mathf.Deg2Rad * latitude);
		
			double zoomLevelExp = Math.Pow(2.0, (double)zoomLevel);
		
		    return (float) ((realLengthInMeters * ppi) / zoomLevelExp / tileSize / MetersPerInch);
		}
		
		public enum AnchorPoint
		{
			TopLeft,
			TopCenter,
			TopRight,
			MiddleLeft,
			MiddleCenter,
			MiddleRight,
			BottomLeft,
			BottomCenter,
			BottomRight
		}
		
		// <summary>
		// Returns a tile template GameObject.
		// </summary>
		public static GameObject CreateTileTemplate()
		{
			return CreateTileTemplate("[Tile Template]", AnchorPoint.MiddleCenter);
		}
		public static GameObject CreateTileTemplate(string name)
		{
			return CreateTileTemplate(name, AnchorPoint.MiddleCenter);
		}
		public static GameObject CreateTileTemplate(AnchorPoint anchorPoint)
		{
			return CreateTileTemplate("[Tile Template]", anchorPoint);
		}
		public static GameObject CreateTileTemplate(string tileName, AnchorPoint anchorPoint)
		{
			GameObject tileTemplate = new GameObject(tileName);
			MeshFilter meshFilter = tileTemplate.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = tileTemplate.AddComponent<MeshRenderer>();
			BoxCollider boxCollider = tileTemplate.AddComponent<BoxCollider>();
			
			// add the geometry
			Mesh mesh = meshFilter.mesh;
			switch (anchorPoint)
			{
			case AnchorPoint.TopLeft:
				mesh.vertices = new Vector3[] {
					new Vector3(1.0f, 0.0f, 0.0f),
					new Vector3(1.0f, 0.0f, -1.0f),
					new Vector3(0.0f, 0.0f, -1.0f),
					new Vector3(0.0f, 0.0f, 0.0f)
				};
				break;
			case AnchorPoint.TopCenter:
				mesh.vertices = new Vector3[] {
					new Vector3(0.5f, 0.0f, 0.0f),
					new Vector3(0.5f, 0.0f, -1.0f),
					new Vector3(-0.5f, 0.0f, -1.0f),
					new Vector3(-0.5f, 0.0f, 0.0f)
				};
				break;
			case AnchorPoint.TopRight:
				mesh.vertices = new Vector3[] {
					new Vector3(0.0f, 0.0f, 0.0f),
					new Vector3(0.0f, 0.0f, -1.0f),
					new Vector3(-1.0f, 0.0f, -1.0f),
					new Vector3(-1.0f, 0.0f, 0.0f)
				};
				break;
			case AnchorPoint.MiddleLeft:
				mesh.vertices = new Vector3[] {
					new Vector3(1.0f, 0.0f, 0.5f),
					new Vector3(1.0f, 0.0f, -0.5f),
					new Vector3(0.0f, 0.0f, -0.5f),
					new Vector3(0.0f, 0.0f, 0.5f)
				};
				break;
			case AnchorPoint.MiddleRight:
				mesh.vertices = new Vector3[] {
					new Vector3(0.0f, 0.0f, 0.5f),
					new Vector3(0.0f, 0.0f, -0.5f),
					new Vector3(-1.0f, 0.0f, -0.5f),
					new Vector3(-1.0f, 0.0f, 0.5f)
				};
				break;
			case AnchorPoint.BottomLeft:
				mesh.vertices = new Vector3[] {
					new Vector3(1.0f, 0.0f, 1.0f),
					new Vector3(1.0f, 0.0f, 0.0f),
					new Vector3(0.0f, 0.0f, 0.0f),
					new Vector3(0.0f, 0.0f, 1.0f)
				};
				break;
			case AnchorPoint.BottomCenter:
				mesh.vertices = new Vector3[] {
					new Vector3(0.5f, 0.0f, 1.0f),
					new Vector3(0.5f, 0.0f, 0.0f),
					new Vector3(-0.5f, 0.0f, 0.0f),
					new Vector3(-0.5f, 0.0f, 1.0f)
				};
				break;
			case AnchorPoint.BottomRight:
				mesh.vertices = new Vector3[] {
					new Vector3(0.0f, 0.0f, 1.0f),
					new Vector3(0.0f, 0.0f, 0.0f),
					new Vector3(-1.0f, 0.0f, 0.0f),
					new Vector3(-1.0f, 0.0f, 1.0f)
				};
				break;
			default: // MiddleCenter
				mesh.vertices = new Vector3[] {
					new Vector3(0.5f, 0.0f, 0.5f),
					new Vector3(0.5f, 0.0f, -0.5f),
					new Vector3(-0.5f, 0.0f, -0.5f),
					new Vector3(-0.5f, 0.0f, 0.5f)
				};
				break;
			}
			mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		
			// add normals
			mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
			// add uv coordinates
			mesh.uv = new Vector2[] { new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f) };
			
			// add a material
            string shaderName = "Somian/Unlit/Transparent";
            Shader shader = Shader.Find(shaderName);
			
	#if DEBUG_LOG
			Debug.Log("DEBUG: shader for tile template: " + shaderName + ", exists: " + (shader != null));
	#endif
			
			meshRenderer.material = new Material(shader);
			
			// setup the collider
			boxCollider.size = new Vector3(1.0f, 0.0f, 1.0f);
			
			return tileTemplate;
		}
	}
}

