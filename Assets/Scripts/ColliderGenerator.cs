using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderGenerator : MonoBehaviour {

    public Vector3[] directions;
    public float distance;
    public Transform source;
    public LayerMask layerToRaycast;
    public Vector3 colliderSize;
    public float updateTresholdDistance;
    public bool useTessellationDisplacement;

    [HideInInspector]
    public float TessellationSimplexNoiseFrequency;
    [HideInInspector]
    public float TessellationSimplexNoiseAmplitude;

    private GameObject[] colliders;
    private Vector3 lastUpdatePosition = Vector3.one * 99999;

	void Start ()
    {
        colliders = new GameObject[directions.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i] = new GameObject("Collider " + directions[i]);
            colliders[i].transform.parent = transform;
            colliders[i].AddComponent<BoxCollider>().size = colliderSize;
            colliders[i].SetActive(false);
        }
	}

    void Update()
    {
        if((source.position - lastUpdatePosition).sqrMagnitude > updateTresholdDistance * updateTresholdDistance)
        {
            lastUpdatePosition = source.position;

            for (int i = 0; i < colliders.Length; i++)
            {
                UpdateCollider(directions[i], colliders[i]);
            }
        }
    }

    void UpdateCollider(Vector3 direction, GameObject collider)
    {
        RaycastHit hitInfo;
        Vector3 sourcePos = source.position - 2 * Mathf.Abs(SimplexDisplacement(source.position)) * direction;
		if (Physics.Raycast(sourcePos, direction, out hitInfo, distance, layerToRaycast))
        {
            SetCollider(collider, hitInfo);
        }
        else
        {
            collider.SetActive(false);
        }
	}

    private void SetCollider(GameObject collider, RaycastHit hitInfo)
    {
        Vector3 contactPoint = hitInfo.point;
        Vector3 normal = hitInfo.normal;

        // Consider tessellation displacement
        if (useTessellationDisplacement)
        {
            float displacement = SimplexDisplacement(contactPoint);
            normal = new Vector3(0, 1, 0); // Because this is the Vector we are using for displacement in the shader
            contactPoint += displacement * normal;
            normal = CalculateSimplexNormal(contactPoint, normal, displacement);
        }

        collider.SetActive(true);
        collider.transform.position = contactPoint;

        collider.transform.up = normal;
    }

    private float SimplexDisplacement(Vector3 contactPoint)
    {
        Vector3 a = contactPoint * TessellationSimplexNoiseFrequency;
        Vector3 b = (contactPoint + new Vector3(0, 0, 1234)) * TessellationSimplexNoiseFrequency;
        Vector3 c = (contactPoint + new Vector3(1234, 0, 0)) * TessellationSimplexNoiseFrequency;

        Vector3 vec = new Vector3(Simplex.Noise.CalcPixel3D(a.x, a.y, a.z),
                                  Simplex.Noise.CalcPixel3D(b.x, b.y, b.z),
                                  Simplex.Noise.CalcPixel3D(c.x, c.y, c.z));

        float displacement = Simplex.Noise.CalcPixel3D(vec.x, vec.y, vec.z);
        displacement = (displacement + displacement * displacement + displacement * displacement * displacement) / 3;

        return displacement * TessellationSimplexNoiseAmplitude;
    }

    Vector3 CalculateSimplexGradient(Vector3 p)
    {
        float delta = 0.01f;
        float partialX = (SimplexDisplacement(p + new Vector3(delta, 0, 0)) - SimplexDisplacement(p)) / delta;
        float partialY = (SimplexDisplacement(p + new Vector3(0, delta, 0)) - SimplexDisplacement(p)) / delta;
        float partialZ = (SimplexDisplacement(p + new Vector3(0, 0, delta)) - SimplexDisplacement(p)) / delta;
        return new Vector3(partialX, partialY, partialZ);
    }
    
    Vector3 CalculateSimplexNormal(Vector3 worldPosition, Vector3 normal, float displacement)
    {
        Vector3 gradient = CalculateSimplexGradient(worldPosition);
        Vector3 h = gradient - Vector3.Dot(gradient, normal) * normal;
        Vector3 n = normal - displacement * h;
        return n.normalized;   
    }
}
