using Priority_Queue;
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
                    hasReachedSurface = true;
                    //hasReachedSurface = hasReachedSurface ||
                    //    (!removedPoints.Contains(neighbours[j]) && voxelMap.IsInside(neighbours[j])) ||
                    //    neighbours[j].z < 0;
                    //Debug.Log("hasReachedSurface = " + hasReachedSurface + " neighbours[j] = " + neighbours[j]);
                }
            }

            voxelMap.ModifyVoxelAt(pointToRemove.x, pointToRemove.y, pointToRemove.z, false);
        }

        // TODO: Generate tunnel to connect holes, do not use hasReachedSurface anymore
    }

    void GenerateHoles(int amount)
    {
        SimplePriorityQueue<KeyValuePair<Vector3Int, int>, int> holesData = new SimplePriorityQueue<KeyValuePair<Vector3Int, int>, int>();

        for (int i = 0; i < amount; i++)
        {
            Vector3Int center = new Vector3Int(prng.Next(0, voxelMap.Width),
                                               prng.Next(0, voxelMap.Height),
                                               prng.Next(i * voxelMap.Depth / amount, (i + 1) * voxelMap.Depth / amount));
            int size = prng.Next(8000, 16000);
            holesData.Enqueue(new KeyValuePair<Vector3Int, int>(center, size), center.z);
        }

        // TODO: parallelyze work here (and take care of thread safety of voxelMap)

        while (holesData.Count > 0)
        {
            KeyValuePair<Vector3Int, int> data = holesData.Dequeue();
            Debug.Log("Generating cave at " + data.Key);
            GenerateIrregularCaveInMap(data.Key, data.Value);
        }
    }

    void SpawnMarchingMap()
    {
        Vector3Int size = new Vector3Int(Mathf.Min(32, voxelMap.Width),
                                         Mathf.Min(32, voxelMap.Height),
                                         Mathf.Min(32, voxelMap.Depth));

        for (int x = size.x / 2; x <= voxelMap.Width - size.x / 2; x += size.x)
        {
            for (int y = size.y / 2; y <= voxelMap.Height - size.y / 2; y += size.y)
            {
                for (int z = size.z / 2; z <= voxelMap.Depth - size.z / 2; z += size.z)
                {
                    Vector3Int center = new Vector3Int(x, y, z);
                    ThreadWorkManager.RequestWork(() => SpawnMarchingMapChunk(center, size));
                }
            }
        }
    }

    void SpawnMarchingMapChunk(Vector3Int center, Vector3Int size)
    {
        MeshData meshData = MarchingCubes.Poligonyze(new PolygonizableVoxelMap(voxelMap), new Bounds(center, size), Vector3.one * 2, 0.5f);
        //Debug.Log("Finished MarchingCubes.Poligonyze at " + center);

        ThreadWorkManager.RequestMainThreadWork(() =>
        {
            GameObject go = new GameObject();
            go.AddComponent<MeshFilter>().mesh = meshData.ToMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = voxelMaterial;
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);
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
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);

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
            res.Add(p + Vector3Int.up);
            res.Add(p - Vector3Int.up);
        }

        res.Add(p - new Vector3Int(0, 0, 1));
        if(includeDown)
            res.Add(p + new Vector3Int(0, 0, 1));
        return res.ToArray();
    }
}
