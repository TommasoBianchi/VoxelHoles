using UnityEngine;
using System.Collections.Generic;

public class MeshData
{

    private List<Vector3> vertices;
    private List<int> triangles;
    //private List<Vector2> uv;
    //private List<Vector3> normals;
    // .....

    private Dictionary<Vector3, int> vertexToIndexDictionary;

    public MeshData()
    {
        this.vertices = new List<Vector3>();
        this.triangles = new List<int>();

        this.vertexToIndexDictionary = new Dictionary<Vector3, int>();
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        if (!vertexToIndexDictionary.ContainsKey(a))
        {
            vertexToIndexDictionary.Add(a, vertices.Count);
            vertices.Add(a);
        }
        if (!vertexToIndexDictionary.ContainsKey(b))
        {
            vertexToIndexDictionary.Add(b, vertices.Count);
            vertices.Add(b);
        }
        if (!vertexToIndexDictionary.ContainsKey(c))
        {
            vertexToIndexDictionary.Add(c, vertices.Count);
            vertices.Add(c);
        }

        triangles.Add(vertexToIndexDictionary[a]);
        triangles.Add(vertexToIndexDictionary[b]);
        triangles.Add(vertexToIndexDictionary[c]);
    }

    public Mesh ToMesh(bool recalculateNormals = true)
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }
}
