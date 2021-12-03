using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class GltfFileLoader : MeshLoader
{
    public override async Task<GameObject> ObjectLoader(string assetPath)
    {
        using var stream = File.Open(assetPath, FileMode.Open);
        var glbData = new byte[stream.Length];
        await stream.ReadAsync(glbData, 0, (int)stream.Length).ConfigureAwait(false);
        var glbAsset = GltfUtility.GetGltfObjectFromGlb(glbData);
        var gameObject = await glbAsset.ConstructAsync().ConfigureAwait(false);
        return gameObject;
        //var exists = File.Exists(assetPath);
        //var asset = await GltfUtility.ImportGltfObjectFromPathAsync(assetPath);
        //return asset.GameObjectReference;
    }
}
