using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class unitychan_ikcontrol : MonoBehaviour
{
    public GameObject ModelObject;
    private Animator anim;

    /**** Foot IK ****/
    [SerializeField]
    private bool useIK = true;
    //��IK�ǽǶȤ��Є��ˤ��뤫�ɤ���
    [SerializeField]
    private bool useIKRot = true;
    //������Υ�������
    private float rightFootWeight = 0f;
    //������Υ�������
    private float leftFootWeight = 0f;
    //�������λ��
    private Vector3 rightFootPos;
    //�������λ��
    private Vector3 leftFootPos;
    //������νǶ�
    private Quaternion rightFootRot;
    //������νǶ�
    private Quaternion leftFootRot;
    //�����������ξ��x
    private float distance;
    //����򸶤�λ�äΥ��ե��åȂ�
    [SerializeField]
    private float offset = 0.1f;
    //�����饤��������λ��
    private Vector3 defaultCenter;
    //���쥤���w�Ф����x
    [SerializeField]
    private float rayRange = 1f;

    //�����饤����λ�ä��{������r�Υ��ԩ`��
    [SerializeField]
    private float smoothing = 100f;

    //���쥤���w�Ф�λ�ä��{����
    [SerializeField]
    private Vector3 rayPositionOffset = Vector3.up * 0.3f;
    // Start is called before the first frame update
    void Start()
    {
        anim = ModelObject.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnAnimatorIK()
    {
        //��IK��ʹ��ʤ����ϤϤ����Խ��ʤˤ⤷�ʤ�
        if (!useIK)
        {
            return;
        }

        //�����˥�`�����ѥ��`������IK�Υ������Ȥ�ȡ��
        rightFootWeight = 1f;
        leftFootWeight = 1f;
        //rightFootWeight = anim.GetFloat("RightFootWeight");
        //leftFootWeight = anim.GetFloat("LeftFootWeight");

        //�������äΥ쥤��ҕҙ��
        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);
        //�������äΥ쥤���w�Ф��I��
        var ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            rightFootPos = hit.point;

            //������IK���O��
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPos + new Vector3(0f, offset, 0f));
            if (useIKRot)
            {
                rightFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                anim.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRot);
            }
        }

        //�������äΥ쥤���w�Ф��I��
        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up);
        //�������äΥ쥤��ҕҙ��
        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            leftFootPos = hit.point;

            //������IK���O��
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPos + new Vector3(0f, offset, 0f));

            if (useIKRot)
            {
                leftFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
                anim.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRot);
            }
        }
    }
}
