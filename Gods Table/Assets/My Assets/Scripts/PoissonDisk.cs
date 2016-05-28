using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace PoissonDiscGeneration
{

    public class PoissonDiscGenerator
    {
        private struct GridPos
        {
            public int x;
            public int y;

            public GridPos(Vector2 sample, float cellSize)
            {
                x = (int)(sample.x / cellSize);
                y = (int)(sample.y / cellSize);
            }
        }

        private float w;
        private float h;
        private float r;
        private float r2;
        private int a;

        private float cellSize;

        private List<Vector2> active2D;
        private Vector2[,] grid2D;

        public PoissonDiscGenerator(float width, float height, float radius, int maxAttempts)
        {
            w = width;
            h = height;
            r = radius;
            r2 = r*r;
            a = maxAttempts;

            cellSize = r/Mathf.Sqrt(2);

            grid2D = new Vector2[Mathf.CeilToInt(w/cellSize),Mathf.CeilToInt(h/cellSize)];
            active2D = new List<Vector2>();
        }

        public List<Vector2> BlockedGeneratePoints2D()
        {
            List<Vector2> ret = new List<Vector2>();
            foreach (Vector2 v in IterativeGeneratePoints2D())
            {
                ret.Add(v);
            }
            return ret;
        }

        public IEnumerable<Vector2> IterativeGeneratePoints2D()
        {
            yield return AddSample(new Vector2(Random.value*w, Random.value*h));

            while (active2D.Count > 0)
            {
                int i = (int) Random.value*active2D.Count;
                Vector2 sample = active2D[i];

                bool placed = false;
                for (int j = 0; j < a; j++)
                {
                    float angle = 2*Mathf.PI*Random.value;
                    // See: http://stackoverflow.com/questions/9048095/create-random-number-within-an-annulus/9048443#9048443
                    float radius = Mathf.Sqrt(Random.value * 3 * r2 + r2);

                    Vector2 candidate = sample + r*new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    candidate.x = Mathf.Clamp(candidate.x, 0, w);
                    candidate.y = Mathf.Clamp(candidate.y, 0, h);

                    if (IsFarEnough(candidate))
                    {
                        placed = true;
                        yield return AddSample(candidate);
                        break;
                    }
                }

                if (!placed)
                {
                    active2D[i] = active2D[active2D.Count - 1];
                    active2D.RemoveAt(active2D.Count - 1);
                }
            }
        }

        private Vector2 AddSample(Vector2 sample)
        {
            active2D.Add(sample);
            GridPos pos = new GridPos(sample, cellSize);
            grid2D[pos.x, pos.y] = sample;
            return sample;
        }

        private bool IsFarEnough(Vector2 sample)
        {
            GridPos pos = new GridPos(sample, cellSize);

            int xmin = Mathf.Max(pos.x - 2, 0);
            int ymin = Mathf.Max(pos.y - 2, 0);
            int xmax = Mathf.Min(pos.x + 2, grid2D.GetLength(0));
            int ymax = Mathf.Min(pos.y + 2, grid2D.GetLength(1));

            for (int y = ymin; y < ymax; y++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    Vector2 s = grid2D[x, y];
                    if (s != Vector2.zero)
                    {
                        Vector2 d = s - sample;
                        if (d.x*d.x + d.y*d.y < r2) return false;
                    }
                }
            }

            return true;
        }
    }
}
