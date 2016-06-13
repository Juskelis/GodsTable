using System;
using UnityEngine;
using System.Collections;

public class IslandGenerator : MonoBehaviour
{
    private MeshFilter target;
    private MeshRenderer renderer;

    private MapGenerator mapGen;

    private bool generated = false;

    public bool flatShaded = false;

    void Start()
    {
        target = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();
        mapGen = GetComponent<MapGenerator>();

        Generate();
    }

    private void Generate()
    {
        generated = true;

        mapGen.seed = (int)DateTime.Now.Ticks & 0x0000FFFF;

        mapGen.RequestMapData(Vector2.zero, OnMapData);
    }

    private void OnMapData(MapData data)
    {
        mapGen.RequestMeshData(data, mapGen.LevelOfDetail, OnMeshData);
        renderer.material.mainTexture = TextureGenerator.TextureFromColorMap(
            data.colorMap,
            MapGenerator.mapChunkSize,
            MapGenerator.mapChunkSize
        );
    }

    private void OnMeshData(MeshData data)
    {
        target.mesh = data.CreateMesh();
        if (flatShaded)
        {
            NoShared();
        }

        generated = false;
    }

    void Update()
    {
        if (!generated && Input.GetKey(KeyCode.Space))
        {
            Generate();
        }
    }

    void NoShared()
    {
        // Create the duplicate game object
        Mesh mesh = Instantiate(target.sharedMesh) as Mesh;
        target.sharedMesh = mesh;

        //Process the triangles
        Vector3[] oldVerts = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            vertices[i] = oldVerts[triangles[i]];
            triangles[i] = i;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        target.mesh = mesh;
    }
}
