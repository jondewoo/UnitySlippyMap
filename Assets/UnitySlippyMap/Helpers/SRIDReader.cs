// 
//  SRIDReader.cs
//  
//  Author:
//		 SharpGIS <sharpgis.net>
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2007 SharpGIS
//  Copyright (c) 2012 Jonathan Derrough

using UnityEngine;

using System.Collections.Generic;

using ProjNet.CoordinateSystems;

namespace UnitySlippyMap
{
	// a helper class from the ProjNet documentation: http://projnet.codeplex.com/wikipage?title=LoadByID
	public class SridReader
	{
		private static string filename = "SRID";
	
		public struct WKTstring {
			/// <summary>
			/// Well-known ID
			/// </summary>
			public int WKID;
			/// <summary>
			/// Well-known Text
			/// </summary>
			public string WKT;
		}
	
		/// <summary>
		/// Enumerates all SRID's in the SRID.csv file.
		/// </summary>
		/// <returns>Enumerator</returns>
		public static IEnumerable<WKTstring> GetSRIDs()
		{
			TextAsset filecontent = Resources.Load(filename) as TextAsset;
			using (System.IO.StringReader sr = new System.IO.StringReader(filecontent.text))
			{
				string line = sr.ReadLine();
				while (line != null)
				{
					int split = line.IndexOf(';');
					if (split > -1)
					{
						WKTstring wkt = new WKTstring();
						wkt.WKID = int.Parse(line.Substring(0, split));
						wkt.WKT = line.Substring(split + 1);
						yield return wkt;
					}
					line = sr.ReadLine();
				}
				sr.Close();
			}
		}
		/// <summary>
		/// Gets a coordinate system from the SRID.csv file
		/// </summary>
		/// <param name="id">EPSG ID</param>
		/// <returns>Coordinate system, or null if SRID was not found.</returns>
		public static ICoordinateSystem GetCSbyID(int id)
		{
			//TODO: Enhance this with an index so we don't have to loop all the lines
			//ICoordinateSystemFactory fac = new CoordinateSystemFactory();
			foreach (SridReader.WKTstring wkt in SridReader.GetSRIDs())
			{
				if (wkt.WKID == id)
				{
					return ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(wkt.WKT) as ICoordinateSystem;
				}
			}
			return null;
		}
	}
}