// 
//  GUI.cs
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

using UnityEngine;

using UnitySlippyMap.Map;

namespace UnitySlippyMap.GUI
{

	/// <summary>
	/// GUI delegate. Returns true if a button was pressed (or if inputs were intercepted in some way)
	/// </summary>
	public delegate bool GUIDelegate(MapBehaviour map);

	/// <summary>
	/// The Map GUI class.
	/// </summary>
	public static class MapGUI
	{
		/// <summary>
		/// Zoom the specified <see cref="UnitySlippyMap.Map.MapBehaviour"/> instance.
		/// </summary>
		/// <param name="map">Map.</param>
		public static bool Zoom(MapBehaviour map)
		{
			GUILayout.BeginVertical();
			
			GUILayout.Label("Zoom: " + map.CurrentZoom);
    		
			bool pressed = false;
    		if (GUILayout.RepeatButton("+"))
    		{
    			map.Zoom(1.0f);
				pressed = true;
    		}
    		if (GUILayout.RepeatButton("-"))
    		{
    			map.Zoom(-1.0f);
				pressed = true;
    		}
			
			GUILayout.EndVertical();
			
			return pressed;
		}
	}
}

