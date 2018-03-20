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

        public TerrainChunk(Vector3 center, TerrainMaster terrainMaster)
        {
            this.center = center;
            this.terrainMaster = terrainMaster;

            ThreadWorkManager.RequestWork(Generate);
        }

        public void Generate()
        {
            MeshData meshData = MarchingCubes.Poligonyze(
                new Bounds(center, terrainMaster.chunkSize),
                Sample,
                terrainMaster.resolution,
                (1 - terrainMaster.soilToAirVolumetricRatio) * 2 - 1);

            ThreadWorkManager.RequestMainThreadWork(() =>
            {
                gameObject = new GameObject();
                gameObject.name = center.ToString();
                gameObject.AddComponent<MeshFilter>().mesh = meshData.ToMesh();
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaster.chunkMaterial;
                gameObject.transform.parent = terrainMaster.transform;
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
            float[] frequencies = { 0.01f };
            float[] amplitudes = { 1, 0.5f, 0.25f };
            float point = 0;

            for (int i = 0; i < frequencies.Length && i < amplitudes.Length; i++)
            {
                point += Noise.CalcPixel3D(x * frequencies[i], y * frequencies[i] * terrainMaster.horizontalness, z * frequencies[i]) * amplitudes[i];
            }

            return ((y - center.y + terrainMaster.chunkSize.y / 2) < terrainMaster.chunkSize.y * terrainMaster.soilToAirVerticalRatio) ? point : -1;
        }
    }
}
