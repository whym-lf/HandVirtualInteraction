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
    //　IKで角度を有効にするかどうか
    [SerializeField]
    private bool useIKRot = true;
    //　右足のウエイト
    private float rightFootWeight = 0f;
    //　左足のウエイト
    private float leftFootWeight = 0f;
    //　右足の位置
    private Vector3 rightFootPos;
    //　左足の位置
    private Vector3 leftFootPos;
    //　右足の角度
    private Quaternion rightFootRot;
    //　左足の角度
    private Quaternion leftFootRot;
    //　右足と左足の距離
    private float distance;
    //　足を付く位置のオフセット値
    [SerializeField]
    private float offset = 0.1f;
    //　コライダの中心位置
    private Vector3 defaultCenter;
    //　レイを飛ばす距離
    [SerializeField]
    private float rayRange = 1f;

    //　コライダの位置を調整する時のスピード
    [SerializeField]
    private float smoothing = 100f;

    //　レイを飛ばす位置の調整値
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
        //　IKを使わない場合はこれ以降なにもしない
        if (!useIK)
        {
            return;
        }

        //　アニメーションパラメータからIKのウエイトを取得
        rightFootWeight = 1f;
        leftFootWeight = 1f;
        //rightFootWeight = anim.GetFloat("RightFootWeight");
        //leftFootWeight = anim.GetFloat("LeftFootWeight");

        //　右足用のレイの視覚化
        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);
        //　右足用のレイを飛ばす処理
        var ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            rightFootPos = hit.point;

            //　右足IKの設定
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPos + new Vector3(0f, offset, 0f));
            if (useIKRot)
            {
                rightFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                anim.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRot);
            }
        }

        //　左足用のレイを飛ばす処理
        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up);
        //　左足用のレイの視覚化
        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            leftFootPos = hit.point;

            //　左足IKの設定
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
