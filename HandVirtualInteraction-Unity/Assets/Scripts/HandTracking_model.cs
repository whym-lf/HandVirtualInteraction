using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;
public class HandTracking_model : MonoBehaviour
{
    public UDPReceive_3d udpReceive;
    public GameObject[] Points;//依照mediapipe handposition（21个点）
    public string label;//标签

    public GameObject Hip;//mediapipe root
    public GameObject LeftHip;
    public GameObject RightHip;

    private List<string> point_str = new List<string>() { "Hip", "LeftHip", "RightHip" };
    private jointPoint[] jointPoints;

    public class jointPoint
    {
        public int index;
        public Vector3 Pos3D = new Vector3();

        public Vector3 Pre3D = new Vector3();

        public Transform Transform = null;
        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public jointPoint Parent = null;
        public jointPoint Child = null;
    }

    void Start()
    {
        for(int i=0; i < Points.Length; i++)
        {
            string op = i.ToString();
            point_str.Add(label + op);
        }

        jointPoints = new jointPoint[point_str.Count];
        //init joinpoint and set index
        for (int i = 0; i < point_str.Count; i++)
        {
            jointPoints[i] = new jointPoint();
            jointPoints[i].index = i;
        }
        //transform
        jointPoints[point_str.IndexOf("Hip")].Transform = Hip.transform;
        jointPoints[point_str.IndexOf("LeftHip")].Transform = LeftHip.transform;
        jointPoints[point_str.IndexOf("RightHip")].Transform = RightHip.transform;
        jointPoints[point_str.IndexOf("Hip")].Pre3D = Hip.transform.position;
        jointPoints[point_str.IndexOf("LeftHip")].Pre3D = LeftHip.transform.position;
        jointPoints[point_str.IndexOf("RightHip")].Pre3D = RightHip.transform.position;
        for (int i = 3; i < point_str.Count; i++)
        {
            jointPoints[i].Transform = Points[i - 3].transform;
            jointPoints[i].Pre3D = Points[i - 3].transform.position;
        }
        //set child for five fingers

        jointPoints[4].Parent = jointPoints[3];
        jointPoints[8].Parent = jointPoints[3];
        jointPoints[12].Parent = jointPoints[3];
        jointPoints[16].Parent = jointPoints[3];
        jointPoints[20].Parent = jointPoints[3];
        int m = 4;
        for (int i = 0; i < 5; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                jointPoints[m].Child = jointPoints[m + 1];
                jointPoints[m + 1].Parent = jointPoints[m];
                m += 1;
            }
            m += 1;
        }
        //set parent for five finger
        //for(int i = 4; i < jointPoints.Length; i++)
        //{
        //    jointPoints[i].Parent = jointPoints
        //}

        //set Inverse and initrotation


        var forward = TriangleNormal(jointPoints[point_str.IndexOf("Hip")].Transform.position, jointPoints[point_str.IndexOf("LeftHip")].Transform.position, jointPoints[point_str.IndexOf("RightHip")].Transform.position);
        // hip
        foreach (var jointPoint in jointPoints[0..3])
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
        //hand
        var wrist = jointPoints[3];
        var wrist_forward = TriangleNormal(wrist.Transform.position, jointPoints[8].Transform.position, jointPoints[20].Transform.position);
        foreach (var jointPoint in jointPoints[3..])
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.InitRotation = jointPoint.Transform.rotation;
            }
            if (jointPoint.Child != null && jointPoint.Parent != null)
            {
                Vector3 point_normal = Vector3.Cross(jointPoint.Transform.position - jointPoint.Parent.Transform.position, jointPoint.Transform.position - jointPoint.Child.Transform.position);
                Vector3 point_target_normal = Vector3.Cross(jointPoint.Transform.position - jointPoint.Child.Transform.position, point_normal).normalized;
                //var point_normal = TriangleNormal(jointPoint.Transform.position, jointPoint.Parent.Transform.position, jointPoint.Child.Transform.position);
                //var point_target_normal = TriangleNormal(jointPoint.Transform.position, jointPoint.Child.Transform.position, point_normal);

                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, point_target_normal);
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.InitRotation;
            }
        }
        //wrist
        var wirst_to = (wrist.Transform.position - jointPoints[8].Transform.position) + (wrist.Transform.position - jointPoints[20].Transform.position);
        wrist.InitRotation = wrist.Transform.rotation;
        wrist.Inverse = Quaternion.Inverse(Quaternion.LookRotation(wrist.Transform.position - wirst_to, wrist_forward));
        wrist.InverseRotation = wrist.Inverse * wrist.InitRotation;
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
            //data import
            for (int i = 0; i < Points.Length; i++)
            {
                float x = (float.Parse(arrString_hand_data[i * 3]) - float.Parse(arrString_hand_data[0 * 3]));
                float y = (float.Parse(arrString_hand_data[i * 3 + 1]) - float.Parse(arrString_hand_data[0 * 3 + 1]));
                float z = (float.Parse(arrString_hand_data[i * 3 + 2]) - float.Parse(arrString_hand_data[0 * 3 + 2]));
                Vector3 root_tar = new Vector3(x, y, z);
                root_tar = transform.position + root_tar;
                jointPoints[i + 3].Pos3D = root_tar;
            }
            //point rotation
            var wrist = jointPoints[3];
            var wrist_forward = TriangleNormal(wrist.Pos3D, jointPoints[8].Pos3D, jointPoints[20].Pos3D);
            var wrist_to = (wrist.Pos3D - jointPoints[8].Pos3D) + (wrist.Pos3D - jointPoints[20].Pos3D);
            for (int i = 1; i < Points.Length; i++)
            {
                if (jointPoints[i + 3].Child != null && jointPoints[i+3].Parent != null)
                {
                    //
                    Vector3 point_normal = Vector3.Cross(jointPoints[i + 3].Pos3D - jointPoints[i + 3].Parent.Pos3D, jointPoints[i + 3].Pos3D - jointPoints[i + 3].Child.Pos3D);
                    Vector3 point_target_normal = Vector3.Cross(jointPoints[i + 3].Pos3D - jointPoints[i + 3].Child.Pos3D, point_normal).normalized;
                    //Vector3 point_normal = TriangleNormal(jointPoints[i + 3].Pos3D, jointPoints[i + 3].Parent.Pos3D, jointPoints[i + 3].Child.Pos3D);
                    //Vector3 point_target_normal = TriangleNormal(jointPoints[i + 3].Pos3D, jointPoints[i + 3].Child.Pos3D, point_normal);
                    //lookrotation
                    Points[i].transform.rotation = Quaternion.LookRotation(-jointPoints[i + 3].Pos3D + jointPoints[i + 3].Child.Pos3D, point_target_normal) * jointPoints[i + 3].InverseRotation;
                }
            }
            //wrist rotation
            Points[0].transform.rotation = Quaternion.LookRotation(wrist_to, wrist_forward) * wrist.InverseRotation;


            //pre3d update
            foreach (jointPoint i in jointPoints)
            {
                i.Pre3D = i.Pos3D;
            }
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
}

