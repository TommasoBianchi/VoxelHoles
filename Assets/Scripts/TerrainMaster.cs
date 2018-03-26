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
    public float maxMountainHeight;
    public float postProcessingScale;
    public float newChunkCheckTreshold;
    public Vector3 newChunkSpawnTreshold;
    public Transform playerTransform;

    public Material chunkMaterial;

    private Dictionary<Vector3, TerrainChunk> terrainChunks = new Dictionary<Vector3, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    private Vector3 lastUpdatePosition;
    private bool isUpdating = false;
    
	void Start ()
    {
        Vector3 playerPosition = playerTransform.position;
        ThreadWorkManager.RequestWork(() => UpdateChunks(playerPosition, visibleChunks));
    }
	
	void Update ()
    {
        if (isUpdating)
            return;

		if((playerTransform.position - lastUpdatePosition).sqrMagnitude > newChunkCheckTreshold * newChunkCheckTreshold)
        {
            UpdateChunks(playerTransform.position, visibleChunks);
        }
    }

    void UpdateChunks(Vector3 playerPosition, List<TerrainChunk> visibleChunks)
    {
        isUpdating = true;

        visibleChunks.ForEach(chunk => chunk.ToggleVisibility(false));
        visibleChunks.Clear();

        Vector3Int currentChunkIndex = new Vector3Int(
            Mathf.RoundToInt(playerPosition.x / (chunkSize.x * postProcessingScale)),
            Mathf.RoundToInt(playerPosition.y / (chunkSize.y * postProcessingScale)),
            Mathf.RoundToInt(playerPosition.z / (chunkSize.z * postProcessingScale))
        );

        Vector3Int checkIndexDist = new Vector3Int(
            Mathf.CeilToInt(newChunkSpawnTreshold.x / (chunkSize.x * postProcessingScale)),
            Mathf.CeilToInt(newChunkSpawnTreshold.y / (chunkSize.y * postProcessingScale)),
            Mathf.CeilToInt(newChunkSpawnTreshold.z / (chunkSize.z * postProcessingScale))
        );

        for (int y = checkIndexDist.y; y <= -checkIndexDist.y; y--) // Render first the upper layer because they are the most visible one by players
        {
            for (int x = -checkIndexDist.x; x <= checkIndexDist.x; x++)
            {
                for (int z = -checkIndexDist.z; z <= checkIndexDist.z; z++)
                {
                    Vector3 newChunkPos = new Vector3(
                        (currentChunkIndex.x + x) * chunkSize.x * postProcessingScale,
                        (currentChunkIndex.y + y) * chunkSize.y * postProcessingScale,
                        (currentChunkIndex.z + z) * chunkSize.z * postProcessingScale
                    );

                    Vector3 dist = playerPosition - newChunkPos;

                    if (Mathf.Abs(dist.x) < newChunkSpawnTreshold.x ||
                        Mathf.Abs(dist.y) < newChunkSpawnTreshold.y ||
                        Mathf.Abs(dist.z) < newChunkSpawnTreshold.z)
                    {
                        GenerateChunk(newChunkPos / postProcessingScale);
                    }
                }
            }
        }

        lastUpdatePosition = playerPosition;
        isUpdating = false;
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
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            MeshData meshData = MarchingCubes.Poligonyze(
                new Bounds(center, terrainMaster.chunkSize),
                Sample,
                terrainMaster.resolution,
                (1 - terrainMaster.soilToAirVolumetricRatio) * 2 - 1);

            timer.Stop();
            Debug.Log("Chunk " + center + ": MarchingCubes.Poligonyze in " + timer.ElapsedMilliseconds + " milliseconds.");

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
            
            return point;
        }

        private float SamplePerlin(float x, float y, float z)
        {
            //float fMountain = 0.0008f;
            //float mountainValue = (Noise.CalcPixel3D(x * fMountain, 0, z * fMountain) + 1) / 2f; // Put in range [0, 1]
            //mountainValue = mountainValue * mountainValue * mountainValue * mountainValue;

            ////float fPlains = 0.01f;
            ////float plainValue = Noise.CalcPixel3D(x * fPlains, 137, z * fPlains);

            ////float value = (mountainValue * 9 + plainValue) / 10;
            //float value = mountainValue;

            ////float heightTreshold = terrainMaster.chunkSize.y / 2 * value;
            //// Make sure heightTreshold is always at least a "tick" below the last computed point
            ////heightTreshold = Mathf.Min(heightTreshold, terrainMaster.chunkSize.y / 2 - terrainMaster.resolution.y - 1);

            //float heightTreshold = value * terrainMaster.maxMountainHeight;

            //float fMountain = 0.01f;
            //float mountainValue = (Noise.CalcPixel3D(x * fMountain, 0, z * fMountain) + 1) / 2f; // Put in range [0, 1]
            //mountainValue = mountainValue * mountainValue * mountainValue * mountainValue;

            float fMountain2 = 0.008f;
            float mountainValue2 = Noise.CalcPixel3D(x * fMountain2, 512, z * fMountain2); // Keep in range [-1, 1]
            mountainValue2 = mountainValue2 * mountainValue2 * mountainValue2;

            //float heightTreshold = mountainValue * terrainMaster.maxMountainHeight + mountainValue2 * terrainMaster.chunkSize.y;
            float heightTreshold = mountainValue2 * terrainMaster.chunkSize.y;

            if (y > heightTreshold)
            {
                float t = 1 - Mathf.InverseLerp(heightTreshold, terrainMaster.chunkSize.y, y);
                return -(1 - t * t * t * t * t * t * t * t);
            }
            else
            {
                float t = Mathf.InverseLerp(0, heightTreshold, y);
                return 1 - t * t * t * t * t * t * t * t;
            }
        }
    }
}
