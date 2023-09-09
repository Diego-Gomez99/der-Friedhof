using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LampEffect : MonoBehaviour
{
    [SerializeField]
    private Light pointlight;

    private void OnTriggerEnter(Collider other)
    {
            
        if(other.CompareTag("Enemy"))
        {
            pointlight.enabled = false;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            pointlight.enabled = true;
        }
    }
}
