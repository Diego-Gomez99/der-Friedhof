using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadFalling : MonoBehaviour
{

    [SerializeField]
    private GameObject headObject;
    private Rigidbody headRigidbody;

    // Start is called before the first frame update
    void Start()
    {
        headRigidbody = headObject.GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            headRigidbody.useGravity = true;
            Destroy(this.gameObject);
        }
    }
}
