using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class testChill : MonoBehaviour
{
    public Transform trans;
    public GameObject gun;

    public GameObject hands;
    public GameObject Middle;
    public GameObject Index;

    public GameObject handweapon;
    public int flag=0;
    public string post;
    public string firetype;


    private Vector3 startposition_trans;
    private Vector3 startangle_trans;


    private void Start()
    {
        startposition_trans = trans.localPosition;
        startangle_trans = trans.localEulerAngles;
    }

    // Update is called once per frame

    void Update()
    {
        DistanceHand();
        GrabbingItems();
        DistanceIndex();
        if (flag == 1)
        {
            //trans.parent = this.transform;
            //trans.position = Vector3.zero;
            trans.parent = handweapon.transform;
            trans.localPosition = new Vector3(-0.1537f, 0.1267f, -0.703f);

            //trans.transform.localPosition = handweapon.transform.localPosition;
            //trans.transform.localPosition = new Vector3(-0.0524f,0.0138f,-0.0022f); //Vector3(-0.0524f,0.0138f,-0.0022f) Vector3(-0.05380f,0.0069f,-0.0838f)
            trans.transform.localEulerAngles = new Vector3(0f,-90f,0f); //Vector3(0f,-90f,0f) Vector3(0f,-150f,-90f)
            flag = 2;
        }

        if(flag == 3)
        {
            trans.parent = null;
            //trans.transform.localPosition = new Vector3(0.227500007f,1.07529998f,0.394f);
            //trans.transform.localEulerAngles =new Vector3(0f,0f,0f);

            trans.localPosition = startposition_trans;
            trans.localEulerAngles = startangle_trans;
            flag = 0;
        }
        if(firetype == "fire" && flag == 2)
        {

            gun.GetComponent<SimpleShoot>().Fire();
            //inputButton();
        }

    }

    private void GrabbingItems()
    {
        float DistanceGun;
        DistanceGun = (hands.transform.position - trans.transform.position).magnitude;
        if(DistanceGun<0.7 & flag==0 & post=="close")
        {
            flag = 1;
        }
        if(post=="open" & flag==2)
        {
            flag = 3;
        }
        //print(firetype + DistanceGun);
    }
    private void DistanceHand()
    {
        float DistanceMH;
        DistanceMH = (hands.transform.position - Middle.transform.position).magnitude;
        if(DistanceMH<0.07)
        {
            post="close";
        }
        if(DistanceMH>0.09)
        {
            post="open";
        }
    }
    private void DistanceIndex()
    {
        float DistanceIH;
        DistanceIH = (hands.transform.position - Index.transform.position).magnitude;
        if(DistanceIH>0.1)
        {
            firetype="stopfire";
            keybd_event(66, 0, 2, 0);
        }
        else if(DistanceIH<0.09)
        {
            firetype="fire";
        }
        //print(firetype+DistanceIH);
    }
    [DllImport("user32.dll", EntryPoint = "keybd_event")]
    static extern void keybd_event
    (
        byte bvk,
        byte bScan,
        int dwFlags,
        int dwExtraInfo
    );
    private void inputButton()
    {

        keybd_event(66, 0, 0, 0);

    }
}
