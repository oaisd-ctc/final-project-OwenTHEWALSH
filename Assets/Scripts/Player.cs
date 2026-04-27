using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Camera Move Settings")]
    [Tooltip("Camera to move. If null, Camera.main will be used.")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("How far to move the camera up (world units).")]
    [SerializeField]
    private float moveUpAmount = 5f;

    [Tooltip("Time in seconds for the camera move.")]
    [SerializeField]
    private float moveDuration = 0.5f;

    [Header("Light Settings")]
    [Tooltip("Light that is a child of the camera. Moves with the camera.")]
    [SerializeField]
    private Light cameraLight;

    [Header("Repel Settings")]
    [Tooltip("Radius to detect objects for repelling.")]
    [SerializeField]
    private float repelRadius = 10f;

    [Tooltip("Force applied to repel objects.")]
    [SerializeField]
    private float repelForce = 20f;

    [Tooltip("Tag to identify objects to repel (e.g., 'HIM').")]
    [SerializeField]
    private string repelTag = "HIM";

    // Prevent starting multiple concurrent moves       
    private Coroutine moveCoroutine;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("Player: No camera found to move. Assign a Camera in the Inspector or ensure there is a Camera tagged MainCamera.");
        }

        if (cameraLight == null)
        {
            Debug.LogWarning("Player: No light assigned. Assign a Light child of the camera in the Inspector for best results.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (targetCamera == null)
                return;

            // Start a single move coroutine (cancels previous if running)
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveCameraUpRoutine(moveUpAmount, moveDuration));
            RepelObjects();
        }
    }

    private IEnumerator MoveCameraUpRoutine(float amount, float duration)
    {
        var camTransform = targetCamera.transform;
        Vector3 start = camTransform.position;
        Vector3 end = start + Vector3.up * amount;

        if (duration <= 0f)
        {
            camTransform.position = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth step for nicer easing
            t = Mathf.SmoothStep(0f, 1f, t);
            camTransform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        camTransform.position = end;
        moveCoroutine = null;
    }

    private void RepelObjects()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, repelRadius);

        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag(repelTag))
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 repelDirection = (collider.transform.position - transform.position).normalized;
                    rb.velocity = Vector3.zero;
                    rb.AddForce(repelDirection * repelForce, ForceMode.Impulse);
                }
            }
        }
    }

    public Camera GetCamera()
    {
        return targetCamera;
    }
}
