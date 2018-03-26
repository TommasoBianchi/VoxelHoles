using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour {

    public Transform target;
    public float translationalSpeed;
    public float rotationalSpeed;

    private float distance;
    private Vector3 lastFramePosition;

    void Start()
    {
        distance = (transform.position - target.position).magnitude;
    }

    void Update ()
    {
        if (target.position != lastFramePosition)
        {
            Vector3 dir = (target.position - lastFramePosition).normalized;

            Vector3 desiredPosition = target.position - dir * distance;
            Quaternion desiredRotation = Quaternion.LookRotation(dir);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, translationalSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationalSpeed * Time.deltaTime);

            lastFramePosition = target.position;
        }
	}
}
