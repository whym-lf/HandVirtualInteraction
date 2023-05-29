using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class PointTracking_3d : MonoBehaviour
{
    public UDPReceive_3d udpReceive;
    public GameObject[] Points;//target point
    public GameObject Hips;//mediapipe root
    public string label;//标签
    public float finger_pointsinterval;//手指点间隔

    void Start()
    {
        
    }

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
        if (label_exist)
        {
            int op = Array.IndexOf(strArray, label);
            for (int i = 2; i <= 3 + 1; i++)
            {
                hand_data.Add(strArray[op + i]);
            }
            //get distance
            float distance = 0;
            if (distancelabel_exist)
            {
                int op_distance = Array.IndexOf(strArray, distance_label);
                distance = float.Parse(strArray[op_distance + 1]);
            }
            //数据表类型转换
            string[] arrString_hand_data = (string[])hand_data.ToArray(typeof(string));
            //Debug.Log("strdata lenth: " + arrString_hand_data.Length);

            for (int i = 0; i < Points.Length; i++)
            {
                GameObject tar_f = Points[i].transform.parent.gameObject;

                Vector3 root_hips = Hips.transform.position;
                Vector3 tarf_root = -(tar_f.transform.position);

                Vector3 before_localpositon = Points[i].transform.localPosition;//test code


                float x = -(float.Parse(arrString_hand_data[i]) * finger_pointsinterval);
                float y = -(float.Parse(arrString_hand_data[i + 1]) * finger_pointsinterval);
                float z = -(float.Parse(arrString_hand_data[i + 2]) * finger_pointsinterval);
                z = z + distance * finger_pointsinterval;
                Vector3 hip_tar = new Vector3(x, y, z);

                Points[i].transform.localPosition = root_hips + tarf_root + hip_tar;
            }
        }
    }
}
