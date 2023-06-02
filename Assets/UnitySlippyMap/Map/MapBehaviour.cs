// 
//  Map.cs
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

using System;
using System.Collections.Generic;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;

using UnityEngine;

using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using UnitySlippyMap.GUI;
using UnitySlippyMap.Input;
using UnitySlippyMap.Helpers;

namespace UnitySlippyMap.Map
{
	/// <summary>
	/// The MapBehaviour class is a singleton handling layers and markers.
	/// Tiles are GameObjects (simple planes) parented to their layer's GameObject, in turn parented to the map's GameObject.
	/// Markers are empty GameObjects parented to the map's GameObject.
	/// The parenting is used to position the tiles and markers in a local referential using the map's center as origin.
	/// 
	/// Below is a basic example of how to create a map with a single OSM layer a few markers.
	/// </summary>
	/// <code>
	/// 
	///		using UnityEngine;
	///
	///		using System;
	///
	///		using UnitySlippyMap.Map;
	///		using UnitySlippyMap.Layers;
	///		using UnitySlippyMap.Markers;
	///
	/// 	public class TestMap : MonoBehaviour
	/// 	{
	///	 		private MapBehaviour map;
	///	
	///	 		public Texture	MarkerTexture;
	///	
	///	 		void Start()
	///	 		{
	///		 		// create the map singleton
	///		 		map = MapBehaviour.Instance;
	///		
	///				// 9 rue Gentil, Lyon, France
	///				map.CenterWGS84 = new double[2] { 4.83527, 45.76487 };
	///				map.UsesLocation = true;
	///				map.InputsEnabled = true;
	///
	///				// create a test layer
	///				TileLayerBehaviour layer = map.CreateLayer<OSMTileLayerBehaviour>("test tile layer");
	///				layer.URLFormat = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
	///
	///				// create some test 2D markers
	///				GameObject go = TileBehaviour.CreateTileTemplate();
	///				go.renderer.material.mainTexture = MarkerTexture;
	///				go.renderer.material.renderQueue = 4000;
	///
	///				GameObject markerGO;
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #1 - 9 rue Gentil, Lyon", new double[2] { 4.83527, 45.76487 }, markerGO);
	///		
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #2 - 31 rue de la Bourse, Lyon", new double[2] { 4.83699, 45.76535 }, markerGO);
	///		
	///				markerGO = Instantiate(go) as GameObject;
	///				map.CreateMarker<MarkerBehaviour>("test marker #3 - 1 place St Nizier, Lyon", new double[2] { 4.83295, 45.76468 }, markerGO);
	///
	///				DestroyImmediate(go);
	/// 
	///			}
	///	
	///			void OnApplicationQuit()
	///			{
	///				map = null;
	///			}
	///		}
	///
	/// </code>
	public class MapBehaviour : MonoBehaviour
	{
	#region Singleton stuff

		/// <summary>
		/// The instance.
		/// </summary>
		private static MapBehaviour instance = null;

		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> singleton.</value>
		public static MapBehaviour Instance {
			get {
				if (null == (object)instance) {
					instance = FindObjectOfType (typeof(MapBehaviour)) as MapBehaviour;
					if (null == (object)instance) {
						var go = new GameObject ("[Map]");
						//go.hideFlags = HideFlags.HideAndDontSave;
						instance = go.AddComponent<MapBehaviour> ();
						instance.EnsureMap ();
					}
				}

				return instance;
			}
		}
	
		/// <summary>
		/// Ensures the map.
		/// </summary>
		private void EnsureMap ()
		{
		}
	
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> class.
		/// </summary>
		private MapBehaviour ()
		{
		}

		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		private void OnDestroy ()
		{
			instance = null;
		}

		/// <summary>
		/// Raises the application quit event.
		/// </summary>
		private void OnApplicationQuit ()
		{
			DestroyImmediate (this.gameObject);
		}
	
	#endregion
	
	#region Variables & properties

		/// <summary>
		/// The current camera used to render the map.
		/// </summary>
		private Camera currentCamera;
		
		/// <summary>
		/// Gets or sets the current camera used to render the map.
		/// </summary>
		/// <value>The current camera used to render the map.</value>
		public Camera CurrentCamera {
			get { return currentCamera; }
			set { currentCamera = value; }
		}

		/// <summary>
		/// Indicates whether this instance is dirty and needs to be updated.
		/// </summary>
		private bool isDirty = false;

		/// <summary>
		/// Gets or sets a value indicating whether this instance is dirty and needs to be updated.
		/// </summary>
		/// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
		public bool IsDirty {
			get { return isDirty; }
			set { isDirty = value; }
		}

		/// <summary>
		/// The center coordinates of the map in the WGS84 coordinate system.
		/// </summary>
		private double[] centerWGS84 = new double[2];

