using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Enemy : MonoBehaviour
{
    // Every enemy behaves the same, so its tuning values are constants rather than serialized
    // fields (a serialized field keeps its old value in the scene when the default changes in code).
    // A hidden enemy appears once the player is this many metres away or closer.
    private const float DetectionDistance = 10.0f;
    // Chase speed, in metres per second: the minimum with the player at DetectionDistance, rising
    // to the maximum as the player gets closer.
    private const float MaxMovementSpeed = 6.0f;
    private const float MinMovementSpeed = 0.5f;
    private const float RotationSpeed = 5.0f;
    // Speed, in metres per second, at which the walk clip plays at its normal rate.
    private const float WalkClipSpeed = 1.8f;

    private Rigidbody rb;
    private Transform target;
    private bool isDead = false;
    private Renderer enemyRenderer;
    private CapsuleCollider capsuleCollider;
    private Collider[] playerColliders;

    private const float AudioVolume = 0.7f;

    private AudioSource myaudio;
    [SerializeField]
    private AudioClip chasesong;

    private Animator animator;

    // Degrees the right arm opens outward (to the enemy's right) from straight back.
    private const float ArmOpenAngle = 85.0f;
    // Degrees the right arm drops below the horizontal.
    private const float ArmDropAngle = 65.0f;
    // Point of the scythe handle that sits in the right hand, in the weapon's local space.
    // The scythe keeps the rotation it has in the scene, relative to the enemy.
    private static readonly Vector3 WeaponGripPoint = new Vector3(1.544571f, 4.177524f, -0.882705f);

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

    // These are the same for every enemy, so they are constants rather than serialized fields
    // (a serialized field keeps its old value in the scene when the default changes in code).
    // Seconds an enemy can stay visible in a row. Then it vanishes for RespawnCooldown seconds,
    // during which it neither chases nor shows up, even with the player in range.
    private const float MaxVisibleTime = 30.0f;
    private const float RespawnCooldown = 10.0f;
    // After the cooldown, it shows up again only with the player at least this many metres away,
    // so it does not appear on top of the player.
    private const float RespawnMinDistance = 5.0f;
    // Cooldown, in seconds, of an enemy that vanished because a closer one took over. After it,
    // RespawnMinDistance applies as well.
    private const float YieldCooldown = 1.0f;
    // A shown enemy farther than this many metres from the player vanishes and, once hidden and
    // with a chance of TeleportChance (0 to 1), moves to where the player is. After VanishCooldown
    // seconds, it comes back once the player is RespawnMinDistance away from where it is.
    private const float VanishDistance = 40.0f;
    private const float VanishCooldown = 3.0f;
    private const float TeleportChance = 0.25f;
    private float cooldownEndTime;
    private bool isRespawning = false;
    // Set while vanishing for distance, so the enemy moves to the player once the smoke hides it.
    private bool teleportOnVanish = false;

    // Only one enemy is shown at a time: the closest to the player of those that could chase it.
    // Another one waits until the shown enemy has vanished, and a closer one takes over from it.
    private static readonly List<Enemy> allEnemies = new List<Enemy>();
    private static Enemy closestCandidate;
    private static int closestCandidateFrame = -1;
    // After any enemy vanishes, no enemy may appear for this many seconds. The smoke has hidden
    // the vanished body by then, so the trail to the next enemy sets off right after.
    private const float AppearAfterVanishDelay = SmokeEffect.CoverDelay;
    private static float lastVanishTime = float.MinValue;
    // Where the last enemy vanished. The next one to appear draws a smoke trail from there.
    private static Vector3? lastVanishPosition;
    // Closer than this, in metres, the trail is skipped (the same enemy coming back in place).
    private const float MinTrailDistance = 1.0f;
    // Seconds between the trail reaching the new enemy and its puff of smoke. Until the puff hides
    // the switch, the enemy stays hidden and still where the trail ended.
    private const float AppearAfterTrailPause = 0.5f;
    private float appearTime;
    // While it appears, a sweep of smoke runs through these points, in metres from the enemy's
    // feet along its right, its up and its front, drawing the scythe's outline to show its size: the
    // handle from beside the enemy, at the height of the trail between enemies, to 4 m ahead and
    // 1 m up, then the blade 2 m to the left, 1 m back and 2 m up.
    private static readonly Vector3[] WeaponSweepPoints =
    {
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 4.0f),
        new Vector3(-2.0f, 3.0f, 3.0f),
    };

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        playerColliders = target.GetComponentsInChildren<Collider>();
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
        if (isVisible && Time.time < appearTime)
        {
            rb.velocity = new Vector3(0.0f, rb.velocity.y, 0.0f);
            animator.SetFloat("walkSpeed", 0.0f);
            return;
        }
        if (Time.time < cooldownEndTime) return;

        if (isVisible && Time.time - visibleSince >= MaxVisibleTime)
        {
            StartCooldown(RespawnCooldown);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (isVisible && distanceToTarget > VanishDistance)
        {
            // Random.value can be 0 or 1, so a chance of 0 never teleports and 1 always does.
            teleportOnVanish = TeleportChance > 0.0f && UnityEngine.Random.value <= TeleportChance;
            StartCooldown(VanishCooldown);
            return;
        }
        if (isRespawning && distanceToTarget < RespawnMinDistance) return;

        // The walk clip plays faster or slower to match how fast the enemy actually moved in the
        // last physics step, so its feet keep up with the ground and it stands still when blocked.
        Vector3 groundVelocity = rb.velocity;
        groundVelocity.y = 0.0f;
        animator.SetFloat("walkSpeed", groundVelocity.magnitude / WalkClipSpeed);

        // Once shown, it keeps chasing until the player is VanishDistance away.
        bool mayChase = (isVisible || distanceToTarget <= DetectionDistance)
            && ClosestCandidate(target.position) == this
            && (isVisible || (!AnotherEnemyVisible() && Time.time - lastVanishTime >= AppearAfterVanishDelay));

        if (mayChase)
        {
            isRespawning = false;
            SetVisible(true);
            float speedMultiplier = Mathf.Clamp01(1 - (distanceToTarget / DetectionDistance));
            float currentMovementSpeed = Mathf.Lerp(MinMovementSpeed, MaxMovementSpeed, speedMultiplier);
            Vector3 directionToPlayer = (target.position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);

            rb.velocity = directionToPlayer * currentMovementSpeed;

            if(!myaudio.isPlaying)
            {
                myaudio.PlayOneShot(chasesong, AudioVolume);
                animator.SetBool("Persiguiendo", true);
            }
        }
        else
        {
            // Shown: it only stops because a closer enemy takes over.
            bool yielding = isVisible;

            myaudio.Stop();
            animator.SetBool("Persiguiendo", false);
            SetVisible(false);
            // Once it has actually vanished (not held by its minimum visible time).
            if (yielding && !isVisible)
            {
                StartCooldown(YieldCooldown);
            }
        }
    }

    private void OnEnable()
    {
        // The first enemy of a freshly loaded scene: forget where the previous run's enemy vanished.
        if (allEnemies.Count == 0)
        {
            lastVanishPosition = null;
        }
        allEnemies.Add(this);
    }

    private void OnDisable()
    {
        allEnemies.Remove(this);
    }

    // Whether this enemy could chase a player this far away, ignoring the other enemies.
    private bool CanChase(float distanceToPlayer)
    {
        return !isDead && !isAttacking && Time.time >= cooldownEndTime
            && (isVisible || distanceToPlayer <= DetectionDistance)
            && !(isRespawning && distanceToPlayer < RespawnMinDistance);
    }

    // The enemy closest to the player among those that could chase it, worked out once per frame.
    private static Enemy ClosestCandidate(Vector3 playerPosition)
    {
        if (closestCandidateFrame == Time.frameCount) return closestCandidate;
        closestCandidateFrame = Time.frameCount;

        closestCandidate = null;
        float closestDistance = float.MaxValue;
        foreach (Enemy enemy in allEnemies)
        {
            float distance = Vector3.Distance(enemy.transform.position, playerPosition);
            if (distance < closestDistance && enemy.CanChase(distance))
            {
                closestCandidate = enemy;
                closestDistance = distance;
            }
        }
        return closestCandidate;
    }

    // A living enemy other than this one is shown, or still fading out (its minimum visible time).
    private bool AnotherEnemyVisible()
    {
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != this && enemy.isVisible && !enemy.isDead) return true;
        }
        return false;
    }

    // Vanishes where it stands and stays hidden and still until the cooldown is over and the
    // player is at least RespawnMinDistance away.
    private void StartCooldown(float duration)
    {
        cooldownEndTime = Time.time + duration;
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
        bool teleport = teleportOnVanish;
        teleportOnVanish = false;
        if (visible == isVisible) return;
        // Keeps a player standing at the edge of DetectionDistance from setting off a puff every frame.
        if (!visible && Time.time - visibleSince < MinVisibleTime) return;

        isVisible = visible;
        float height = capsuleCollider.height * transform.lossyScale.y;
        // Seconds the trail takes to cross over, and the pause after it, before the puff.
        float trailTime = 0.0f;
        float pause = 0.0f;
        // Not for an enemy shown because it was killed.
        bool sweep = visible && !isDead;
        if (visible)
        {
            if (sweep)
            {
                // It appears facing the player, so the sweep shows where the scythe will reach.
                Vector3 toPlayer = target.position - transform.position;
                toPlayer.y = 0.0f;
                if (toPlayer != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(toPlayer);
                }
            }
            if (sweep && lastVanishPosition.HasValue
                && Vector3.Distance(lastVanishPosition.Value, transform.position) >= MinTrailDistance)
            {
                SmokeEffect.PlayTrail(lastVanishPosition.Value, transform.position, height, AppearAfterTrailPause);
                trailTime = SmokeEffect.TrailTravelTime;
                pause = AppearAfterTrailPause;
            }
            appearTime = Time.time + trailTime + pause;
            visibleSince = appearTime;
        }
        else
        {
            weaponCollider.enabled = false;
            lastVanishTime = Time.time;
            lastVanishPosition = transform.position;
        }

        if (pendingVisibility != null)
        {
            StopCoroutine(pendingVisibility);
        }
        pendingVisibility = StartCoroutine(ApplyVisibilityAfterSmoke(visible, trailTime, pause, sweep, height, teleport && !visible));
    }

    private IEnumerator ApplyVisibilityAfterSmoke(bool visible, float trailTime, float pause, bool sweep, float height, bool teleport)
    {
        if (trailTime > 0.0f)
        {
            yield return new WaitForSeconds(trailTime);
        }
        // Once the trail is there, a sweep of smoke along the scythe's reach hints at its size.
        if (sweep)
        {
            Vector3[] sweepPoints = new Vector3[WeaponSweepPoints.Length];
            for (int i = 0; i < sweepPoints.Length; i++)
            {
                sweepPoints[i] = transform.position + transform.rotation * WeaponSweepPoints[i];
            }
            SmokeEffect.PlayTrail(sweepPoints, height, 0.0f);
        }
        if (pause > 0.0f)
        {
            yield return new WaitForSeconds(pause);
        }
        SmokeEffect.Play(transform.position, height, capsuleCollider.radius * transform.lossyScale.x);
        yield return new WaitForSeconds(SmokeEffect.CoverDelay);
        pendingVisibility = null;
        ApplyVisibility(visible);
        if (teleport)
        {
            rb.velocity = Vector3.zero;
            rb.position = target.position;
            transform.position = target.position;
        }
    }

    private void ApplyVisibility(bool visible)
    {
        foreach (Renderer part in renderers)
        {
            part.enabled = visible;
        }
        // A dead enemy's scythe stays disarmed.
        weaponCollider.enabled = visible && !isDead;
        // The player walks through a hidden body. The collider stays on so the body keeps standing on the ground.
        foreach (Collider playerCollider in playerColliders)
        {
            Physics.IgnoreCollision(capsuleCollider, playerCollider, !visible);
        }
    }

    // Runs after the Animator, so it overrides the clip's pose for the right arm:
    // the arm points back and down, opened outward, and the scythe follows the hand.
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
                Vector3 entry = HandleGroundEntry(butt);
                int markSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                ScytheGroundMark.Create(entry, markSeed, true);
                DeathMarkers.Record(weapon, entry, markSeed);
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

        // A negative turn around the up axis moves backward towards the enemy's right.
        Vector3 armHorizontal = Quaternion.AngleAxis(-ArmOpenAngle, transform.up) * -transform.forward;
        float drop = ArmDropAngle * Mathf.Deg2Rad;
        Vector3 armDirection = Mathf.Cos(drop) * armHorizontal - Mathf.Sin(drop) * transform.up;
        AimBone(rightUpperArm, rightLowerArm, armDirection);
        AimBone(rightLowerArm, rightHand, armDirection);

        weapon.localRotation = weaponRestLocalRotation;
        weapon.position += rightHand.position - weapon.TransformPoint(WeaponGripPoint);

        Vector3 displacement = weapon.position - transform.TransformPoint(weaponRestLocalPosition);
        weaponCollider.center = weaponColliderRestCenter - weapon.InverseTransformVector(displacement);
    }

    // Where the handle crosses the ground, going up the handle from the butt. The enemy's feet are the ground.
    private Vector3 HandleGroundEntry(Vector3 butt)
    {
        float ground = transform.position.y;
        Vector3 down = weapon.TransformDirection(WeaponButtDirection).normalized;
        if (butt.y < ground && down.y < -0.1f)
        {
            butt -= down * ((ground - butt.y) / -down.y);
        }
        butt.y = ground;
        return butt;
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

    // Editor only, in Play mode, in the Scene view and in the Game view with Gizmos on.
    // A hidden enemy is outlined: white while it waits for the player, orange during the cooldown,
    // and red once the cooldown is over but the player is still closer than RespawnMinDistance.
    // A label above it counts down to vanishing (while visible) or to the end of the cooldown.
    // The scythe's hitbox is drawn in red while it can kill.
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || isDead || capsuleCollider == null) return;

        DrawWeaponHitbox();
        if (isAttacking) return;

        float height = capsuleCollider.height * transform.lossyScale.y;
        float radius = capsuleCollider.radius * transform.lossyScale.x;
        Vector3 labelPosition = transform.position + Vector3.up * (height + 0.3f);

        if (isVisible)
        {
            float untilHidden = Mathf.Max(0.0f, MaxVisibleTime - (Time.time - visibleSince));
            DrawGizmoLabel(labelPosition, $"Vanishes in {untilHidden:0.0} s", Color.white);
            return;
        }

        string label = null;
        if (Time.time < cooldownEndTime)
        {
            Gizmos.color = new Color(1.0f, 0.55f, 0.1f);
            label = $"Returns in {cooldownEndTime - Time.time:0.0} s";
        }
        else if (isRespawning && Vector3.Distance(transform.position, target.position) < RespawnMinDistance)
        {
            Gizmos.color = Color.red;
            label = "Player too close";
        }
        else
        {
            Gizmos.color = Color.white;
        }
        Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(radius * 2.0f, height, radius * 2.0f));
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (height - radius), radius);
        if (label != null)
        {
            DrawGizmoLabel(labelPosition, label, Gizmos.color);
        }
    }

    // The scythe's kill trigger, while it is armed.
    private void DrawWeaponHitbox()
    {
        if (!weaponCollider.enabled) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = weapon.localToWorldMatrix;
        Gizmos.DrawWireCube(weaponCollider.center, weaponCollider.size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private static void DrawGizmoLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = color;
        UnityEditor.Handles.Label(position, text, style);
#endif
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
