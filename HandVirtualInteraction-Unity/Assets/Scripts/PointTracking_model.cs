using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;
public class PointTracking_model : MonoBehaviour
{
    public UDPReceive_3d udpReceive;
    public GameObject Point;//旋转节点
    public GameObject Target;//指向节点
    public string point_label;//point标签
    public string target_label;//target标签

    public GameObject Hip;//mediapipe root
    public GameObject LeftHip;
    public GameObject RightHip;

    private List<string> point_str = new List<string>() { "Hip", "LeftHip", "RightHip"};
    private jointPoint[] jointPoints;


    //public float KalmanParamQ;
    //public float KalmanParamR;

    public class jointPoint
    {
        public int index;
        public Vector3 Pos3D = new Vector3();

        public Transform Transform = null;
        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public jointPoint Child = null;

        //Kalman
        public Vector3 Now3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }

    void Start()
    {
        point_str.Add(point_label);
        point_str.Add(target_label);

        jointPoints = new jointPoint[point_str.Count];
        //set index
        for (int i = 0; i < point_str.Count; i++)
        {
            jointPoints[i] = new jointPoint();
            jointPoints[i].index = i;
        }
        //trainsform
        jointPoints[point_str.IndexOf("Hip")].Transform = Hip.transform;
        jointPoints[point_str.IndexOf("LeftHip")].Transform = LeftHip.transform;
        jointPoints[point_str.IndexOf("RightHip")].Transform = RightHip.transform;
        jointPoints[point_str.IndexOf(point_label)].Transform = Point.transform;
        jointPoints[point_str.IndexOf(target_label)].Transform = Target.transform;

        //set child
        jointPoints[point_str.IndexOf(point_label)].Child = jointPoints[point_str.IndexOf(target_label)];

        //set Inverse and initrotation
        var forward = TriangleNormal(jointPoints[point_str.IndexOf("Hip")].Transform.position, jointPoints[point_str.IndexOf("LeftHip")].Transform.position, jointPoints[point_str.IndexOf("RightHip")].Transform.position);
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.InitRotation = jointPoint.Transform.rotation;
            }
            if (jointPoint.Child != null)
            {
                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.InitRotation;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        string data = udpReceive.data;
        ArrayList point_data = new ArrayList();
        ArrayList target_data = new ArrayList();
        string[] strArray = data.Split(new char[] { '{', '}', ':', '[', ']', '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);
        //point_label是否存在
        bool point_label_exist = false;
        //target_label是否存在
        bool target_label_exist = false;
        //label存在判断
        if (Array.IndexOf(strArray, point_label) != -1)
        {
            point_label_exist = true;
        }
        if (Array.IndexOf(strArray, target_label) != -1)
        {
            target_label_exist = true;
        }
        
        if (point_label_exist && target_label_exist)
        {
            //存在数据提取
            int op_point = Array.IndexOf(strArray, point_label);
            for (int i = 2; i <= 3 + 1; i++)
            {
                point_data.Add(strArray[op_point + i]);
            }
            int op_target = Array.IndexOf(strArray, target_label);
            for (int i = 2; i <= 3 + 1; i++)
            {
                target_data.Add(strArray[op_target + i]);
            }
            //数据表类型转换
            string[] arrString_point_data = (string[])point_data.ToArray(typeof(string));
            string[] arrString_target_data = (string[])target_data.ToArray(typeof(string));
            //关键点坐标提取
            float px = -(float.Parse(arrString_point_data[0]));
            float py = -(float.Parse(arrString_point_data[1]));
            float pz = -(float.Parse(arrString_point_data[2]));
            Vector3 root_point = new Vector3(px, py, pz);
            float tx = -(float.Parse(arrString_target_data[0]));
            float ty = -(float.Parse(arrString_target_data[1]));
            float tz = -(float.Parse(arrString_target_data[2]));
            Vector3 root_target = new Vector3(tx, ty, tz);

            root_point = jointPoints[point_str.IndexOf("Hip")].Transform.position + root_point;
            root_target = jointPoints[point_str.IndexOf("Hip")].Transform.position + root_target;
            //Points.transform.localPosition = root_hips + tarf_root + hip_tar;
            var forward = TriangleNormal(jointPoints[point_str.IndexOf("Hip")].Transform.position, jointPoints[point_str.IndexOf("LeftHip")].Transform.position, jointPoints[point_str.IndexOf("RightHip")].Transform.position);
            Point.transform.rotation = Quaternion.LookRotation(root_point - root_target, forward) * jointPoints[point_str.IndexOf(point_label)].InverseRotation;

        }
    }
    private Quaternion GetInverse(jointPoint p1, jointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }
    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    //void KalmanUpdate(jointPoint measurement)
    //{
    //    measurementUpdate(measurement);
    //    measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
    //    measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
    //    measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
    //    measurement.X = measurement.Pos3D;
    //}

    //void measurementUpdate(jointPoint measurement)
    //{
    //    measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
    //    measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
    //    measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);
    //    measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
    //    measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
    //    measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);
    //}
}
