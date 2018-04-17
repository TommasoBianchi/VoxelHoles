using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingPlayer : MonoBehaviour {

    public float speed;
    public float rotationSpeed;
    public float maxUpDownAngle;

    public float activationDelay;

    private Camera eyesCamera;
    private bool isActive;

    void Start()
    {
        eyesCamera = GetComponentInChildren<Camera>();

        isActive = false;
        GetComponent<Rigidbody>().useGravity = false;
        StartCoroutine(StartAfterDelay(activationDelay));

        Cursor.visible = false;
    }

    IEnumerator StartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isActive = true;
        GetComponent<Rigidbody>().useGravity = true;
    }

    void Update()
    {
        if (!isActive)
            return;

        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

        transform.Translate(dir * speed * Time.deltaTime);

        transform.Rotate(0, Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed, 0, Space.World);

        float xRotation = transform.rotation.eulerAngles.x;
        xRotation = (xRotation > 180) ? xRotation - 360 : xRotation;
        if ((xRotation > maxUpDownAngle && Input.GetAxis("Mouse Y") < 0) ||
           (xRotation < -maxUpDownAngle && Input.GetAxis("Mouse Y") > 0))
        {
            return;
        }

        eyesCamera.transform.Rotate(-Input.GetAxis("Mouse Y") * Time.deltaTime * rotationSpeed, 0, 0, Space.Self);
    }
}
