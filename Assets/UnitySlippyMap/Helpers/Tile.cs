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

namespace UnitySlippyMap
{

// FIXME: not sure the use of a namespace is appropriate
// TODO: refactor the whole thing
public class Tile : MonoBehaviour
{
    #region Private members & properties

    private int			textureId;
    public int			TextureId
    {
        get
        {
            return textureId;
        }
        set
        {
            textureId = value;
        }
    }

	private bool showing = false;
	public bool Showing
	{
		get { return showing; }
	}

	private Material	material;
	private float		apparitionDuration = 0.5f;
	private float		apparitionStartTime = 0.0f;
	

    #endregion
	
	#region MonoBehaviour implementation

	private void Update()
	{
		if (showing)
		{
			float delta = Time.time - apparitionStartTime;
			float a = 1.0f;
			if (delta <= apparitionDuration)
			{
				a = delta / apparitionDuration;
			}
			else
			{
				showing = false;
				Map.Instance.IsDirty = true;
			}
			Color color = material.color;
			color.a = a;
			material.color = color;
		}
	}
	
	#endregion
	
    #region Public enums

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
	
	public void Show()
	{
		showing = true;
		Color color = material.color;
		color.a = 0.0f;
		material.color = color;
		apparitionStartTime = Time.time;
	}

    // <summary>
	// Returns a tile template GameObject.
	// </summary>
	public static Tile CreateTileTemplate()
	{
		return CreateTileTemplate("[Tile Template]", AnchorPoint.MiddleCenter);
	}
    public static Tile CreateTileTemplate(string name)
	{
		return CreateTileTemplate(name, AnchorPoint.MiddleCenter);
	}
    public static Tile CreateTileTemplate(AnchorPoint anchorPoint)
	{
		return CreateTileTemplate("[Tile Template]", anchorPoint);
	}
    public static Tile CreateTileTemplate(string tileName, AnchorPoint anchorPoint)
	{
		GameObject tileTemplate = new GameObject(tileName);
        Tile tile = tileTemplate.AddComponent<Tile>();
		MeshFilter meshFilter = tileTemplate.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = tileTemplate.AddComponent<MeshRenderer>();
		BoxCollider boxCollider = tileTemplate.AddComponent<BoxCollider>();
			
		// add the geometry
		Mesh mesh = meshFilter.mesh;
		switch (anchorPoint)
		{
		case AnchorPoint.TopLeft:
			mesh.vertices = new Vector3[] {
				new Vector3(1.0f, 0.0f, 0.0f),
				new Vector3(1.0f, 0.0f, -1.0f),
				new Vector3(0.0f, 0.0f, -1.0f),
				new Vector3(0.0f, 0.0f, 0.0f)
			};
			break;
		case AnchorPoint.TopCenter:
			mesh.vertices = new Vector3[] {
				new Vector3(0.5f, 0.0f, 0.0f),
				new Vector3(0.5f, 0.0f, -1.0f),
				new Vector3(-0.5f, 0.0f, -1.0f),
				new Vector3(-0.5f, 0.0f, 0.0f)
			};
			break;
		case AnchorPoint.TopRight:
			mesh.vertices = new Vector3[] {
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 0.0f, -1.0f),
				new Vector3(-1.0f, 0.0f, -1.0f),
				new Vector3(-1.0f, 0.0f, 0.0f)
			};
			break;
		case AnchorPoint.MiddleLeft:
			mesh.vertices = new Vector3[] {
				new Vector3(1.0f, 0.0f, 0.5f),
				new Vector3(1.0f, 0.0f, -0.5f),
				new Vector3(0.0f, 0.0f, -0.5f),
				new Vector3(0.0f, 0.0f, 0.5f)
			};
			break;
		case AnchorPoint.MiddleRight:
			mesh.vertices = new Vector3[] {
				new Vector3(0.0f, 0.0f, 0.5f),
				new Vector3(0.0f, 0.0f, -0.5f),
				new Vector3(-1.0f, 0.0f, -0.5f),
				new Vector3(-1.0f, 0.0f, 0.5f)
			};
			break;
		case AnchorPoint.BottomLeft:
			mesh.vertices = new Vector3[] {
				new Vector3(1.0f, 0.0f, 1.0f),
				new Vector3(1.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 0.0f, 1.0f)
			};
			break;
		case AnchorPoint.BottomCenter:
			mesh.vertices = new Vector3[] {
				new Vector3(0.5f, 0.0f, 1.0f),
				new Vector3(0.5f, 0.0f, 0.0f),
				new Vector3(-0.5f, 0.0f, 0.0f),
				new Vector3(-0.5f, 0.0f, 1.0f)
			};
			break;
		case AnchorPoint.BottomRight:
			mesh.vertices = new Vector3[] {
				new Vector3(0.0f, 0.0f, 1.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(-1.0f, 0.0f, 0.0f),
				new Vector3(-1.0f, 0.0f, 1.0f)
			};
			break;
		default: // MiddleCenter
			mesh.vertices = new Vector3[] {
				new Vector3(0.5f, 0.0f, 0.5f),
				new Vector3(0.5f, 0.0f, -0.5f),
				new Vector3(-0.5f, 0.0f, -0.5f),
				new Vector3(-0.5f, 0.0f, 0.5f)
			};
			break;
		}
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		
		// add normals
		mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
		// add uv coordinates
		mesh.uv = new Vector2[] { new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f) };
			
