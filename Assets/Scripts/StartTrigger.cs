using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triggertest : MonoBehaviour
{
    [Tooltip("Camera to detect for trigger.")]
    [SerializeField]
    private Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Check if camera is within trigger zone
        if (targetCamera != null)
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null && triggerCollider.isTrigger)
            {
                if (triggerCollider.bounds.Contains(targetCamera.transform.position))
                {
                    Debug.Log($"Trigger Test: {targetCamera.gameObject.name} is in trigger zone!");
                }
            }
        }
    }
}
