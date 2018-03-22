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

            collider.SetActive(true);
            collider.transform.position = contactPoint;
            //collider.transform.rotation = Quaternion.LookRotation(Vector3.forward, normal);
            collider.transform.up = normal;
        }
        else
        {
            collider.SetActive(false);
        }
	}
}
