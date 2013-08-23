// 
//  Layer.cs
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

namespace UnitySlippyMap
{

// <summary>
// An abstract class representing a map layer.
// One can derive from it to add custom content to the map.
// </summary>
public abstract class Layer : MonoBehaviour
{
	public Map	Map;
	
	#region Protected members & properties
	
	protected float				minZoom;
	public float				MinZoom { get { return minZoom; } set { minZoom = value; } }
	
	protected float				maxZoom;
	public float				MaxZoom { get { return maxZoom; } set { maxZoom = value; } }
	
	#endregion
	
	#region Layer interface

	public abstract void UpdateContent();
	
	#endregion
}

}