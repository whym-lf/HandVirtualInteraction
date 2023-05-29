using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldButton_OnTrigger : MonoBehaviour
{
    public GameObject World_UI;
    public CapsuleCollider lhand_collider;
    public CapsuleCollider rhand_collider;


    public GameObject Weapon_target;
    public GameObject[] Weapon_others;

    private Animator weapon_target_animator;
    private Animator[] weapon_others_animator;
    void Start()
    {
        weapon_others_animator = new Animator[Weapon_others.Length];
        weapon_target_animator = Weapon_target.GetComponent<Animator>();
        for (int i = 0; i < Weapon_others.Length; i++)
        {
            this.weapon_others_animator[i] = Weapon_others[i].GetComponent<Animator>();
        }
    }

    void Update()
    {
        
    }

    //检测碰撞
    private void OnTriggerEnter(Collider collision)
    {
        //判断碰撞体为双手
        if(collision.gameObject.name == "Character1_RightHand" || collision.gameObject.name == "Character1_LeftHand")
        {
            //碰撞时触发的消息
            Debug.Log("碰撞到了" + collision.gameObject.name);

            //按钮触发UI中动画控制，关闭UI
            World_UI.GetComponent<RollingController>().animation_control("DownFlick");

            //判断当前物体的active状态
            if(weapon_target_animator.GetBool(weapon_target_animator.parameters[0].name) == false)
            {
                //目标动画状态修改为可见，其余目标修改为不可见
                weapon_target_animator.SetBool(weapon_target_animator.parameters[0].name, true);
                foreach (Animator one in weapon_others_animator)
                {
                    one.SetBool(one.parameters[0].name, false);
                }
            }
            //若当前物体的状态为可见，则修改为否
            else if(weapon_target_animator.GetBool(weapon_target_animator.parameters[0].name) == true)
            {
                //目标动画状态修改为可见，其余目标修改为不可见
                weapon_target_animator.SetBool(weapon_target_animator.parameters[0].name, false);
            }
            
        }
    }

}
