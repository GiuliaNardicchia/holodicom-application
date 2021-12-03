using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditor : MonoBehaviour
{

    //MeshRenderer meshRenderer;
    //public Texture albedoTexture;

    /*Vector3[] vertices;
    int[] triangles;

    public int xSize = 20;
    public int zSize = 20;

    public int textureWidth = 1024;
    public int textureHeight = 1024;

    public Gradient gradient;

    //servono per normalizzare l'altezza (i valori del gradiente vanno da 0 a 1)
    float minHeigth;
    float maxHeigth;*/

    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.RecalculateNormals();

        //meshRenderer = GetComponent<MeshRenderer>();
        //meshRenderer.material.color = Color.gray;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
