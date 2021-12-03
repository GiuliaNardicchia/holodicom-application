using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using Microsoft.MixedReality.Toolkit.Utilities;

public abstract class MeshLoader : MonoBehaviour
{
    public abstract Task<GameObject> ObjectLoader(string assetPath);

    public async Task<MeshFilter> LoadGltfModelAsync(string assetPath)
    {
        var gameObject = await ObjectLoader(assetPath);
        var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
        Destroy(gameObject);
        return meshFilter;
    }
}
