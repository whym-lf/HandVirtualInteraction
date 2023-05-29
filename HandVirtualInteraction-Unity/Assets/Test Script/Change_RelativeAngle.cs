using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Change_RelativeAngle : MonoBehaviour
{
    public GameObject Point;
    public GameObject Target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = Target.transform.position - Point.transform.position;
        Quaternion ang = Quaternion.LookRotation(dir);
        Point.transform.rotation = ang;

        Debug.DrawRay(transform.position, transform.forward * 100, Color.blue);  //绘制正方体forward方向
        Debug.DrawRay(transform.position, dir, Color.green);  //绘制向量dir
        Debug.DrawRay(transform.position, ang.eulerAngles, Color.red);  //即：绘制正方体的旋转轴
    }
}

