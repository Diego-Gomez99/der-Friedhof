using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerasController : MonoBehaviour
{
    public Camera currentCamera;
    
    public void SwitchCamera(Camera newCamera)
    {
        currentCamera.enabled = false;
        currentCamera.GetComponent<AudioListener>().enabled = false;

        newCamera.enabled = true;
        newCamera.GetComponent<AudioListener>().enabled = true;

        currentCamera = newCamera;
        
    }
}
