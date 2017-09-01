// 
//  TileDownloader.cs
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

using UnityEngine;

using System.Collections.Generic;

namespace UnitySlippyMap
{

// <summary>
// A singleton class in charge of managing shared materials.
// </summary>
public class SharedMaterialManager : MonoBehaviour
{
    #region Singleton implementation

    private static SharedMaterialManager instance = null;
    public static SharedMaterialManager Instance
    {
        get
        {
            if (null == (object)instance)
            {
                instance = FindObjectOfType(typeof(SharedMaterialManager)) as SharedMaterialManager;
                if (null == (object)instance)
                {
                    var go = new GameObject("[SharedMaterialManager]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    instance = go.AddComponent<SharedMaterialManager>();
                    instance.EnsureSharedMaterialManager();
                }
            }

            return instance;
        }
    }

    private void EnsureSharedMaterialManager()
    {
        materials = new Dictionary<string, Material>();
    }

    private SharedMaterialManager()
    {
    }

    private void OnApplicationQuit()
    {
        DestroyImmediate(this.gameObject);
    }

    #endregion

    #region Private members & properties

    private Dictionary<string, Material> materials;

    #endregion

    #region MonoBehaviour implementation

    private void Start()
    {
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        instance = null;
    }

    #endregion

    #region Private methods

    #endregion

    #region Public methods

    /// <summary>
    /// Returns a shared material, or create one if it doesn't exist.
    /// </summary>
    /// <param name="name">The name of the material.</param>
    /// <returns>The shared material.</returns>
    public Material GetSharedMaterial(string materialName, string shaderName)
    {
        if (materials.ContainsKey(materialName))
        {
            Debug.Log("DEBUG: use shared material: " + materialName);
            return materials[materialName];
        }

        Debug.Log("DEBUG: create new shared material: " + materialName);

        Material material;
        material = new Material(Shader.Find(shaderName));
        materials[materialName] = material;
        return material;
    }

    public void RemoveSharedMaterial(string materialName)
    {
        Destroy(materials[materialName]);
        materials.Remove(materialName);
    }

    #endregion
}

}