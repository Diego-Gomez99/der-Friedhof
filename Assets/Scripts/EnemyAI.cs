using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Enemy : MonoBehaviour
{
    public float detectionDistance = 5.0f;
    public float maxMovementSpeed = 2.0f;
    public float minMovementSpeed = 0.5f;
    public float rotationSpeed = 5.0f;
    public float fadeDuration = 2.0f;

    private Rigidbody rb;
    private Transform target;
    private bool isDead = false;
    private Renderer enemyRenderer;
    private CapsuleCollider capsuleCollider;

    [Header("Volume")]
    [SerializeField]
    private float audiovolume;


    private AudioSource myaudio;
    [SerializeField]
    private AudioClip chasesong;

    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        enemyRenderer = GetComponent<Renderer>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        myaudio = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isDead) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= detectionDistance)
        {
            float speedMultiplier = Mathf.Clamp01(1 - (distanceToTarget / detectionDistance));
            float currentMovementSpeed = Mathf.Lerp(minMovementSpeed, maxMovementSpeed, speedMultiplier);
            Vector3 directionToPlayer = (target.position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            rb.velocity = directionToPlayer * currentMovementSpeed;

            if(!myaudio.isPlaying)
            {
                myaudio.PlayOneShot(chasesong, audiovolume);
                animator.SetBool("Persiguiendo", true);
            }
        }
        else
        {
            myaudio.Stop();
            animator.SetBool("Persiguiendo", false);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable death colliders
        capsuleCollider.enabled = false;
        transform.Find("weapon").GetComponent<BoxCollider>().enabled = false;

        // Disable further updates and interactions.
        // enabled = false;

        // Start the fade-out coroutine.
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        //Color startColor = enemyRenderer.material.color;
        //Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;
            //enemyRenderer.material.color = Color.Lerp(startColor, endColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the material becomes completely transparent.
        //enemyRenderer.material.color = endColor;

        // Alternatively, you can use Destroy(gameObject) to remove the enemy from the scene.
        Destroy(gameObject);
    }

}
