using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class PointTracking_ratio : MonoBehaviour
{
    //基于父节点的比例pozition计算
    public UDPReceive_3d udpReceive;
    public GameObject tar;//target point
    public GameObject tar_f;
    public string label;//标签
    public string label_f;

    private List<string> point_str = new List<string>();
    private jointPoint[] jointPoints;

    public bool limit_Kalman;
    public float KalmanParamQ;
    public float KalmanParamR;


    public class jointPoint
    {
        public int index;
        public Vector3 Pos3D = new Vector3();

        public Transform Transform = null;
        //public float ratio;//模型坐标模长

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
        point_str.Add(label);
        point_str.Add(label_f);
        jointPoints = new jointPoint[point_str.Count];
        //set index
        for (int i = 0; i < point_str.Count; i++)
        {
            jointPoints[i] = new jointPoint();
            jointPoints[i].index = i;
        }
        //trainsform
        //jointPoints[point_str.IndexOf(label)].Transform = tar.transform;
        //jointPoints[point_str.IndexOf(label_f)].Transform = tar_f.transform;

        //init rotation
        jointPoints[point_str.IndexOf(label)].InitRotation = tar.transform.rotation;
        jointPoints[point_str.IndexOf(label_f)].InitRotation = tar_f.transform.rotation;
        //set parent
        jointPoints[point_str.IndexOf(label)].Parent = jointPoints[point_str.IndexOf(label_f)];
        //ratio
        //jointPoints[point_str.IndexOf(label)].ratio = (jointPoints[point_str.IndexOf(label)].Transform.position - jointPoints[point_str.IndexOf(label)].Parent.Transform.position).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
        string data = udpReceive.data;
        ArrayList tar_data = new ArrayList();
        ArrayList tar_f_data = new ArrayList();
        string[] strArray = data.Split(new char[] { '{', '}', ':', '[', ']', '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);
        //point_label是否存在
        bool label_exist = false;
        //target_label是否存在
        bool label_f_exist = false;
        //label存在判断
        if (Array.IndexOf(strArray, label) != -1)
        {
            label_exist = true;
        }
        if (Array.IndexOf(strArray, label_f) != -1)
        {
            label_f_exist = true;
        }

        //wirst distance
        //distance label是否存在
        bool distancelabel_exist = false;
        string distance_label = "";
        if (label == "RightWrist")
        {
            distance_label = "RightHandDistance";
            //label存在判断
            if (Array.IndexOf(strArray, distance_label) != -1)
            {
                distancelabel_exist = true;
            }
        }
        else if (label == "LeftWrist")
        {
            distance_label = "LeftHandDistance";
            //label存在判断
            if (Array.IndexOf(strArray, distance_label) != -1)
            {
                distancelabel_exist = true;
            }
        }

        //label存在
        if (label_exist && label_f_exist)
        {
            //存在数据提取
            int op_point = Array.IndexOf(strArray, label);
            for (int i = 2; i <= 3 + 1; i++)
            {
                tar_data.Add(strArray[op_point + i]);
            }
            int op_target = Array.IndexOf(strArray, label_f);
            for (int i = 2; i <= 3 + 1; i++)
            {
                tar_f_data.Add(strArray[op_target + i]);
            }
            //get distance
            float distance = 0;
            if (distancelabel_exist)
            {
                int op_distance = Array.IndexOf(strArray, distance_label);
                distance = float.Parse(strArray[op_distance + 1]);
            }
            //数据表类型转换
            string[] arrString_tar_data = (string[])tar_data.ToArray(typeof(string));
            string[] arrString_tar_f_data = (string[])tar_f_data.ToArray(typeof(string));
            //关键点坐标提取
            float tx = (float.Parse(arrString_tar_data[0]));
            float ty = (float.Parse(arrString_tar_data[1]));
            float tz = (float.Parse(arrString_tar_data[2]));
            if (distancelabel_exist)
            {
                tz = -distance;
            }
            
            Vector3 hip_target = new Vector3(tx, ty, tz);
            float fx = (float.Parse(arrString_tar_f_data[0]));
            float fy = (float.Parse(arrString_tar_f_data[1]));
            float fz = (float.Parse(arrString_tar_f_data[2]));
            Vector3 hip_target_f = new Vector3(fx, fy, fz);
            jointPoints[point_str.IndexOf(label)].Pos3D = hip_target;
            jointPoints[point_str.IndexOf(label_f)].Pos3D = hip_target_f;

            //Kalman
            if(limit_Kalman == true)
            {
                foreach (jointPoint i in jointPoints)
                {
                    if (KalmanParamQ != 0 && KalmanParamR != 0)
                    {
                        KalmanUpdate(i);
                    }
                }
            }
            
            //旋转更新
            var dir1 = tar.transform.position - tar_f.transform.position;
            var dir2 = -(jointPoints[point_str.IndexOf(label)].Pos3D - jointPoints[point_str.IndexOf(label_f)].Pos3D);
            Quaternion rot = Quaternion.FromToRotation(dir1, dir2);
            Quaternion rot1 = tar_f.transform.rotation;
            tar_f.transform.rotation = Quaternion.Lerp(rot1, rot * rot1, 10);
            //坐标更新
            //this.transform.localPosition = dir2;
        }
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
}
