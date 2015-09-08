// 
//  Inputs.cs
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

namespace UnitySlippyMap.Input
{

	/// <summary>
	/// Input delegate.
	/// </summary>
	public delegate void InputDelegate(Map map, bool wasInputInterceptedByGUI);
	
	public static class MapInput
	{
		private static Vector3	lastHitPosition = Vector3.zero;
		private static float	lastZoomFactor = 0.0f;
		
		public static void BasicTouchAndKeyboard(Map map, bool wasInputInterceptedByGUI)
		{
    		// handle inputs on touch devices and desktop
    		// the map is told to update its layers and markers once a movement is complete
    		// when panning the map, the map's root GameObject is moved ; once the panning is done, all the children are offseted and the root's position is reset
    		bool panning = false;
    		bool panningStopped = false;
    		Vector3 screenPosition = Vector3.zero;
    
    		bool zooming = false;
    		bool zoomingStopped = false;
    		float zoomFactor = 0.0f;

			if (Application.platform == RuntimePlatform.IPhonePlayer
	    		|| Application.platform == RuntimePlatform.Android)
    		{
				if (wasInputInterceptedByGUI == false)
				{
                int touchCount = UnityEngine.Input.touchCount;
    			if (touchCount > 0)
    			{
    				// movements
    				panning = true;
    				panningStopped = true;
                    
                    int validTouchCount = touchCount;
    				foreach (Touch touch in UnityEngine.Input.touches)
    				{
    					if (touch.phase != TouchPhase.Ended)
                        {
    	                    screenPosition += new Vector3(touch.position.x, touch.position.y);
        					panningStopped = false;
                        }
                        else
                        {
                            --validTouchCount;
                        }
    					
    					// reset the last hit position to avoid a sudden jump when a finger is added or removed
    					if (touch.phase == TouchPhase.Began
                            || touch.phase == TouchPhase.Ended)
    						lastHitPosition = Vector3.zero;
    				}
    				
                    if (validTouchCount != 0)
                        screenPosition /= validTouchCount;
                    else
                    {
                        screenPosition = Vector3.zero;
                        panningStopped = true;
                    }
                    
    				if (panningStopped)
    					panning = false;
                }
                
                if (touchCount > 1)
                {
    				// zoom
    				zooming = true;
    				zoomingStopped = true;
    				bool newFingerSetup = false;

                    int validTouchCount = touchCount;
                    for (int i = 0; i < touchCount; ++i)
    				{
                        Touch touch = UnityEngine.Input.GetTouch(i);
                        
    					if (touch.phase != TouchPhase.Ended)
                        {
                            zoomFactor += Vector3.Distance(screenPosition, new Vector3(touch.position.x, touch.position.y));
    						zoomingStopped = false;
                        }
                        else
                        {
                            --validTouchCount;
                        }
    					
    					// reset the last zoom factor to avoid a sudden jump when a finger is added or removed
    					if (touch.phase == TouchPhase.Began
    						|| touch.phase == TouchPhase.Ended)
    						newFingerSetup = true;
    				}
                    
                    if (validTouchCount != 0)
    				    zoomFactor /= validTouchCount * 10.0f;
                    else
                    {
                        zoomFactor = 0.0f;
                        zoomingStopped = true;
                    }
                    
                    /*
                    Debug.Log("DEBUG: zooming: touch count: " + validTouchCount + ", factor: " + zoomFactor + ", zooming stopped: " + zoomingStopped + ", new finger setup: " + newFingerSetup);
                    string dbg = "DEBUG: touches:\n";
                    for (int i = 0; i < touchCount; ++i)
                    {
                        Touch touch = Input.GetTouch(i);
                        dbg += touch.phase + "\n";
                    }
                    Debug.Log(dbg);
                    */
    				
    				if (newFingerSetup)
    					lastZoomFactor = zoomFactor;
    				if (zoomingStopped)
    					zooming = false;
    			}
				}
    		}
    		else
    		{
				if (wasInputInterceptedByGUI == false)
				{
	    			// movements
	    			if (UnityEngine.Input.GetMouseButton(0))
	    			{
                        panning = true;
	    				screenPosition = UnityEngine.Input.mousePosition;
	    			}
	    			else if (UnityEngine.Input.GetMouseButtonUp(0))
	    			{
	    				panningStopped = true;
	    			}
	    			
	    			// zoom
	    			if (UnityEngine.Input.GetKey(KeyCode.Z))
	    			{
	    				zooming = true;
	    				zoomFactor = 1.0f;
	    				lastZoomFactor = 0.0f;
	    			}
	    			else if (UnityEngine.Input.GetKeyUp(KeyCode.Z))
	    			{
	    				zoomingStopped = true;
	    			}
	    			if (UnityEngine.Input.GetKey(KeyCode.S))
	    			{
	    				zooming = true;
	    				zoomFactor = -1.0f;
	    				lastZoomFactor = 0.0f;
	    			}
	    			else if (UnityEngine.Input.GetKeyUp(KeyCode.S))
	    			{
	    				zoomingStopped = true;
	    			}
				}
    		}
			
    		if (panning)
    		{
    			// disable the centerWGS84 update with the last location
    			map.UpdatesCenterWithLocation = false;
    			
    			// apply the movements
    			Ray ray = map.CurrentCamera.ScreenPointToRay(screenPosition);
    			RaycastHit hitInfo;
    			if (Physics.Raycast(ray, out hitInfo))
    			{
    				Vector3 displacement = Vector3.zero;
    				if (lastHitPosition != Vector3.zero)
    				{
    					displacement = hitInfo.point - lastHitPosition;
    				}
    				lastHitPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
    				
    				if (displacement != Vector3.zero)
    				{
    					// update the centerWGS84 property to the new centerWGS84 wgs84 coordinates of the map
    					double[] displacementMeters = new double[2] { displacement.x / map.RoundedScaleMultiplier, displacement.z / map.RoundedScaleMultiplier };
    					double[] centerMeters = new double[2] { map.CenterEPSG900913[0], map.CenterEPSG900913[1] };
    					centerMeters[0] -= displacementMeters[0];
    					centerMeters[1] -= displacementMeters[1];
    					map.CenterEPSG900913 = centerMeters;
    					
    #if DEBUG_LOG
    					Debug.Log("DEBUG: Map.Update: new centerWGS84 wgs84: " + centerWGS84[0] + ", " + centerWGS84[1]);
    #endif
    				}
    
    				map.HasMoved = true;
    			}
    		}
    		else if (panningStopped)
    		{
    			// reset the last hit position
    			lastHitPosition = Vector3.zero;
    			
    			// trigger a tile update
    			map.IsDirty = true;
    		}
    
    		// apply the zoom
    		if (zooming)
    		{			
				map.Zoom(zoomFactor - lastZoomFactor);
    			lastZoomFactor = zoomFactor;
    		}
    		else if (zoomingStopped)
    		{
    			lastZoomFactor = 0.0f;
    		}
		}
	}
}

