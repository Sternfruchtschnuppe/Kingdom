using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class WorldGenerator : MonoBehaviour
{
    public GameObject waterTilePrefab;
    public GameObject landTilePrefab;
    public TileSO[] tileSOs;
    
    public int width = 20;
    public int height = 20;
    public float hexSize = 1f;
    public float noiseScale = 0.1f;
    
    public float heightScale = 5f;
    public int seed;
    public bool fixSeed = false;
    
    public float[] heightLevelWeights = new []{1f, 1f, 1f, 1f, 1f};

    public float borderSize = 3f;
    public float borderIntensity = 1f;

    public int playerCount = 2;
    
    private TileData[] tileData;
    
    private int[] neighbors;
    
    [ContextMenu("Generate")]
    void Generate()
    {
        Clear();
        
        float[] noiseValues = new float[width * height];
        tileData = new TileData[width * height];
        if (!fixSeed)
        {
            seed = new System.Random().Next();
        }
        Random.InitState(seed);
        Vector2 seededOffset = new Vector2(Random.Range(0f, 10000f), Random.Range(0, 10000f));
        int zone = 1;
        
        //compute player start positions
        float initAngle = Random.Range(0, Mathf.PI * 2f);
        float anglePerPlayer = Mathf.PI * 2f / playerCount;
        Vector2 mapCenter = new Vector2(width / 2f, height / 2f);
        int[] playerPositions = new int[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            float randomOffset = Random.Range(-anglePerPlayer * 0.15f, anglePerPlayer * 0.15f);
            var playerPos = new Vector2(
                Mathf.Cos(initAngle + anglePerPlayer * i + randomOffset), 
                Mathf.Sin(initAngle + anglePerPlayer * i + randomOffset));
            playerPos = mapCenter + playerPos * Random.Range(0.5f, 0.8f) * mapCenter;
            int pIdx = Mathf.RoundToInt(playerPos.y) * width + Mathf.RoundToInt(playerPos.x);
            playerPositions[i] = pIdx;
            tileData[pIdx].isStartPosition = true;
        }

        //setup neighbors array
        var idx = 0;
        neighbors = new int[width * height * 6];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                var neighborArray = GetNeighbors(idx);
                for (var i = 0; i < neighborArray.Length; i++)
                {
                    neighbors[idx * 6 + i] = neighborArray[i];
                } 
                ++idx;
            }
        }

        // sample noise values, including border values
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float n = RawNoise(x, z, noiseScale, seededOffset);

                Vector2 borderInfluenceVec = new Vector2(
                    Mathf.Min(x - borderSize, width - 1 - borderSize - x, 0f),
                    Mathf.Min(z - borderSize, height - 1 - borderSize - z, 0f));

                float p = 2;
                float borderInfluence = -Mathf.Pow(
                    Mathf.Pow(Mathf.Abs(borderInfluenceVec.x), p) +
                    Mathf.Pow(Mathf.Abs(borderInfluenceVec.y), p),
                    1f / p);

                n += borderInfluence * borderIntensity;

                if (x == 0 || x == width - 1 || z == 0 || z == height - 1) n = float.NegativeInfinity;
                noiseValues[z * width + x] = n;
            }
        }
        
        noiseValues = NormalizeNoiseValues(noiseValues, heightLevelWeights);
        
        // make tiles not water around player position
        for (int i = 0; i < playerCount; i++)
        {
            int pIdx = playerPositions[i];
            // tileData[pIdx].zone = zone;
            noiseValues[pIdx] = 1f;
            foreach (var nIdx in GetNeighboursCached(pIdx))
            {
                if (nIdx == -1) continue;
                noiseValues[nIdx] = 1f;
                // tileData[nIdx].zone = zone;
            }

            // ++zone;
        }

        // fill land and water tiles
        idx = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                var heightValue = noiseValues[idx] * heightScale;
                tileData[idx].tileType = heightValue == 0 ? TileData.TileType.Water : TileData.TileType.Land;
                tileData[idx].id = idx;
                ++idx;
            }
        }
        
        //create coast fields
        // idx = 0;
        // for (int z = 0; z < height; z++)
        // {
        //     for (int x = 0; x < width; x++)
        //     {
        //         if (tileData[idx].tileType == TileData.TileType.Water)
        //         {
        //             bool waterNeighbor = false;
        //             bool landNeighbor = false;
        //             foreach (var nIdx in GetNeighbors(idx))
        //             {
        //                 if (nIdx == -1) continue;
        //                 waterNeighbor = waterNeighbor || tileData[nIdx].tileType == TileData.TileType.Water;
        //                 landNeighbor = landNeighbor || tileData[nIdx].tileType == TileData.TileType.Land;
        //             }
        //             //todo continue coast tiles
        //             if (waterNeighbor && landNeighbor)
        //             {
        //                 tileData[idx].tileType = TileData.TileType.Coast;
        //             }
        //         }
        //         
        //         ++idx;
        //     }
        // }
        
        //generate zones
        idx = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++, idx++)
            {
                if (tileData[idx].tileType == TileData.TileType.Water || tileData[idx].zone != 0)
                    continue;

                int target = tileData[idx].isStartPosition ? 5 : Random.Range(3, 6);
                List<int> zoneTiles = new List<int>();
                Queue<int> q = new Queue<int>();

                tileData[idx].zone = zone;
                q.Enqueue(idx);
                zoneTiles.Add(idx);

                while (q.Count > 0 && zoneTiles.Count < target)
                {
                    int c = q.Dequeue();

                    Vector3 center = Vector3.zero;
                    foreach (var i in zoneTiles) center += IdxToPos(i);
                    center /= zoneTiles.Count;

                    var nbs = GetNeighboursCached(c).ToArray()
                        .Where(n => n != -1 && tileData[n].tileType != TileData.TileType.Water && tileData[n].zone == 0)
                        .OrderBy(n => Vector3.Distance(IdxToPos(n), center));

                    foreach (var n in nbs)
                    {
                        tileData[n].zone = zone;
                        if (tileData[n].isStartPosition) target = 5;
                        q.Enqueue(n);
                        zoneTiles.Add(n);
                        if (zoneTiles.Count >= target) break;
                    }
                }

                if (zoneTiles.Count < 3)
                {
                    foreach (var i in zoneTiles)
                    {
                        var nbZone = GetNeighboursCached(i).ToArray()
                            .Where(n => n != -1 && tileData[n].zone != 0 && tileData[n].zone != zone)
                            .Select(n => tileData[n].zone)
                            .FirstOrDefault();

                        if (nbZone != 0)
                            tileData[i].zone = nbZone;
                    }
                }
                else
                {
                    zone++;
                }
            }
        }


        idx = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                var prefab = tileData[idx].tileType == TileData.TileType.Water ? waterTilePrefab : landTilePrefab;
                Tile tile = Instantiate(prefab, IdxToPos(x, z), Quaternion.identity, transform).GetComponent<Tile>();
                tile.Initialize(tileData[idx]);
                bool[] borders = new bool[6];
                if (tileData[idx].tileType == TileData.TileType.Land)
                {
                    var span = GetNeighboursCached(idx);
                    for (var i = 0; i < span.Length; i++)
                    {
                        if (tileData[span[i]].tileType == TileData.TileType.Water || tileData[span[i]].zone != tileData[idx].zone)
                        {
                            borders[i] = true;
                        }
                    }
                }
                tile.SetBorders(borders, false, false);
                
                
                // if (TryMatchTile(idx, out var prefab, out var rotationOffset))
                // {
                //     Tile tile = Instantiate(prefab, new Vector3(xPos, 0, zPos), Quaternion.Euler(0f, -rotationOffset, 0f), transform).GetComponent<Tile>();
                //     tile.tileData = tileData[idx];
                // }
                ++idx;
            }
        }
    }

    Vector3 IdxToPos(int idx)
    {
        return IdxToPos(idx % width, idx / width);
    }

    Vector3 IdxToPos(int x, int z)
    {
        return new Vector3(x * hexSize + (z % 2 == 0 ? 0f : hexSize * 0.5f), 0f, z * 0.5f * hexSize * Mathf.Sqrt(3f));
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private static float RawNoise(float x, float y, float noiseScale, Vector2 seededOffset)
    {
        float nx = (x + seededOffset.x) * noiseScale;
        float ny = (y + seededOffset.y) * noiseScale;

        return Mathf.PerlinNoise(nx, ny);
    }

    float[] NormalizeNoiseValues(float[] noiseValues, float[] levelWeights)
    {
        int n = noiseValues.Length;
        var indices = Enumerable.Range(0, n).OrderBy(i => noiseValues[i]).ToArray();
        var result = new float[n];
        int levels = levelWeights.Length;
        
        float weightSum = levelWeights.Sum();
        float[] normalizedLevelWeights = levelWeights.Select(l => l / weightSum).ToArray();
        
        int k = 0;
        for (int l = 0; l < levels; l++)
        {
            int count = l == levels - 1 ? n - k : (int)(normalizedLevelWeights[l] * n);
            float v = levels == 1 ? 0f : (float)l / (levels - 1);

            for (int i = 0; i < count && k < n; i++, k++)
            {
                result[indices[k]] = v;
            }
        }

        return result;
    }

    int[] GetNeighbors(int idx)
    {
        int[] res = { -1, -1, -1, -1, -1, -1 };
        int max = width * height;
        bool evenRow = (idx / width) % 2 == 0;
        bool lftEdge = idx % width == 0;
        bool rgtEdge = (idx + 1) % width == 0;
        
        int top = idx + width;
        if (top < max)
        {
            if (evenRow)
            {
                res[0] = top;
                if (!lftEdge) res[5] = top - 1;
            }
            else
            {
                res[5] = top;
                if(!rgtEdge) res[0] = top + 1;
            }
        }
        
        int bot = idx - width;
        if (bot >= 0)
        {
            if (evenRow)
            {
                res[2] = bot;
                if (!lftEdge) res[3] = bot - 1;
            }
            else
            {
                res[3] = bot;
                if(!rgtEdge) res[2] = bot + 1;
            }
        }

        if (!lftEdge) res[4] = idx - 1;
        if (!rgtEdge) res[1] = idx + 1;
        
        return res;
    }

    ReadOnlySpan<int> GetNeighboursCached(int idx)
    {
        return neighbors.AsSpan(idx * 6, 6);
    }
    
    string GetTileNeighborHash(int idx)
    {
        return string.Join("", GetNeighbors(idx).Select(i => i == -1 ? "0" : ((int)tileData[i].tileType).ToString()));
    }

    bool TryMatchTile(int idx, out GameObject prefab, out float lastRotationOffset)
    {
        
        var tileType = tileData[idx].tileType;
        string n = GetTileNeighborHash(idx);
        prefab = null;
        lastRotationOffset = 0;

        // if (tileData[idx].tileType == TileData.TileType.Coast)
        // {
        //     prefab = tileSOs[0].tilePrefab;
        //     return true;
        // }
        // return false;
            
        foreach (var so in tileSOs)
        {
            if (so.tileType != tileType) continue;
            string[] p = so.neighborString.Split(',');
            for (int o = 0; o < 6; o++)
            {
                bool ok = true;
                for (int i = 0; i < 6; i++)
                {
                    char b = n[i];
                    ok = false;
                    foreach (var a in p[(i + o) % 6])
                    {
                        if (a == '?' || a == b)
                        {
                            ok = true;
                            break;
                        }
                        // if (a != '?' && a != b) { ok = false; break; }
                    }
                    if (!ok) break;
                    // if (a != '?' && (a != 'l' || tileData[b].tileType != TileData.TileType.Water) && a != b) { ok = false; break; }
                }
                if (ok)
                {
                    prefab = so.tilePrefab;
                    lastRotationOffset = o * 60f;
                    return true;
                }
            }
        }
        return false;
    }

}
