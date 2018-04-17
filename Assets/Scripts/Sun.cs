using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour {

    public float dayLengthInSeconds;
    public float nightLengthInSeconds;

    private float dailyRotationalSpeed;
    private float nightlyRotationalSpeed;

    void Start ()
    {
        dailyRotationalSpeed = 180 / dayLengthInSeconds;
        nightlyRotationalSpeed = 180 / nightLengthInSeconds;
    }
	
	void Update ()
    {
        if(transform.rotation.eulerAngles.x >= 0 && transform.rotation.eulerAngles.x <= 180)
            transform.Rotate(dailyRotationalSpeed * Time.deltaTime, 0, 0);
        else
            transform.Rotate(nightlyRotationalSpeed * Time.deltaTime, 0, 0);
    }
}
