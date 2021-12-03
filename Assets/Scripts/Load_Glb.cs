using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System.IO;

public class Load_Glb : MonoBehaviour
{
    public MeshLoader loader;
    async void Start()
    {
        var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", "kidney.glb");
        var modelUri = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BoomBox/glTF-Binary/BoomBox.glb";
        await loader.ObjectLoader(modelUri);
    }

}
