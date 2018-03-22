using Simplex;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMaster : MonoBehaviour {
    
    public Vector3 chunkSize;
    public Vector3 resolution;
    public float horizontalness;
    [Range(0, 1)]
    public float soilToAirVerticalRatio;
    [Range(0, 1)]
    public float soilToAirVolumetricRatio;
    public float postProcessingScale;

    public Material chunkMaterial;

    private Dictionary<Vector3, TerrainChunk> terrainChunks = new Dictionary<Vector3, TerrainChunk>();
    private HashSet<TerrainChunk> visibleChunks = new HashSet<TerrainChunk>();
    
	void Start ()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                GenerateChunk(new Vector3(chunkSize.x * x, 0, chunkSize.z * z));
            }
        }
        GenerateChunk(new Vector3(0, -chunkSize.y, 0));
        //GenerateChunk(Vector3.zero);
	}
	
	void Update ()
    {
		
	}

    private void GenerateChunk(Vector3 center)
    {
        if (!terrainChunks.ContainsKey(center))
        {
            terrainChunks.Add(center, new TerrainChunk(center, this));
        }

        TerrainChunk newChunk = terrainChunks[center];
        newChunk.ToggleVisibility(true);
        visibleChunks.Add(newChunk);
    }
    
    private class TerrainChunk
    {
        private Vector3 center;
        private TerrainMaster terrainMaster;

        private GameObject gameObject;
        private bool isVisible = true;

        private float chunkAltitude;

        public TerrainChunk(Vector3 center, TerrainMaster terrainMaster)
        {
            this.center = center;
            this.terrainMaster = terrainMaster;

            float f = 0.01f;
            this.chunkAltitude = Noise.CalcPixel3D(center.x / terrainMaster.chunkSize.x * f, 0, center.z / terrainMaster.chunkSize.z * f);

            ThreadWorkManager.RequestWork(Generate);
        }

        public void Generate()
        {
            MeshData meshData = MarchingCubes.Poligonyze(
                new Bounds(center, terrainMaster.chunkSize),
                Sample,
                terrainMaster.resolution,
                (1 - terrainMaster.soilToAirVolumetricRatio) * 2 - 1);

            Matrix4x4 matrix = Matrix4x4.Scale(Vector3.one * terrainMaster.postProcessingScale) * Matrix4x4.Translate(-center);
            meshData.Transform(matrix);

            ThreadWorkManager.RequestMainThreadWork(() =>
            {
                gameObject = new GameObject();
                gameObject.name = center.ToString();
                gameObject.layer = LayerMask.NameToLayer("Terrain");

                Mesh mesh = meshData.ToMesh();

                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaster.chunkMaterial;
                gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;

                gameObject.transform.parent = terrainMaster.transform;
                gameObject.transform.position = center * terrainMaster.postProcessingScale;
                gameObject.SetActive(isVisible);
            });
        }

        public void ToggleVisibility(bool visible)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(visible);
            }
        }

        private float Sample(float x, float y, float z)
        {
            if (y <= 0)
                return SampleSimplex(x, y, z);
            else
                return SamplePerlin(x, y, z);
        }

        private float SampleSimplex(float x, float y, float z)
        {
            float[] frequencies = { 0.02f };
            float[] amplitudes = { 1, 0.5f, 0.25f };
            float point = 0;

            for (int i = 0; i < frequencies.Length && i < amplitudes.Length; i++)
            {
                point += Noise.CalcPixel3D(x * frequencies[i], y * frequencies[i] * terrainMaster.horizontalness, z * frequencies[i]) * amplitudes[i];
            }

            //return ((y - center.y + terrainMaster.chunkSize.y / 2) < terrainMaster.chunkSize.y * terrainMaster.soilToAirVerticalRatio) ? point : -1;
            return point;
        }

        private float SamplePerlin(float x, float y, float z)
        {
            float fMountain = 0.01f;
            float mountainValue = (Noise.CalcPixel3D(x * fMountain, 0, z * fMountain) + 1) / 2f; // Put in range [0, 1]
            mountainValue = mountainValue * mountainValue * mountainValue * mountainValue;

            float fPlains = 0.03f;
            float plainValue = Noise.CalcPixel3D(x * fPlains, 137, z * fPlains);

            float value = (mountainValue * 9 + plainValue) / 10;

            float heightTreshold = terrainMaster.chunkSize.y / 2 * value;
            // Make sure heightTreshold is always at least a "tick" below the last computed point
            heightTreshold = Mathf.Min(heightTreshold, terrainMaster.chunkSize.y / 2 - terrainMaster.resolution.y - 1);

            if (y > heightTreshold)
            {
                float t = 1 - Mathf.InverseLerp(heightTreshold, terrainMaster.chunkSize.y, y);
                return -(1 - t * t * t * t * t * t * t * t);
                //return Mathf.Lerp(0, -1, (y - heightTreshold) / (terrainMaster.chunkSize.y - heightTreshold));
            }
            else
            {
                float t = Mathf.InverseLerp(0, heightTreshold, y);
                return 1 - t * t * t * t * t * t * t * t;
                //return Mathf.Lerp(1, 0, (heightTreshold - y) / (heightTreshold));
            }

            //return (y > terrainMaster.chunkSize.y / 2 * value) ? -1 : 1;
        }
    }
}
