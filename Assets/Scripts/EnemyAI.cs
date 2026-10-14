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
    // Once the player is dead, enemies that reach this distance from the body swing the scythe at it.
    public float corpseAttackDistance = 1.5f;

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

    // Scythe pose during the attack. The clip is a great sword raise and plunge, and the scythe is
    // held near the butt of the handle like the sword's hilt. It follows the hand's position, but
    // its angle comes from the hand's height instead of the hand's rotation (the clip's backswing
    // would spin the long scythe and drag the blade along the ground): with the hands up, it lies
    // flat with the butt ahead and the blade behind, pointing up; with the hands down, the butt is
    // driven into the ground and the scythe stands almost upright with the blade facing the player.
    // These depend on the scythe mesh and the clip, so they are not serialized (a changed default
    // would not reach the enemies already in the scenes).
    // Point of the handle in the right hand, in the weapon's local space.
    private static readonly Vector3 AttackGripPoint = new Vector3(2.9f, 4.09f, -0.58f);
    // From the grip towards the butt, and from the head of the handle towards the blade tip,
    // in the weapon's local space.
    private static readonly Vector3 WeaponButtDirection = new Vector3(0.99f, -0.03f, 0.13f);
    private static readonly Vector3 WeaponBladeDirection = new Vector3(-0.4f, -3.4f, -2.1f);
    // Right hand heights above the feet, in the enemy's local space, where the scythe is fully
    // raised and fully planted. The clip goes from about 1.84 overhead to 0.31 at the plunge.
    private const float RaisedHandHeight = 1.6f;
    private const float PlantedHandHeight = 0.4f;
    // Degrees the planted scythe leans forward, towards the player, from upright.
    private const float PlantedLean = 10.0f;
    // The butt, and points along the blade and the head of the handle, in the weapon's local space.
    // Only the butt may go into the ground, and only ButtMaxDepth metres.
    private static readonly Vector3 WeaponButtPoint = new Vector3(3.95f, 4.03f, -0.44f);
    private static readonly Vector3[] WeaponBladePoints =
    {
        new Vector3(-2.4f, 0.96f, -3.25f),
        new Vector3(-3.3f, 2.2f, -2.6f),
        new Vector3(-4.1f, 4.57f, -1.47f),
        new Vector3(-2.0f, 4.38f, -1.18f),
    };
    private const float ButtMaxDepth = 0.2f;

    private Transform weapon;
    private BoxCollider weaponCollider;
    private Vector3 weaponRestLocalPosition;
    private Quaternion weaponRestLocalRotation;
    private Vector3 weaponColliderRestCenter;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;

    // While attacking, the clip moves the arm and the scythe follows the hand.
    private bool isAttacking = false;
    // Once the plunge bottoms out, the scythe stays where it was driven in, relative to the enemy,
    // instead of following the small movements of the hand.
    private bool isWeaponPlanted = false;
    private Vector3 plantedLocalPosition;
    private Quaternion plantedLocalRotation;
    private float previousHandHeight = float.MaxValue;

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

        if (GameManager.instance.isGameOver && distanceToTarget <= corpseAttackDistance)
        {
            Attack();
            return;
        }

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
            if (isWeaponPlanted)
            {
                weapon.localPosition = plantedLocalPosition;
                weapon.localRotation = plantedLocalRotation;
                return;
            }

            Quaternion scytheFrame = Quaternion.Inverse(Quaternion.LookRotation(WeaponButtDirection, WeaponBladeDirection));
            Quaternion raised = Quaternion.LookRotation(transform.forward, transform.up) * scytheFrame;
            Vector3 plantedButt = Quaternion.AngleAxis(PlantedLean, transform.right) * -transform.up;
            Quaternion planted = Quaternion.LookRotation(plantedButt, transform.forward) * scytheFrame;
            float handHeight = transform.InverseTransformPoint(rightHand.position).y;
            float plant = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(RaisedHandHeight, PlantedHandHeight, handHeight));
            weapon.rotation = Quaternion.Slerp(raised, planted, plant);
            weapon.position += rightHand.position - weapon.TransformPoint(AttackGripPoint);
            KeepWeaponAboveGround();

            // The plunge has bottomed out when the hand stops going down with the scythe fully planted.
            if (plant >= 1.0f && handHeight >= previousHandHeight)
            {
                isWeaponPlanted = true;
                plantedLocalPosition = weapon.localPosition;
                plantedLocalRotation = weapon.localRotation;
            }
            previousHandHeight = handHeight;
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

    // Tilts the scythe around the hand, as little as possible, so that the blade stays above the
    // ground and the butt goes in no deeper than ButtMaxDepth. The enemy's feet are the ground.
    private void KeepWeaponAboveGround()
    {
        Vector3 grip = rightHand.position;
        Vector3 axis = Vector3.Cross(weapon.TransformDirection(WeaponButtDirection), Vector3.up);
        if (axis.sqrMagnitude < 0.0001f) return;

        float best = 0.0f;
        float bestDepth = WeaponDepth(grip, Quaternion.identity);
        for (float angle = 3.0f; angle <= 90.0f && bestDepth > 0.0f; angle += 3.0f)
        {
            foreach (float signed in new[] { angle, -angle })
            {
                float depth = WeaponDepth(grip, Quaternion.AngleAxis(signed, axis));
                if (depth < bestDepth)
                {
                    best = signed;
                    bestDepth = depth;
                }
            }
        }
        if (best != 0.0f)
        {
            weapon.RotateAround(grip, axis, best);
        }
    }

    // How far the scythe, turned by tilt around the grip, goes below where it is allowed. 0 if it does not.
    private float WeaponDepth(Vector3 grip, Quaternion tilt)
    {
        float ground = transform.position.y;
        float depth = Mathf.Max(0.0f, ground - ButtMaxDepth - TiltedHeight(grip, tilt, WeaponButtPoint));
        foreach (Vector3 point in WeaponBladePoints)
        {
            depth = Mathf.Max(depth, ground - TiltedHeight(grip, tilt, point));
        }
        return depth;
    }

    private float TiltedHeight(Vector3 grip, Quaternion tilt, Vector3 localPoint)
    {
        return (grip + tilt * (weapon.TransformPoint(localPoint) - grip)).y;
    }

    private void AimBone(Transform bone, Transform child, Vector3 direction)
    {
        bone.rotation = Quaternion.FromToRotation(child.position - bone.position, direction) * bone.rotation;
    }

    // Called when this enemy kills the player, and when an enemy reaches the player's body after
    // that: it stops, faces the player and swings the scythe.
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
