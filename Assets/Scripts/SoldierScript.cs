using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SoldierScript : MonoBehaviour
{
    public float moveSpeed = 5.0f; // Speed of soldier's movement
    public float moveDuration = 5.0f; // Duration of soldier's movement
    private bool isMoving = false; // Flag to track if soldier is currently moving

    void Start()
    {

    }
    void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            // Start the soldier's movement
            StartCoroutine(MoveRightForDuration());
        }
    }

    IEnumerator MoveRightForDuration()
    {
        isMoving = true;
        float elapsedTime = 0.0f;
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.right * moveSpeed * moveDuration;

        // Move the soldier to the right over time
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / moveDuration);
            yield return null;
        }

        isMoving = false;
    }
}
