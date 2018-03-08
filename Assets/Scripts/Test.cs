using Priority_Queue;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public GameObject voxelPrefab;

    private VoxelMap voxelMap;
    
	void Start () {
        voxelMap = new VoxelMap(16, 16);

        GenerateMap();

        SpawnMap();
	}
	
	void GenerateMap () {
        //for (int i = 0; i < 10; i++)
        //{
        //    Vector3Int center = new Vector3Int(Random.Range(0, 16), Random.Range(0, 16), Random.Range(0, 64));
        //    int radius = Random.Range(2, 8);
        //    GenerateHoleInMap(center, radius);
        //}

        GenerateHoles(10);

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
        points.Add(center);

        for (int i = 0; i < size && points.Count > 0; i++)
        {
            int randIndex = Random.Range(0, points.Count);
            Vector3Int pointToRemove = points[randIndex];

            Vector3Int[] neighbours = GetNeighbouringPoints(pointToRemove);
            for (int j = 0; j < neighbours.Length; j++)
            {
                if (voxelMap.CheckVoxelAt(neighbours[j]))
                {
                    points.Add(neighbours[j]);
                }
            }

            voxelMap.ModifyVoxelAt(pointToRemove.x, pointToRemove.y, pointToRemove.z, false);
        }
    }

    IEnumerator GenerateHoles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3Int center = new Vector3Int(Random.Range(0, 16), Random.Range(0, 16), Random.Range(0, 64));
            int size = Random.Range(400, 1600);
            GenerateIrregularCaveInMap(center, size);
            yield return null;
        }
    }

    void SpawnMap()
    {
        StartCoroutine(voxelMap.ForeachForCoroutines((x, y, z, voxelPresent) =>
        {
            if (voxelPresent)
            {
                // TODO: replace with some actual mesh work
                Instantiate(voxelPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
            }
        }));
    }

    Vector3Int[] GetNeighbouringPoints(Vector3Int p)
    {
        Vector3Int[] res = new Vector3Int[5];
        res[0] = p + Vector3Int.right;
        res[1] = p - Vector3Int.right;
        res[2] = p + Vector3Int.up;
        //res[3] = p - Vector3Int.up;
        res[3] = p + new Vector3Int(0, 0, 1);
        res[4] = p - new Vector3Int(0, 0, 1);
        return res;
    }
}
