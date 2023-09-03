using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerasController : MonoBehaviour
{

    [SerializeField]
    private Camera[] cameras;
  
    private int currentCameraIndex = 0;


   public void SwitchCamera(int newIndex)
    {
        cameras[currentCameraIndex].gameObject.SetActive(false);

        cameras[newIndex].gameObject.SetActive(true);

        currentCameraIndex = newIndex;

    }
}
