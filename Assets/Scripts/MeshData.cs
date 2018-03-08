using UnityEngine;
using System.Collections.Generic;

public class MeshData
{

    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector2> uv;
    private List<Vector3> normals;
    // .....

    private Dictionary<Vector3, int> vertexToIndexDictionary;
    private bool normalsReady = false;

    public MeshData() : this(new List<Vector3>(), new List<int>(), new List<Vector2>(), new List<Vector3>()) { }
    public MeshData(List<Vector3> vertices, List<int> triangles, List<Vector2> uv, List<Vector3> normals)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uv = uv;
        this.normals = normals;

        this.vertexToIndexDictionary = new Dictionary<Vector3, int>();
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = CalculateNormal(a, b, c);

        if (!vertexToIndexDictionary.ContainsKey(a))
        {
            vertexToIndexDictionary.Add(a, vertices.Count);
            vertices.Add(a);
            uv.Add(new Vector2((a.x + a.y) / 4, a.z / 4));
            normals.Add(Vector3.zero);
        }
        if (!vertexToIndexDictionary.ContainsKey(b))
        {
            vertexToIndexDictionary.Add(b, vertices.Count);
            vertices.Add(b);
            uv.Add(new Vector2((b.x + b.y) / 4, b.z / 4));
            normals.Add(Vector3.zero);
        }
        if (!vertexToIndexDictionary.ContainsKey(c))
        {
            vertexToIndexDictionary.Add(c, vertices.Count);
            vertices.Add(c);
            uv.Add(new Vector2((c.x + c.y) / 4, c.z / 4));
            normals.Add(Vector3.zero);
        }

        triangles.Add(vertexToIndexDictionary[a]);
        triangles.Add(vertexToIndexDictionary[b]);
        triangles.Add(vertexToIndexDictionary[c]);

        normals[vertexToIndexDictionary[a]] += normal;
        normals[vertexToIndexDictionary[b]] += normal;
        normals[vertexToIndexDictionary[c]] += normal;

        normalsReady = false;
    }

    public void CalculateNormals()
    {
        for (int i = 0; i < normals.Count; i++)
        {
            normals[i].Normalize();
        }

        normalsReady = true;
    }

    // Math taken from https://math.stackexchange.com/a/305914
    private Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v = b - a;
        Vector3 w = c - a;
        return new Vector3(
            v.x * w.x - v.z * w.y,
            v.z * w.z - v.x * w.z,
            v.x * w.y - v.y * w.x
        );
    }

    public Mesh ToMesh(bool recalculateNormals = false)
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();

        if (recalculateNormals)
            mesh.RecalculateNormals();
        else
        {
            if (!normalsReady) CalculateNormals();
            mesh.normals = normals.ToArray();
        }

        return mesh;
    }

    // TODO: needs to be refactored somewhere else
    // TODO: this way it produces only garbage. Think of a better way to do it or use marching cubes
    public void Smooth()
    {
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),

            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 0),
            new Vector3(0, 1, -1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, -1),
            new Vector3(-1, 1, -1),

            new Vector3(1, -1, 0),
            new Vector3(0, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(-1, -1, 0),
            new Vector3(0, -1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, -1),
            new Vector3(-1, -1, -1),

            new Vector3(1, 0, 1),
            new Vector3(1, 0, -1),
            new Vector3(-1, 0, 1),
            new Vector3(-1, 0, -1)
        };

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 displacement = Vector3.zero;
            int neighbourCount = 0;

            for (int j = 0; j < directions.Length; j++)
            {
                Vector3 p = vertices[i] + directions[j];

                if (vertexToIndexDictionary.ContainsKey(p))
                {
                    displacement += (p - vertices[i]);
                    neighbourCount++;
                }
            }

            displacement.Normalize();
            float smoothFactor = Mathf.Lerp(-1f, 1f, (1 - neighbourCount / (float)directions.Length));

            //Debug.Log("Smoothing vertex at " + vertices[i] + " wich has " + neighbourCount + " neighbours and a displacement of " + displacement);

            vertices[i] += displacement * smoothFactor;
        }
    }
}
