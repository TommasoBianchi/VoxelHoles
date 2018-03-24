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
		if(Physics.Raycast(source.position, direction, out hitInfo, distance, layerToRaycast))
        {
            Vector3 contactPoint = hitInfo.point;
            Vector3 normal = hitInfo.normal;

            // Consider tessellation displacement
            float _SimplexNoiseFrequency = 0.03f;
            float displacement = Simplex.Noise.CalcPixel3D(contactPoint.x * _SimplexNoiseFrequency, contactPoint.y * _SimplexNoiseFrequency, contactPoint.z * _SimplexNoiseFrequency);
            float sign = Mathf.Sign(displacement);
            displacement = displacement * displacement * sign;
            contactPoint += displacement * normal;

            collider.SetActive(true);
            collider.transform.position = contactPoint;

            collider.transform.up = normal;
        }
        else
        {
            collider.SetActive(false);
        }
	}
}
