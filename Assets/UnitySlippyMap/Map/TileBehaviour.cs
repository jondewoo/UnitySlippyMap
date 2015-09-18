// 
//  Tile.cs
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

namespace UnitySlippyMap.Map
{
	/// <summary>
	/// The tile implementation inherits from MonoBehaviour.
	/// </summary>
	public class TileBehaviour : MonoBehaviour
	{
    #region Private members & properties

		/// <summary>
		/// The texture identifier.
		/// </summary>
		private int			textureId;

		/// <summary>
		/// Gets or sets the texture identifier.
		/// </summary>
		/// <value>The texture identifier.</value>
		public int			TextureId {
			get {
				return textureId;
			}
			set {
				textureId = value;
			}
		}

		/// <summary>
		/// The showing flag.
		/// </summary>
		private bool showing = false;

		/// <summary>
		/// Gets a value indicating whether this <see cref="UnitySlippyMap.Map.Tile"/> is showing.
		/// </summary>
		/// <value><c>true</c> if showing; otherwise, <c>false</c>.</value>
		public bool Showing {
			get { return showing; }
		}

		/// <summary>
		/// The material.
		/// </summary>
		private Material material;

		/// <summary>
		/// The duration of the apparition.
		/// </summary>
		private float apparitionDuration = 0.5f;

		/// <summary>
		/// The apparition start time.
		/// </summary>
		private float apparitionStartTime = 0.0f;
	

    #endregion
	
	#region MonoBehaviour implementation

		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// </summary>
		private void Update ()
		{
			if (showing) {
				float delta = Time.time - apparitionStartTime;
				float a = 1.0f;
				if (delta <= apparitionDuration) {
					a = delta / apparitionDuration;
				} else {
					showing = false;
					MapBehaviour.Instance.IsDirty = true;
				}
				Color color = material.color;
				color.a = a;
				material.color = color;
			}
		}
	
	#endregion
	
    #region Public enums

		/// <summary>
		/// The anchor points enumeration.
		/// </summary>
		public enum AnchorPoint
		{
			TopLeft,
			TopCenter,
			TopRight,
			MiddleLeft,
			MiddleCenter,
			MiddleRight,
			BottomLeft,
			BottomCenter,
			BottomRight
		}

    #endregion 

    #region Public methods
	
		/// <summary>
		/// Show this instance.
		/// </summary>
		public void Show ()
		{
			showing = true;
			Color color = material.color;
			color.a = 0.0f;
			material.color = color;
			apparitionStartTime = Time.time;
		}

		/// <summary>
		/// Creates a tile template GameObject.
		/// </summary>
		public static TileBehaviour CreateTileTemplate ()
		{
			return CreateTileTemplate ("[Tile Template]", AnchorPoint.MiddleCenter);
		}

		/// <summary>
		/// Creates a tile template GameObject.
		/// </summary>
		/// <returns>The tile template.</returns>
		/// <param name="name">Name.</param>
		public static TileBehaviour CreateTileTemplate (string name)
		{
			return CreateTileTemplate (name, AnchorPoint.MiddleCenter);
		}

		/// <summary>
		/// Creates a tile template GameObject.
		/// </summary>
		/// <returns>The tile template.</returns>
		/// <param name="anchorPoint">Anchor point.</param>
		public static TileBehaviour CreateTileTemplate (AnchorPoint anchorPoint)
		{
			return CreateTileTemplate ("[Tile Template]", anchorPoint);
		}

