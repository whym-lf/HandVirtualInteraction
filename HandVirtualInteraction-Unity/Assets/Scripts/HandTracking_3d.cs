using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class HandTracking_3d : MonoBehaviour
{
    public UDPReceive_3d udpReceive;
    public GameObject[] Points;//����mediapipe handposition����wrist����20���㣩
    public string label;//��ǩ
    public float finger_pointsinterval;//��ָ����

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        string data = udpReceive.data;
        ArrayList hand_data = new ArrayList();
        string[] strArray = data.Split(new char[] { '{', '}', ':', '[', ']', '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);

        //label�Ƿ����
        bool label_exist = false;
        //label�����ж�
        if (Array.IndexOf(strArray, label) != -1)
        {
            label_exist = true;
        }

        //label����
        if (label_exist)
        {
            int op = Array.IndexOf(strArray, label);
            for (int i = 2; i <= Points.Length * 3 + 1; i++)
            {
                hand_data.Add(strArray[op + i]);
            }
            //���ݱ�����ת��
            string[] arrString_hand_data = (string[])hand_data.ToArray(typeof(string));
            //Debug.Log("strdata lenth: " + arrString_hand_data.Length);

            for (int i = 1; i < Points.Length; i++)
            {
                Vector3 tar_f = -(Points[i].transform.parent.gameObject.transform.position);
                Vector3 root_wrist = this.transform.position;
                
                float x = -(float.Parse(arrString_hand_data[i * 3]) - float.Parse(arrString_hand_data[0 * 3])) * finger_pointsinterval;
                float y = -(float.Parse(arrString_hand_data[i * 3 + 1]) - float.Parse(arrString_hand_data[0 * 3 + 1])) * finger_pointsinterval;
                float z = -(float.Parse(arrString_hand_data[i * 3 + 2]) - float.Parse(arrString_hand_data[0 * 3 + 2])) * finger_pointsinterval;
                Vector3 wrist_tar = new Vector3(x, y, z);

                Points[i].transform.localPosition = tar_f + root_wrist + wrist_tar;
            }
        }
    }
}
