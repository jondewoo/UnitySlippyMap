// 
//  TextureAtlas.cs
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

public class TextureAtlas
{
    public class TextureInfo
    {
        private Rect        rect;
        public Rect         Rect { get { return rect; } }
        private Texture2D   texture;
        public Texture2D    Texture { get { return texture; } }

        public TextureInfo(Rect rect, Texture2D texture)
        {
            this.rect = rect;
            this.texture = texture;
        }
    }

    #region Private members & properties

    private int                         size;
    private Texture2D                   texture;
    private MaxRectsBinPack             pack;
    private Dictionary<int, Rect>       rects;

    #endregion

    #region Private methods
    #endregion

    #region Public methods

    public TextureAtlas(int size, string name = null)
    {
        this.size = size;
        texture = new Texture2D(size, size);
        if (name != null)
            texture.name = name;
        else
            texture.name = Guid.NewGuid().ToString();
        pack = new MaxRectsBinPack(size, size, false);
        rects = new Dictionary<int, Rect>();
        Debug.Log("DEBUG: new atlas: " + size);
    }

    /// <summary>
    /// Defragment the atlas.
    /// </summary>
    public void Defragment()
    {
    }

    public float Occupancy()
    {
        return pack.Occupancy();
    }

    /// <summary>
    /// Add a texture to the atlas.
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <returns>A unique id for the texture.</returns>
    public int AddTexture(Texture2D texture)
    {
        Rect rect = pack.Insert(texture.width, texture.height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
        if (rect == new Rect())
            return -1;

        int newIndex = 0;
        while (rects.ContainsKey(newIndex))
        {
            ++newIndex;
        }

        rects.Add(newIndex, rect);

        this.texture.SetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height), texture.GetPixels());
        this.texture.Apply();

        Debug.Log("DEBUG: " + this.texture.width + " " + this.texture.height);
        Debug.Log("DEBUG: new rect in atlas: " + Mathf.RoundToInt(rect.x) + " " + Mathf.RoundToInt(rect.y) + " " + Mathf.RoundToInt(rect.width) + " " + Mathf.RoundToInt(rect.height) + " size: " + (rect.width * rect.height) + " texture size: " + texture.GetPixels().Length);

        return newIndex;
    }

    /// <summary>
    /// Removes a texture from the atlas.
    /// </summary>
    /// <param name="id">The unique of the texture to remove.</param>
    public void RemoveTexture(int id)
    {
        pack.Remove(rects[id]);

        Debug.Log("DEBUG: removed rect in atlas: " + rects[id]);

        rects.Remove(id);
    }

    public TextureInfo GetTextureInfo(int id)
    {
        return new TextureInfo(rects[id], texture);
    }

    #endregion
}