		/// <summary>
		/// Gets or sets the center coordinates of the map in the WGS84 coordinate system.
		/// </summary>
		/// <value>
		/// When set, the map is refreshed and the <see cref="UnitySlippyMap.Map.CenterEPSG900913">center
		/// coordinates of the map in the EPSG 900913 coordinate system</see> are updated.
		/// </value>
		public double[] CenterWGS84 {
			get { return centerWGS84; }
			set {
				if (value == null) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterWGS84: value cannot be null");
#endif
					return;
				}
				
				if (value [0] > 180.0)
					value [0] -= 360.0;
				else if (value [0] < -180.0)
					value [0] += 360.0;
				
				centerWGS84 = value;

				double[] newCenterESPG900913 = wgs84ToEPSG900913Transform.Transform (centerWGS84);

				centerEPSG900913 = ComputeCenterEPSG900913 (newCenterESPG900913);

				Debug.Log("center: " + centerEPSG900913[0] + " " + centerEPSG900913[1]);

				FitVerticalBorder ();
				IsDirty = true;
			}
		}
	
		/// <summary>
		/// The center coordinates in the EPSG 900913 coordinate system.
		/// </summary>
		private double[] centerEPSG900913 = new double[2];
	
		/// <summary>
		/// Gets or sets the center coordinates in the EPSG 900913 coordinate system.
		/// </summary>
		/// <value>When set, the map is refreshed and the center coordinates of the map in WGS84 are also updated.</value>
		public double[] CenterEPSG900913 {
			get {
				return centerEPSG900913;
			}
			set {
				if (value == null) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterEPSG900913: value cannot be null");
#endif
					return;
				}

				centerEPSG900913 = ComputeCenterEPSG900913 (value);
				centerWGS84 = epsg900913ToWGS84Transform.Transform (centerEPSG900913);

				FitVerticalBorder ();
				IsDirty = true;
			}
		}
	
		// <summary>
		// Is used to constraint the map panning.
		// </summary>
		// TODO: implement the constraint
		//private double[]						size = new double[2];
	
		/// <summary>
		/// The current zoom.
		/// </summary>
		private float currentZoom;

		/// <summary>
		/// Gets or sets the current zoom.
		/// </summary>
		/// <value>When set, the map is refreshed.</value>
		public float CurrentZoom {
			get { return currentZoom; }
			set {
				if (value < minZoom
					|| value > maxZoom) {
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.Zoom: value must be inside range [" + minZoom + " - " + maxZoom + "]");
#endif
					return;
				}

				if (currentZoom == value)
					return;

				currentZoom = value;

				float diff = value - roundedZoom;
				if (diff > 0.0f && diff >= zoomStepLowerThreshold)
					roundedZoom = (int)Mathf.Ceil (currentZoom);
				else if (diff < 0.0f && diff <= -zoomStepUpperThreshold)
					roundedZoom = (int)Mathf.Floor (currentZoom);

				UpdateInternals ();

				FitVerticalBorder ();
			}
		}
	
		/// <summary>
		/// The zoom step upper threshold.
		/// </summary>
		private float zoomStepUpperThreshold = 0.8f;

		/// <summary>
		/// Gets or sets the zoom step upper threshold.
		/// </summary>
		/// <value>The zoom step upper threshold determines if the zoom level of the map should change when zooming out.</value>
		public float ZoomStepUpperThreshold {
			get { return zoomStepUpperThreshold; }
			set { zoomStepUpperThreshold = value; }
		}
	
		/// <summary>
		/// The zoom step lower threshold.
		/// </summary>
		private float zoomStepLowerThreshold = 0.2f;

		/// <summary>
		/// Gets or sets the zoom step lower threshold.
		/// </summary>
		/// <value>The zoom step upper threshold determines if the zoom level of the map should change when zooming in.</value>
		public float ZoomStepLowerThreshold {
			get { return zoomStepLowerThreshold; }
			set { zoomStepLowerThreshold = value; }
		}
	
		/// <summary>
		/// The minimum zoom level for this map.
		/// </summary>
		private float minZoom = 3.0f;

		/// <summary>
		/// Gets or sets the minimum zoom.
		/// </summary>
		/// <value>
		/// This is the mininum zoom value for the map.
		/// Inferior zoom values are clamped when setting the <see cref="UnitySlippyMap.Map.CurrentZoom"/>.
		/// Additionally, values are always clamped between 3 and 19.
		/// </value>
		public float MinZoom {
			get { return minZoom; }
			set {
				if (value < 3.0f
					|| value > 19.0f) {
					minZoom = Mathf.Clamp (value, 3.0f, 19.0f);
				} else {		
					minZoom = value;
				}
			
				if (minZoom > maxZoom) {
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MinZoom: clamp value [" + minZoom + "] to max zoom [" + maxZoom + "]");
#endif
					minZoom = maxZoom;
				}
			}
		}
	
		/// <summary>
		/// The maximum zoom level for this map.
		/// </summary>
		private float maxZoom = 19.0f;

		/// <summary>
		/// Gets or sets the maximum zoom.
		/// </summary>
		/// <value>
		/// This is the maximum zoom value for the map.
		/// Superior zoom values are clamped when setting the <see cref="UnitySlippyMap.Map.CurrentZoom"/>.
		/// Additionally, values are always clamped between 3 and 19.
		/// </value>
		public float MaxZoom {
			get { return maxZoom; }
			set {
				if (value < 3.0f
					|| value > 19.0f) {
					maxZoom = Mathf.Clamp (value, 3.0f, 19.0f);
				} else {		
					maxZoom = value;
				}
			
				if (maxZoom < minZoom) {
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MaxZoom: clamp value [" + maxZoom + "] to min zoom [" + minZoom + "]");
#endif
					maxZoom = minZoom;
				}
			}
		}

		/// <summary>
		/// The rounded zoom.
		/// </summary>
		/// <value>It is updated when <see cref="UnitySlippyMap.Map.CurrentZoom"/> is set.</value>
		private int roundedZoom;

		/// <summary>
		/// Gets the rounded zoom.
		/// </summary>
		/// <value>The rounded zoom is updated when <see cref="UnitySlippyMap.Map.CurrentZoom"/> is set.</value>
		public int RoundedZoom { get { return roundedZoom; } }
	
		/// <summary>
		/// The half map scale.
		/// </summary>
		/// <value>
		/// It is used throughout the implementation to rule the camera elevation
		/// and the size/scale of the tiles.
		/// </value>
		private float halfMapScale = 0.0f;

		/// <summary>
		/// Gets the half map scale.
		/// </summary>
		/// <value>
		/// The half map scale is a value used throughout the implementation to rule the camera elevation
		/// and the size/scale of the tiles.
		/// </value>
		public float HalfMapScale { get { return halfMapScale; } }
	
		/// <summary>
		/// The rounded half map scale.
		/// </summary>
		private float roundedHalfMapScale = 0.0f;

		/// <summary>
		/// Gets the rounded half map scale.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.HalfMapScale"/> .</value>
		public float RoundedHalfMapScale { get { return roundedHalfMapScale; } }
	
		/// <summary>
		/// The number of meters per pixel in respect to the latitude and zoom level of the map.
		/// </summary>
		private float metersPerPixel = 0.0f;

		/// <summary>
		/// Gets the meters per pixel.
		/// </summary>
		/// <value>The number of meters per pixel in respect to the latitude and zoom level of the map.</value>
		public float MetersPerPixel { get { return metersPerPixel; } }

		/// <summary>
		/// The rounded meters per pixel.
		/// </summary>
		private float roundedMetersPerPixel = 0.0f;

		/// <summary>
		/// Gets the rounded meters per pixel.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.MetersPerPixel"/>.</value>
		public float RoundedMetersPerPixel { get { return roundedMetersPerPixel; } }

		/// <summary>
		/// The scale multiplier.
		/// </summary>
		/// <value>It helps converting meters (EPSG 900913) to Unity3D world coordinates.</value>
		private float scaleMultiplier = 0.0f;

		/// <summary>
		/// Gets the scale multiplier.
		/// </summary>
		/// <value>The scale multiplier helps converting meters (EPSG 900913) to Unity3D world coordinates.</value>
		public float ScaleMultiplier { get { return scaleMultiplier; } }

		/// <summary>
		/// The rounded scale multiplier.
		/// </summary>
		private float roundedScaleMultiplier = 0.0f;

		/// <summary>
		/// Gets the rounded scale multiplier.
		/// </summary>
		/// <value>See <see cref="UnitySlippyMap.Map.ScaleMultiplier"/>.</value>
		public float RoundedScaleMultiplier { get { return roundedScaleMultiplier; } }

		/// <summary>
		/// The scale divider.
		/// </summary>
		/// <value>
		/// It is an arbitrary value used to keep values within single floating point range when converting coordinates
		/// to Unity3D world coordinates.</value>
		private float scaleDivider = 20000.0f;

		/// <summary>
		/// The tile resolution.
		/// </summary>
		private float tileResolution = 256.0f;

		/// <summary>
		/// Gets the tile resolution.
		/// </summary>
		/// <value>The tile resolution in pixels.</value>
		public float TileResolution { get { return tileResolution; } }

		/// <summary>
		/// The screen scale.
		/// </summary>
		private float screenScale = 1.0f;

		/// <summary>
		/// The "uses location" flag.
		/// </summary>
		/// <value>It indicates whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's location.</value>
		private bool usesLocation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's location.
		/// </summary>
		/// <value><c>true</c> if uses location; otherwise, <c>false</c>.</value>
		public bool UsesLocation {
			get { return usesLocation; }
			set {
				if (usesLocation == value)
					return;
			
				usesLocation = value;
			
				if (usesLocation) {
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
						|| UnityEngine.Input.location.status == LocationServiceStatus.Failed)) {
						UnityEngine.Input.location.Start ();
					} else {
#if DEBUG_LOG
					Debug.LogError("ERROR: Map.UseLocation: Location is not authorized on the device.");
#endif
					}
				} else {
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
						|| UnityEngine.Input.location.status == LocationServiceStatus.Running)) {
						UnityEngine.Input.location.Start ();
					}
				}
			}
		}
	
		/// <summary>
		/// The "updates center with location" flag.
		/// </summary>
		private bool updatesCenterWithLocation = true;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> updates its center with the host's location.
		/// </summary>
		/// <value>
		/// <c>true</c> if update center with location; otherwise, <c>false</c>.
		/// It is automatically set to <c>false</c> when the map is manipulated by the user.
		/// </value>
		public bool UpdatesCenterWithLocation {
			get {
				return updatesCenterWithLocation;
			}
		
			set {
				updatesCenterWithLocation = value;
			}
		}
	
		/// <summary>
		/// The "uses orientation" flag.
		/// </summary>
		private bool usesOrientation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> uses the host's orientation.
		/// </summary>
		/// <value><c>true</c> if use orientation; otherwise, <c>false</c>.</value>
		public bool UsesOrientation {
			get { return usesOrientation; }
			set {
				if (usesOrientation == value)
					return;
			
				usesOrientation = value;
			
				if (usesOrientation) {
					// http://docs.unity3d.com/Documentation/ScriptReference/Compass-enabled.html
					// Note, that if you want Input.compass.trueHeading property to contain a valid value,
					// you must also enable location updates by calling Input.location.Start().
					if (usesLocation == false) {
						if (UnityEngine.Input.location.isEnabledByUser
							&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
							|| UnityEngine.Input.location.status == LocationServiceStatus.Failed)) {
							UnityEngine.Input.location.Start ();
						} else {
#if DEBUG_LOG
						Debug.LogError("ERROR: Map.UseOrientation: Location is not authorized on the device.");
#endif
						}
					}
					UnityEngine.Input.compass.enabled = true;
				} else {
					if (usesLocation == false) {
						if (UnityEngine.Input.location.isEnabledByUser
							&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
							|| UnityEngine.Input.location.status == LocationServiceStatus.Running))
							UnityEngine.Input.location.Start ();
					}
					UnityEngine.Input.compass.enabled = false;
				}
			}
		}
	
		/// <summary>
		/// The "camera follows orientation" flag.
		/// </summary>
		private bool cameraFollowsOrientation = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/>'s camera follows the host's orientation.
		/// </summary>
		/// <value>
		/// <c>true</c> if the camera follows the host's orientation; otherwise, <c>false</c>.
		/// If set to <c>true</c>, <see cref="UnitySlippyMap.Map.CameraFollowsOrientation"/> is set to <c>true</c>.
		/// </value>
		public bool CameraFollowsOrientation {
			get { return cameraFollowsOrientation; }
			set {
				cameraFollowsOrientation = value;
				lastCameraOrientation = 0.0f;
			}
		}
	
		/// <summary>
		/// The last camera orientation.
		/// </summary>
		private float lastCameraOrientation = 0.0f;

		/// <summary>
		/// The list of <see cref="UnitySlippyMap.Marker"/> instances.
		/// </summary>
		private List<MarkerBehaviour> markers = new List<MarkerBehaviour> ();

		/// <summary>
		/// Gets the list of markers.
		/// </summary>
		/// <value>The list of <see cref="UnitySlippyMap.Marker"/> instances.</value>
		public List<MarkerBehaviour> Markers { get { return markers; } }

		/// <summary>
		/// The "shows GUI controls" flag.
		/// </summary>
		private bool showsGUIControls = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> shows GUI controls.
		/// </summary>
		/// <value><c>true</c> if show GUI controls; otherwise, <c>false</c>.</value>
		public bool ShowsGUIControls
		{
			get { return showsGUIControls; }
			set { showsGUIControls = value; }
		}

		/// <summary>
		/// The "inputs enabled" flag.
		/// </summary>
		private bool inputsEnabled = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> inputs are enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if inputs enabled; otherwise, <c>false</c>.
		/// TODO: implement inputs in a user oriented customizable way
		/// </value>
		public bool InputsEnabled
		{
			get { return inputsEnabled; }
			set { inputsEnabled = value; }
		}

		/// <summary>
		/// The location marker.
		/// </summary>
		private LocationMarkerBehaviour locationMarker;

		/// <summary>
		/// The list of <see cref="UnitySlippyMap.Layer"/> instances.
		/// </summary>
		private List<LayerBehaviour> layers = new List<LayerBehaviour> ();
	
		/// <summary>
		/// The "has moved" flag.
		/// </summary>
		private bool hasMoved = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="UnitySlippyMap.Map.MapBehaviour"/> has moved.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance has moved; otherwise, <c>false</c>.
		/// The map will not update when it is true and will set it to false at the end of its Update.
		/// </value>
		public bool HasMoved {
			get { return hasMoved; }
			set { hasMoved = value; }
		}
    
		/// <summary>
		/// The GUI delegate.
		/// </summary>
		private GUIDelegate guiDelegate;

		/// <summary>
		/// Gets or sets the GUI delegate.
		/// </summary>
		/// <value>The GUI delegate.</value>
		public GUIDelegate GUIDelegate {
			get { return guiDelegate; }
			set { guiDelegate = value; }
		}
	
		/// <summary>
		/// The input delegate.
		/// </summary>
		private InputDelegate inputDelegate;

		/// <summary>
		/// Gets or sets the input delegate.
		/// </summary>
		/// <value>The input delegate.</value>
		public InputDelegate InputDelegate {
			get { return inputDelegate; }
			set { inputDelegate = value; }
		}
	
		/// <summary>
		/// The "was input intercepted by GUI" flag.
		/// </summary>
		private bool wasInputInterceptedByGUI;
	
	
	
		/// <summary>
		/// The Well-Known Text representation of the EPSG900913 projection.
		/// </summary>
		// <value>ProjNet Dll: http://projnet.codeplex.com/</value>
		private static string wktEPSG900913 =
        "PROJCS[\"WGS84 / Simple Mercator\", " +
			"GEOGCS[\"WGS 84\", " +
			"DATUM[\"World Geodetic System 1984\", SPHEROID[\"WGS 84\", 6378137.0, 298.257223563,AUTHORITY[\"EPSG\",\"7030\"]], " +
			"AUTHORITY[\"EPSG\",\"6326\"]]," +
			"PRIMEM[\"Greenwich\", 0.0, AUTHORITY[\"EPSG\",\"8901\"]], " +
			"UNIT[\"degree\",0.017453292519943295], " +
			"AXIS[\"Longitude\", EAST], AXIS[\"Latitude\", NORTH]," +
			"AUTHORITY[\"EPSG\",\"4326\"]], " +
			"PROJECTION[\"Mercator_1SP\"]," +
			"PARAMETER[\"semi_minor\", 6378137.0], " +
			"PARAMETER[\"latitude_of_origin\",0.0], " +
			"PARAMETER[\"central_meridian\", 0.0], " +
			"PARAMETER[\"scale_factor\",1.0], " +
			"PARAMETER[\"false_easting\", 0.0], " +
			"PARAMETER[\"false_northing\", 0.0]," +
			"UNIT[\"m\", 1.0], " +
			"AXIS[\"x\", EAST], AXIS[\"y\", NORTH]," +
			"AUTHORITY[\"EPSG\",\"900913\"]]";

		/// <summary>
		/// Gets the Well-Known Text representation of the EPSG900913 projection.
		/// </summary>
		public static string WKTEPSG900913 { get { return wktEPSG900913; } }

		/// <summary>
		/// The CoordinateTransformationFactory instance.
		/// </summary>
		private CoordinateTransformationFactory ctFactory;

		/// <summary>
		/// Gets the CoordinateTransformationFactory instance.
		/// </summary>
		public CoordinateTransformationFactory CTFactory { get { return ctFactory; } }

		/// <summary>
		/// The EPSG 900913 ICoordinateSystem instance.
		/// </summary>
		private ICoordinateSystem epsg900913;

		/// <summary>
		/// Gets the EPSG 900913 ICoordinateSystem instance.
		/// </summary>
		public ICoordinateSystem EPSG900913 { get { return epsg900913; } }

		/// <summary>
		/// The WGS84 to EPSG 900913 ICoordinateTransformation instance.
		/// </summary>
		private ICoordinateTransformation wgs84ToEPSG900913;

		/// <summary>
		/// Gets the WGS84 to EPSG 900913 ICoordinateTransformation instance.
		/// </summary>
		public ICoordinateTransformation WGS84ToEPSG900913 { get { return wgs84ToEPSG900913; } }

		/// <summary>
		/// The WGS84 to EPSG 900913 IMathTransform instance.
		/// </summary>
		private IMathTransform wgs84ToEPSG900913Transform;

		/// <summary>
		/// Gets the WGS84 to EPSG900913 IMathTransform instance.
		/// </summary>
		public IMathTransform WGS84ToEPSG900913Transform { get { return wgs84ToEPSG900913Transform; } }

		/// <summary>
		/// The EPSG 900913 to WGS84 IMathTransform instance.
		/// </summary>
		private IMathTransform epsg900913ToWGS84Transform;

		/// <summary>
		/// Gets the EPSG 900913 to WGS84 IMathTransform instance.
		/// </summary>
		public IMathTransform EPSG900913ToWGS84Transform { get { return epsg900913ToWGS84Transform; } }
	
	#endregion
    
    #region Private methods
    
		/// <summary>
		/// Fits the vertical border.
		/// </summary>
		private void FitVerticalBorder ()
		{
			//TODO: take into account the camera orientation

			if (currentCamera != null) {
				double[] camCenter = new double[] {
										centerEPSG900913 [0],
										centerEPSG900913 [1]
								};
				double offset = Mathf.Floor (currentCamera.pixelHeight * 0.5f) * metersPerPixel;
				if (camCenter [1] + offset > GeoHelpers.HalfEarthCircumference) {
					camCenter [1] -= camCenter [1] + offset - GeoHelpers.HalfEarthCircumference;
					CenterEPSG900913 = camCenter;
				} else if (camCenter [1] - offset < -GeoHelpers.HalfEarthCircumference) {
					camCenter [1] -= camCenter [1] - offset + GeoHelpers.HalfEarthCircumference;
					CenterEPSG900913 = camCenter;
				}
			}
		}

		/// <summary>
		/// Computes the center EPS g900913.
		/// </summary>
		/// <returns>The center coordinate in the EPSG 900913 coordinate system.</returns>
		/// <param name="pos">Position.</param>
		private double[] ComputeCenterEPSG900913 (double[] pos)
		{
			Vector3 displacement = new Vector3 ((float)(centerEPSG900913 [0] - pos [0]) * roundedScaleMultiplier, 0.0f, (float)(centerEPSG900913 [1] - pos [1]) * roundedScaleMultiplier);
			Vector3 rootPosition = this.gameObject.transform.position;
			this.gameObject.transform.position = new Vector3 (
			rootPosition.x + displacement.x,
			rootPosition.y + displacement.y,
			rootPosition.z + displacement.z);

			if (pos [0] > GeoHelpers.HalfEarthCircumference)
				pos [0] -= GeoHelpers.EarthCircumference;
			else if (pos [0] < -GeoHelpers.HalfEarthCircumference)
				pos [0] += GeoHelpers.EarthCircumference;

			return pos;
		}

		/// <summary>
		/// Updates the internals of the <see cref="UnitySlippyMap.Map.MapBehaviour"/> instance.
		/// </summary>
		private void UpdateInternals ()
		{
			// FIXME: the half map scale is a value used throughout the implementation to rule the camera elevation
			// and the size/scale of the tiles, it depends on fixed tile size and resolution (here 256 and 72) so I am not
			// sure it would work for a tile layer with different values...
			// maybe there is a way to take the values out of the calculations and reintroduce them on Layer level...
			// FIXME: the 'division by 20000' helps the values to be kept in range for the Unity3D engine, not sure
			// this is the right approach either, feels kinda voodooish...
		
			halfMapScale = GeoHelpers.OsmZoomLevelToMapScale (currentZoom, 0.0f, tileResolution, 72) / scaleDivider;
			roundedHalfMapScale = GeoHelpers.OsmZoomLevelToMapScale (roundedZoom, 0.0f, tileResolution, 72) / scaleDivider;

			metersPerPixel = GeoHelpers.MetersPerPixel (0.0f, (float)currentZoom);
			roundedMetersPerPixel = GeoHelpers.MetersPerPixel (0.0f, (float)roundedZoom);
        
			// FIXME: another voodoish value to help converting meters (EPSG 900913) to Unity3D world coordinates
			scaleMultiplier = halfMapScale / (metersPerPixel * tileResolution);
			roundedScaleMultiplier = roundedHalfMapScale / (roundedMetersPerPixel * tileResolution);
		}
    
    #endregion
	
	#region MonoBehaviour implementation
	
		/// <summary>
		/// Raises the Awake event.
		/// </summary>
		private void Awake ()
		{
			// initialize the coordinate transformation
			epsg900913 = CoordinateSystemWktReader.Parse (wktEPSG900913) as ICoordinateSystem;
			ctFactory = new CoordinateTransformationFactory ();
			wgs84ToEPSG900913 = ctFactory.CreateFromCoordinateSystems (GeographicCoordinateSystem.WGS84, epsg900913);
			wgs84ToEPSG900913Transform = wgs84ToEPSG900913.MathTransform;
			epsg900913ToWGS84Transform = wgs84ToEPSG900913Transform.Inverse ();
		}
	
		/// <summary>
		/// Raises the Start event.
		/// </summary>
		private void Start ()
		{
			// setup the gui scale according to the screen resolution
			if (Application.platform == RuntimePlatform.Android
				|| Application.platform == RuntimePlatform.IPhonePlayer)
				screenScale = (Screen.orientation == ScreenOrientation.LandscapeLeft ? Screen.width : Screen.height) / 480.0f;
			else
				screenScale = 2.0f;

			// initialize the camera position and rotation
			currentCamera.transform.rotation = Quaternion.Euler (90.0f, 0.0f, 0.0f);
			Zoom (0.0f);
		}

		/// <summary>
		/// Raises the GUI event.
		/// </summary>
		private void OnGUI ()
		{
			// FIXME: gaps beween tiles appear when zooming and panning the map at the same time on iOS, precision ???
			// TODO: optimise, use one mesh for the tiles and combine textures in a big one (might resolve the gap bug above)

			// process the user defined GUI
			if (ShowsGUIControls && guiDelegate != null) {
				wasInputInterceptedByGUI = guiDelegate (this);
			}
		
			if (Event.current.type != EventType.Repaint
				&& Event.current.type != EventType.MouseDown
				&& Event.current.type != EventType.MouseDrag
				&& Event.current.type != EventType.MouseMove
				&& Event.current.type != EventType.MouseUp)
				return;
		
			if (InputsEnabled && inputDelegate != null) {
				inputDelegate (this, wasInputInterceptedByGUI);
			}
		
		}

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// During an update cycle:
		/// * The map may use the host's location to update its center (see <see cref="UnitySlippyMap.Map.MapBehaviour.UsesLocation"/>
		/// for more information).
		/// * It will then update the location marker accordingly if set (see <see cref="UnitySlippyMap.Map.MapBehaviour.SetLocationMarker<T>"/>
		/// for more information).
		/// * The camera may be rotated to follow the host's orientation (see <see cref="UnitySlippyMap.Map.MapBehaviour.UsesOrientation"/>
		/// for more information).
		/// * If the map has moved (see <see cref="UnitySlippyMap.Map.MapBehaviour.HasMoved"/> for more information) since the last update cycle,
		/// all tile downloading jobs are paused temporarily until the next update cycle when the map won't have moved.
		/// * If the map has moved (see <see cref="UnitySlippyMap.Map.MapBehaviour.HasMoved"/> for more information) and is dirty
		/// (see <see cref="UnitySlippyMap.Map.MapBehaviour.IsDirty"/> for more information), all markers and layers are updated
		/// and the GameObject supporting the map behaviour is repositioned to the center of the world (Vector3.Zero).
		/// </summary>
		private void Update ()
		{
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.Begin("Map.Update");
#endif
		
			// update the centerWGS84 with the last location if enabled
			if (usesLocation
				&& UnityEngine.Input.location.status == LocationServiceStatus.Running) {
				if (updatesCenterWithLocation) {
					if (UnityEngine.Input.location.lastData.longitude <= 180.0f
						&& UnityEngine.Input.location.lastData.longitude >= -180.0f
						&& UnityEngine.Input.location.lastData.latitude <= 90.0f
						&& UnityEngine.Input.location.lastData.latitude >= -90.0f) {
						if (CenterWGS84 [0] != UnityEngine.Input.location.lastData.longitude
							|| CenterWGS84 [1] != UnityEngine.Input.location.lastData.latitude)
							CenterWGS84 = new double[2] {
																UnityEngine.Input.location.lastData.longitude,
																UnityEngine.Input.location.lastData.latitude
														};
					
						//Debug.Log("DEBUG: Map.Update: new location: " + Input.location.lastData.longitude + " " + Input.location.lastData.latitude + ":  " + Input.location.status);					
					} else {
						Debug.LogWarning ("WARNING: Map.Update: bogus location (bailing): " + UnityEngine.Input.location.lastData.longitude + " " + UnityEngine.Input.location.lastData.latitude + ":  " + UnityEngine.Input.location.status);
					}
				}
			
				if (locationMarker != null) {
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (locationMarker.gameObject.active == false)
					locationMarker.gameObject.SetActiveRecursively(true);
#else
					if (locationMarker.gameObject.activeSelf == false)
						locationMarker.gameObject.SetActive (true);
#endif
					if (UnityEngine.Input.location.lastData.longitude <= 180.0f
						&& UnityEngine.Input.location.lastData.longitude >= -180.0f
						&& UnityEngine.Input.location.lastData.latitude <= 90.0f
						&& UnityEngine.Input.location.lastData.latitude >= -90.0f) {
						locationMarker.CoordinatesWGS84 = new double[2] {
														UnityEngine.Input.location.lastData.longitude,
														UnityEngine.Input.location.lastData.latitude
												};
					} else {
//#if DEBUG_LOG
						Debug.LogWarning ("WARNING: Map.Update: bogus location (bailing): " + UnityEngine.Input.location.lastData.longitude + " " + UnityEngine.Input.location.lastData.latitude + ":  " + UnityEngine.Input.location.status);
//#endif
					}
				}
			}
		
			// update the orientation of the location marker
			if (usesOrientation) {
				float heading = 0.0f;
				// TODO: handle all device orientations
				switch (Screen.orientation) {
				case ScreenOrientation.LandscapeLeft:
					heading = UnityEngine.Input.compass.trueHeading;
					break;
				case ScreenOrientation.Portrait: // FIXME: not tested, likely wrong, legacy code
					heading = -UnityEngine.Input.compass.trueHeading;
					break;
				}

				if (cameraFollowsOrientation) {
					if (lastCameraOrientation == 0.0f) {
						currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, heading);

						lastCameraOrientation = heading;
					} else {
						float cameraRotationSpeed = 1.0f;
						float relativeAngle = (heading - lastCameraOrientation) * cameraRotationSpeed * Time.deltaTime;
						if (relativeAngle > 0.01f) {
							currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, relativeAngle);
	
							//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
							lastCameraOrientation += relativeAngle;
						} else {
							currentCamera.transform.RotateAround (Vector3.zero, Vector3.up, heading - lastCameraOrientation);
	
							//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
							lastCameraOrientation = heading;
						}
					}
					
					IsDirty = true;
				}
				
				if (locationMarker != null
					&& locationMarker.OrientationMarker != null) {
					//Debug.Log("DEBUG: " + heading);
					locationMarker.OrientationMarker.rotation = Quaternion.AngleAxis (heading, Vector3.up);
				}
			}
		
			// pause the loading operations when moving
			if (hasMoved == true) {
				TileDownloaderBehaviour.Instance.PauseAll ();
			} else {
				TileDownloaderBehaviour.Instance.UnpauseAll ();
			}
			
			// update the tiles if needed
			if (IsDirty == true && hasMoved == false) {
#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: update layers & markers");
#endif
			
				IsDirty = false;
			
				if (locationMarker != null
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				&& locationMarker.gameObject.active == true)
#else
					&& locationMarker.gameObject.activeSelf == true)