		/// <summary>
		/// Creates a tile template GameObject.
		/// </summary>
		/// <returns>The tile template.</returns>
		/// <param name="tileName">Tile name.</param>
		/// <param name="anchorPoint">Anchor point.</param>
		public static TileBehaviour CreateTileTemplate (string tileName, AnchorPoint anchorPoint)
		{
			GameObject tileTemplate = new GameObject (tileName);
			TileBehaviour tile = tileTemplate.AddComponent<TileBehaviour> ();
			MeshFilter meshFilter = tileTemplate.AddComponent<MeshFilter> ();
			MeshRenderer meshRenderer = tileTemplate.AddComponent<MeshRenderer> ();
			BoxCollider boxCollider = tileTemplate.AddComponent<BoxCollider> ();
			
			// add the geometry
			Mesh mesh = meshFilter.mesh;
			switch (anchorPoint) {
			case AnchorPoint.TopLeft:
				mesh.vertices = new Vector3[] {
				new Vector3 (1.0f, 0.0f, 0.0f),
				new Vector3 (1.0f, 0.0f, -1.0f),
				new Vector3 (0.0f, 0.0f, -1.0f),
				new Vector3 (0.0f, 0.0f, 0.0f)
			};
				break;
			case AnchorPoint.TopCenter:
				mesh.vertices = new Vector3[] {
				new Vector3 (0.5f, 0.0f, 0.0f),
				new Vector3 (0.5f, 0.0f, -1.0f),
				new Vector3 (-0.5f, 0.0f, -1.0f),
				new Vector3 (-0.5f, 0.0f, 0.0f)
			};
				break;
			case AnchorPoint.TopRight:
				mesh.vertices = new Vector3[] {
				new Vector3 (0.0f, 0.0f, 0.0f),
				new Vector3 (0.0f, 0.0f, -1.0f),
				new Vector3 (-1.0f, 0.0f, -1.0f),
				new Vector3 (-1.0f, 0.0f, 0.0f)
			};
				break;
			case AnchorPoint.MiddleLeft:
				mesh.vertices = new Vector3[] {
				new Vector3 (1.0f, 0.0f, 0.5f),
				new Vector3 (1.0f, 0.0f, -0.5f),
				new Vector3 (0.0f, 0.0f, -0.5f),
				new Vector3 (0.0f, 0.0f, 0.5f)
			};
				break;
			case AnchorPoint.MiddleRight:
				mesh.vertices = new Vector3[] {
				new Vector3 (0.0f, 0.0f, 0.5f),
				new Vector3 (0.0f, 0.0f, -0.5f),
				new Vector3 (-1.0f, 0.0f, -0.5f),
				new Vector3 (-1.0f, 0.0f, 0.5f)
			};
				break;
			case AnchorPoint.BottomLeft:
				mesh.vertices = new Vector3[] {
				new Vector3 (1.0f, 0.0f, 1.0f),
				new Vector3 (1.0f, 0.0f, 0.0f),
				new Vector3 (0.0f, 0.0f, 0.0f),
				new Vector3 (0.0f, 0.0f, 1.0f)
			};
				break;
			case AnchorPoint.BottomCenter:
				mesh.vertices = new Vector3[] {
				new Vector3 (0.5f, 0.0f, 1.0f),
				new Vector3 (0.5f, 0.0f, 0.0f),
				new Vector3 (-0.5f, 0.0f, 0.0f),
				new Vector3 (-0.5f, 0.0f, 1.0f)
			};
				break;
			case AnchorPoint.BottomRight:
				mesh.vertices = new Vector3[] {
				new Vector3 (0.0f, 0.0f, 1.0f),
				new Vector3 (0.0f, 0.0f, 0.0f),
				new Vector3 (-1.0f, 0.0f, 0.0f),
				new Vector3 (-1.0f, 0.0f, 1.0f)
			};
				break;
			default: // MiddleCenter
				mesh.vertices = new Vector3[] {
				new Vector3 (0.5f, 0.0f, 0.5f),
				new Vector3 (0.5f, 0.0f, -0.5f),
				new Vector3 (-0.5f, 0.0f, -0.5f),
				new Vector3 (-0.5f, 0.0f, 0.5f)
			};
				break;
			}
			mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		
			// add normals
			mesh.normals = new Vector3[] {
				Vector3.up,
				Vector3.up,
				Vector3.up,
				Vector3.up
			};
			// add uv coordinates
			mesh.uv = new Vector2[] {
				new Vector2 (1.0f, 1.0f),
				new Vector2 (1.0f, 0.0f),
				new Vector2 (0.0f, 0.0f),
				new Vector2 (0.0f, 1.0f)
			};
			
			// add a material
			string shaderName = "Somian/Unlit/Transparent";
			Shader shader = Shader.Find (shaderName);
			
#if DEBUG_LOG
		Debug.Log("DEBUG: shader for tile template: " + shaderName + ", exists: " + (shader != null));
#endif
			
			tile.material = meshRenderer.material = new Material (shader);
			
			// setup the collider
			boxCollider.size = new Vector3 (1.0f, 0.0f, 1.0f);
		
			return tile;
		}
	
		/// <summary>
		/// Sets the texture.
		/// </summary>
		/// <param name="texture">Texture.</param>
		public void SetTexture (Texture2D texture)
		{
			material = this.gameObject.renderer.material;
			material.mainTexture = texture;
			material.mainTexture.wrapMode = TextureWrapMode.Clamp;
			material.mainTexture.filterMode = FilterMode.Trilinear;
			this.renderer.enabled = true;
			this.Show ();
		}

		/// <summary>
		/// Gets the tile key.
		/// </summary>
		/// <returns>The tile key.</returns>
		/// <param name="roundedZoom">Rounded zoom.</param>
		/// <param name="tileX">Tile x.</param>
		/// <param name="tileY">Tile y.</param>
		public static string GetTileKey (int roundedZoom, int tileX, int tileY)
		{
			return roundedZoom + "_" + tileX + "_" + tileY;
		}

    #endregion
	}

}