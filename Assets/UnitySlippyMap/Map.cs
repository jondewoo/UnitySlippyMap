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
using System.Collections;
using System.Collections.Generic;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;

using UnitySlippyMap;

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
	
	// <summary>
	// Should be set to true to tell the map to update its layers and markers.
	// </summary>
	private bool							needsToUpdate = false;
	
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
			
			double[] newCenterESPG900913 = Tile.WGS84ToMeters(value[0], value[1]);
			Vector3 displacement = new Vector3((float)(centerEPSG900913[0] - newCenterESPG900913[0]) * roundedScaleMultiplier, 0.0f, (float)(centerEPSG900913[1] - newCenterESPG900913[1]) * roundedScaleMultiplier);
			Vector3 rootPosition = this.gameObject.transform.position;
			this.gameObject.transform.position = new Vector3(
				rootPosition.x + displacement.x,
				rootPosition.y + displacement.y,
				rootPosition.z + displacement.z);			

			centerWGS84 = value;
			centerEPSG900913 = newCenterESPG900913;
            
            //UpdateInternals();
            
			needsToUpdate = true;
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
			
			Vector3 displacement = new Vector3((float)(centerEPSG900913[0] - value[0]) * roundedScaleMultiplier, 0.0f, (float)(centerEPSG900913[1] - value[1]) * roundedScaleMultiplier);
			Vector3 rootPosition = this.gameObject.transform.position;
			this.gameObject.transform.position = new Vector3(
				rootPosition.x + displacement.x,
				rootPosition.y + displacement.y,
				rootPosition.z + displacement.z);
			
			centerEPSG900913 = value;
			centerWGS84 = Tile.MetersToWGS84(centerEPSG900913[0], centerEPSG900913[1]);
            
			needsToUpdate = true;
		}
	}
	
	// <summary>
	// Is used to constraint the map panning.
	// </summary>
	// TODO: implement the constraint
    //private double[]						size = new double[2];
	
	private float							currentZoom = 15.0f;
	public float							CurrentZoom
	{
		get { return currentZoom; }
		set
		{
			if (value < minZoom
				|| value > (maxZoom + 0.5f))
			{
#if DEBUG_LOG
				Debug.LogError("ERROR: Map.Zoom: value must be inside range [" + minZoom + " - " + maxZoom + "]");
#endif
				return ;
			}
			
			currentZoom = value;
			roundedZoom = (int)Mathf.Floor(currentZoom);

            UpdateInternals();
		}
	}
	
	private float							minZoom = 1.0f;
	public float							MinZoom
	{
		get { return minZoom; }
		set
		{
			if (value < 1.0f
				|| value > 18.0f)
			{
				minZoom = Mathf.Clamp(value, 1.0f, 18.0f);
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
			if (value < 1.0f
				|| value > 18.0f)
			{
				maxZoom = Mathf.Clamp(value, 1.0f, 18.0f);
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
				if (Input.location.isEnabledByUser
					&& (Input.location.status == LocationServiceStatus.Stopped
					|| Input.location.status == LocationServiceStatus.Failed))
				{
					Input.location.Start();
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
				if (Input.location.isEnabledByUser
					&& (Input.location.status == LocationServiceStatus.Initializing
					|| Input.location.status == LocationServiceStatus.Running))
				{
					Input.location.Start();
				}
			}
		}
	}
	
	// <summary>
	// Is set to false is the map is manipulated by the user.
	// </summary>
	// TODO: should be set back to true after manipulation on user input
	private bool							needsToUpdateCenterWithLocation = true;
	
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
					if (Input.location.isEnabledByUser
						&& (Input.location.status == LocationServiceStatus.Stopped
						|| Input.location.status == LocationServiceStatus.Failed))
					{
						Input.location.Start();
					}
					else
					{
#if DEBUG_LOG
						Debug.LogError("ERROR: Map.UseOrientation: Location is not authorized on the device.");
#endif
					}
				}
				Input.compass.enabled = true;
			}
			else
			{
				if (useLocation == false)
				{
					if (Input.location.isEnabledByUser
						&& (Input.location.status == LocationServiceStatus.Initializing
						|| Input.location.status == LocationServiceStatus.Running))
						Input.location.Start();
				}
				Input.compass.enabled = false;
			}
		}
	}

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
	
	private bool							mapMoved = false;
	private Vector3							lastHitPosition = Vector3.zero;
	private float							lastZoomFactor = 0.0f;
    
	// FIXME: tests of the ProjNet Dll: http://projnet.codeplex.com/
	// seemed promising but encountered limitations and positioning errors with EPSG 900913
	// such a library will be necessary to enable support of arbitrary coordinate systems
	
	/*
	private ICoordinateSystem				csEPSG900913;
	CoordinateTransformationFactory			ctFactory;
	ICoordinateTransformation				trans4326To900913;
	*/
	// crs conversion
	/*
	csEPSG900913 = 
		CoordinateSystemWktReader.Parse(
			"PROJCS[\"Mercator Spheric\", GEOGCS[\"WGS84basedSpheric_GCS\", DATUM[\"WGS84basedSpheric_Datum\", SPHEROID[\"WGS84based_Sphere\", 6378137, 0], TOWGS84[0, 0, 0, 0, 0, 0, 0]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.01745329251994328, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH]]") as ICoordinateSystem;
	
	ctFactory = new CoordinateTransformationFactory();
	trans4326To900913 = ctFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, csEPSG900913);
	
	double[] center900913 = trans4326To900913.MathTransform.Transform(centerWGS84);
	double[] centerTile900913 = trans4326To900913.MathTransform.Transform(centerTile);
	*/
	
	#endregion
    
    #region Private methods
    
    private void UpdateInternals()
    {
        // FIXME: the half map scale is a value used throughout the implementation to rule the camera elevation
        // and the size/scale of the tiles, it depends on fixed tile size and resolution (here 256 and 72) so I am not
        // sure it would work for a tile layer with different values...
        // maybe there is a way to take the values out of the calculations and reintroduce them on Layer level...
        // FIXME: the 'division by 20000' helps the values to be kept in range for the Unity3D engine, not sure
        // this is the right approach either, feels kinda voodooish...
        halfMapScale = Tile.OsmZoomLevelToMapScale(currentZoom, /*(float)centerWGS84[1]*/0.0f, 256.0f, 72) / 20000.0f;
        roundedHalfMapScale = Tile.OsmZoomLevelToMapScale(roundedZoom, (float)/*(float)centerWGS84[1]*/0.0f, 256.0f, 72) / 20000.0f;
        
        metersPerPixel = Tile.MetersPerPixel(0.0f, (float)currentZoom);
        roundedMetersPerPixel = Tile.MetersPerPixel(0.0f, (float)roundedZoom);
        
        // FIXME: another voodoish value to help converting meters (EPSG 900913) to Unity3D world coordinates
        scaleMultiplier = halfMapScale / (metersPerPixel * 256.0f);
        roundedScaleMultiplier = roundedHalfMapScale / (roundedMetersPerPixel * 256.0f);
    }
    
    #endregion
	
	#region MonoBehaviour implementation
	
	private void Awake()
	{
		// initialize the zoom variables
		CurrentZoom = currentZoom;
	}
	
	private void Start ()
	{
		// initialize the camera position and rotation
		Camera.main.transform.position = new Vector3(
			0,
            //Tile.OsmZoomLevelToMapScale(currentZoom, 0.0f, 256.0f, 72) / 10000.0f,
            Tile.OsmZoomLevelToMapScale(currentZoom, 0.0f, 256.0f, 72) / 20000.0f,
			0);
        Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

        // set the update flag to tell the behaviour the user is manipulating the map
        mapMoved = true;
        needsToUpdate = true;
	}
	
	private void OnGUI()
	{
#if UNITY_EDITOR
		// TODO: more complete GUI (zoom, translation, ...), customization
        if (ShowGUIControls)
        {
    		GUI.Label(new Rect(0, Screen.height - 100, 100, 100), "Zoom: " + currentZoom);
    		
    		if (GUI.RepeatButton(new Rect(Screen.width - 100, 100, 100, 100), "+"))
    		{
    			Zoom(1.0f);
    		}
    		if (GUI.RepeatButton(new Rect(Screen.width - 100, 200, 100, 100), "-"))
    		{
    			Zoom(-1.0f);
    		}
        }
#endif
	}
	
	private void Update ()
	{
        if (InputsEnabled)
        {
    		// handle inputs on touch devices and desktop
    		// the map is told to update its layers and markers once a movement is complete
    		// when panning the map, the map's root GameObject is moved ; once the panning is done, all the children are offseted and the root's position is reset
    		// FIXME: gaps beween tiles appear when zooming and panning the map at the same time on iOS
    		bool panning = false;
    		bool panningStopped = false;
    		Vector3 screenPosition = Vector3.zero;
    
    		bool zooming = false;
    		bool zoomingStopped = false;
    		float zoomFactor = 0.0f;
    		
    		if (Application.platform == RuntimePlatform.IPhonePlayer
    			|| Application.platform == RuntimePlatform.Android)
    		{
                int touchCount = Input.touchCount;
    			if (Input.touchCount > 0)
    			{
    				// movements
    				panning = true;
    				panningStopped = true;
                    
                    int validTouchCount = touchCount;
    				foreach (Touch touch in Input.touches)
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
                    
                    //Debug.Log("DEBUG: panning: touch count: " + touchCount + ", screen pos: (" + screenPosition.x + " " + screenPosition.y + " " + screenPosition.z + "), panning stopped: " + panningStopped);
                    
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
                        Touch touch = Input.GetTouch(i);
                        
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
    		else
    		{
    			// movements
    			if (Input.GetMouseButton(0))
    			{
    				panning = true;
    				screenPosition = Input.mousePosition;
    			}
    			else if (Input.GetMouseButtonUp(0))
    			{
    				panningStopped = true;
    			}
    			
    			// zoom
    			if (Input.GetKey(KeyCode.Z))
    			{
    				zooming = true;
    				zoomFactor = 1.0f;
    				lastZoomFactor = 0.0f;
    			}
    			else if (Input.GetKeyUp(KeyCode.Z))
    			{
    				zoomingStopped = true;
    			}
    			if (Input.GetKey(KeyCode.S))
    			{
    				zooming = true;
    				zoomFactor = -1.0f;
    				lastZoomFactor = 0.0f;
    			}
    			else if (Input.GetKeyUp(KeyCode.S))
    			{
    				zoomingStopped = true;
    			}
    		}
    		
    		if (panning)
    		{
    			// disable the centerWGS84 update with the last location
    			needsToUpdateCenterWithLocation = false;
    			
    			// apply the movements
    			Ray ray = Camera.main.ScreenPointToRay(screenPosition);
    			RaycastHit hitInfo;
    			if (Physics.Raycast(ray, out hitInfo))
    			{
    				//Debug.Log("DEBUG: last hit: " + lastHitPosition + ", hit: " + hitInfo.point);
    				Vector3 displacement = Vector3.zero;
    				if (lastHitPosition != Vector3.zero)
    				{
    					displacement = hitInfo.point - lastHitPosition;
    					/*
    					Vector3 rootPosition = this.gameObject.transform.position;
    					this.gameObject.transform.position = new Vector3(
    						rootPosition.x + displacement.x,
    						rootPosition.y + displacement.y,
    						rootPosition.z + displacement.z);
    						*/
    				}
    				lastHitPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
    				//Debug.Log("DEBUG: last hit: " + lastHitPosition + ", hit: " + hitInfo.point);
    				
    				if (displacement != Vector3.zero)
    				{
    					// update the centerWGS84 property to the new centerWGS84 wgs84 coordinates of the map
    					double[] displacementMeters = new double[2] { displacement.x / roundedScaleMultiplier, displacement.z / roundedScaleMultiplier };
    					double[] centerMeters = new double[2] { centerEPSG900913[0], centerEPSG900913[1] };
    					centerMeters[0] -= displacementMeters[0];
    					centerMeters[1] -= displacementMeters[1];
    					CenterEPSG900913 = centerMeters;
    					
    #if DEBUG_LOG
    					Debug.Log("DEBUG: Map.Update: new centerWGS84 wgs84: " + centerWGS84[0] + ", " + centerWGS84[1]);
    #endif
    				}
    
    				mapMoved = true;
    			}
    		}
    		else if (panningStopped)
    		{
    			// reset the last hit position
    			lastHitPosition = Vector3.zero;
    			
    			// trigger a tile update
    			needsToUpdate = true;
    		}
    
    		// apply the zoom
    		if (zooming)
    		{			
    			//if (lastZoomFactor != 0.0f)// && zoomFactor != 0.0f)
    				Zoom(zoomFactor - lastZoomFactor);
    			lastZoomFactor = zoomFactor;
    		}
    		else if (zoomingStopped)
    		{
    			lastZoomFactor = 0.0f;
    		}
        }
		
		// update the centerWGS84 with the last location if enabled
		if (useLocation
			&& Input.location.status == LocationServiceStatus.Running)
		{
			if (needsToUpdateCenterWithLocation)
			{
				CenterWGS84 = new double[2] { Input.location.lastData.longitude, Input.location.lastData.latitude };
			}
			
			if (locationMarker != null)
			{
				if (locationMarker.gameObject.active == false)
					locationMarker.gameObject.SetActiveRecursively(true);
				locationMarker.CoordinatesWGS84 = new double[2] { Input.location.lastData.longitude, Input.location.lastData.latitude };
			}
		}
		
		// update the orientation of the location marker
		if (useOrientation
			&& locationMarker != null
			&& locationMarker.OrientationMarker != null)
		{
            float heading = 0.0f;
            switch (Screen.orientation)
            {
            case ScreenOrientation.LandscapeLeft:
                //heading = -Input.compass.trueHeading; // test for device up
                heading = Input.compass.trueHeading;
                /*
                if (heading > 360.0f) {
                    heading -= 360.0f;
                }
                */
                // TODO: handle all device orientations
                break ;
            case ScreenOrientation.Portrait: // FIME: not tested, likely wrong, legacy code
                heading = -Input.compass.trueHeading;
                break ;
            }
            //Debug.Log("DEBUG: " + heading);
			locationMarker.OrientationMarker.rotation = Quaternion.AngleAxis(heading, Vector3.up);
		}
		
		// update the tiles if needed
		if (needsToUpdate == true && mapMoved == false)
		{
			needsToUpdate = false;
			
			if (locationMarker != null
				&& locationMarker.gameObject.active == true)
				locationMarker.UpdateMarker();
			
			foreach (Layer layer in layers)
			{
				layer.UpdateContent();
			}
			
			foreach (Marker marker in markers)
			{
				marker.UpdateMarker();
			}
			
			if (this.gameObject.transform.position != Vector3.zero)
				this.gameObject.transform.position = Vector3.zero;

#if DEBUG_LOG
			Debug.Log("DEBUG: Map.Update: updated layers");
#endif
		}
		
		// reset the deferred update flag
		mapMoved = false;
	}
	
	#endregion
	
	#region Map methods
	
	public void CenterOnLocation()
    {
        needsToUpdateCenterWithLocation = true;
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
			&& Input.location.status == LocationServiceStatus.Running)
			marker.CoordinatesWGS84 = new double[2] { Input.location.lastData.longitude, Input.location.lastData.latitude };
		else
			markerObject.SetActiveRecursively(false);
		
		// set the location marker
		locationMarker = marker;
		
		// tell the map to update
		needsToUpdate = true;
		
		return marker;
	}

	
	// <summary>
	// Creates a new named layer.
	// </summary>
	public T CreateLayer<T>(string name) where T : Layer
	{
		// create a GameObject as the root of the layer and add the templated Layer component to it
        GameObject layerRoot = new GameObject(name);
		layerRoot.transform.parent = this.gameObject.transform;
		T layer = layerRoot.AddComponent<T>();
		
		// setup the layer
		layer.Map = this;
		
		// add the layer to the layers' list
		layers.Add(layer);
		
		// tell the map to update
		needsToUpdate = true;
		
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
		needsToUpdate = true;
		
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
		CurrentZoom += zoomSpeed * Time.deltaTime;
		
		// move the camera
		Transform cameraTransform = Camera.main.transform;
		cameraTransform.position = new Vector3(
			cameraTransform.position.x,
            //Tile.OsmZoomLevelToMapScale(currentZoom, 0.0f, 256.0f, 72) / 10000.0f,
            Tile.OsmZoomLevelToMapScale(currentZoom, 0.0f, 256.0f, 72) / 20000.0f,
			cameraTransform.position.z);
		
		// set the update flag to tell the behaviour the user is manipulating the map
		mapMoved = true;
		needsToUpdate = true;
	}
	
	#endregion
}
