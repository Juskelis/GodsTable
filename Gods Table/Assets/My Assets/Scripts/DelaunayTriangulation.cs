﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace DelaunayTriangulation
{
    public class DelaunayTriangulator
    {
        public static List<Triangle> Triangulate(List<Vector2> points)
        {
            if(points.Count < 3) throw new ArgumentException("Can not triangulate less than three vertices");

            List<Triangle> triangles = new List<Triangle>();

            Triangle superTriangle = SuperTriangle(points);
            triangles.Add(superTriangle);

            for (int i = 0; i < points.Count; i++)
            {
                List<Edge> edgeBuffer= new List<Edge>();

                for (int j = triangles.Count - 1; j >= 0; j--)
                {
                    Triangle t = triangles[j];
                    if (t.ContainsInCircumcircle(points[i]) > 0)
                    {
                        edgeBuffer.Add(new Edge(t.Vertex1, t.Vertex2));
                        edgeBuffer.Add(new Edge(t.Vertex2, t.Vertex3));
                        edgeBuffer.Add(new Edge(t.Vertex3, t.Vertex1));
                        triangles.RemoveAt(j);
                    }
                }

                for (int j = edgeBuffer.Count - 2; j >= 0; j--)
                {
                    for (int k = edgeBuffer.Count - 1; k >= j+1; k--)
                    {
                        if (edgeBuffer[j] == edgeBuffer[k])
                        {
                            edgeBuffer.RemoveAt(k);
                            edgeBuffer.RemoveAt(j);
                            k--;
                        }
                    }
                }

                for (int j = 0; j < edgeBuffer.Count; j++)
                {
                    triangles.Add(new Triangle(edgeBuffer[j].StartPoint, edgeBuffer[j].EndPoint, points[i]));
                }
            }

            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                if (triangles[i].SharesVertexWith(superTriangle)) triangles.RemoveAt(i);
            }

            return triangles;
        }

        public static Triangle SuperTriangle(List<Vector2> points)
        {
            float maxBound = points[0].x;

            for (int i = 0; i < points.Count; i++)
            {
                float xAbs = Mathf.Abs(points[i].x);
                float yAbs = Mathf.Abs(points[i].y);

                if (xAbs > maxBound) maxBound = xAbs;
                if (yAbs > maxBound) maxBound = yAbs;
            }

            Vector2 p1 = new Vector2(10*maxBound, 0);
            Vector2 p2 = new Vector2(0, 10*maxBound);
            Vector2 p3 = new Vector2(-10*maxBound, -10*maxBound);

            return new Triangle(p1,p2,p3);
        }
    }

    public class Triangle
    {
        public Vector2 Vertex1;
        public Vector2 Vertex2;
        public Vector2 Vertex3;
        public Vector2 center;
        public Vector2 circumcenter;

        public Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;

            center = CalculateCenter();
            circumcenter = CalculateCircumcenter();
        }

        public Vector2 CalculateCenter()
        {
            return (Vertex1 + Vertex2 + Vertex3) / 3f;
        }

        public Vector2 CalculateCircumcenter()
        {
            float a = Vertex2.x - Vertex1.x;
            float b = Vertex2.y - Vertex1.y;
            float c = Vertex3.x - Vertex1.x;
            float d = Vertex3.y - Vertex1.y;

            float e = a*(Vertex1.x + Vertex2.x) + b*(Vertex1.y + Vertex2.y);
            float f = c*(Vertex1.x + Vertex3.x) + d*(Vertex1.y + Vertex3.y);
            float g = 2.0f*(a*(Vertex3.y - Vertex2.y) - b*(Vertex3.x - Vertex2.x));
            if(Mathf.Abs(g) < float.Epsilon) return new Vector2(Mathf.Infinity, Mathf.Infinity);
            return new Vector2((d*e - b*f)/g, (a*f - c*e)/g);
        }

        // ret > 0 == inside
        // ret = 0 == on
        // ret < 0 == outside
        public float ContainsInCircumcircle(Vector2 point)
        {
            float ax = Vertex1.x - point.x;
            float ay = Vertex1.y - point.y;
            float bx = Vertex2.x - point.x;
            float by = Vertex2.y - point.y;
            float cx = Vertex3.x - point.x;
            float cy = Vertex3.y - point.y;

            float det_ab = ax * by - bx * ay;
            float det_bc = bx * cy - cx * by;
            float det_ca = cx * ay - ax * cy;

            float a_squared = ax * ax + ay * ay;
            float b_squared = bx * bx + by * by;
            float c_squared = cx * cx + cy * cy;

            return a_squared * det_bc + b_squared * det_ca + c_squared * det_ab;
        }

        public int NumberOfSharedVertices(Triangle triangle)
        {
            int ret = 0;

            if (Vertex1 == triangle.Vertex1) ret++;
            if (Vertex1 == triangle.Vertex2) ret++;
            if (Vertex1 == triangle.Vertex3) ret++;

            if (Vertex2 == triangle.Vertex1) ret++;
            if (Vertex2 == triangle.Vertex2) ret++;
            if (Vertex2 == triangle.Vertex3) ret++;

            if (Vertex3 == triangle.Vertex1) ret++;
            if (Vertex3 == triangle.Vertex2) ret++;
            if (Vertex3 == triangle.Vertex3) ret++;

            return ret;
        }

        public bool SharesVertexWith(Triangle triangle)
        {
            return NumberOfSharedVertices(triangle) > 0;
        }

        public bool SharesEdgeWidth(Triangle triangle)
        {
            return NumberOfSharedVertices(triangle) > 1;
        }

        #region Operators
        public override int GetHashCode()
        {
            return Vertex1.GetHashCode() ^ Vertex2.GetHashCode() ^ Vertex3.GetHashCode() ^ center.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (Triangle)obj;
        }

        public static bool operator ==(Triangle left, Triangle right)
        {
            if ((object)left == (object)right) return true;

            if ((object)left == null || (object)right == null) return false;

            return left.NumberOfSharedVertices(right) >= 3 && left.center == right.center;
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }
        #endregion
    }

    public class Edge
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;

        public Edge(Vector2 start, Vector2 end)
        {
            StartPoint = start;
            EndPoint = end;
        }

        #region Operators
        public override int GetHashCode()
        {
            return StartPoint.GetHashCode() ^ EndPoint.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (Edge) obj;
        }

        public static bool operator ==(Edge left, Edge right)
        {
            if ((object) left == (object) right) return true;

            if ((object) left == null || (object) right == null) return false;

            return (left.StartPoint == right.StartPoint && left.EndPoint == right.EndPoint)
                   || (left.StartPoint == right.EndPoint && left.EndPoint == right.StartPoint);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !(left == right);
        }
        #endregion
    }
}
