﻿using Priority_Queue;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public int width = 32;
    public int height = 32;
    public int holesAmount = 20;
    public int lateralBias = 4;

    public GameObject voxelPrefab;
    public Material voxelMaterial;

    private VoxelMap voxelMap;

    private System.Random prng = new System.Random();
    
	void Start () {
        voxelMap = new VoxelMap(width, height);

        ThreadWorkManager.RequestWork(() =>
        {
            GenerateMap();

            SpawnMap();
            SpawnMarchingMap();
        });
	}
	
	void GenerateMap () {
        //for (int i = 0; i < 10; i++)
        //{
        //    Vector3Int center = new Vector3Int(Random.Range(0, 16), Random.Range(0, 16), Random.Range(0, 64));
        //    int radius = Random.Range(2, 8);
        //    GenerateHoleInMap(center, radius);
        //}

        GenerateHoles(holesAmount);

        //GenerateIrregularCaveInMap(new Vector3Int(4, 4, 4), 1200);
    }

    void GenerateHoleInMap(Vector3Int center, int radius)
    {
        int halfRadius = radius;
        for (int x = center.x - halfRadius; x <= center.x + halfRadius; x++)
        {
            for (int y = center.y - halfRadius; y <= center.y + halfRadius; y++)
            {
                for (int z = center.z - halfRadius; z <= center.z + halfRadius; z++)
                {
                    int sqrDist = (center - new Vector3Int(x, y, z)).sqrMagnitude;
                    if(sqrDist <= radius * radius)
                    {
                        voxelMap.ModifyVoxelAt(x, y, z, false);
                    }
                }
            }
        }
    }

    //void GenerateIrregularCaveInMap(Vector3Int center, int size)
    //{
    //    SimplePriorityQueue<Vector3Int, int> points = new SimplePriorityQueue<Vector3Int, int>();
    //    points.Enqueue(center, 0);

    //    for (int i = 0; i < size && points.Count > 0; i++)
    //    {
    //        Vector3Int pointToRemove = points.Dequeue();
    //        if (Random.Range(0.0f, 1.0f) > 1.0f / i)
    //        {
    //            continue;
    //        }

    //        Vector3Int[] neighbours = GetNeighbouringPoints(pointToRemove);
    //        for (int j = 0; j < neighbours.Length; j++)
    //        {
    //            if (voxelMap.CheckVoxelAt(neighbours[j]))
    //            {
    //                int sqrDist = (center - neighbours[j]).sqrMagnitude;

    //                if (points.Contains(neighbours[j]))
    //                {
    //                    if (points.GetPriority(neighbours[j]) > sqrDist)
    //                    {
    //                        points.UpdatePriority(neighbours[j], sqrDist);
    //                    }
    //                }
    //                else
    //                {
    //                    points.Enqueue(neighbours[j], sqrDist);
    //                }
    //            }
    //        }

    //        voxelMap.ModifyVoxelAt(pointToRemove.x, pointToRemove.y, pointToRemove.z, false);
    //    }
    //}

    void GenerateIrregularCaveInMap(Vector3Int center, int size)
    {
        List<Vector3Int> points = new List<Vector3Int>();
        HashSet<Vector3Int> removedPoints = new HashSet<Vector3Int>();
        points.Add(center);
        bool hasReachedSurface = false;

        for (int i = 0; (i < size || !hasReachedSurface) && points.Count > 0; i++)
        {
            int randIndex = prng.Next(0, points.Count);
            Vector3Int pointToRemove = points[randIndex];
            removedPoints.Add(pointToRemove);

            Vector3Int[] neighbours = GetNeighbouringPoints(pointToRemove, lateralBias);
            for (int j = 0; j < neighbours.Length; j++)
            {
                if (voxelMap.CheckVoxelAt(neighbours[j]))
                {
                    points.Add(neighbours[j]);
                }
                else
                {
                    hasReachedSurface = hasReachedSurface || !removedPoints.Contains(neighbours[j]);
                }
            }

            voxelMap.ModifyVoxelAt(pointToRemove.x, pointToRemove.y, pointToRemove.z, false);
        }
    }

    void GenerateHoles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Debug.Log("GenerateIrregularCaveInMap " + i);
            Vector3Int center = new Vector3Int(prng.Next(0, voxelMap.Width), prng.Next(0, voxelMap.Height), prng.Next(0, voxelMap.Depth));
            int size = prng.Next(2000, 8000);
            GenerateIrregularCaveInMap(center, size);
        }
    }

    void SpawnMarchingMap()
    {
        MeshData meshData = MarchingCubes.Poligonyze(new PolygonizableVoxelMap(voxelMap), Vector3.one * 2, 0.5f);
        Debug.Log("Finished MarchingCubes.Poligonyze");

        ThreadWorkManager.RequestMainThreadWork(() =>
        {
            GameObject go = new GameObject();
            go.AddComponent<MeshFilter>().mesh = meshData.ToMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = voxelMaterial;
        });
    }

    void SpawnMap()
    {
        MeshData meshData = new MeshData();

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        // Invert y and z for visualization purposes
        voxelMap.Foreach((x, y, z, voxelPresent) =>
        {
            if (voxelPresent)
            {
                //Instantiate(voxelPrefab, new Vector3(x, y, z), Quaternion.identity, transform);

                for (int i = 0; i < directions.Length; i++)
                {
                    if (voxelMap.CheckVoxelAt(x + directions[i].x, y + directions[i].y, z + directions[i].z) == false)
                    {
                        CreateSquare(new Vector3Int(x, y, z), meshData, directions[i]);
                    }
                }
            }
        });

        meshData.CalculateNormals();

        ThreadWorkManager.RequestMainThreadWork(() =>
        {
            GameObject go = new GameObject();
            go.AddComponent<MeshFilter>().mesh = meshData.ToMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = voxelMaterial;

            /*MeshData m = new MeshData();

            //CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(1, 0, 0));
            CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(-1, 0, 0));
            CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(0, 1, 0));
            CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(0, -1, 0));
            //CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(0, 0, 1));
            CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(0, 0, -1));

            CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(1, 0, 0));
            //CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(-1, 0, 0));
            //CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(0, 1, 0));
            CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(0, -1, 0));
            //CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(0, 0, 1));
            CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(0, 0, -1));

            CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(1, 0, 0));
            CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(-1, 0, 0));
            CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(0, 1, 0));
            //CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(0, -1, 0));
            //CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(0, 0, 1));
            CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(0, 0, -1));

            //CreateSquare(new Vector3Int(100, 100, 100), m, new Vector3Int(1, 0, 0));
            CreateSquare(new Vector3Int(100, 100, 101), m, new Vector3Int(-1, 0, 0));
            CreateSquare(new Vector3Int(100, 100, 101), m, new Vector3Int(0, 1, 0));
            CreateSquare(new Vector3Int(100, 100, 101), m, new Vector3Int(0, -1, 0));
            CreateSquare(new Vector3Int(100, 100, 101), m, new Vector3Int(0, 0, 1));
            //CreateSquare(new Vector3Int(100, 100, 101), m, new Vector3Int(0, 0, -1));

            CreateSquare(new Vector3Int(101, 100, 101), m, new Vector3Int(1, 0, 0));
            //CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(-1, 0, 0));
            //CreateSquare(new Vector3Int(101, 100, 100), m, new Vector3Int(0, 1, 0));
            CreateSquare(new Vector3Int(101, 100, 101), m, new Vector3Int(0, -1, 0));
            CreateSquare(new Vector3Int(101, 100, 101), m, new Vector3Int(0, 0, 1));
            //CreateSquare(new Vector3Int(101, 100, 101), m, new Vector3Int(0, 0, -1));

            CreateSquare(new Vector3Int(101, 101, 101), m, new Vector3Int(1, 0, 0));
            CreateSquare(new Vector3Int(101, 101, 101), m, new Vector3Int(-1, 0, 0));
            CreateSquare(new Vector3Int(101, 101, 101), m, new Vector3Int(0, 1, 0));
            //CreateSquare(new Vector3Int(101, 101, 100), m, new Vector3Int(0, -1, 0));
            CreateSquare(new Vector3Int(101, 101, 101), m, new Vector3Int(0, 0, 1));
            //CreateSquare(new Vector3Int(101, 101, 101), m, new Vector3Int(0, 0, -1));

            m.Smooth();
            go = new GameObject();
            go.AddComponent<MeshFilter>().mesh = m.ToMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = voxelMaterial;*/
        });
    }

    void CreateSquare(Vector3Int voxelCenter, MeshData meshData, Vector3Int normal)
    {
        Quaternion rotation = Quaternion.FromToRotation(new Vector3Int(0, 1, 0), normal);
        Vector3 a = rotation * new Vector3(0.5f, 0, 0.5f) + voxelCenter + (Vector3)normal * 0.5f;
        Vector3 b = rotation * new Vector3(0.5f, 0, -0.5f) + voxelCenter + (Vector3)normal * 0.5f;
        Vector3 c = rotation * new Vector3(-0.5f, 0, -0.5f) + voxelCenter + (Vector3)normal * 0.5f;
        Vector3 d = rotation * new Vector3(-0.5f, 0, 0.5f) + voxelCenter + (Vector3)normal * 0.5f;

        meshData.AddTriangle(a, b, c);
        meshData.AddTriangle(a, c, d);
    }

    Vector3Int[] GetNeighbouringPoints(Vector3Int p, int lateralBias = 0, bool includeDown = false)
    {
        List<Vector3Int> res = new List<Vector3Int>();

        for (int i = 0; i < lateralBias + 1; i++)
        {
            res.Add(p + Vector3Int.right);
            res.Add(p - Vector3Int.right);
            res.Add(p + new Vector3Int(0, 0, 1));
            res.Add(p - new Vector3Int(0, 0, 1));
        }        

        res.Add(p + Vector3Int.up);
        if(includeDown)
            res.Add(p - Vector3Int.up);
        return res.ToArray();
    }
}
