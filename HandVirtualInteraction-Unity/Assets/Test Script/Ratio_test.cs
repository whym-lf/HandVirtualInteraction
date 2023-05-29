using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ratio_test : MonoBehaviour
{
    public GameObject parent;
    public GameObject child;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //var dir1 = child.transform.position - parent.transform.position;
        //var dir2 = this.transform.position - parent.transform.position;
        //success
        //Quaternion rot = Quaternion.FromToRotation(dir1, dir2);
        //Quaternion rot1 = parent.transform.rotation;
        //parent.transform.rotation = Quaternion.Lerp(rot1, rot * rot1, 10);


        float init_angle_z = Vector3.Angle(Vector3.right, new Vector3(parent.transform.right.normalized.x, parent.transform.right.normalized.y, 0));
        float init_angle_y = Vector3.Angle(Vector3.right, new Vector3(parent.transform.right.x, 0, parent.transform.right.z));


        Vector3 from = child.transform.position - parent.transform.position;
        Vector3 to = this.transform.position - parent.transform.position;
        float angle_z = Vector3.Angle(new Vector3(from.x, from.y, 0), new Vector3(to.x, to.y, 0));
        float angle_y = Vector3.Angle(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z));

        //parent.transform.rotation = Quaternion.AngleAxis(angle_z, transform.forward);
        //parent.transform.rotation = Quaternion.AngleAxis(angle_y, transform.up);

        //Vector3 from = child.transform.position - parent.transform.position;
        //Vector3 to = this.transform.position - parent.transform.position;





        //Vector3 diff = target.position - transform.position;
        //transform.LookAt(target);




    }
    public Vector3 RotateRound(Vector3 position, Vector3 center, Vector3 axis, float angle)
    {
        Vector3 point = Quaternion.AngleAxis(angle, axis) * (position - center);
        Vector3 resultVec3 = center + point;
        return resultVec3;
    }
    //void AutoRotateZRange(Transform t)
    //{
    //    int dir = 1;//方向值，控制来回旋转
    //    float axisZ = 0;//旋转的局部坐标z值
    //    if (!t) return;
    //    axisZ += 10f * Time.deltaTime * dir;
    //    if (axisZ >= 20f)
    //    {
    //        dir = -1;
    //    }
    //    if (axisZ <= -20f)
    //    {
    //        dir = 1;
    //    }
    //    axisZ = ClampAngle(axisZ, -20f, 20f);
    //    Quaternion quaternion = Quaternion.Euler(t.transform.localEulerAngles.x, t.transform.localEulerAngles.y, axisZ);
    //    t.transform.localRotation = quaternion;
    //}
    //float ClampAngle(float angle, float min, float max)
    //{
    //    if (angle < -360)
    //    {
    //        angle += 360;
    //    }
    //    if (angle > 360)
    //    {
    //        angle -= 360;
    //    }
    //    return Mathf.Clamp(angle, min, max);
    //}
}

