using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wapon_Controller : MonoBehaviour
{
    public GameObject Weapon_target;
    public GameObject Weapon_other;


    // Start is called before the first frame update
    void Start()
    { 
        Animator weapon_first_animator = Weapon_target.GetComponent<Animator>();
        Animator weapon_second_animator = Weapon_other.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void object_control(GameObject button)
    {
        
    }
}
