using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{

    [Header("SpeedMovement")]
    [SerializeField]
    private float speedmovement;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");


        Vector3 playermovement = new Vector3(horizontalInput, 0.0f, verticalInput) * speedmovement * Time.deltaTime;


        transform.Translate (playermovement);
    }
}
