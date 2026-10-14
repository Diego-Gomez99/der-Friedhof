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

    [Header("Weapon arm")]
    // Point of the scythe handle that sits in the right hand, in the weapon's local space.
    [SerializeField]
    private Vector3 weaponGripPoint = new Vector3(-0.25f, 4.31f, -0.92f);
    // Degrees the right arm opens outward (to the enemy's right) from straight ahead.
    [SerializeField]
    private float armOpenAngle = 45.0f;
    // Degrees the scythe turns to the enemy's right around the vertical axis.
    [SerializeField]
    private float weaponTurnAngle = 12.0f;

    private Transform weapon;
    private BoxCollider weaponCollider;
    private Vector3 weaponRestLocalPosition;
    private Quaternion weaponRestLocalRotation;
    private Vector3 weaponColliderRestCenter;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;

    // While attacking, the clip moves the arm and the scythe keeps its pose relative to the hand.
    private bool isAttacking = false;
    private Quaternion weaponRotationInHand;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        enemyRenderer = GetComponent<Renderer>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        myaudio = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        weapon = transform.Find("weapon");
        weaponCollider = weapon.GetComponent<BoxCollider>();
        weaponRestLocalPosition = weapon.localPosition;
        weaponRestLocalRotation = weapon.localRotation;
        weaponColliderRestCenter = weaponCollider.center;
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private void Update()
    {
        if (isDead || isAttacking) return;

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

    // Runs after the Animator, so it overrides the clip's pose for the right arm:
    // the arm points ahead, opened slightly outward, and the scythe follows the hand.
    // The scythe's kill trigger stays where it was authored, at the player's height.
    private void LateUpdate()
    {
        if (rightUpperArm == null || rightLowerArm == null || rightHand == null) return;

        if (isAttacking)
        {
            weapon.rotation = rightHand.rotation * weaponRotationInHand;
            weapon.position += rightHand.position - weapon.TransformPoint(weaponGripPoint);
            return;
        }

        // A positive turn around the up axis moves forward towards the enemy's right.
        Vector3 armDirection = Quaternion.AngleAxis(armOpenAngle, transform.up) * transform.forward;
        AimBone(rightUpperArm, rightLowerArm, armDirection);
        AimBone(rightLowerArm, rightHand, armDirection);

        weapon.localRotation = Quaternion.AngleAxis(weaponTurnAngle, Vector3.up) * weaponRestLocalRotation;
        weapon.position += rightHand.position - weapon.TransformPoint(weaponGripPoint);

        Vector3 displacement = weapon.position - transform.TransformPoint(weaponRestLocalPosition);
        weaponCollider.center = weaponColliderRestCenter - weapon.InverseTransformVector(displacement);
    }

    private void AimBone(Transform bone, Transform child, Vector3 direction)
    {
        bone.rotation = Quaternion.FromToRotation(child.position - bone.position, direction) * bone.rotation;
    }

    // Called when this enemy kills the player: it stops, faces the player and swings the scythe.
    public void Attack()
    {
        if (isDead || isAttacking) return;
        isAttacking = true;

        rb.velocity = Vector3.zero;
        myaudio.Stop();
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0.0f;
        if (toPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        // Keep the grip the scythe had in the aimed pose of the last frame.
        weaponRotationInHand = Quaternion.Inverse(rightHand.rotation) * weapon.rotation;
        animator.SetBool("Persiguiendo", false);
        animator.SetTrigger("attack");
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
