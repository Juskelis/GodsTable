using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap, ColorMap, Mesh, FalloffMap
    }

    [SerializeField] private DrawMode drawMode;
    [SerializeField] private HeightCalculator.NormalizeMode normalizeMode;

    public const int mapChunkSize = 241;
    
    [SerializeField]
    [Range(0, 6)]
    private int editorLevelOfDetail;

    [SerializeField]
    private float noiseScale;
    [SerializeField]
    private int ocataves;
    [SerializeField]
    [Range(0,1)]
    private float persistance;
    [SerializeField]
    private float lacunarity;

    [SerializeField]
    private int seed;
    [SerializeField]
    private Vector2 offset;

    [SerializeField]
    private float meshHeightMultiplier;
    [SerializeField]
    private AnimationCurve meshHeightCurve;

    [SerializeField]
    private TerrainType[] regions;

    [SerializeField] private bool useFalloff = false;
    private float[,] falloffMap;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    [SerializeField]
    private bool autoGenerate = true;
    public bool GenerateOnUpdate { get { return autoGenerate; } }

    void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
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
        else if(drawMode == DrawMode.FalloffMap)
            display.DrawTexture(TextureGenerator.TextureFromNoiseMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
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

        Color[] colors = new Color[mapChunkSize*mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloff)
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

    void OnValidate()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
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
