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

using System.Diagnostics;

public class TextureAtlas
{
    public class TextureInfo
    {
        private Rect        rect;
        public Rect         Rect { get { return rect; } }
        private Texture2D   texture;
		public Texture2D 	Texture { get { return texture; } }

        public TextureInfo(Rect rect, Texture2D texture)
        {
            this.rect = rect;
            this.texture = texture;
        }
    }

    #region Private members & properties

    private Texture2D                   texture;
	//public Texture2D 					Texture { get { return texture; } }
    private MaxRectsBinPack             pack;
    private Dictionary<int, Rect>       rects;
	private bool						isDirty = false;
	public bool							IsDirty { get { return isDirty; } }

    #endregion

    #region Private methods
    #endregion

    #region Public methods

    public TextureAtlas(int size, string name = null)
    {
        texture = new Texture2D(size, size);
        if (name != null)
            texture.name = name;
        else
            texture.name = Guid.NewGuid().ToString();
        pack = new MaxRectsBinPack(size, size, false);
        rects = new Dictionary<int, Rect>();
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
	
	public void Apply()
	{
		isDirty = false;
		Stopwatch watch = new Stopwatch();
		watch.Start();
		texture.Apply();
		watch.Stop();
		
		TimeSpan ts = watch.Elapsed;
        UnityEngine.Debug.Log(String.Format("DEBUG: applied in: {0:00}:{1:00}:{2:00}.{3:00}", 
                    ts.Hours, ts.Minutes, ts.Seconds, 
                    ts.Milliseconds/10));
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
		
		int x = Mathf.RoundToInt(rect.x);
		int y = Mathf.RoundToInt(rect.y);
		int width = Mathf.RoundToInt(rect.width);
		int height = Mathf.RoundToInt(rect.height);
		
		Stopwatch watch = new Stopwatch();
			
		watch.Start();
		this.texture.SetPixels(x, y, width, height, texture.GetPixels());
		this.texture.Apply();
		//this.isDirty = true;
		watch.Stop();
		
		TimeSpan ts = watch.Elapsed;
        UnityEngine.Debug.Log(String.Format("DEBUG: set pixel done in: {0:00}:{1:00}:{2:00}.{3:00}", 
                    ts.Hours, ts.Minutes, ts.Seconds, 
                    ts.Milliseconds/10));

		/*
		UnityThreadHelper.TaskDistributor.Dispatch(() => {
			Stopwatch watch = new Stopwatch();
			
			watch.Start();
			
			// essayer d'écrire block par block en parallèle avec des synchros
			int blockSize = 128;
			int length = (width - 1) * (height - 1);
			for (int i = 0; i < length; i += blockSize)
			{
				UnityThreadHelper.Dispatcher.Dispatch(() => {
					//UnityEngine.Debug.Log("DEBUG: name: " + texture.name + " x: " + (i % texture.width) + " y: " + Mathf.RoundToInt((float)i / (float)texture.width) + " (" + i + "/" + texture.width + ") block size: " + blockSize + " length: " + length);
	        		this.texture.SetPixels(x + (i % width), y + Mathf.RoundToInt((float)i / (float)width), blockSize, 1, texture.GetPixels(i % texture.width, Mathf.RoundToInt((float)i / (float)texture.width), blockSize, 1));
				});
			}
			this.isDirty = true;
			
			watch.Stop();
			
			TimeSpan ts = watch.Elapsed;
	        UnityEngine.Debug.Log(String.Format("DEBUG: set pixel done in: {0:00}:{1:00}:{2:00}.{3:00}", 
	                    ts.Hours, ts.Minutes, ts.Seconds, 
	                    ts.Milliseconds/10));
		});
		*/
		
		/*
		watch.Reset();
		watch.Start();

		this.texture.Apply();
		
		watch.Stop();
		
		ts = watch.Elapsed;
        UnityEngine.Debug.Log(String.Format("DEBUG: applied in: {0:00}:{1:00}:{2:00}.{3:00}", 
                    ts.Hours, ts.Minutes, ts.Seconds, 
                    ts.Milliseconds/10));
                    */

        return newIndex;
    }

    /// <summary>
    /// Removes a texture from the atlas.
    /// </summary>
    /// <param name="id">The unique of the texture to remove.</param>
    public void RemoveTexture(int id)
    {
        pack.Remove(rects[id]);
        rects.Remove(id);
    }

    public TextureInfo GetTextureInfo(int id)
    {
        return new TextureInfo(rects[id], texture);
    }

    #endregion
}
