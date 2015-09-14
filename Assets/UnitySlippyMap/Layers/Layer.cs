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

namespace UnitySlippyMap.Layers
{

/// <summary>
/// An abstract class representing a map layer.
/// One can derive from it to add custom content to the map.
/// </summary>
public abstract class Layer : MonoBehaviour
{
	/// <summary>
	/// The <see cref="UnitySlippyMap.Map"/> instance to which this <see cref="UnitySlippyMap.Layers.Layer"/> instance belongs.
	/// </summary>
	public Map	Map;
	
	#region Protected members & properties
	
	/// <summary>
	/// The minimum zoom.
	/// </summary>
	protected float minZoom;
	
	/// <summary>
	/// Gets or sets the minimum zoom.
	/// </summary>
	/// <value>The minimum zoom.</value>
	public float MinZoom { get { return minZoom; } set { minZoom = value; } }
	
	/// <summary>
	/// The max zoom.
	/// </summary>
	protected float maxZoom;

	/// <summary>
	/// Gets or sets the max zoom.
	/// </summary>
	/// <value>The max zoom.</value>
	public float MaxZoom { get { return maxZoom; } set { maxZoom = value; } }
	
	#endregion
	
	#region Layer interface
	
	/// <summary>
	/// Updates the content.
	/// </summary>
	public abstract void UpdateContent();
	
	#endregion
}

}