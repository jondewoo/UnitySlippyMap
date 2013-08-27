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

namespace UnitySlippyMap
{

// <summary>
// A class representing an Open Street Map tile layer.
// </summary>
public class OSMTileLayer : WebTileLayer
{
	#region Private members & properties
	
    /// <summary>
    /// The format for the URL parameters as in String.Format().
    /// </summary>
	private string		urlParametersFormat = "{0}/{1}/{2}";
	public string		URLParametersFormat
    {
        get { return urlParametersFormat; } 
        set 
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value == String.Empty)
                throw new ArgumentException("value cannot be empty");
            urlParametersFormat = value; 
        }
    }
    /// <summary>
    /// The extension of the tile files.
    /// </summary>
	private string		tileImageExtension = ".png";
    public string       TileImageExtension { get { return tileImageExtension; } set { tileImageExtension = value; if (tileImageExtension == null) tileImageExtension = String.Empty; } }
	
	#endregion

    #region OSMTileLayer implementation

    public OSMTileLayer()
    {
        isReadyToBeQueried = true;
    }

    #endregion

    #region MonoBehaviour implementation
    
    private new void Awake()
    {
        base.Awake();
        minZoom = 1;
        maxZoom = 18;
    }

    #endregion

    #region TileLayer implementation

    protected override void GetTileCountPerAxis(out int tileCountOnX, out int tileCountOnY)
	{
		tileCountOnX = tileCountOnY = (int)Mathf.Pow(2, Map.RoundedZoom);
	}
	
	protected override void GetCenterTile(int tileCountOnX, int tileCountOnY, out int tileX, out int tileY, out float offsetX, out float offsetZ)
	{
		int[] tileCoordinates = GeoHelpers.WGS84ToTile(Map.CenterWGS84[0], Map.CenterWGS84[1], Map.RoundedZoom);
		double[] centerTile = GeoHelpers.TileToWGS84(tileCoordinates[0], tileCoordinates[1], Map.RoundedZoom);
        double[] centerTileMeters = Map.WGS84ToEPSG900913Transform.Transform(centerTile); //GeoHelpers.WGS84ToMeters(centerTile[0], centerTile[1]);

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
			nTileX = tileX + 1;
			nTileY = tileY;
			nOffsetX = offsetX + Map.RoundedHalfMapScale;
            nOffsetZ = offsetZ;
			ret = true;
			break ;
			
		case NeighbourTileDirection.West:
			nTileX = tileX - 1;
			nTileY = tileY;
			nOffsetX = offsetX - Map.RoundedHalfMapScale;
            nOffsetZ = offsetZ;
			ret = true;
			break ;
		}
		

		return ret;
	}

	#endregion

	#region WebTileLayer implementation
	
	protected override string GetTileURL(int tileX, int tileY, int roundedZoom)
	{
        //double[] tile = GeoHelpers.TileToWGS84(tileX, tileY, roundedZoom);
        //Debug.Log("DEBUG: tile: " + tileX + " " + tileY + " => " + tile[0] + " " + tile[1]);
        return String.Format(Path.Combine(BaseURL, URLParametersFormat).Replace("\\", "/") + TileImageExtension, roundedZoom, tileX, tileY);
	}

	#endregion
}

}