
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{

    [Header("SpeedMovement")]
    [SerializeField]
    private float speedmovement;
    [SerializeField]
    private float rotationSpeed;

    private Animator playerAnimator;
    private Rigidbody rb;

    private DirectionsEnum direction = DirectionsEnum.NORTH;

    public CamerasController camerasController;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayerDirection();
        UpdatePlayerMovement();
    }

    void UpdatePlayerMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        playerAnimator.SetFloat("turnSpeed", horizontalInput);
        float verticalInput = Input.GetAxis("Vertical");
        playerAnimator.SetFloat("speed", verticalInput);
        Vector3 playermovement;
        //playermovement.Normalize();


        switch (direction)
        {
            default:
            case DirectionsEnum.NORTH:
                playermovement = new Vector3(horizontalInput, 0.0f, verticalInput) * speedmovement * Time.deltaTime;
                break;

            case DirectionsEnum.SOUTH:
                playermovement = new Vector3(-horizontalInput, 0.0f, -verticalInput) * speedmovement * Time.deltaTime;
                break;

            case DirectionsEnum.EAST:
                playermovement = new Vector3(verticalInput, 0.0f, -horizontalInput) * speedmovement * Time.deltaTime;
                break;

            case DirectionsEnum.WEST:
                playermovement = new Vector3(-verticalInput, 0.0f, horizontalInput) * speedmovement * Time.deltaTime;
                break;
        }

       transform.Translate(playermovement, Space.World);

        //rb.velocity = (playermovement * speedmovement);

        if (playermovement != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(playermovement, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);

        }


    }

    void UpdatePlayerDirection()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Debug.Log(horizontalInput + " " + verticalInput);

        if (Mathf.Abs(horizontalInput) < 1 && Mathf.Abs(verticalInput) < 1)
        {
            Camera currentCamera = camerasController.currentCamera;
            CameraController cameraController = currentCamera.GetComponent<CameraController>();
            DirectionsEnum cameraDirection = cameraController.direction;
            direction = cameraDirection;
        }
    }
}