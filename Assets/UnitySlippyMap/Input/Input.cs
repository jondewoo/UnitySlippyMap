// 
//  Inputs.cs
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

namespace UnitySlippyMap.Input
{

	/// <summary>
	/// Input delegate.
	/// </summary>
	public delegate void InputDelegate (MapBehaviour map,bool wasInputInterceptedByGUI);

	/// <summary>
	/// A class defining a basic set of user inputs.
	/// </summary>
	public static class MapInput
	{
		/// <summary>
		/// The last raycast hit position.
		/// </summary>
		private static Vector3	lastHitPosition = Vector3.zero;

		/// <summary>
		/// The last zoom factor.
		/// </summary>
		private static float	lastZoomFactor = 0.0f;

		/// <summary>
		/// Handles inputs on touch devices and desktop.
		/// The <see cref="UnitySlippyMap.Map.MapBehaviour"/> instance is told to update its layers and markers once a movement is complete.
		/// When panning the map, the map's root GameObject is moved. Once the panning is done, all the children are offseted and the root's position is reset.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="wasInputInterceptedByGUI">If set to <c>true</c> was input intercepted by GU.</param>
		public static void BasicTouchAndKeyboard (MapBehaviour map, bool wasInputInterceptedByGUI)
		{
			bool panning = false;
			bool panningStopped = false;
			Vector3 screenPosition = Vector3.zero;
    
			bool zooming = false;
			bool zoomingStopped = false;
			float zoomFactor = 0.0f;

			if (Application.platform == RuntimePlatform.IPhonePlayer
				|| Application.platform == RuntimePlatform.Android) {
				if (wasInputInterceptedByGUI == false) {
					int touchCount = UnityEngine.Input.touchCount;
					if (touchCount > 0) {
						// movements
						panning = true;
						panningStopped = true;
                    
						int validTouchCount = touchCount;
						foreach (Touch touch in UnityEngine.Input.touches) {
							if (touch.phase != TouchPhase.Ended) {
								screenPosition += new Vector3 (touch.position.x, touch.position.y);
								panningStopped = false;
							} else {
								--validTouchCount;
							}
    					
							// reset the last hit position to avoid a sudden jump when a finger is added or removed
							if (touch.phase == TouchPhase.Began
								|| touch.phase == TouchPhase.Ended)
								lastHitPosition = Vector3.zero;
						}
    				
						if (validTouchCount != 0)
							screenPosition /= validTouchCount;
						else {
							screenPosition = Vector3.zero;
							panningStopped = true;
						}
                    
						if (panningStopped)
							panning = false;
					}
                
					if (touchCount > 1) {
						// zoom
						zooming = true;
						zoomingStopped = true;
						bool newFingerSetup = false;

						int validTouchCount = touchCount;
						for (int i = 0; i < touchCount; ++i) {
							Touch touch = UnityEngine.Input.GetTouch (i);
                        
							if (touch.phase != TouchPhase.Ended) {
								zoomFactor += Vector3.Distance (screenPosition, new Vector3 (touch.position.x, touch.position.y));
								zoomingStopped = false;
							} else {
								--validTouchCount;
							}
    					
							// reset the last zoom factor to avoid a sudden jump when a finger is added or removed
							if (touch.phase == TouchPhase.Began
								|| touch.phase == TouchPhase.Ended)
								newFingerSetup = true;
						}
                    
						if (validTouchCount != 0)
							zoomFactor /= validTouchCount * 10.0f;
						else {
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
			} else {
				if (wasInputInterceptedByGUI == false) {
					// movements
					if (UnityEngine.Input.GetMouseButton (0)) {
						panning = true;
						screenPosition = UnityEngine.Input.mousePosition;
					} else if (UnityEngine.Input.GetMouseButtonUp (0)) {
						panningStopped = true;
					}
	    			
					// zoom
					if (UnityEngine.Input.GetKey (KeyCode.Z)) {
						zooming = true;
						zoomFactor = 1.0f;
						lastZoomFactor = 0.0f;
					} else if (UnityEngine.Input.GetKeyUp (KeyCode.Z)) {
						zoomingStopped = true;
					}
					if (UnityEngine.Input.GetKey (KeyCode.S)) {
						zooming = true;
						zoomFactor = -1.0f;
						lastZoomFactor = 0.0f;
					} else if (UnityEngine.Input.GetKeyUp (KeyCode.S)) {
						zoomingStopped = true;
					}
				}
			}
			
			if (panning) {
				// disable the centerWGS84 update with the last location
				map.UpdatesCenterWithLocation = false;
    			
				// apply the movements
				Ray ray = map.CurrentCamera.ScreenPointToRay (screenPosition);
				RaycastHit hitInfo;
				if (Physics.Raycast (ray, out hitInfo)) {
					Vector3 displacement = Vector3.zero;
					if (lastHitPosition != Vector3.zero) {
						displacement = hitInfo.point - lastHitPosition;
					}
					lastHitPosition = new Vector3 (hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
    				
					if (displacement != Vector3.zero) {
						// update the centerWGS84 property to the new centerWGS84 wgs84 coordinates of the map
						double[] displacementMeters = new double[2] {
							displacement.x / map.RoundedScaleMultiplier,
							displacement.z / map.RoundedScaleMultiplier
						};
						double[] centerMeters = new double[2] {
							map.CenterEPSG900913 [0],
							map.CenterEPSG900913 [1]
						};
						centerMeters [0] -= displacementMeters [0];
						centerMeters [1] -= displacementMeters [1];
						map.CenterEPSG900913 = centerMeters;
    					
						#if DEBUG_LOG
    					Debug.Log("DEBUG: Map.Update: new centerWGS84 wgs84: " + centerWGS84[0] + ", " + centerWGS84[1]);
						#endif
					}
    
					map.HasMoved = true;
				}
			} else if (panningStopped) {
				// reset the last hit position
				lastHitPosition = Vector3.zero;
    			
				// trigger a tile update
				map.IsDirty = true;
			}
    
			// apply the zoom
			if (zooming) {			
				map.Zoom (zoomFactor - lastZoomFactor);
				lastZoomFactor = zoomFactor;
			} else if (zoomingStopped) {
				lastZoomFactor = 0.0f;
			}
		}
	}
}

