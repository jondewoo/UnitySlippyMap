// 
//  Marker.cs
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

namespace UnitySlippyMap.Markers
{

// <summary>
// A simple marker class.
// </summary>
public class Marker : MonoBehaviour
{
	public Map Map;
	
	// <summary>
	// The coordinates of the marker in WGS84.
	// </summary>
	private double[]	coordinatesWGS84 = new double[2];
	public double[]		CoordinatesWGS84
	{
		get { return coordinatesWGS84; }
		set
		{
			if (value == null)
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Marker.CoordinatesWGS84: value cannot be null");
#endif
				return ;
			}
			
			coordinatesWGS84 = value;
            coordinatesEPSG900913 = Map.WGS84ToEPSG900913Transform.Transform(coordinatesWGS84); //GeoHelpers.WGS84ToMeters(coordinatesWGS84[0], coordinatesWGS84[1]);

			Reposition();
		}
	}
	
	// <summary>
	// The coordinates of the marker in EPSG 900913.
	// </summary>
	private double[]	coordinatesEPSG900913 = new double[2];
	public double[]		CoordinatesEPSG900913
	{
		get { return coordinatesEPSG900913; }
		set
		{
			if (value == null)
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Marker.CoordinatesEPSG900913: value cannot be null");
#endif
				return ;
			}
			
			coordinatesEPSG900913 = value;
            coordinatesWGS84 = Map.EPSG900913ToWGS84Transform.Transform(coordinatesEPSG900913); //GeoHelpers.MetersToWGS84(coordinatesEPSG900913[0], coordinatesEPSG900913[1]);

			Reposition();
		}
	}
	
	#region MonoBehaviour implementation
	
	protected void Update()
	{
		if (this.gameObject.transform.localScale.x != Map.HalfMapScale)
			this.gameObject.transform.localScale = new Vector3(Map.HalfMapScale, Map.HalfMapScale, Map.HalfMapScale);

		Reposition();
	}
	
	#endregion
	
	#region Private methods
	
	// <summary>
	// Places the marker to its 'real' position.
	// When zooming in and out, the marker's position at a specified zoom level in Unity3D shifts and needs to be corrected.
	// </summary>
	private void Reposition()
	{
		double[] offsetEPSG900913 = new double[2] { coordinatesEPSG900913[0] - Map.CenterEPSG900913[0], coordinatesEPSG900913[1] - Map.CenterEPSG900913[1] };
		
		double offset = offsetEPSG900913[0];
		if (offset < 0.0)
			offset = -offset;
		if (offset > GeoHelpers.HalfEarthCircumference)
			offsetEPSG900913[0] += GeoHelpers.EarthCircumference;
					
		this.gameObject.transform.position = new Vector3(
			offsetEPSG900913[0] == 0.0 ? 0.0f : (float)offsetEPSG900913[0] * Map.ScaleMultiplier,
			this.gameObject.transform.position.y,
			offsetEPSG900913[1] == 0.0 ? 0.0f : (float)offsetEPSG900913[1] * Map.ScaleMultiplier);
	}
	
	#endregion
	
	#region Public methods
	
	// <summary>
	// Offsets the marker's position before the map's root position is reset.
	// </summary>
	public virtual void UpdateMarker()
	{
		// move the marker by the map's root translation
		Vector3 displacement = Map.gameObject.transform.position;
		if (displacement != Vector3.zero)
		{
			this.gameObject.transform.position += displacement;
		}
	}
	
	#endregion
}

}