using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using FloodFill;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap, ColorMap, Mesh, FalloffMap
    }

    public enum FalloffMode
    {
        None, Square, Circle
    }

    [SerializeField] private DrawMode drawMode;
    [SerializeField] private HeightCalculator.NormalizeMode normalizeMode;

    public const int mapChunkSize = 241;
    
    [SerializeField]
    [Range(0, 6)]
    private int editorLevelOfDetail;
    public int LevelOfDetail { get { return editorLevelOfDetail; } }

    [SerializeField]
    private float noiseScale;
    [SerializeField]
    private int ocataves;
    [SerializeField]
    [Range(0,1)]
    private float persistance;
    [SerializeField]
    private float lacunarity;


    public int seed;
    [SerializeField]
    private Vector2 offset;

    [SerializeField]
    private float meshHeightMultiplier;
    [SerializeField]
    private AnimationCurve meshHeightCurve;

    [SerializeField]
    private TerrainType[] regions;

    [SerializeField] private FalloffMode falloffMode;
    private float[,] falloffMap;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    [SerializeField]
    private bool autoGenerate = true;
    public bool GenerateOnUpdate { get { return autoGenerate; } }

    void Awake()
    {
        if(falloffMode == FalloffMode.Square)
            falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
        else
            falloffMap = FalloffGenerator.GenerateCircularFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) display.DrawTexture(TextureGenerator.TextureFromNoiseMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColorMap) display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLevelOfDetail),
                TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize)
            );
        else if (drawMode == DrawMode.FalloffMap)
        {
            if(falloffMode == FalloffMode.Square)
                display.DrawTexture(TextureGenerator.TextureFromNoiseMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
            else
                display.DrawTexture(TextureGenerator.TextureFromNoiseMap(FalloffGenerator.GenerateCircularFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, levelOfDetail, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(
            mapData.heightMap,
            meshHeightMultiplier,
            meshHeightCurve,
            lod
        );
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    
    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = HeightCalculator.GenerateNoiseGrid(
            mapChunkSize,
            mapChunkSize,
            seed,
            noiseScale,
            ocataves,
            persistance,
            lacunarity,
            center + offset,
            normalizeMode
        );

        bool[,] waterMap = new bool[mapChunkSize,mapChunkSize];
        bool[,] oceanMap = new bool[mapChunkSize,mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (falloffMode != FalloffMode.None)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                waterMap[x, y] = false;
                oceanMap[x, y] = false;
                if (x == 0 || y == 0 || x == mapChunkSize - 2 || y == mapChunkSize - 2)
                {
                    waterMap[x, y] = true;
                    oceanMap[x, y] = true;
                }

                if (noiseMap[x, y] <= regions[2].startHeight || oceanMap[x,y])
                {
                    waterMap[x, y] = true;
                }
            }
        }

        FloodFiller.Fill(ref oceanMap,waterMap);

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                waterMap[x, y] = waterMap[x,y] && !oceanMap[x,y];
                //waterMap[x, y] = false;
            }
        }

        GenerateRivers(ref waterMap, ref oceanMap, ref noiseMap);

        Color[] colors = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {

                colors[y * mapChunkSize + x] = Color.black;
                if (oceanMap[x, y] && !waterMap[x,y])
                {
                    colors[y * mapChunkSize + x] = regions[0].color;
                }
                else if (waterMap[x, y])
                {
                    colors[y * mapChunkSize + x] = Color.white;// regions[1].color;
                }
                else if(false)
                {
                    float currentHeight = noiseMap[x, y];
                    for (int i = 2; i < regions.Length; i++)
                    {
                        if (currentHeight >= regions[i].startHeight)
                        {
                            colors[y * mapChunkSize + x] = regions[i].color;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        return new MapData(noiseMap, colors);
    }

    private void findMinMax(ref float[,] heightMap, int x, int y, ref int minX, ref int minY, bool findMin = true)
    {
        if (x <= 0 || x >= mapChunkSize - 1) return;
        if (y <= 0 || y >= mapChunkSize - 1) return;

        float min = float.MaxValue;//heightMap[x,y];
        if ((heightMap[x - 1, y] <= min) == findMin)
        {
            minX = x - 1;
            minY = y;
            min = heightMap[x - 1, y];
        }
        if ((heightMap[x + 1, y] <= min) == findMin)
        {
            minX = x + 1;
            minY = y;
            min = heightMap[x + 1, y];
        }
        if ((heightMap[x, y - 1] <= min) == findMin)
        {
            minX = x;
            minY = y - 1;
            min = heightMap[x, y - 1];
        }
        if ((heightMap[x, y + 1] <= min) == findMin)
        {
            minX = x;
            minY = y + 1;
            min = heightMap[x, y + 1];
        }
    }

    private void GenerateRivers(ref bool[,] waterMap, ref bool[,] oceanMap, ref float[,] heightmap, int numberOfRivers = 50)
    {
        System.Random rng = new System.Random(seed);

        int xpos = 0;
        int ypos = 0;

        for (int i = 0; i < numberOfRivers; i++)
        {
            xpos = rng.Next(mapChunkSize/2) + mapChunkSize/4;
            ypos = rng.Next(mapChunkSize/2) + mapChunkSize/4;
            
            if (!oceanMap[xpos, ypos] && !waterMap[xpos,ypos])
            {
                i--;
                continue;
            }
            

            bool u = false;
            bool l = false;

            int minX = xpos;
            int minY = ypos;
            findMinMax(ref heightmap, xpos, ypos, ref minX, ref minY);

            bool topHalf = ypos > minY;//mapChunkSize/2;
            bool leftHalf = xpos > minX;//mapChunkSize/2;

            bool v = false;

            for (int j = 0; j < rng.Next(50,100); j++)
            {

                u = topHalf;
                l = leftHalf;
                v = true;

                minX = xpos;
                minY = ypos;
                findMinMax(ref heightmap, xpos, ypos, ref minX, ref minY);
                if (minX == xpos && minY == ypos)
                {
                    //at the max
                    break;
                }

                //choose based on lowest direction
                if (!oceanMap[xpos, ypos] && !waterMap[xpos, ypos])
                {
                    if (heightmap[xpos + (l ? -1 : 1), ypos] > heightmap[xpos, ypos + (u ? -1 : 1)])
                    {
                        v = false;
                    }
                }
                //else break;

                waterMap[xpos, ypos] = true;

                if (v)
                {
                    //vertical
                    ypos += u ? 1 : -1;
                }
                else
                {
                    //horizontal
                    xpos += l ? 1 : -1;
                }

                xpos = Mathf.Clamp(xpos, 0, mapChunkSize-1);
                ypos = Mathf.Clamp(ypos, 0, mapChunkSize-1);

            }
        }
    }
    
    /*
    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = HeightCalculator.GenerateNoiseGrid(
            mapChunkSize,
            mapChunkSize,
            seed,
            noiseScale,
            ocataves,
            persistance,
            lacunarity,
            center + offset,
            normalizeMode
        );


        Color[] colors = new Color[mapChunkSize*mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (falloffMode != FalloffMode.None)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].startHeight)
                    {
                        colors[y*mapChunkSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colors);
    }
    */
    void OnValidate()
    {
        if(falloffMode == FalloffMode.Square)
            falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
        else
            falloffMap = FalloffGenerator.GenerateCircularFalloffMap(mapChunkSize);
        if (lacunarity < 1) lacunarity = 1;
        if (ocataves < 0) ocataves = 0;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float startHeight;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
