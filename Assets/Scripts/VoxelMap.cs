using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class VoxelMap {

    // TODO: update to use a more sparse representation that is less memory intensive (maybe?)
    private UInt64[,] map; // Columns are represented as 64 bits masks (0 no voxel, 1 voxel). If more than 64 h-levels are needed switch to 3D matrix

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }

    public VoxelMap(int width, int height, bool preFill = true)
    {
        this.map = new UInt64[width, height];

        this.Width = width;
        this.Height = height;
        this.Depth = 64;

        if (preFill)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    map[i, j] = UInt64.MaxValue;
                }
            }
        }
    }

    public bool CheckVoxelAt(int x, int y, int z)
    {
        UInt64 mask = (UInt64)1 << z;
        return (IsInside(x, y, z)) && ((map[x, y] & mask) != 0);
    }

    public bool CheckVoxelAt(Vector3Int p)
    {
        return CheckVoxelAt(p.x, p.y, p.z);
    }

    public void ModifyVoxelAt(int x, int y, int z, bool placeVoxel)
    {
        if (!IsInside(x, y, z)) return;

        UInt64 mask = (UInt64)1 << z;
        if (placeVoxel)
        {
            map[x, y] = map[x, y] | mask;
        }
        else
        {
            map[x, y] = map[x, y] & (~mask);
        }
    }

    public void Foreach(Action<int, int, int, bool> action)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    action(x, y, z, CheckVoxelAt(x, y, z));
                }
            }
        }
    }

    public IEnumerator ForeachForCoroutines(Action<int, int, int, bool> action)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    action(x, y, z, CheckVoxelAt(x, y, z));
                }
                yield return null;
            }
        }
    }

    public bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
    }

    public bool IsInside(Vector3Int p)
    {
        return IsInside(p.x, p.y, p.z);
    }
}
