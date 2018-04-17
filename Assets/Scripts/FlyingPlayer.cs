using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPlayer : MonoBehaviour {

    public float speed;
    public float mouseScollSpeed;

    public System.Action UpdateAction { get { return Update; } }

	void Update ()
    {
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("UpDown"), Input.GetAxis("Vertical")).normalized;

        transform.Translate(dir * speed * Time.deltaTime);

        transform.Rotate(0, Input.GetAxis("Mouse X") * Time.deltaTime * mouseScollSpeed, 0, Space.World);

        float xRotation = transform.rotation.eulerAngles.x;
        xRotation = (xRotation > 180) ? xRotation - 360 : xRotation;
        if ((xRotation > 30 && Input.GetAxis("Mouse Y") < 0) || 
           (xRotation < -30 && Input.GetAxis("Mouse Y") > 0))
        {
            return;
        }

        transform.Rotate(-Input.GetAxis("Mouse Y") * Time.deltaTime * mouseScollSpeed, 0, 0, Space.Self);
    }
}
