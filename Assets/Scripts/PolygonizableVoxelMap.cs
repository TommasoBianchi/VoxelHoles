using System.Collections.Generic;
using UnityEngine;

public class PolygonizableVoxelMap : MarchingCubes.IPoligonyzable
{
    public Bounds bounds { get; private set; }
    
    private float[,,] fieldValues;
    private VoxelMap voxelMap;

    public PolygonizableVoxelMap(VoxelMap voxelMap)
    {
        this.voxelMap = voxelMap;
        Vector3 size = new Vector3(voxelMap.Width, voxelMap.Height, voxelMap.Depth);
        this.bounds = new Bounds(size / 2, size);
        this.fieldValues = new float[voxelMap.Width, voxelMap.Height, voxelMap.Depth];

        for (int x = 0; x < voxelMap.Width; x++)
        {
            for (int y = 0; y < voxelMap.Height; y++)
            {
                for (int z = 0; z < voxelMap.Depth; z++)
                {
                    int neighbourCount = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                if (voxelMap.CheckVoxelAt(x + i, y + j, z + k))
                                    neighbourCount++;
                            }
                        }
                    }

                    fieldValues[x, y, z] = neighbourCount / 27.0f;
                }
            }
        }
    }

    public float Sample(float x, float y, float z)
    {
        float total = 0;
        int count = 0;
        Vector3Int center = new Vector3Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    // TODO: use linear interpolation based on distance of (i, j, k) from center
                    if (center.x + i >= 0 && center.x + i < fieldValues.GetLength(0) &&
                       center.y + j >= 0 && center.y + j < fieldValues.GetLength(1) &&
                       center.z + k >= 0 && center.z + k < fieldValues.GetLength(2))
                    {
                        total += fieldValues[center.x + i, center.y + j, center.z + k];
                        count++;
                    }
                }
            }
        }

        //return total / count;
        if (center.x >= 0 && center.x < fieldValues.GetLength(0) &&
           center.y >= 0 && center.y < fieldValues.GetLength(1) &&
           center.z >= 0 && center.z < fieldValues.GetLength(2))
            return fieldValues[center.x, center.y, center.z];
        //return voxelMap.CheckVoxelAt(center.x, center.y, center.z) ? 1 : 0;
        else
            return 0;
    }
}
