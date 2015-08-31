// 
//  GeoHelpers.cs
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

namespace UnitySlippyMap
{
    // <summary>
    // Helper class ported mostly from: http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/
    // </summary>
    public class GeoHelpers
    {
        public static double OriginShift = 2.0 * Math.PI * 6378137.0 / 2.0;
        public static float MetersPerInch = 2.54f / 100.0f;
    	public static double HalfEarthCircumference = 6378137.0 * Math.PI;
		public static double EarthCircumference = HalfEarthCircumference * 2.0;

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
        // Returns the numbers of meters per pixel in respect to the latitude and zoom level of the map.
        // </summary>
        public static float MetersPerPixel(float latitude, float zoomLevel)
        {
            double realLengthInMeters = EarthCircumference * Math.Cos(Mathf.Deg2Rad * latitude);
            return (float)(realLengthInMeters / Math.Pow(2.0, zoomLevel + 8));
        }

        // <summary>
        // Returns the Open Street Map zoom level in respect to the map scale, latitude, tile size and resolution.
        // </summary>
        public static float MapScaleToOsmZoomLevel(float mapScale, float latitude, float tileSize, float ppi)
        {
            double realLengthInMeters = EarthCircumference * Math.Cos(Mathf.Deg2Rad * latitude);
            double zoomLevelExp = (realLengthInMeters * ppi) / (tileSize * MetersPerInch * mapScale);

            return (float)Math.Log(zoomLevelExp, 2.0);
        }

        // <summary>
        // Returns the map scale in respect to the Open Street Map zoom level, latitude, tile size and resolution.
        // </summary>
        public static float OsmZoomLevelToMapScale(float zoomLevel, float latitude, float tileSize, float ppi)
        {
            double realLengthInMeters = EarthCircumference * Math.Cos(Mathf.Deg2Rad * latitude);

            double zoomLevelExp = Math.Pow(2.0, (double)zoomLevel);

            return (float)((realLengthInMeters * ppi) / zoomLevelExp / tileSize / MetersPerInch);
        }
		
		// <summary>
		// Returns WGS84 given a RaycastHit and Map instance.
		// </summary>
		// <param name='r'>
		// The RaycastHit
		// </param>
		// <param name='m'>
		// The Map instance
		// </param>
		public static double[] RaycastHitToWGS84(Map map, RaycastHit r)
		{
			double[] RaycastHitToEPSG900913 = new double[]{(map.CenterEPSG900913[0]) + (r.point.x/map.ScaleMultiplier) , (map.CenterEPSG900913[1]) + (r.point.z/map.ScaleMultiplier)};
			return map.EPSG900913ToWGS84Transform.Transform(RaycastHitToEPSG900913);
		}

    }
}
