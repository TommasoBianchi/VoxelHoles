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

    public MeshData()
    {
        this.vertices = new List<Vector3>();
        this.triangles = new List<int>();
        this.uv = new List<Vector2>();
        this.normals = new List<Vector3>();

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
}
