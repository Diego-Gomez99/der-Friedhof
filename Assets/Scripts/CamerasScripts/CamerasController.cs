using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerasController : MonoBehaviour
{
    public Camera currentCamera;

    public void SwitchCamera(Camera newCamera)
    {
        currentCamera.enabled = false;
        newCamera.enabled = true;
        currentCamera = newCamera;
    }
}