		// add a material
        string shaderName = "Somian/Unlit/Transparent";
        Shader shader = Shader.Find(shaderName);
			
#if DEBUG_LOG
		Debug.Log("DEBUG: shader for tile template: " + shaderName + ", exists: " + (shader != null));
#endif
			
		tile.material = meshRenderer.material = new Material(shader);
			
		// setup the collider
		boxCollider.size = new Vector3(1.0f, 0.0f, 1.0f);
		
		return tile;
    }
	
	public void SetTexture(Texture2D texture)
	{
		/*
		tile.TextureId = TextureAtlasManager.Instance.AddTexture(texture);
		TextureAtlas.TextureInfo textureInfo = TextureAtlasManager.Instance.GetTextureInfo(tile.TextureId);
		Material sharedMaterial = SharedMaterialManager.Instance.GetSharedMaterial(textureInfo.Texture.name, "Somian/Unlit/Transparent");
		GameObject gameObject = tile.gameObject;
		gameObject.renderer.sharedMaterial = sharedMaterial;
		if (sharedMaterial.mainTexture == null)
		{
		    sharedMaterial.mainTexture = textureInfo.Texture;
		    sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
		    sharedMaterial.mainTexture.filterMode = FilterMode.Trilinear;
		}
		
		gameObject.GetComponent<MeshFilter>().mesh.uv = new Vector2[4] {
		    new Vector2(textureInfo.Rect.xMax / textureInfo.Texture.width, textureInfo.Rect.yMax / textureInfo.Texture.height),
		    new Vector2(textureInfo.Rect.xMax / textureInfo.Texture.width, textureInfo.Rect.yMin / textureInfo.Texture.height),
		    new Vector2(textureInfo.Rect.xMin / textureInfo.Texture.width, textureInfo.Rect.yMin / textureInfo.Texture.height),
		    new Vector2(textureInfo.Rect.xMin / textureInfo.Texture.width, textureInfo.Rect.yMax / textureInfo.Texture.height)
		};
		*/
		
		material = this.gameObject.renderer.material;
		material.mainTexture = texture;
		material.mainTexture.wrapMode = TextureWrapMode.Clamp;
		material.mainTexture.filterMode = FilterMode.Trilinear;
		this.renderer.enabled = true;
		this.Show();
	}

	public static string GetTileKey(int roundedZoom, int tileX, int tileY)
	{
		return roundedZoom + "_" + tileX + "_" + tileY;
	}

    #endregion
}

}