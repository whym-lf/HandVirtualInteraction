using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Limit_Angle : MonoBehaviour
{
    public GameObject point;
    public float limit_max_angle;
    public float limit_min_angle;
    private Transform init_transform;
    // Start is called before the first frame update
    void Start()
    {
        init_transform = point.transform;
    }

    // Update is called once per frame
    void Update()
    {
        float angle_x = point.transform.localEulerAngles.x;
        float angle_y = point.transform.localEulerAngles.y;
        float angle_z = point.transform.localEulerAngles.z;
        angle_x = CheckAngle(angle_x);
        //angle_y = CheckAngle(angle_y);
        //angle_z = CheckAngle(angle_z);
        angle_x = Mathf.Clamp(angle_x, limit_min_angle, limit_max_angle);
        //angle_y = Mathf.Clamp(angle_y, limit_min_angle, limit_max_angle);
        //angle_z = Mathf.Clamp(angle_z, limit_min_angle, limit_max_angle);

        point.transform.localEulerAngles = new Vector3(angle_x, angle_y, angle_z);
    }

    public float CheckAngle(float value)
    {
        float angle = value - 180;
        if (angle > 0)
            return angle - 180;
        return angle + 180;
    }
}
