// 
//  Map.cs
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

using System;
using System.Collections.Generic;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;

using UnitySlippyMap.GUI;
using UnitySlippyMap.Input;

//using UnitySlippyMap.Input;

// <summary>
// The Map class is a singleton handling layers and markers.
// Tiles are GameObjects (simple planes) parented to their layer's GameObject, in turn parented to the map's GameObject.
// Markers are empty GameObjects parented to the map's GameObject.
// The parenting is used to position the tiles and markers in a local referential using the map's center as origin.
// </summary>
// <example>
// using UnityEngine;
//
// using System;
//
// using UnitySlippyMap;
//
// public class TestMap : MonoBehaviour
// {
//	 private Map		map;
//	
//	 public Texture	MarkerTexture;
//	
//	 void Start()
//	 {
//		 // create the map singleton
//		 map = Map.Instance;
//		
//		 // 9 rue Gentil, Lyon, France
//		 map.CenterWGS84 = new double[2] { 4.83527, 45.76487 };
//		 map.UseLocation = true;
//		 map.InputsEnabled = true;
//				
//		 // create a test layer
//		 TileLayer layer = map.CreateLayer<OSMTileLayer>("test tile layer");
//		 layer.URLFormat = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
//		
//		 // create some test 2D markers
//		 GameObject go = Tile.CreateTileTemplate();
//		 go.renderer.material.mainTexture = MarkerTexture;
//		 go.renderer.material.renderQueue = 4000;
//		
//		 GameObject markerGO;
//		 markerGO = Instantiate(go) as GameObject;
//		 map.CreateMarker<Marker>("test marker - 9 rue Gentil, Lyon", new double[2] { 4.83527, 45.76487 }, markerGO);
//		
//		 markerGO = Instantiate(go) as GameObject;
//		 map.CreateMarker<Marker>("test marker - 31 rue de la Bourse, Lyon", new double[2] { 4.83699, 45.76535 }, markerGO);
//		
//		 markerGO = Instantiate(go) as GameObject;
//		 map.CreateMarker<Marker>("test marker - 1 place St Nizier, Lyon", new double[2] { 4.83295, 45.76468 }, markerGO);
//
//		 DestroyImmediate(go);
//	 }
//	
//	 void OnApplicationQuit()
//	 {
//		 map = null;
//	 }
// }
// </example>

namespace UnitySlippyMap
{

public class Map : MonoBehaviour
{
	#region Singleton stuff
		
	private static Map instance = null;
	public static Map Instance
	{
		get
		{
            if (null == (object)instance)
            {
                instance = FindObjectOfType(typeof (Map)) as Map;
                if (null == (object)instance)
                {
                    var go = new GameObject("[Map]");
                    //go.hideFlags = HideFlags.HideAndDontSave;
                    instance = go.AddComponent<Map>();
                    instance.EnsureMap();
                }
			}

			return instance;
		}
	}
	
	private void EnsureMap()
	{
	}
	
	private Map()
	{
	}

    private void OnDestroy()
    {
        instance = null;
    }

	private void OnApplicationQuit()
	{
		DestroyImmediate(this.gameObject);
	}
	
	#endregion
	
	#region Variables & properties

	/// <summary>
	/// Camera used to render the map
	/// </summary>
	private Camera currentCamera;
	public Camera CurrentCamera
	{
		get { return currentCamera; }
		set { currentCamera = value; }
	}

	// <summary>
	// Should be set to true to tell the map to update its layers and markers.
	// </summary>
	private bool							isDirty = false;
	public bool								IsDirty
	{
		get { return isDirty; }
		set { isDirty = value; }
	}

	// <summary>
	// Holds the center coordinates of the map in WGS84.
	// </summary>
	private double[]						centerWGS84 = new double[2];
	public double[]							CenterWGS84
	{
		get { return centerWGS84; }
		set
		{
			if (value == null)
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterWGS84: value cannot be null");
#endif
				return ;
			}

			double[] newCenterESPG900913 = wgs84ToEPSG900913Transform.Transform(value);

			centerEPSG900913 = ComputeCenterEPSG900913(newCenterESPG900913);

			if (value[0] > 180.0)
				value[0] -= 360.0;
			else if (value[0] < -180.0)
				value[0] += 360.0;

			centerWGS84 = value;

			FitVerticalBorder();
			IsDirty = true;
		}
	}
	
