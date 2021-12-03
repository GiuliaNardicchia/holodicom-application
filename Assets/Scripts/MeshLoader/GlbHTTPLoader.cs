using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GlbHTTPLoader : MeshLoader
{
    public override async Task<GameObject> ObjectLoader(string assetPath)
    {
        var response = await Rest.GetAsync(assetPath, readResponseData: true);
        var glbAsset = GltfUtility.GetGltfObjectFromGlb(response.ResponseData);
        var gameObject = await glbAsset.ConstructAsync();
        return gameObject;
    }
}
