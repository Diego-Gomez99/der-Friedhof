
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour
{

    [Header("SpeedMovement")]
    [SerializeField]
    private float speedmovement;
    [SerializeField]
    private float rotationSpeed;

    [Header("Death")]
    [SerializeField]
    private float gameOverDelay = 3.5f; // Seconds the death animation plays before the Game Over panel shows.
    [SerializeField]
    private AudioClip deathSound;
    [SerializeField]
    private float deathSoundStartOffset = 0.0f; // Seconds skipped at the start of the clip so the scream lands with the hit.
    [SerializeField]
    private AudioClip slashImpactSound; // Played by the full-screen slash effect on top of its own slice sound.


    private Animator playerAnimator;
    private Rigidbody rb;
    private AudioSource audioSource; // Shared with WalkSteps; missing in scenes without footsteps.

    private DirectionsEnum direction = DirectionsEnum.NORTH;
    private float disableDistance = 10.0f; // The distance within which enemies should be disabled.
    private int safeSpotsInside; // SS triggers the player is standing in. Counted, because two of them can overlap.


    public CamerasController camerasController;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
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
        if (GameManager.instance.isGameOver)
        {
            rb.velocity = Vector3.zero;
            playerAnimator.SetFloat("turnSpeed", 0.0f);
            playerAnimator.SetFloat("speed", 0.0f);
            return;
        }

        UpdatePlayerMovement();

        // Checked here instead of in OnTriggerStay, which stops firing while the player stands still and its Rigidbody sleeps.
        if (safeSpotsInside > 0)
        {
            KillNearbyEnemies();
        }
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
        // Standing in an SS (under a green lamp) keeps killing the enemies around the player, see FixedUpdate.
        if (other.CompareTag("SS"))
        {
            safeSpotsInside++;
            KillNearbyEnemies();
        }
        else if (other.CompareTag("Enemy"))
        {
            // The player touched an object with the "Enemy" tag.
            // End the game or perform game over actions here.
            EndGame(other.GetComponentInParent<Enemy>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SS"))
        {
            safeSpotsInside = Mathf.Max(0, safeSpotsInside - 1);
        }
    }

    private void KillNearbyEnemies()
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

    // Custom method to end the game.
    private void EndGame(Enemy killer)
    {
        // The enemy's weapon and body colliders can both fire the trigger.
        if (GameManager.instance.isGameOver)
        {
            return;
        }

        GameManager.instance.isGameOver = true;
        Vector3 attackerPosition = killer != null ? killer.transform.position : transform.position - transform.forward;
        if (killer != null)
        {
            killer.Attack();
        }
        SlashEffect.Play(slashImpactSound, transform.position + Vector3.up, attackerPosition + Vector3.up);
        playerAnimator.SetTrigger("die");
        if (audioSource != null && deathSound != null)
        {
            // Footsteps use PlayOneShot on this source, so taking over its clip does not affect them.
            audioSource.clip = deathSound;
            audioSource.time = Mathf.Clamp(deathSoundStartOffset, 0.0f, deathSound.length - 0.01f);
            audioSource.Play();
        }
        StartCoroutine(ShowGameOverAfterDeath());
    }

    private IEnumerator ShowGameOverAfterDeath()
    {
        yield return new WaitForSeconds(gameOverDelay);
        GameManager.instance.GameOver();
    }

    private void LoadPlayerPosition()
    {
        float playerPosX = PlayerPrefs.GetFloat("PlayerPosX");
        float playerPosY = PlayerPrefs.GetFloat("PlayerPosY");
        float playerPosZ = PlayerPrefs.GetFloat("PlayerPosZ");

        transform.position = new Vector3(PlayerPrefs.GetFloat("PlayerPosX"), PlayerPrefs.GetFloat("PlayerPosY"), PlayerPrefs.GetFloat("PlayerPosZ"));
        
    }
}
