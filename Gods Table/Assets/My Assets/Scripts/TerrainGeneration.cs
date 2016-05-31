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
    private float edgeSpacing;

    private List<Triangle> meshTris;
    private List<Triangle> delaunayTris;
    private List<Edge> voronoiEdges;
    private List<Vector2> points;

    private Mesh mesh;
    private MeshFilter filter;
    
    void Start()
    {
        edgeSpacing = minimumPointDistance / 2;
        skinWidth = edgeSpacing/2;
        meshTris = new List<Triangle>();

        filter = GetComponent<MeshFilter>();

        mesh = new Mesh();

        filter.mesh = mesh;
        mesh.name = transform.name;

        PoissonDiscGenerator gen = new PoissonDiscGenerator(width - skinWidth * 2, height - skinWidth * 2, minimumPointDistance, maximumAttempts);

        points = gen.BlockedGeneratePoints2D();
        Vector2 temp;
        for (int i = 0; i < points.Count; i++)
        {
            temp = points[i];
            temp.x += skinWidth - width/2;
            temp.y += skinWidth - height/2;
            points[i] = temp;
        }
        delaunayTris = DelaunayTriangulator.Triangulate(points);

        voronoiEdges = VoronoiGenerator.Generate(delaunayTris);

        GenerateMesh();
    }

    /*
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        if (delaunayTris != null)
        {
            foreach (var tri in delaunayTris)
            {
                Gizmos.DrawLine(tri.Vertex1, tri.Vertex2);
                Gizmos.DrawLine(tri.Vertex2, tri.Vertex3);
                Gizmos.DrawLine(tri.Vertex3, tri.Vertex1);

                Gizmos.DrawSphere(tri.center, 0.01f);
            }
        }

        Gizmos.color = Color.green;
        if (points != null)
        {
            foreach (var point in points)
            {
                Gizmos.DrawSphere(point, 0.05f);
            }
        }

        Gizmos.color = Color.blue;
        if (voronoiEdges != null)
        {
            foreach (var edge in voronoiEdges)
            {
                Gizmos.DrawLine(edge.StartPoint, edge.EndPoint);
            }
        }
    }
    */
    bool WithinBounds(Vector2 point)
    {
        if (point.x > width/2 - skinWidth) return false;
        if (point.x < skinWidth - width/2) return false;
        if (point.y > height/2 - skinWidth) return false;
        if (point.y < skinWidth - height/2) return false;
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
        
        int verticalCount = (int)(height/edgeSpacing);
        int horizontalCount = (int)(width/edgeSpacing);

        //weird bounds on this because end points are the corners
        //  which we already added
        for (int i = 1; i < horizontalCount; i++)
        {
            points.Add(new Vector2(-width/2 + edgeSpacing*i, -height/2));
            points.Add(new Vector2(-width/2 + edgeSpacing*i, height/2));
        }
        for (int i = 1; i < verticalCount; i++)
        {
            points.Add(new Vector2(-width/2, -height/2 + edgeSpacing*i));
            points.Add(new Vector2(width/2, -height/2 + edgeSpacing*i));
        }
        #endregion

        //make a Delaunay Triangulation of all the new points
        meshTris = DelaunayTriangulator.Triangulate(points);

        //generate mesh out of triangles
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for(int i = 0; i < meshTris.Count; i++)
        {
            verts.Add(new Vector3(meshTris[i].Vertex1.x, 0, meshTris[i].Vertex1.y));
            verts.Add(new Vector3(meshTris[i].Vertex2.x, 0, meshTris[i].Vertex2.y));
            verts.Add(new Vector3(meshTris[i].Vertex3.x, 0, meshTris[i].Vertex3.y));

            tris.Add(i * 3 + 2);
            tris.Add(i * 3 + 1);
            tris.Add(i * 3);
        }

        //finalize mesh
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
