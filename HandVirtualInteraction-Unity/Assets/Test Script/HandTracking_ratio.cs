using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;
public class HandTracking_ratio : MonoBehaviour
{    
    //基于父节点的比例pozition计算
    public UDPReceive_3d udpReceive;
    public GameObject[] ModelPoints;//model point(21)
    public string label;//标签


    private List<string> point_str = new List<string>();
    private jointPoint[] jointPoints;

    public bool limit_angle;
    public bool limit_Kalman;
    public float KalmanParamQ;
    public float KalmanParamR;

    //限制角度
    private float limit_max_angle = 10;
    private float limit_min_angle = -10;
    public class jointPoint
    {
        public int index;
        public Vector3 Pos3D = new Vector3();

        public Transform Transform = null;

        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public jointPoint Parent = null;

        //Kalman
        public Vector3 Now3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();


    }
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < ModelPoints.Length; i++)
        {
            string op = i.ToString();
            point_str.Add(label + op);
        }

        jointPoints = new jointPoint[point_str.Count];
        //init joinpoint and set index、transform
        for (int i = 0; i < point_str.Count; i++)
        {
            jointPoints[i] = new jointPoint();
            jointPoints[i].index = i;
            jointPoints[i].Transform = ModelPoints[i].transform;
            jointPoints[i].InitRotation = ModelPoints[i].transform.rotation;
        }
        //set parent
        jointPoints[1].Parent = jointPoints[0];
        jointPoints[5].Parent = jointPoints[0];
        jointPoints[9].Parent = jointPoints[0];
        jointPoints[13].Parent = jointPoints[0];
        jointPoints[17].Parent = jointPoints[0];
        int m = 2;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                jointPoints[m].Parent = jointPoints[m - 1];
                m += 1;
            }
            m += 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        string data = udpReceive.data;
        ArrayList hand_data = new ArrayList();
        string[] strArray = data.Split(new char[] { '{', '}', ':', '[', ']', '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);

        //label是否存在
        bool label_exist = false;
        //label存在判断
        if (Array.IndexOf(strArray, label) != -1)
        {
            label_exist = true;
        }

        //label存在
        if (label_exist)
        {
            int op = Array.IndexOf(strArray, label);
            for (int i = 2; i <= 21 * 3 + 1; i++)
            {
                hand_data.Add(strArray[op + i]);
            }
            //数据表类型转换
            string[] arrString_hand_data = (string[])hand_data.ToArray(typeof(string));
            //关键点坐标提取
            for (int i = 0; i < ModelPoints.Length; i++)
            {
                float x = (float.Parse(arrString_hand_data[i * 3]) - float.Parse(arrString_hand_data[0 * 3]));
                float y = (float.Parse(arrString_hand_data[i * 3 + 1]) - float.Parse(arrString_hand_data[0 * 3 + 1]));
                float z = (float.Parse(arrString_hand_data[i * 3 + 2]) - float.Parse(arrString_hand_data[0 * 3 + 2]));
                Vector3 wrist_tar = new Vector3(x, y, z);
                jointPoints[i].Pos3D = wrist_tar;
            }
            //Kalman
            if (limit_Kalman == true)
            {
                foreach (jointPoint i in jointPoints)
                {
                    if (KalmanParamQ != 0 && KalmanParamR != 0)
                    {
                        KalmanUpdate(i);
                    }
                }
            }
            //坐标更新
            for (int i = 1;i < ModelPoints.Length; i++)
            {
                var dir1 = ModelPoints[i].transform.position - ModelPoints[i].transform.parent.gameObject.transform.position;
                var dir2 = -(jointPoints[i].Pos3D - jointPoints[i].Parent.Pos3D);
                Quaternion rot = Quaternion.FromToRotation(dir1, dir2);
                Quaternion rot1 = ModelPoints[i].transform.parent.gameObject.transform.rotation;
                ModelPoints[i].transform.parent.gameObject.transform.rotation = Quaternion.Lerp(rot1, rot * rot1, 10);
            }
            if (limit_angle == true)
            {
                //limit angle？（限制某个轴的旋转角度？？？？？）
                for (int i = 1; i < ModelPoints.Length; i++)
                {
                    float angle_x = ModelPoints[i].transform.localEulerAngles.x;
                    float angle_y = ModelPoints[i].transform.localEulerAngles.y;
                    float angle_z = ModelPoints[i].transform.localEulerAngles.z;
                    angle_x = CheckAngle(angle_x);
                    angle_x = Mathf.Clamp(angle_x, limit_min_angle, limit_max_angle);
                    ModelPoints[i].transform.localEulerAngles = new Vector3(angle_x, angle_y, angle_z);
                }
            }
        }
        if (limit_angle == true)
        {
            //limit angle？（限制某个轴的旋转角度？？？？？）
            for (int i = 1; i < ModelPoints.Length; i++)
            {
                float angle_x = ModelPoints[i].transform.localEulerAngles.x;
                float angle_y = ModelPoints[i].transform.localEulerAngles.y;
                float angle_z = ModelPoints[i].transform.localEulerAngles.z;
                angle_x = CheckAngle(angle_x);
                angle_x = Mathf.Clamp(angle_x, limit_min_angle, limit_max_angle);
                ModelPoints[i].transform.localEulerAngles = new Vector3(angle_x, angle_y, angle_z);
                //LimitViewX(ModelPoints[i], -0.4f, 0.4f);
            }
        }

    }
    public float CheckAngle(float value)
    {
        //value = value % 360;
        //float angle = value - 180;
        //if (angle > 0)
        //    return angle - 180;
        //return angle + 180;
        
        value = value % 360;
        float angle = value;
        if(angle > 90)
        {
            return angle - 180;
        }
        else if(angle < -90)
        {
            return angle + 180;
        }
        return angle;
    }
    public void KalmanUpdate(jointPoint measurement)
    {
        measurementUpdate(measurement);
        measurement.Pos3D.x = measurement.X.x + (measurement.Pos3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Pos3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Pos3D.z - measurement.X.z) * measurement.K.z;
        measurement.X = measurement.Pos3D;
    }

    public void measurementUpdate(jointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
        measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
        measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);
        measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
        measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
        measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);
    }
    
    public void LimitViewX(GameObject Object, float minView, float MaxView)
    {
        float rotationX = Mathf.Clamp(Object.transform.rotation.x, minView, MaxView);
        Quaternion a = new Quaternion(rotationX, Object.transform.rotation.y, Object.transform.rotation.z, Object.transform.rotation.w);
        Object.transform.rotation = a;
    }

}
