using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AssetLoader : MeshLoader
{
    public GameObject mesh;

    public override Task<GameObject> ObjectLoader(string assetPath)
    {
        return Task.FromResult(mesh);
    }
}
