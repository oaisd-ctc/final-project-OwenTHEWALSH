using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndTrigger : MonoBehaviour
{
    [Tooltip("Camera to detect for trigger.")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("Minimum duration player must stay in trigger (seconds).")]
    [SerializeField]
    private float minimumTriggerDuration = 1.0f;

    private bool playerInTrigger = false;
    private float triggerTimer = 0f;
    private bool hasTriggered = false;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || targetCamera == null)
            return;

        bool currentlyInTrigger = triggerCollider.bounds.Contains(targetCamera.transform.position);

        // Track time in trigger
        if (currentlyInTrigger)
        {
            if (!playerInTrigger)
            {
                // Just entered trigger
                playerInTrigger = true;
                triggerTimer = 0f;
            }
            else if (!hasTriggered)
            {
                // Already in trigger, increase timer
                triggerTimer += Time.deltaTime;

                // Check if minimum duration met
                if (triggerTimer >= minimumTriggerDuration)
                {
                    TriggerEnd();
                }
            }
        }
        else
        {
            // Player left trigger
            if (playerInTrigger && !hasTriggered)
            {
                Debug.Log("EndTrigger: Player left trigger before minimum duration!");
                ResetTrigger();
            }
            playerInTrigger = false;
        }
    }

    private void TriggerEnd()
    {
        hasTriggered = true;
        Debug.Log("EndTrigger: Minimum duration reached! Loading Title Screen...");
        SceneManager.LoadScene("Title Screen");
    }

    private void ResetTrigger()
    {
        playerInTrigger = false;
        triggerTimer = 0f;
    }
}
