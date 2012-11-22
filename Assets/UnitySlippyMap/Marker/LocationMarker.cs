// 
//  LocationMarker.cs
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
using System.Collections;

public class LocationMarker : Marker
{
    private Transform orientationMarker;
	public Transform OrientationMarker
    {
        get { return orientationMarker; }
        set
        {
            if (orientationMarker != null)
            {
                orientationMarker.parent = null;
            }
            
            orientationMarker = value;
            
            if (orientationMarker != null)
            {
                orientationMarker.parent = this.transform;
                orientationMarker.localPosition = Vector3.zero;
#if UNITY_4_0
                orientationMarker.gameObject.SetActive(this.gameObject.activeSelf);
#else
                orientationMarker.gameObject.SetActiveRecursively(this.gameObject.active);
#endif
            }
        }
    }
}
