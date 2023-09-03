using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCameraCollider : MonoBehaviour
{
    public int ColliderIndex;

    public CamerasController Cameras;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Cameras.SwitchCamera(ColliderIndex);
    }
}
