using UnityEngine;
using System.Collections;
using PoissonDiscGeneration;
using System.Collections.Generic;
using DelaunayTriangulation;
using VoronoiGeneration;
using UnityEditor;

public class TestingScript : MonoBehaviour
{
    [SerializeField]
    private bool drawPoints = true;
    [SerializeField]
    private bool drawTris = true;
    [SerializeField]
    private bool drawVoronoi = true;


    [SerializeField]
    private float width = 10;
    [SerializeField]
    private float height = 10;
    [SerializeField]
    private float minimumPointDistance = 0.25f;
    [SerializeField]
    private int maximumAttempts = 30;

    private PoissonDiscGenerator gen;
    private List<Vector2> points;
    private List<Triangle> tris;
    private List<Edge> edges;

	// Use this for initialization
	void Start ()
	{
	    gen = new PoissonDiscGenerator(width, height, minimumPointDistance, maximumAttempts);
	    points = gen.BlockedGeneratePoints2D();
	    tris = DelaunayTriangulator.Triangulate(points);
	    edges = VoronoiGenerator.Generate(tris);

        print("points: " + points.Count);
        print("tris: " + tris.Count);
        print("edge: " + edges.Count);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        if (drawTris && tris != null)
        {
            foreach (var tri in tris)
            {
                Gizmos.DrawLine(tri.Vertex1, tri.Vertex2);
                Gizmos.DrawLine(tri.Vertex2, tri.Vertex3);
                Gizmos.DrawLine(tri.Vertex3, tri.Vertex1);

                Gizmos.DrawSphere(tri.center, 0.01f);
            }
        }

        Gizmos.color = Color.green;
        if (drawPoints && points != null)
        {
            foreach (var point in points)
            {
                Gizmos.DrawSphere(point, 0.05f);
            }
        }

        Gizmos.color = Color.blue;
        if (drawVoronoi && edges != null)
        {
            foreach (var edge in edges)
            {
                Gizmos.DrawLine(edge.StartPoint, edge.EndPoint);
            }
        }
    }
}
