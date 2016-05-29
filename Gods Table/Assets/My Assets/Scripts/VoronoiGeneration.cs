using UnityEngine;
using System.Collections;
using DelaunayTriangulation;
using System.Collections.Generic;

namespace VoronoiGeneration
{
    public class VoronoiGenerator
    {
        public static List<Edge> Generate(List<Triangle> delaunayTriangles)
        {
            List<Edge> edges = new List<Edge>();

            for (int i = 0; i < delaunayTriangles.Count; i++)
            {
                for (int j = i + 1; j < delaunayTriangles.Count; j++)
                {
                    if (delaunayTriangles[i].SharesEdgeWidth(delaunayTriangles[j]))
                    {
                        //make edge between centers
                        Edge toAdd = new Edge(delaunayTriangles[i].circumcenter, delaunayTriangles[j].circumcenter);
                        edges.Add(toAdd);
                    }
                }
            }

            return edges;
        }
    }
}