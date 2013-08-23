// 
//  TextureAtlasManager.cs
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

using System.Collections.Generic;

namespace UnitySlippyMap
{

// <summary>
// A singleton class in charge of managing texture atlases.
// </summary>
public class TextureAtlasManager : MonoBehaviour
{
    #region Singleton implementation

    private static TextureAtlasManager instance = null;
    public static TextureAtlasManager Instance
    {
        get
        {
            if (null == (object)instance)
            {
                instance = FindObjectOfType(typeof(TextureAtlasManager)) as TextureAtlasManager;
                if (null == (object)instance)
                {
                    var go = new GameObject("[TextureAtlasManager]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    instance = go.AddComponent<TextureAtlasManager>();
                    instance.EnsureAtlasManager();
                }
            }

            return instance;
        }
    }

    private void EnsureAtlasManager()
    {
        atlases = new Dictionary<int, TextureAtlas>();
        textureAtlasMap = new Dictionary<int, KeyValuePair<int, int>>();
    }

    private TextureAtlasManager()
    {
    }

    private void OnApplicationQuit()
    {
        DestroyImmediate(this.gameObject);
    }

    #endregion

    #region Private members & properties

    private Dictionary<int, TextureAtlas>           atlases;
    private Dictionary<int, KeyValuePair<int, int>> textureAtlasMap;
    private int                                     atlasSize = 512;
	
	private float									lastTimeTextureWasApplied = 0.0f;
	private float									applyDelay = 1.0f;

    #endregion

    #region MonoBehaviour implementation

    private void Start()
    {
    }

    private void Update()
    {
		/*
		if (lastTimeTextureWasApplied == 0.0f
			|| (lastTimeTextureWasApplied - Time.time) > applyDelay)
		{
			foreach (KeyValuePair<int, TextureAtlas> entry in atlases)
			{
				if (entry.Value.IsDirty)
				{
					entry.Value.Apply();
					lastTimeTextureWasApplied = Time.time;
					break ;
				}
			}
		}
		*/
    }

    private void OnDestroy()
    {
        instance = null;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Creates a new square texture atlas.
    /// </summary>
    /// <param name="size">The size of one side of the square texture.</param>
    /// <returns></returns>
    private int CreateAtlas()
    {
        TextureAtlas atlas = new TextureAtlas(atlasSize);

        int newIndex = 0;
        while (atlases.ContainsKey(newIndex))
        {
            ++newIndex;
        }

        atlases.Add(newIndex, atlas);

        return newIndex;
    }

    #endregion

    #region Public methods

    public int AddTexture(Texture2D texture)
    {
        // sort the atlases by lowest occupancy rate
        List<KeyValuePair<int, TextureAtlas>> list = new List<KeyValuePair<int, TextureAtlas>>();
        foreach (KeyValuePair<int, TextureAtlas> entry in atlases)
        {
            list.Add(entry);
        }
        list.Sort((firstPair, nextPair) =>
        {
            return Mathf.RoundToInt(firstPair.Value.Occupancy() - nextPair.Value.Occupancy());
        });

        // add the texture to the first atlas that can contain it
        int atlasId = -1;
        int textureId = -1;
        foreach (KeyValuePair<int, TextureAtlas> entry in list)
        {
            textureId = entry.Value.AddTexture(texture);
            if (textureId != -1)
            {
                atlasId = entry.Key;
                break;
            }
        }

        // else create a new atlas
        if (textureId == -1)
        {
            atlasId = CreateAtlas();
            textureId = atlases[atlasId].AddTexture(texture);
        }

        // add the texture to the map
        int newId = 0;
        while (textureAtlasMap.ContainsKey(newId))
        {
            ++newId;
        }
        textureAtlasMap.Add(newId, new KeyValuePair<int, int>(atlasId, textureId));

        Debug.Log("DEBUG: new texture: " + textureId + " in atlas: " + atlasId);
         
        return newId;
    }

    public void RemoveTexture(int textureId)
    {
        Debug.Log("DEBUG: TextureAtlasManager.RemoveTexture: textureId: " + textureId);
		if (textureAtlasMap.ContainsKey(textureId) == false)
			return ;
        KeyValuePair<int, int> entry = textureAtlasMap[textureId];
        atlases[entry.Key].RemoveTexture(entry.Value);
        textureAtlasMap.Remove(textureId);
    }

    public TextureAtlas.TextureInfo GetTextureInfo(int textureId)
    {
        KeyValuePair<int, int> entry = textureAtlasMap[textureId];
        return atlases[entry.Key].GetTextureInfo(entry.Value);
    }

    #endregion
}


}