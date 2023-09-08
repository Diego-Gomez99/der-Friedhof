using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Enemy : MonoBehaviour
{
    public float detectionDistance = 5.0f;
    public float maxMovementSpeed = 2.0f; // Maximum movement speed.
    public float minMovementSpeed = 0.5f; // Minimum movement speed when far from the player.
    public float rotationSpeed = 5.0f; // Adjust this value for the rotation speed.

    private Rigidbody rb;
    private Transform target;
    private bool death = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    private void Update()
    {
        if (death) return;

        // Calculate the distance to the target.
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= detectionDistance)
        {
            // Calculate a speed multiplier based on the distance.
            float speedMultiplier = Mathf.Clamp01(1 - (distanceToTarget / detectionDistance));

            // Interpolate the movement speed between min and max based on the speed multiplier.
            float currentMovementSpeed = Mathf.Lerp(minMovementSpeed, maxMovementSpeed, speedMultiplier);

            // Calculate the direction to the player.
            Vector3 directionToPlayer = (target.position - transform.position).normalized;

            // Calculate the desired rotation only around the Y-axis.
            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

            // Smoothly rotate the enemy to face the player only around the Y-axis.
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Apply a force to move the enemy towards the player using the rigidbody.
            rb.velocity = directionToPlayer * currentMovementSpeed;
        }
    }

    public void die()
    {
        death = true;
    }
}
