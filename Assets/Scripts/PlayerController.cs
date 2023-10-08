
using UnityEngine;
using UnityEngine.SceneManagement;


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
    private float disableDistance = 10.0f; // The distance within which enemies should be disabled.


    public CamerasController camerasController;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        //GameManager.instance.RespawnPlayer(gameObject);
        //LoadPlayerPosition();
    }

    // Start is called before the first frame update
    void Start()
    {
        // gameObject.transform.position = new Vector3(PlayerPrefs.GetFloat("PlayerPosX"), PlayerPrefs.GetFloat("PlayerPosY"), PlayerPrefs.GetFloat("PlayerPosZ"));
        // Debug.Log(PlayerPrefs.GetFloat("PlayerPosX"));
        // Debug.Log(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void FixedUpdate()
    {
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
            case DirectionsEnum.NORTH: playermovement = new Vector3(horizontalInput, 0.0f, verticalInput);   break;
            case DirectionsEnum.SOUTH: playermovement = new Vector3(-horizontalInput, 0.0f, -verticalInput); break;
            case DirectionsEnum.EAST:  playermovement = new Vector3(verticalInput, 0.0f, -horizontalInput);  break;
            case DirectionsEnum.WEST:  playermovement = new Vector3(-verticalInput, 0.0f, horizontalInput);  break;
        }


        // transform.Translate(playermovement * speedmovement * Time.deltaTime, Space.World);
        rb.velocity = (playermovement * speedmovement);

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

        if (Mathf.Abs(horizontalInput) < 1 && Mathf.Abs(verticalInput) < 1)
        {
            Camera currentCamera = camerasController.currentCamera;
            CameraController cameraController = currentCamera.GetComponent<CameraController>();
            DirectionsEnum cameraDirection = cameraController.direction;
            direction = cameraDirection;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object the player touched has the "SS" tag.
        if (other.CompareTag("SS"))
        {
            // Find all enemy objects in the scene.
            Enemy[] enemies = FindObjectsOfType<Enemy>();

            // Iterate through the enemy objects and disable them if they are within the specified distance.
            foreach (Enemy enemy in enemies)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy < disableDistance)
                {
                    enemy.Die();
                }
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            // The player touched an object with the "Enemy" tag.
            // End the game or perform game over actions here.
            EndGame();
        }
    }

    // Custom method to end the game.
    private void EndGame()
    {
        // You can load the game over scene or perform other game-over actions.
        GameManager.instance.isGameOver = true;
    }

    private void LoadPlayerPosition()
    {
        float playerPosX = PlayerPrefs.GetFloat("PlayerPosX");
        float playerPosY = PlayerPrefs.GetFloat("PlayerPosY");
        float playerPosZ = PlayerPrefs.GetFloat("PlayerPosZ");

        transform.position = new Vector3(PlayerPrefs.GetFloat("PlayerPosX"), PlayerPrefs.GetFloat("PlayerPosY"), PlayerPrefs.GetFloat("PlayerPosZ"));
        
    }
}