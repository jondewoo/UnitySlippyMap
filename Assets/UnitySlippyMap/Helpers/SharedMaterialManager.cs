// 
//  TileDownloader.cs
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