// 
//  GUI.cs
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

namespace UnitySlippyMap.GUI
{

	/// <summary>
	/// GUI delegate. Returns true if a button was pressed (or if inputs were intercepted in some way)
	/// </summary>
	public delegate bool GUIDelegate(Map map);
	
	public static class MapGUI
	{
		public static bool Zoom(Map map)
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

