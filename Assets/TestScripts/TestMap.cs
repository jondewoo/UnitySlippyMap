// 
//  TestMap.cs
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

using UnitySlippyMap;

public class TestMap : MonoBehaviour
{
	private Map		map;
	
	public Texture	MarkerTexture;
	
	void Start()
	{
		// create the map singleton
		map = Map.Instance;
		
		// 9 rue Gentil, Lyon
		map.CenterWGS84 = new double[2] { 4.83527, 45.76487 };
		map.UseLocation = true;
				
		// create a test layer
		TileLayer layer = map.CreateLayer<OSMTileLayer>("test tile layer");
		layer.URLFormat = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
		
		// create some test 2D markers
		GameObject go = Tile.CreateTileTemplate();
		go.renderer.material.mainTexture = MarkerTexture;
		go.renderer.material.renderQueue = 4000;
		
		GameObject markerGO;
		markerGO = Instantiate(go) as GameObject;
		map.CreateMarker<Marker>("test marker - 9 rue Gentil, Lyon", new double[2] { 4.83527, 45.76487 }, markerGO);
		
		markerGO = Instantiate(go) as GameObject;
		map.CreateMarker<Marker>("test marker - 31 rue de la Bourse, Lyon", new double[2] { 4.83699, 45.76535 }, markerGO);
		
		markerGO = Instantiate(go) as GameObject;
		map.CreateMarker<Marker>("test marker - 1 place St Nizier, Lyon", new double[2] { 4.83295, 45.76468 }, markerGO);

		DestroyImmediate(go);
	}
	
	void OnApplicationQuit()
	{
		map = null;
	}
}

