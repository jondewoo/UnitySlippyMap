// 
//  LocationMarker.cs
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

namespace UnitySlippyMap.Markers
{
	/// <summary>
	/// The Location marker behaviour.
	/// </summary>
	public class LocationMarkerBehaviour : MarkerBehaviour
	{
		/// <summary>
		/// The orientation marker.
		/// </summary>
		private Transform orientationMarker;

		/// <summary>
		/// Gets or sets the orientation marker.
		/// </summary>
		/// <value>The orientation marker.</value>
		public Transform OrientationMarker {
			get { return orientationMarker; }
			set {
				if (orientationMarker != null) {
					orientationMarker.parent = null;
				}
            
				orientationMarker = value;
            
				if (orientationMarker != null) {
					orientationMarker.parent = this.transform;
					orientationMarker.localPosition = Vector3.zero; 
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
                orientationMarker.gameObject.SetActiveRecursively(this.gameObject.active);
#else
					orientationMarker.gameObject.SetActive (this.gameObject.activeSelf);
#endif
				}
			}
		}
	}

}