using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography.X509Certificates;

public static class HeightCalculator
{
    public enum NormalizeMode { Local, Global }

    private static int SYSTEM_RANDOM_RANGE = 100000;

    public static float[,] GenerateNoiseGrid(int width, int height, int seed, float noiseScale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        noiseScale = (noiseScale <= 0) ? 1 : noiseScale;

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float xOff = prng.Next(-SYSTEM_RANDOM_RANGE, SYSTEM_RANDOM_RANGE) + offset.x;
            float yOff = prng.Next(-SYSTEM_RANDOM_RANGE, SYSTEM_RANDOM_RANGE) - offset.y;
            octaveOffsets[i] = new Vector2(xOff, yOff);

            maxPossibleHeight += amplitude;

            amplitude *= persistance;

        }

        float[,] noiseGrid = new float[width,height];
        
        float minLocalNoiseHeight = float.MaxValue;
        float maxLocalNoiseHeight = float.MinValue;

        float halfWidth = width / 2f;
        float halfheight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1;
                frequency = 1;

                float noiseSum = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x- halfWidth + octaveOffsets[i].x) /noiseScale * frequency;
                    float sampleY = (y- halfheight + octaveOffsets[i].y) /noiseScale * frequency;

                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY)*2 - 1;
                    noiseSum += noiseValue*amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                minLocalNoiseHeight = Mathf.Min(minLocalNoiseHeight, noiseSum);
                maxLocalNoiseHeight = Mathf.Max(maxLocalNoiseHeight, noiseSum);

                noiseGrid[x, y] = noiseSum;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseGrid[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseGrid[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseGrid[x, y] + 1)/(maxPossibleHeight);
                    noiseGrid[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);

                }
            }
        }


        return noiseGrid;
    }

    // returns 0..1 value
    public static float GetValue(Vector2 location)
    {
        return Mathf.PerlinNoise(location.x, location.y);
    }
}