	// <summary>
	// Holds the center coordinates of the map in EPSG 900913.
	// </summary>
	private double[]						centerEPSG900913 = new double[2];
	public double[]							CenterEPSG900913
	{
		get
		{
			return centerEPSG900913;
		}
		set
		{
			if (value == null)
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.CenterEPSG900913: value cannot be null");
#endif
				return ;
			}

			centerEPSG900913 = ComputeCenterEPSG900913(value);
            centerWGS84 = epsg900913ToWGS84Transform.Transform(centerEPSG900913);

			FitVerticalBorder();
			IsDirty = true;
		}
	}
	
	// <summary>
	// Is used to constraint the map panning.
	// </summary>
	// TODO: implement the constraint
    //private double[]						size = new double[2];
	
	private float							currentZoom;
	public float							CurrentZoom
	{
		get { return currentZoom; }
		set
		{
			if (value < minZoom
				|| value > maxZoom)
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.Zoom: value must be inside range [" + minZoom + " - " + maxZoom + "]");
#endif
				return;
			}

			if (currentZoom == value)
				return;

			currentZoom = value;
			//roundedZoom = (int)Mathf.Round(currentZoom);
			float diff = value - roundedZoom;
			if (diff > 0.0f && diff >= zoomStepLowerThreshold)
				roundedZoom = (int)Mathf.Ceil(currentZoom);
			else if (diff < 0.0f && diff <= -zoomStepUpperThreshold)
				roundedZoom = (int)Mathf.Floor(currentZoom);

			UpdateInternals();

			FitVerticalBorder();
		}
	}
	
	private float							zoomStepUpperThreshold = 0.8f;
	public float							ZoomStepUpperThreshold
	{
		get { return zoomStepUpperThreshold; }
		set { zoomStepUpperThreshold = value; }
	}
	
	private float							zoomStepLowerThreshold = 0.2f;
	public float							ZoomStepLowerThreshold
	{
		get { return zoomStepLowerThreshold; }
		set { zoomStepLowerThreshold = value; }
	}
	
	private float							minZoom = 3.0f;
	public float							MinZoom
	{
		get { return minZoom; }
		set
		{
			if (value < 3.0f
				|| value > 18.0f)
			{
				minZoom = Mathf.Clamp(value, 3.0f, 18.0f);
			}
			else
			{		
				minZoom = value;
			}
			
			if (minZoom > maxZoom)
			{
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MinZoom: clamp value [" + minZoom + "] to max zoom [" + maxZoom + "]");
#endif
				minZoom = maxZoom;
			}
		}
	}
	
	private float							maxZoom = 18.0f;
	public float							MaxZoom
	{
		get { return maxZoom; }
		set
		{
			if (value < 3.0f
				|| value > 18.0f)
			{
				maxZoom = Mathf.Clamp(value, 3.0f, 18.0f);
			}
			else
			{		
				maxZoom = value;
			}
			
			if (maxZoom < minZoom)
			{
#if DEBUG_LOG
				Debug.LogWarning("WARNING: Map.MaxZoom: clamp value [" + maxZoom + "] to min zoom [" + minZoom + "]");
#endif
				maxZoom = minZoom;
			}
		}
	}

	private int								roundedZoom;
	public int								RoundedZoom { get { return roundedZoom; } }
	
	private float							halfMapScale = 0.0f;
	public float							HalfMapScale { get { return halfMapScale; } }
	
	private float							roundedHalfMapScale = 0.0f;
	public float							RoundedHalfMapScale { get { return roundedHalfMapScale; } }
	
	private float							roundedMetersPerPixel = 0.0f;
	public float							RoundedMetersPerPixel { get { return roundedMetersPerPixel; } }
	
	private float							metersPerPixel = 0.0f;
	public float							MetersPerPixel { get { return metersPerPixel; } }
	
	private float							roundedScaleMultiplier = 0.0f;
	public float							RoundedScaleMultiplier { get { return roundedScaleMultiplier; } }
	
	private float							scaleMultiplier = 0.0f;
	public float							ScaleMultiplier { get { return scaleMultiplier; } }

    private float                           scaleDivider = 20000.0f;

    private float                           tileResolution = 256.0f;
    public float                            TileResolution { get { return tileResolution; } }

    private float                           screenScale = 1.0f;
	
	// <summary>
	// Enables/disables the use of the device's location service.
	// </summary>
	private bool							useLocation = false;
	public bool								UseLocation
	{
		get { return useLocation; }
		set
		{
			if (useLocation == value)
				return ;
			
			useLocation = value;
			
			if (useLocation)
			{
				if (UnityEngine.Input.location.isEnabledByUser
					&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
					|| UnityEngine.Input.location.status == LocationServiceStatus.Failed))
				{
					UnityEngine.Input.location.Start();
				}
				else
				{
#if DEBUG_LOG
					Debug.LogError("ERROR: Map.UseLocation: Location is not authorized on the device.");
#endif
				}
			}
			else
			{
				if (UnityEngine.Input.location.isEnabledByUser
					&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
					|| UnityEngine.Input.location.status == LocationServiceStatus.Running))
				{
					UnityEngine.Input.location.Start();
				}
			}
		}
	}
	
	// <summary>
	// Is set to false is the map is manipulated by the user.
	// </summary>
	private bool							updateCenterWithLocation = true;
	public bool								UpdateCenterWithLocation
	{
		get
		{
			return updateCenterWithLocation;
		}
		
		set
		{
			updateCenterWithLocation = value;
		}
	}
	
	// <summary>
	// Enables/disables the use of the device's orientation/compass.
	// </summary>
	private bool							useOrientation = false;
	public bool								UseOrientation
	{
		get { return useOrientation; }
		set
		{
			if (useOrientation == value)
				return ;
			
			useOrientation = value;
			
			if (useOrientation)
			{
				// http://docs.unity3d.com/Documentation/ScriptReference/Compass-enabled.html
				// Note, that if you want Input.compass.trueHeading property to contain a valid value,
				// you must also enable location updates by calling Input.location.Start().
				if (useLocation == false)
				{
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Stopped
						|| UnityEngine.Input.location.status == LocationServiceStatus.Failed))
					{
						UnityEngine.Input.location.Start();
					}
					else
					{
#if DEBUG_LOG
						Debug.LogError("ERROR: Map.UseOrientation: Location is not authorized on the device.");
#endif
					}
				}
				UnityEngine.Input.compass.enabled = true;
			}
			else
			{
				if (useLocation == false)
				{
					if (UnityEngine.Input.location.isEnabledByUser
						&& (UnityEngine.Input.location.status == LocationServiceStatus.Initializing
						|| UnityEngine.Input.location.status == LocationServiceStatus.Running))
						UnityEngine.Input.location.Start();
				}
				UnityEngine.Input.compass.enabled = false;
			}
		}
	}
	
	/// <summary>
	/// Enables/disables the camera to follow the compass orientation.
	/// </summary>
	private bool							cameraFollowsOrientation = false;
	public bool								CameraFollowsOrientation
	{
		get { return cameraFollowsOrientation; }
		set 
		{
			cameraFollowsOrientation = value;
			lastCameraOrientation = 0.0f;
		}
	}
	
	private float							lastCameraOrientation = 0.0f;
	
    private List<Marker> markers = new List<Marker>();
    public List<Marker> Markers { get { return markers; } }
    
    /// <summary>
    /// Enables/disables showing GUI controls.
    /// </summary>
    public bool                             ShowGUIControls = false;
    /// <summary>
    /// Enables/disables of the platform specific controls.
    /// TODO: implement inputs in a user oriented customizable way
    /// </summary>
    public bool                             InputsEnabled = false;
    
	private LocationMarker					locationMarker;
	
	private List<Layer>						layers = new List<Layer>();
	
	/// <summary>
	/// Tells the map if it is being manipulated. The map will not update when it is true and will set it to false at the end of its Update.
	/// </summary>
	private bool							hasMoved = false;
	public bool								HasMoved
	{
		get { return hasMoved; }
		set { hasMoved = value; }
	}
    
	private GUIDelegate						guiDelegate;
	public GUIDelegate						GUIDelegate
	{
		get { return guiDelegate; }
		set { guiDelegate = value; }
	}
	
	private InputDelegate					inputDelegate;
	public InputDelegate					InputDelegate
	{
		get { return inputDelegate; }
		set { inputDelegate = value; }
	}
	
	private bool							wasInputInterceptedByGUI;
	
	
	
	// ProjNet Dll: http://projnet.codeplex.com/
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
    public static string                    WKTEPSG900913 { get { return wktEPSG900913; } }

    private CoordinateTransformationFactory ctFactory;
    public CoordinateTransformationFactory  CTFactory { get { return ctFactory; } }
    private ICoordinateSystem               epsg900913;
    public ICoordinateSystem                EPSG900913 { get { return epsg900913; } }
    private ICoordinateTransformation       wgs84ToEPSG900913;
    public ICoordinateTransformation        WGS84ToEPSG900913 { get { return wgs84ToEPSG900913; } }
    private IMathTransform                  wgs84ToEPSG900913Transform;
    public IMathTransform                   WGS84ToEPSG900913Transform { get { return wgs84ToEPSG900913Transform; } }
    private IMathTransform                  epsg900913ToWGS84Transform;
    public IMathTransform                   EPSG900913ToWGS84Transform { get { return epsg900913ToWGS84Transform; } }
	
	#endregion
    
    #region Private methods
    
	private void FitVerticalBorder()
	{
		//TODO: take into account the camera orientation

		if (currentCamera != null)
		{
			double[] camCenter = new double[] { centerEPSG900913[0], centerEPSG900913[1] };
			double offset = Mathf.Floor(currentCamera.pixelHeight * 0.5f) * metersPerPixel;
			if (camCenter[1] + offset > GeoHelpers.HalfEarthCircumference)
			{
				camCenter[1] -= camCenter[1] + offset - GeoHelpers.HalfEarthCircumference;
				CenterEPSG900913 = camCenter;
			}
			else if (camCenter[1] - offset < -GeoHelpers.HalfEarthCircumference)
			{
				camCenter[1] -= camCenter[1] - offset + GeoHelpers.HalfEarthCircumference;
				CenterEPSG900913 = camCenter;
			}
		}
	}

	private double[] ComputeCenterEPSG900913(double[] pos)
	{
		Vector3 displacement = new Vector3((float)(centerEPSG900913[0] - pos[0]) * roundedScaleMultiplier, 0.0f, (float)(centerEPSG900913[1] - pos[1]) * roundedScaleMultiplier);
		Vector3 rootPosition = this.gameObject.transform.position;
		this.gameObject.transform.position = new Vector3(
			rootPosition.x + displacement.x,
			rootPosition.y + displacement.y,
			rootPosition.z + displacement.z);

		if (pos[0] > GeoHelpers.HalfEarthCircumference)
			pos[0] -= GeoHelpers.EarthCircumference;
		else if (pos[0] < -GeoHelpers.HalfEarthCircumference)
			pos[0] += GeoHelpers.EarthCircumference;

		return pos;
	}

    private void UpdateInternals()
    {
        // FIXME: the half map scale is a value used throughout the implementation to rule the camera elevation
        // and the size/scale of the tiles, it depends on fixed tile size and resolution (here 256 and 72) so I am not
        // sure it would work for a tile layer with different values...
        // maybe there is a way to take the values out of the calculations and reintroduce them on Layer level...
        // FIXME: the 'division by 20000' helps the values to be kept in range for the Unity3D engine, not sure
        // this is the right approach either, feels kinda voodooish...
		
        halfMapScale = GeoHelpers.OsmZoomLevelToMapScale(currentZoom, 0.0f, tileResolution, 72) / scaleDivider;
        roundedHalfMapScale = GeoHelpers.OsmZoomLevelToMapScale(roundedZoom, 0.0f, tileResolution, 72) / scaleDivider;

        metersPerPixel = GeoHelpers.MetersPerPixel(0.0f, (float)currentZoom);
        roundedMetersPerPixel = GeoHelpers.MetersPerPixel(0.0f, (float)roundedZoom);
        
        // FIXME: another voodoish value to help converting meters (EPSG 900913) to Unity3D world coordinates
        scaleMultiplier = halfMapScale / (metersPerPixel * tileResolution);
        roundedScaleMultiplier = roundedHalfMapScale / (roundedMetersPerPixel * tileResolution);
    }
    
    #endregion
	
	#region MonoBehaviour implementation
	
	private void Awake()
	{
        // initialize the coordinate transformation
        epsg900913 = CoordinateSystemWktReader.Parse(wktEPSG900913) as ICoordinateSystem;
        ctFactory = new CoordinateTransformationFactory();
        wgs84ToEPSG900913 = ctFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, epsg900913);
        wgs84ToEPSG900913Transform = wgs84ToEPSG900913.MathTransform;
        epsg900913ToWGS84Transform = wgs84ToEPSG900913Transform.Inverse();
    }
	
	private void Start ()
	{
        // setup the gui scale according to the screen resolution
        if (Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer)
            screenScale = (Screen.orientation == ScreenOrientation.Landscape ? Screen.width : Screen.height) / 480.0f;
        else
            screenScale = 2.0f;

        // initialize the camera position and rotation
        currentCamera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
		Zoom(0.0f);
	}
	
	private void OnGUI()
	{
    	// FIXME: gaps beween tiles appear when zooming and panning the map at the same time on iOS, precision ???
		// TODO: optimise, use one mesh for the tiles and combine textures in a big one (might resolve the gap bug above)

		// process the user defined GUI
        if (ShowGUIControls && guiDelegate != null)
        {
			wasInputInterceptedByGUI = guiDelegate(this);
        }
		
		if (Event.current.type != EventType.Repaint
            && Event.current.type != EventType.MouseDown
            && Event.current.type != EventType.MouseDrag
            && Event.current.type != EventType.MouseMove
            && Event.current.type != EventType.MouseUp)
			return ;
		
        if (InputsEnabled && inputDelegate != null)
        {
			inputDelegate(this, wasInputInterceptedByGUI);
        }
		
	}
	
	private void Update()
	{
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.Begin("Map.Update");
#endif
		
		// update the centerWGS84 with the last location if enabled
		if (useLocation
			&& UnityEngine.Input.location.status == LocationServiceStatus.Running)
		{
			if (updateCenterWithLocation)
			{
				if (UnityEngine.Input.location.lastData.longitude <= 180.0f
					&& UnityEngine.Input.location.lastData.longitude >= -180.0f
					&& UnityEngine.Input.location.lastData.latitude <= 90.0f
					&& UnityEngine.Input.location.lastData.latitude >= -90.0f)
				{
					if (CenterWGS84[0] != UnityEngine.Input.location.lastData.longitude
					|| CenterWGS84[1] != UnityEngine.Input.location.lastData.latitude)
						CenterWGS84 = new double[2] { UnityEngine.Input.location.lastData.longitude, UnityEngine.Input.location.lastData.latitude };
					
					//Debug.Log("DEBUG: Map.Update: new location: " + Input.location.lastData.longitude + " " + Input.location.lastData.latitude + ":  " + Input.location.status);					
				}
				else
				{
//#if DEBUG_LOG
					Debug.LogWarning("WARNING: Map.Update: bogus location (bailing): " + UnityEngine.Input.location.lastData.longitude + " " + UnityEngine.Input.location.lastData.latitude + ":  " + UnityEngine.Input.location.status);
//#endif
				}
			}
			
			if (locationMarker != null)
			{
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (locationMarker.gameObject.active == false)
					locationMarker.gameObject.SetActiveRecursively(true);
#else
				if (locationMarker.gameObject.activeSelf == false)
					locationMarker.gameObject.SetActive(true);
#endif
				if (UnityEngine.Input.location.lastData.longitude <= 180.0f
					&& UnityEngine.Input.location.lastData.longitude >= -180.0f
					&& UnityEngine.Input.location.lastData.latitude <= 90.0f
					&& UnityEngine.Input.location.lastData.latitude >= -90.0f)
				{
					locationMarker.CoordinatesWGS84 = new double[2] { UnityEngine.Input.location.lastData.longitude, UnityEngine.Input.location.lastData.latitude };
				}
				else
				{
//#if DEBUG_LOG
					Debug.LogWarning("WARNING: Map.Update: bogus location (bailing): " + UnityEngine.Input.location.lastData.longitude + " " + UnityEngine.Input.location.lastData.latitude + ":  " + UnityEngine.Input.location.status);
//#endif
				}
			}
		}
		
		// update the orientation of the location marker
		if (useOrientation)
		{
            float heading = 0.0f;
            // TODO: handle all device orientations
            switch (Screen.orientation)
            {
            case ScreenOrientation.LandscapeLeft:
					heading = UnityEngine.Input.compass.trueHeading;
                break ;
            case ScreenOrientation.Portrait: // FIXME: not tested, likely wrong, legacy code
				heading = -UnityEngine.Input.compass.trueHeading;
                break ;
            }

			if (cameraFollowsOrientation)
			{
				if (lastCameraOrientation == 0.0f)
				{
					currentCamera.transform.RotateAround(Vector3.zero, Vector3.up, heading);

					lastCameraOrientation = heading;
				}
				else
				{
					float cameraRotationSpeed = 1.0f;
					float relativeAngle = (heading - lastCameraOrientation) * cameraRotationSpeed * Time.deltaTime;
					if (relativeAngle > 0.01f)
					{
						currentCamera.transform.RotateAround(Vector3.zero, Vector3.up, relativeAngle);
	
						//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
						lastCameraOrientation += relativeAngle;
					}
					else
					{
						currentCamera.transform.RotateAround(Vector3.zero, Vector3.up, heading - lastCameraOrientation);
	
						//Debug.Log("DEBUG: cam: " + lastCameraOrientation + ", heading: " + heading +  ", rel angle: " + relativeAngle);
						lastCameraOrientation = heading;
					}
				}
					
				IsDirty = true;
			}
				
			if (locationMarker != null
				&& locationMarker.OrientationMarker != null)
			{
	            //Debug.Log("DEBUG: " + heading);
				locationMarker.OrientationMarker.rotation = Quaternion.AngleAxis(heading, Vector3.up);
			}
		}
		
		// pause the loading operations when moving
		if (hasMoved == true)
		{
			TileDownloader.Instance.PauseAll();
		}
		else
		{
			TileDownloader.Instance.UnpauseAll();
		}
			
		// update the tiles if needed
		if (IsDirty == true && hasMoved == false)
		{
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
				locationMarker.UpdateMarker();
			
			foreach (Layer layer in layers)
			{	
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (layer.gameObject.active == true
#else
				if (layer.gameObject.activeSelf == true
#endif
					&& layer.enabled == true
					&& CurrentZoom >= layer.MinZoom
					&& CurrentZoom <= layer.MaxZoom)
					layer.UpdateContent();
			}
			
			foreach (Marker marker in markers)
			{
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
				if (marker.gameObject.active == true
#else
				if (marker.gameObject.activeSelf == true
#endif
					&& marker.enabled == true)
					marker.UpdateMarker();
			}
			
			if (this.gameObject.transform.position != Vector3.zero)
				this.gameObject.transform.position = Vector3.zero;

#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: updated layers");
#endif
		}
		
		// TODO: pause the TileDownloader when moving
		
		// reset the deferred update flag
		hasMoved = false;
						
#if DEBUG_PROFILE
		UnitySlippyMap.Profiler.End("Map.Update");
#endif
	}
	
	#endregion
	
	#region Map methods
	
	/// <summary>
	/// Centers the map on the location of the device.
	/// </summary>
	public void CenterOnLocation()
    {
		if (locationMarker != null
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
			&& locationMarker.gameObject.active == true)
#else
			&& locationMarker.gameObject.activeSelf == true)
#endif
			CenterWGS84 = locationMarker.CoordinatesWGS84;
        updateCenterWithLocation = true;
    }
	
	// <summary>
	// Sets the the marker for the device's location and orientation using a GameObject for display.
	// </summary>
	public T SetLocationMarker<T>(GameObject locationGo) where T : LocationMarker
	{
		return SetLocationMarker<T>(locationGo, null);
	}
	
	public T SetLocationMarker<T>(GameObject locationGo, GameObject orientationGo) where T : LocationMarker
	{
		// create a GameObject and add the templated Marker component to it
        GameObject markerObject = new GameObject("[location marker]");
		markerObject.transform.parent = this.gameObject.transform;
		
		T marker = markerObject.AddComponent<T>();
		
		locationGo.transform.parent = markerObject.transform;
		locationGo.transform.localPosition = Vector3.zero;
		
		if (orientationGo != null)
		{
			marker.OrientationMarker = orientationGo.transform;
		}
		
		// setup the marker
		marker.Map = this;
		if (useLocation
			&& UnityEngine.Input.location.status == LocationServiceStatus.Running)
			marker.CoordinatesWGS84 = new double[2] { UnityEngine.Input.location.lastData.longitude, UnityEngine.Input.location.lastData.latitude };
		else
#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
			markerObject.SetActiveRecursively(false);
#else
			markerObject.SetActive(false);
#endif
		
		// set the location marker
		locationMarker = marker;
		
		// tell the map to update
		IsDirty = true;
		
		return marker;
	}

	
	// <summary>
	// Creates a new named layer.
	// </summary>
	public T CreateLayer<T>(string name) where T : Layer
	{
		// create a GameObject as the root of the layer and add the templated Layer component to it
        GameObject layerRoot = new GameObject(name);
        Transform layerRootTransform = layerRoot.transform;
        //Debug.Log("DEBUG: layer root: " + layerRootTransform.position + " this position: " + this.gameObject.transform.position);
		layerRootTransform.parent = this.gameObject.transform;
        layerRootTransform.localPosition = Vector3.zero;
		T layer = layerRoot.AddComponent<T>();
		
		// setup the layer
		layer.Map = this;
		layer.MinZoom = minZoom;
		layer.MaxZoom = maxZoom;
		
		// add the layer to the layers' list
		layers.Add(layer);
		
		// tell the map to update
		IsDirty = true;
		
		return layer;
	}
	
	// <summary>
	// Creates a new named marker at the specified coordinates using a GameObject for display.
	// </summary>
	public T CreateMarker<T>(string name, double[] coordinatesWGS84, GameObject go) where T : Marker
	{
		// create a GameObject and add the templated Marker component to it
        GameObject markerObject = new GameObject(name);
		markerObject.transform.parent = this.gameObject.transform;
		
		//go.name = "go - " + name;
		go.transform.parent = markerObject.gameObject.transform;
		go.transform.localPosition = Vector3.zero;
		
		T marker = markerObject.AddComponent<T>();
		
		// setup the marker
		marker.Map = this;
		marker.CoordinatesWGS84 = coordinatesWGS84;
		
		// add marker to the markers' list
		markers.Add(marker);
		
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
    public void RemoveMarker(Marker m)
    {
        if (m == null)
            throw new ArgumentNullException("m");
        
        if (markers.Contains(m) == false)
            throw new ArgumentOutOfRangeException("m");
        
        markers.Remove(m);
        
        DestroyImmediate(m.gameObject);
    }
	
	// <summary>
	// Zooms the map.
	// </summary>
	public void Zoom(float zoomSpeed)
	{
		// apply the zoom
		CurrentZoom += 4.0f * zoomSpeed * Time.deltaTime;

		// move the camera
		// FIXME: the camera jumps on the first zoom when tilted, 'cause cam altitude and zoom value are unsynced by the rotation
		Transform cameraTransform = currentCamera.transform;
		float y = GeoHelpers.OsmZoomLevelToMapScale(currentZoom, 0.0f, tileResolution, 72) / scaleDivider * screenScale;
		float t = y / cameraTransform.forward.y;
		cameraTransform.position = new Vector3(
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