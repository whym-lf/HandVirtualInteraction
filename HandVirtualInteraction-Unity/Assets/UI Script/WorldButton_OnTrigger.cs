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

    //�����ײ
    private void OnTriggerEnter(Collider collision)
    {
        //�ж���ײ��Ϊ˫��
        if(collision.gameObject.name == "Character1_RightHand" || collision.gameObject.name == "Character1_LeftHand")
        {
            //��ײʱ��������Ϣ
            Debug.Log("��ײ����" + collision.gameObject.name);

            //��ť����UI�ж������ƣ��ر�UI
            World_UI.GetComponent<RollingController>().animation_control("DownFlick");

            //�жϵ�ǰ�����active״̬
            if(weapon_target_animator.GetBool(weapon_target_animator.parameters[0].name) == false)
            {
                //Ŀ�궯��״̬�޸�Ϊ�ɼ�������Ŀ���޸�Ϊ���ɼ�
                weapon_target_animator.SetBool(weapon_target_animator.parameters[0].name, true);
                foreach (Animator one in weapon_others_animator)
                {
                    one.SetBool(one.parameters[0].name, false);
                }
            }
            //����ǰ�����״̬Ϊ�ɼ������޸�Ϊ��
            else if(weapon_target_animator.GetBool(weapon_target_animator.parameters[0].name) == true)
            {
                //Ŀ�궯��״̬�޸�Ϊ�ɼ�������Ŀ���޸�Ϊ���ɼ�
                weapon_target_animator.SetBool(weapon_target_animator.parameters[0].name, false);
            }
            
        }
    }

}
