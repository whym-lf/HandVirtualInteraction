using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meshcollider_control : MonoBehaviour
{
    private SkinnedMeshRenderer weaponMeshRender;
    private MeshCollider weaponMeshCollider;
    // Start is called before the first frame update
    void Start()
    {
        weaponMeshCollider = GetComponent<MeshCollider>();
        weaponMeshRender = GetComponent<SkinnedMeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMesh();
    }
    private void UpdateMesh()
    {
        // weapon mesh
        Mesh weaponColliderMesh = new Mesh();
        weaponMeshRender.BakeMesh(weaponColliderMesh);
        
        weaponMeshCollider.sharedMesh = null;
        weaponMeshCollider.sharedMesh = weaponColliderMesh;
    }
}
