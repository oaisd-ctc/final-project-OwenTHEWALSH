using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StartTrigger : MonoBehaviour
{
    [Tooltip("Camera to detect for trigger.")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("HIM GameObject to show when triggered.")]
    [SerializeField]
    private GameObject himGameObject;

    [Tooltip("Text GameObject with dialogue to activate.")]
    [SerializeField]
    private GameObject textGameObject;

    [Tooltip("Dialogue text for HIM's introduction.")]
    [SerializeField]
    private string introductionDialogue = "";

    [Tooltip("Climb script reference for QTE activation.")]
    [SerializeField]
    private GameObject climbGameObject;

    [Header("Timing Settings")]
    [Tooltip("Delay before intro text appears after HIM is shown (seconds).")]
    [SerializeField]
    private float delayBeforeIntroText = 1.0f;

    [Tooltip("Delay before QTE starts after intro text ends (seconds).")]
    [SerializeField]
    private float delayBeforeQTE = 2.0f;

    [Tooltip("Minimum duration player must stay in trigger (seconds).")]
    [SerializeField]
    private float minimumTriggerDuration = 2.0f;

    private Climb climbScript;
    private HIM himScript;

    private bool hasTriggered = false;
    private float triggerTimer = 0f;
    private bool playerInTrigger = false;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (himGameObject != null)
        {
            himGameObject.SetActive(false);
            himScript = himGameObject.GetComponent<HIM>();
        }

        if (textGameObject != null)
        {
            textGameObject.SetActive(false);
        }

        if (climbGameObject != null)
        {
            climbScript = climbGameObject.GetComponent<Climb>();
        }
        else
        {
            climbScript = FindObjectOfType<Climb>();
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
                
                if (!hasTriggered)
                {
                    TriggerHIM();
                }
            }
            else if (!hasTriggered)
            {
                // Already in trigger, increase timer
                triggerTimer += Time.deltaTime;

                // Check if minimum duration met
                if (triggerTimer >= minimumTriggerDuration)
                {
                    ActivateQTE();
                }
            }
        }
        else
        {
            // Player left trigger
            if (playerInTrigger && !hasTriggered)
            {
                Debug.Log("StartTrigger: Player left trigger before minimum duration!");
                ResetTrigger();
            }
            playerInTrigger = false;
        }
    }

    private void TriggerHIM()
    {
        Debug.Log("StartTrigger: Camera entered trigger zone!");

        // Activate HIM GameObject
        if (himGameObject != null)
        {
            himGameObject.SetActive(true);
            if (himScript != null)
            {
                himScript.ShowHIM(introductionDialogue);
                Debug.Log("StartTrigger: HIM has appeared!");
            }
        }

        // Activate text GameObject with delay
        if (textGameObject != null)
        {
            StartCoroutine(ShowTextWithDelay());
        }
        else
        {
            Debug.LogWarning("StartTrigger: Text GameObject not assigned");
        }

        // Lock player movement
        if (climbScript != null)
        {
            climbScript.LockPlayerMovement(true);
        }
    }

    private IEnumerator ShowTextWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeIntroText);
        
        if (textGameObject != null)
        {
            textGameObject.SetActive(true);
            Debug.Log("StartTrigger: Text activated!");
        }
    }

    private void ActivateQTE()
    {
        hasTriggered = true;
        Debug.Log("StartTrigger: Minimum duration reached! QTE starting...");

        // Start QTE countdown timer
        if (climbScript != null)
        {
            climbScript.ActivateClimb(textGameObject, introductionDialogue, delayBeforeQTE);
            Debug.Log("StartTrigger: QTE countdown started!");
        }
        else
        {
            Debug.LogWarning("StartTrigger: Climb script not assigned");
        }
    }

    private void ResetTrigger()
    {
        Debug.Log("StartTrigger: Resetting trigger...");
        hasTriggered = false;
        triggerTimer = 0f;

        if (himGameObject != null)
        {
            himGameObject.SetActive(false);
        }

        if (textGameObject != null)
        {
            textGameObject.SetActive(false);
        }

        if (climbScript != null)
        {
            climbScript.LockPlayerMovement(false);
        }
    }
}
