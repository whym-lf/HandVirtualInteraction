using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RollingController : MonoBehaviour
{
    public UDPReceive_3d udpReceive;


    private Animator m_animator;

    public GameObject panel_first;
    public GameObject panel_second;

    private string action_key = "ActionKey";
    private string action_one_actrict;

    private string animation_name;
    void Start()
    {
        m_animator = GetComponent<Animator>();

        panel_first.SetActive(false);
        panel_second.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //palel切换控制
        string before_animation_name = animation_name;
        animation_name = getCurrentClipInfo();
        panel_active_chage(animation_name, before_animation_name);


        //udp数据接收
        string data = udpReceive.data;
        string[] strArray = data.Split(new char[] { '{', '}', ':', '[', ']', '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);
        bool label_exist = false;
        //动态手势label存在判断
        if (Array.IndexOf(strArray, action_key) != -1)
        {
            label_exist = true;
        }
        string action = "";
        if (label_exist)
        {
            int op_index = Array.IndexOf(strArray, action_key);
            action = strArray[op_index + 2];
        }

        //手势控制动画切换
        animation_control(action);

    }
    public string getCurrentClipInfo() // 获取当前执行的动画
    {
        AnimatorClipInfo[] m_CurrentClipInfo = gameObject.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0);
        return m_CurrentClipInfo[0].clip.name;
    }

    public void panel_active_chage(string animation_name, string before_animation_name)//panel active控制（防止按钮误触）
    {
        if (animation_name != before_animation_name)
        {
            if (animation_name == "RollingUp")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(false);
            }
            else if (before_animation_name == "RollingUp")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(false);
            }
            else if (animation_name == "RollingRight")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(true);
            }
            else if (before_animation_name == "RollingRight")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(false);
            }
            else if (animation_name == "RollingLeft")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(true);
            }
            else if (before_animation_name == "RollingLeft")
            {
                panel_first.SetActive(false);
                panel_second.SetActive(true);
            }
            else if (animation_name == "RollingDown_Idle")
            {
                panel_first.SetActive(true);
                panel_second.SetActive(false);
            }
            else if (before_animation_name == "RollingDown_Idle")
            {
                panel_first.SetActive(false);
                panel_second.SetActive(false);
            }
            else if (animation_name == "RollingDown_IdleLeft")
            {
                panel_first.SetActive(false);
                panel_second.SetActive(true);
            }
            else if (before_animation_name == "RollingDown_IdleLeft")
            {
                panel_first.SetActive(false);
                panel_second.SetActive(false);
            }
        }
    }

    public void animation_control(string action)//动态手势动画控制
    {
        if (action != "")
        {
            //操作唯一限制
            if (action_one_actrict != action)
            {
                action_one_actrict = action;
                //ui动画前置判断及动画触发
                if (action == "LeftFlick" && getCurrentClipInfo() == "RollingIdle")
                {
                    m_animator.SetTrigger("TriggerLeft");
                }
                else if (action == "RightFlick" && getCurrentClipInfo() == "RollingIdle_left")
                {
                    m_animator.SetTrigger("TriggerRight");
                }
                else if (action == "UpFlick" && getCurrentClipInfo() == "RollingUi_Inital")
                {
                    m_animator.SetTrigger("TriggerUp");
                }
                else if (action == "DownFlick" && (getCurrentClipInfo() == "RollingIdle_left" || getCurrentClipInfo() == "RollingIdle"))
                {
                    m_animator.SetTrigger("TriggerDown");
                }
            }
        }
    }


}