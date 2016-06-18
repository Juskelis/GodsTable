using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;

namespace FloodFill
{
    public static class FloodFiller
    {
        private enum Direction { up,down,left,right }

        public delegate void InnerAction<G,S>(ref G[,] _grid, S[,] _spaces, int _x, int _y);

        //spaces sets false for all cells that cannot be filled
        //assumes square grid with outer edges filled in already
        //basically spirals inward like a snek
        public static void Fill(ref bool[,] grid, bool[,] spaces)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            int min_x = 0;
            int max_x = width;
            int min_y = 0;
            int max_y = height;

            while (min_x < max_x && min_y < max_y)
            {
                for (int y = min_y; y < max_y; y++)
                {
                    for (int x = min_x; x < max_x; x++)
                    {
                        //this is an eyesore and I'm so sorry
                        if (spaces[x, y]
                            && (
                                (x > 0 && grid[x - 1, y]) ||
                                (y > 0 && grid[x, y - 1]) ||
                                (x < width && grid[x + 1, y]) ||
                                (y < height && grid[x, y + 1]))
                               )
                        {
                            grid[x, y] = true;
                        }
                    }
                }

                for (int y = max_y-1; y >= min_y; y--)
                {
                    for (int x = max_x-1; x >= min_x; x--)
                    {
                        //this is an eyesore and I'm so sorry
                        if (spaces[x, y]
                            && (
                                (x > 0 && grid[x - 1, y]) ||
                                (y > 0 && grid[x, y - 1]) ||
                                (x < width && grid[x + 1, y]) ||
                                (y < height && grid[x, y + 1]))
                               )
                        {
                            grid[x, y] = true;
                        }
                    }
                }

                min_x++;
                max_x--;

                min_y++;
                max_y--;
            }
        }

        //generic version that lets me change the visit logic
        public static void Fill<G,S>(ref G[,] grid, S[,] spaces, InnerAction<G,S> visitAction)
        {
            InnerAction<G,S> myAction = visitAction;

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            int min_x = 0;
            int max_x = width;
            int min_y = 0;
            int max_y = height;

            while (min_x < max_x)
            {
                for (int y = min_y; y < max_y; y++)
                {
                    for (int x = min_x; x < max_x; x++)
                    {
                        myAction(ref grid, spaces, x, y);
                    }
                }

                for (int y = max_y - 1; y >= min_y; y--)
                {
                    for (int x = max_x - 1; x >= min_x; x--)
                    {
                        myAction(ref grid, spaces, x, y);
                    }
                }

                min_x++;
                max_x--;

                min_y++;
                max_y--;
            }
        }

        //generic version that's WAAAAAY slower
        public static void Fill(bool[,] grid, bool[,] spaces, Pair<int,int> start)
        {
            Queue<Pair<int, int>> nextLocations = new Queue<Pair<int, int>>();
            nextLocations.Enqueue(new Pair<int, int>(start.x, start.y));

            grid[start.x, start.y] = true;

            while (nextLocations.Count > 0)
            {
                var current = nextLocations.Dequeue();
                var x = current.x;
                var y = current.y;

                if (x > 0 && spaces[x - 1, y] && !grid[x - 1, y])
                {
                    grid[x - 1, y] = true;
                    nextLocations.Enqueue(new Pair<int, int>(x - 1, y));
                }
                if (x < grid.GetLength(0) - 2 && spaces[x + 1, y] && !grid[x + 1, y])
                {
                    grid[x + 1, y] = true;
                    nextLocations.Enqueue(new Pair<int, int>(x + 1, y));
                }
                if (y > 0 && spaces[x, y - 1] && !grid[x, y - 1])
                {
                    grid[x, y - 1] = true;
                    nextLocations.Enqueue(new Pair<int, int>(x, y - 1));
                }
                if (y < grid.GetLength(1) - 2 && spaces[x, y + 1] && !grid[x, y + 1])
                {
                    grid[x, y + 1] = true;
                    nextLocations.Enqueue(new Pair<int, int>(x, y + 1));
                }
            }
        }
    }

    public class Pair<X, Y>
    {
        private X _x;
        public X x { get { return _x; } }
        private Y _y;
        public Y y { get { return _y; } }

        public Pair(X first, Y second)
        {
            _x = first;
            _y = second;
        }

        public X first { get { return _x; } }

        public Y second { get { return _y; } }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj == this)
                return true;
            Pair<X, Y> other = obj as Pair<X, Y>;
            if (other == null)
                return false;

            return
                (((first == null) && (other.first == null))
                    || ((first != null) && first.Equals(other.first)))
                  &&
                (((second == null) && (other.second == null))
                    || ((second != null) && second.Equals(other.second)));
        }

        public override int GetHashCode()
        {
            int hashcode = 0;
            if (first != null)
                hashcode += first.GetHashCode();
            if (second != null)
                hashcode += second.GetHashCode();

            return hashcode;
        }
    }
}
