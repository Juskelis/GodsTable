using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DelaunayTriangulation;
using PoissonDiscGeneration;
using VoronoiGeneration;

[RequireComponent(typeof(MeshFilter))]

public class TerrainGeneration : MonoBehaviour
{
    [SerializeField]
    private float width;
    [SerializeField]
    private float height;

    [SerializeField]
    private float minimumPointDistance;
    [SerializeField]
    private int maximumAttempts;

    private float skinWidth;

    private List<Triangle> meshTris;
    private List<Triangle> delaunayTris;
    private List<Edge> voronoiEdges;
    private List<Vector2> points;

    private Mesh mesh;
    private MeshFilter filter;
    
    void Start()
    {
        skinWidth = minimumPointDistance / 2;
        meshTris = new List<Triangle>();

        filter = GetComponent<MeshFilter>();

        mesh = new Mesh();

        filter.mesh = mesh;
        mesh.name = transform.name;

        PoissonDiscGenerator gen = new PoissonDiscGenerator(width - skinWidth * 2, height - skinWidth * 2, minimumPointDistance, maximumAttempts);
        //points = gen.BlockedGeneratePoints2D();
        Vector2 extends = new Vector2(width, height);
        foreach(Vector2 v in gen.IterativeGeneratePoints2D())
        {
            //shift the points in by skinWidth and center it
            points.Add((v + Vector2.one * skinWidth) - extends);
        }

        delaunayTris = DelaunayTriangulator.Triangulate(points);

        voronoiEdges = VoronoiGenerator.Generate(delaunayTris);

        GenerateMesh();
    }

    bool WithinBounds(Vector2 point)
    {
        if (point.x > width/2 - skinWidth) return false;
        if (point.x < skinWidth - width/2) return false;
        if (point.y > height/2 - skinWidth) return false;
        if (point.y > skinWidth - height/2) return false;
        return true;
    }

    void GenerateMesh()
    {
        //add voronoi vertices as points if they are within the skinWidth
        for(int i = 0; i < voronoiEdges.Count; i++)
        {
            if(WithinBounds(voronoiEdges[i].StartPoint))
                points.Add(voronoiEdges[i].StartPoint);
            if(WithinBounds(voronoiEdges[i].EndPoint))
                points.Add(voronoiEdges[i].EndPoint);
        }

        #region Adding Edge Points
        // corners
        points.Add(new Vector2(-width / 2, -height / 2));
        points.Add(new Vector2(width / 2, -height / 2));
        points.Add(new Vector2(-width / 2, height / 2));
        points.Add(new Vector2(width / 2, height / 2));
        
        // TODO : add edges
        #endregion

        //make a Delaunay Triangulation of all the new points
        meshTris = DelaunayTriangulator.Triangulate(points);

        //generate mesh out of triangles
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for(int i = 0; i < meshTris.Count; i++)
        {
            verts.Add(meshTris[i].Vertex1);
            verts.Add(meshTris[i].Vertex2);
            verts.Add(meshTris[i].Vertex3);

            tris.Add(i * 3);
            tris.Add(i * 3 + 1);
            tris.Add(i * 3 + 2);
        }

        //finalize mesh
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