#endif
					locationMarker.UpdateMarker ();
			
				foreach (LayerBehaviour layer in layers) {	
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (layer.gameObject.active == true
#else
					if (layer.gameObject.activeSelf == true
#endif
						&& layer.enabled == true
						&& CurrentZoom >= layer.MinZoom
						&& CurrentZoom <= layer.MaxZoom)
						layer.UpdateContent ();
				}
			
				foreach (MarkerBehaviour marker in markers) {
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (marker.gameObject.active == true
#else
					if (marker.gameObject.activeSelf == true
#endif
						&& marker.enabled == true)
						marker.UpdateMarker ();
				}
			
				if (this.gameObject.transform.position != Vector3.zero)
					this.gameObject.transform.position = Vector3.zero;

#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: updated layers");
#endif
			}
		
			// reset the deferred update flag
			hasMoved = false;
						
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.End("Map.Update");
#endif
		}
	
	#endregion
	
	#region MapBehaviour methods
	
		/// <summary>
		/// Centers the map on the location of the host.
		/// </summary>
		public void CenterOnLocation ()
		{
			if (locationMarker != null
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
			&& locationMarker.gameObject.active == true)
#else
				&& locationMarker.gameObject.activeSelf == true)
#endif
				CenterWGS84 = locationMarker.CoordinatesWGS84;
			updatesCenterWithLocation = true;
		}

		/// <summary>
		/// Sets the marker for the host's location and orientation using a GameObject instance for display.
		/// </summary>
		/// <returns>The location marker.</returns>
		/// <param name="locationGo">The GameObject instance.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T SetLocationMarker<T> (GameObject locationGo) where T : LocationMarkerBehaviour
		{
			return SetLocationMarker<T> (locationGo, null);
		}
	
		/// <summary>
		/// Sets the marker for the host's location and orientation using two GameObject instances for display.
		/// </summary>
		/// <returns>The location marker.</returns>
		/// <param name="locationGo">The location GameObject instance.</param>
		/// <param name="orientationGo">The orientation GameObject instance.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T SetLocationMarker<T> (GameObject locationGo, GameObject orientationGo) where T : LocationMarkerBehaviour
		{
			// create a GameObject and add the templated Marker component to it
			GameObject markerObject = new GameObject ("[location marker]");
			markerObject.transform.parent = this.gameObject.transform;
		
			T marker = markerObject.AddComponent<T> ();
		
			locationGo.transform.parent = markerObject.transform;
			locationGo.transform.localPosition = Vector3.zero;
		
			if (orientationGo != null) {
				marker.OrientationMarker = orientationGo.transform;
			}
		
			// setup the marker
			marker.Map = this;
			if (usesLocation
				&& UnityEngine.Input.location.status == LocationServiceStatus.Running)
				marker.CoordinatesWGS84 = new double[2] {
										UnityEngine.Input.location.lastData.longitude,
										UnityEngine.Input.location.lastData.latitude
								};
			else
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				markerObject.SetActiveRecursively(false);
#else
				markerObject.SetActive (false);
#endif
		
			// set the location marker
			locationMarker = marker;
		
			// tell the map to update
			IsDirty = true;
		
			return marker;
		}


		/// <summary>
		/// Creates a new named layer.
		/// </summary>
		/// <returns>The layer.</returns>
		/// <param name="name">The layer's name.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T CreateLayer<T> (string name) where T : LayerBehaviour
		{
			// create a GameObject as the root of the layer and add the templated Layer component to it
			GameObject layerRoot = new GameObject (name);
			Transform layerRootTransform = layerRoot.transform;
			//Debug.Log("DEBUG: layer root: " + layerRootTransform.position + " this position: " + this.gameObject.transform.position);
			layerRootTransform.parent = this.gameObject.transform;
			layerRootTransform.localPosition = Vector3.zero;
			T layer = layerRoot.AddComponent<T> ();
		
			// setup the layer
			layer.Map = this;
			layer.MinZoom = minZoom;
			layer.MaxZoom = maxZoom;
		
			// add the layer to the layers' list
			layers.Add (layer);
		
			// tell the map to update
			IsDirty = true;
		
			return layer;
		}

		/// <summary>
		/// Creates a new named marker at the specified coordinates using a GameObject for display.
		/// </summary>
		/// <returns>The marker.</returns>
		/// <param name="name">Name.</param>
		/// <param name="coordinatesWGS84">Coordinates WG s84.</param>
		/// <param name="go">Go.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T CreateMarker<T> (string name, double[] coordinatesWGS84, GameObject go) where T : MarkerBehaviour
		{
			// create a GameObject and add the templated Marker component to it
			GameObject markerObject = new GameObject (name);
			markerObject.transform.parent = this.gameObject.transform;
		
			//go.name = "go - " + name;
			go.transform.parent = markerObject.gameObject.transform;
			go.transform.localPosition = Vector3.zero;
		
			T marker = markerObject.AddComponent<T> ();
		
			// setup the marker
			marker.Map = this;
			marker.CoordinatesWGS84 = coordinatesWGS84;
		
			// add marker to the markers' list
			markers.Add (marker);
		
			// tell the map to update
			IsDirty = true;
		
			return marker;
		}
    
		/// <summary>
		/// Removes the marker.
		/// </summary>
		/// <param name='m'>
		/// The marker.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
		/// specified by the method.
		/// </exception>
		public void RemoveMarker (MarkerBehaviour m)
		{
			if (m == null)
				throw new ArgumentNullException ("m");
        
			if (markers.Contains (m) == false)
				throw new ArgumentOutOfRangeException ("m");
        
			markers.Remove (m);
        
			DestroyImmediate (m.gameObject);
		}

		/// <summary>
		/// Zooms the map at the specified zoomSpeed.
		/// </summary>
		/// <param name="zoomSpeed">Zoom speed.</param>
		public void Zoom (float zoomSpeed)
		{
			// apply the zoom
			CurrentZoom += 4.0f * zoomSpeed * Time.deltaTime;

			// move the camera
			// FIXME: the camera jumps on the first zoom when tilted, because the cam altitude and zoom value are unsynced by the rotation
			Transform cameraTransform = currentCamera.transform;
			float y = GeoHelpers.OsmZoomLevelToMapScale (currentZoom, 0.0f, tileResolution, 72) / scaleDivider * screenScale;
			float t = y / cameraTransform.forward.y;
			cameraTransform.position = new Vector3 (
			t * cameraTransform.forward.x,
			y,
			t * cameraTransform.forward.z);
		
			// set the update flag to tell the behaviour the user is manipulating the map
			hasMoved = true;
			IsDirty = true;
		}
	
	#endregion
		
	}

}