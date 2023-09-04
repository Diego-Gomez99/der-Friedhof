using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{

    [Header("SpeedMovement")]
    [SerializeField]
    private float speedmovement;

    public CamerasController camerasController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera currentCamera = camerasController.currentCamera;
        CameraController cameraController = currentCamera.GetComponent<CameraController>();
        DirectionsEnum direction = cameraController.direction;

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

        transform.Translate (playermovement);
    }
}
