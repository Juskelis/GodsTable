using UnityEngine;
using System.Collections;

namespace VectorField
{
    public static class VectorFieldGenerator {
        

        //takes the height field and makes it a distance from each of the targets
        public static void UpdateField(ref bool[,] targets, ref float[,] heightMap, int seed = 0)
        {

            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            float noiseWidth = 10f;
            float noiseHeight = noiseWidth;

            float growth = 0.02f;

            System.Random rng = new System.Random(seed);

            //have to split the versions like this because
            //  there may be inconsistencies btwn the two
            float[,] Clock = new float[width, height];
            float[,] CounterClock = new float[width, height];

            int xmin = 1;
            int ymin = 1;
            int xmax = width-1;
            int ymax = height-1;

            while(xmin < xmax && ymin < ymax)
            {
                //clockwise
                for (int y = ymin; y < ymax; y++)
                {
                    for (int x = xmin; x < xmax; x++)
                    {
                        if (targets[x, y])
                        {
                            Clock[x, y] = 0;
                        }
                        else
                        {
                            float sum = Clock[x - 1, y];
                            sum += Clock[x + 1, y];
                            sum += Clock[x, y - 1];
                            sum += Clock[x, y + 1];
                            Clock[x, y] = sum / 4f;
                            float noise = Mathf.PerlinNoise(x / noiseWidth, y / noiseHeight);
                            Clock[x, y] += growth;
                        }
                    }
                }

                // counter-clockwise
                for (int y = ymin; y < ymax; y++)
                {
                    for (int x = xmin; x < xmax; x++)
                    {
                        if (targets[x, y])
                        {
                            CounterClock[x, y] = 0;
                        }
                        else
                        {
                            float sum = CounterClock[x - 1, y];
                            sum += CounterClock[x + 1, y];
                            sum += CounterClock[x, y - 1];
                            sum += CounterClock[x, y + 1];
                            CounterClock[x, y] = sum / 4f;
                            CounterClock[x, y] += growth;
                        }
                    }
                }

                xmin++;
                ymin++;
                xmax--;
                ymax--;
            }

            
            //combine results
            for(int j = 0; j < height; j++)
            {
                for(int i = 0; i < width; i++)
                {
                    heightMap[i, j] *= (Clock[i, j] + CounterClock[i,j])/2f;
                }
            }
        }
    }
}