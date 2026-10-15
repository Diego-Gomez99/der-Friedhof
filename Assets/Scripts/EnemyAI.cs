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
    // Speed, in metres per second, at which the walk clip plays at its normal rate.
    private const float WalkClipSpeed = 1.8f;

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
    // Whether the ground impact effect has played for this swing.
    private bool hasHitGround = false;
    // Once dead, the clip moves the arm freely and the scythe stays in the hand as it was.
    private Vector3 deadWeaponHandPosition;
    private Quaternion deadWeaponHandRotation;

    // The enemy is only seen, and its scythe only kills, while it chases the player.
    // Attacking and dead enemies stay visible.
    // Seconds an enemy stays visible at least, once shown, before it can vanish again.
    private const float MinVisibleTime = 1.0f;
    private Renderer[] renderers;
    private bool isVisible = true;
    private float visibleSince;
    private Coroutine pendingVisibility;

    [Header("Visibility")]
    // Seconds an enemy can stay visible in a row. Then it vanishes for respawnCooldown seconds,
    // during which it neither chases nor shows up, even with the player in range.
    [SerializeField]
    private float maxVisibleTime = 30.0f;
    [SerializeField]
    private float respawnCooldown = 10.0f;
    // After the cooldown, it shows up again only with the player at least this many metres away,
    // so it does not appear on top of the player.
    [SerializeField]
    private float respawnMinDistance = 5.0f;
    private float cooldownEndTime;
    private bool isRespawning = false;

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

        // Hidden from the start, without smoke.
        renderers = GetComponentsInChildren<Renderer>();
        isVisible = false;
        ApplyVisibility(false);
    }

    private void Update()
    {
        if (isDead || isAttacking) return;
        if (Time.time < cooldownEndTime) return;

        if (isVisible && Time.time - visibleSince >= maxVisibleTime)
        {
            StartCooldown();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (isRespawning && distanceToTarget < respawnMinDistance) return;

        // The walk clip plays faster or slower to match how fast the enemy actually moved in the
        // last physics step, so its feet keep up with the ground and it stands still when blocked.
        Vector3 groundVelocity = rb.velocity;
        groundVelocity.y = 0.0f;
        animator.SetFloat("walkSpeed", groundVelocity.magnitude / WalkClipSpeed);

        if (distanceToTarget <= detectionDistance)
        {
            isRespawning = false;
            SetVisible(true);
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
            SetVisible(false);
        }
    }

    // Vanishes where it stands and stays hidden and still until the cooldown is over and the
    // player is at least respawnMinDistance away.
    private void StartCooldown()
    {
        cooldownEndTime = Time.time + respawnCooldown;
        isRespawning = true;
        rb.velocity = new Vector3(0.0f, rb.velocity.y, 0.0f);
        animator.SetFloat("walkSpeed", 0.0f);
        animator.SetBool("Persiguiendo", false);
        myaudio.Stop();
        SetVisible(false);
    }

    // Shows or hides the enemy and its scythe inside a puff of black smoke. The switch happens once
    // the smoke covers the body. The scythe is disarmed at once when hiding, and armed only when shown.
    private void SetVisible(bool visible)
    {
        if (visible == isVisible) return;
        // Keeps a player standing at the edge of detectionDistance from setting off a puff every frame.
        if (!visible && Time.time - visibleSince < MinVisibleTime) return;

        isVisible = visible;
        if (visible)
        {
            visibleSince = Time.time;
        }
        else
        {
            weaponCollider.enabled = false;
        }

        SmokeEffect.Play(transform.position, capsuleCollider.height * transform.lossyScale.y, capsuleCollider.radius * transform.lossyScale.x);
        if (pendingVisibility != null)
        {
            StopCoroutine(pendingVisibility);
        }
        pendingVisibility = StartCoroutine(ApplyVisibilityAfterSmoke(visible));
    }

    private IEnumerator ApplyVisibilityAfterSmoke(bool visible)
    {
        yield return new WaitForSeconds(SmokeEffect.CoverDelay);
        pendingVisibility = null;
        ApplyVisibility(visible);
    }

    private void ApplyVisibility(bool visible)
    {
        foreach (Renderer part in renderers)
        {
            part.enabled = visible;
        }
        // A dead enemy's scythe stays disarmed.
        weaponCollider.enabled = visible && !isDead;
    }

    // Runs after the Animator, so it overrides the clip's pose for the right arm:
    // the arm points ahead, opened slightly outward, and the scythe follows the hand.
    // The scythe's kill trigger stays where it was authored, at the player's height.
    private void LateUpdate()
    {
        if (rightUpperArm == null || rightLowerArm == null || rightHand == null) return;

        if (isDead)
        {
            weapon.SetPositionAndRotation(
                rightHand.TransformPoint(deadWeaponHandPosition), rightHand.rotation * deadWeaponHandRotation);
            return;
        }

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

            // The dirt flies as soon as the butt touches the ground, before the plunge bottoms out.
            Vector3 butt = weapon.TransformPoint(WeaponButtPoint);
            bool buttInGround = butt.y <= transform.position.y;

            // The plunge has bottomed out when the hand stops going down with the scythe fully planted.
            if (plant >= 1.0f && handHeight >= previousHandHeight)
            {
                isWeaponPlanted = true;
                plantedLocalPosition = weapon.localPosition;
                plantedLocalRotation = weapon.localRotation;
                buttInGround = true;
            }

            if (buttInGround && !hasHitGround)
            {
                hasHitGround = true;
                butt.y = transform.position.y;
                GroundImpactEffect.Play(butt);
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

    // Editor only: outlines a hidden enemy in Play mode, in the Scene view and in the Game view
    // with Gizmos on. White while it waits for the player, orange while it cannot show up
    // (cooldown, or waiting for the player to be respawnMinDistance away).
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || isVisible || isDead || capsuleCollider == null) return;

        float height = capsuleCollider.height * transform.lossyScale.y;
        float radius = capsuleCollider.radius * transform.lossyScale.x;
        bool blocked = Time.time < cooldownEndTime
            || (isRespawning && Vector3.Distance(transform.position, target.position) < respawnMinDistance);
        Gizmos.color = blocked ? new Color(1.0f, 0.55f, 0.1f) : Color.white;
        Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(radius * 2.0f, height, radius * 2.0f));
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (height - radius), radius);
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

        animator.SetBool("Persiguiendo", false);
        animator.SetTrigger("attack");
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        SetVisible(true);

        // It stops where it is and the body stays lying there. Kinematic, so it does not fall
        // through the ground once its collider is off.
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        capsuleCollider.enabled = false;
        weaponCollider.enabled = false;
        myaudio.Stop();

        if (rightHand != null)
        {
            deadWeaponHandPosition = rightHand.InverseTransformPoint(weapon.position);
            deadWeaponHandRotation = Quaternion.Inverse(rightHand.rotation) * weapon.rotation;
        }

        animator.SetBool("Persiguiendo", false);
        animator.SetTrigger("die");
    }

}
