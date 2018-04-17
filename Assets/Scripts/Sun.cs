using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour {

    public float dayLengthInSeconds;

    private float rotationalSpeed;

	void Start ()
    {
        rotationalSpeed = 360 / dayLengthInSeconds;
	}
	
	void Update ()
    {
        transform.Rotate(rotationalSpeed * Time.deltaTime, 0, 0);
	}
}
