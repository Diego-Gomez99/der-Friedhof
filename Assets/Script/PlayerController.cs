using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{

    [Header("SpeedMovement")]
    [SerializeField]
    private float speedmovement;

    private DirectionsEnum direction = DirectionsEnum.NORTH;

    public CamerasController camerasController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //UpdatePlayerDirection();
        UpdatePlayerMovement();
    }

    void UpdatePlayerMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 playermovement;

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

        transform.Translate(playermovement);
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
