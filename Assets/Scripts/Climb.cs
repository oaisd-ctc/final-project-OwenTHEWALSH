using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Climb : MonoBehaviour
{
    [Tooltip("HIM script reference for tracking.")]
    [SerializeField]
    private GameObject himGameObject;

    [Tooltip("Text box GameObject for displaying text.")]
    [SerializeField]
    private GameObject textBoxGameObject;
        
    [Tooltip("Probability of QTE triggering (0-1).")]
    [SerializeField]
    private float qteChance = 0.25f;

    [Tooltip("Time window to press E during QTE.")]
    [SerializeField]
    private float qteWindow = 2f;

    [Tooltip("Normal upward movement amount.")]
    [SerializeField]
    private float normalMoveAmount = 5f;

    [Tooltip("Double upward movement amount on QTE success.")]
    [SerializeField]
    private float doubleMoveAmount = 10f;

    // New STOP settings: HIM will say "STOP" after a random delay in this range.
    [Tooltip("Minimum delay (seconds) before HIM says STOP.")]
    [SerializeField]
    private float stopMinDelay = 5f;

    [Tooltip("Maximum delay (seconds) before HIM says STOP.")]
    [SerializeField]
    private float stopMaxDelay = 10f;

    [Tooltip("Duration (seconds) the STOP check is active. If player moves during this window it's game over.")]
    [SerializeField]
    private float stopCheckDuration = 2f;

    // Threshold to consider the player has moved.
    [Tooltip("Movement threshold to detect player movement (meters).")]
    [SerializeField]
    private float moveThreshold = 0.1f;

    private HIM himScript;
    private GameObject textBox;
    private Vector3 lastPlayerPosition;
    private bool textActive;
    private bool qteActive;
    private float qteTimer;
    private bool climbTriggered = false;
    private bool qteEnabled = false;
    private bool movementLocked = false;
    public bool GameOver = false;

    // STOP state
    private bool stopActive = false;
    private Vector3 stopStartPosition;
    private Coroutine stopCoroutine;

    private void Start()
    {
        if (himGameObject != null)
        {
            himScript = himGameObject.GetComponent<HIM>();
        }
        else
        {
            himScript = FindObjectOfType<HIM>();
        }

        if (textBoxGameObject != null)
        {
            textBox = textBoxGameObject;
            textBox.SetActive(false);
        }
        // Climb sequence starts only after trigger activation
    }

    private void Update()
    {
        if (GameOver)
            return;

        // Check if player moved while intro text is active -> immediate Game Over
        if (textActive)
        {
            if (Camera.main != null && Vector3.Distance(Camera.main.transform.position, lastPlayerPosition) > moveThreshold)
            {
                Debug.Log("Climb: Player moved during intro text! Game Over.");
                textActive = false;
                GameOver = true;
                movementLocked = true;
                qteActive = false;
                qteEnabled = false;
                stopActive = false;

                if (textBox != null)
                {
                    textBox.SetActive(false);
                }

                if (stopCoroutine != null)
                {
                    StopCoroutine(stopCoroutine);
                    stopCoroutine = null;
                }

                return;
            }
        }

        // STOP takes priority: if STOP is active and player moves -> Game Over
        if (stopActive && Camera.main != null)
        {
            if (Vector3.Distance(Camera.main.transform.position, stopStartPosition) > moveThreshold)
            {
                Debug.Log("Climb: Player moved during STOP! Game Over by STOP priority.");
                TriggerStopFailed();
                return;
            }
        }

        // Handle Space key to attempt climb (only after QTE is enabled and movement not locked)
        if (!GameOver && qteEnabled && !movementLocked && Input.GetKeyDown(KeyCode.Space))
        {
            // If STOP is active, do not allow attempts that could bypass STOP priority
            if (stopActive)
            {
                Debug.Log("Climb: Attempt ignored because STOP is active.");
            }
            else
            {
                AttemptClimb();
            }
        }

        // Handle QTE input
        if (qteActive)
        {
            qteTimer -= Time.deltaTime;

            if (qteTimer <= 0f)
            {
                QTEFailed();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                // If STOP is active, STOP overrides QTE success
                if (stopActive)
                {
                    Debug.Log("Climb: QTE input ignored due to STOP priority.");
                    qteActive = false;
                }
                else
                {
                    QTESuccess();
                }
            }
        }
    }

    public void LockPlayerMovement(bool isLocked)
    {
        movementLocked = isLocked;
    }

    public void ActivateClimb(GameObject displayTextBox, string displayText, float delayBeforeClimb)
    {
        if (climbTriggered)
            return;

        climbTriggered = true;
        textBox = displayTextBox;
        lastPlayerPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        if (textBox != null)
        {
            textBox.SetActive(true);
        }
        
        StartCoroutine(TextBoxRoutine(displayText, delayBeforeClimb));

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }
        stopCoroutine = StartCoroutine(StopRoutine());
    }

    private IEnumerator TextBoxRoutine(string text, float delayBeforeClimb)
    {
        textActive = true;
        if (textBox != null)
        {
            TextMeshProUGUI textComponent = textBox.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }   
        yield return new WaitForSeconds(3f);

        if (GameOver)
            yield break;

        textActive = false;
        if (textBox != null)
        {
            textBox.SetActive(false);
        }

        Debug.Log($"Climb: Waiting {delayBeforeClimb} seconds before QTE starts...");
        yield return new WaitForSeconds(delayBeforeClimb);

        if (GameOver)
            yield break;

        movementLocked = false;
        qteEnabled = true;
        Debug.Log("Climb: QTE enabled! Press Space to climb!");
    }

    private void AttemptClimb()
    {
        float randomValue = Random.value;
        
        if (randomValue < qteChance)
        {
            StartCoroutine(QTERoutine());
        }
        else
        {
            MovePlayerUp(normalMoveAmount);
            Debug.Log("Climb: Normal climb! No QTE this time.");
        }
    }

    private IEnumerator QTERoutine()
    {
        qteActive = true;
        qteTimer = qteWindow;
        Debug.Log($"Climb: QTE started! Press E in {qteWindow} seconds!");
        yield return new WaitForSeconds(qteWindow);
        
        if (qteActive)
        {
            QTEFailed();
        }
    }

    private IEnumerator StopRoutine()
    {
        float delay = Random.Range(stopMinDelay, stopMaxDelay);
        Debug.Log($"Climb: STOP will be announced in {delay:F2} seconds.");
        yield return new WaitForSeconds(delay);

        if (GameOver)
            yield break;

        if (himScript != null)
        {
            himScript.ShowHIM("STOP");
        }
        else
        {
            Debug.Log("Climb: HIM not assigned, but STOP announced internally.");
        }

        bool previousQteEnabled = qteEnabled;
        qteEnabled = false;

        if (qteActive)
        {
            qteActive = false;
            Debug.Log("Climb: Active QTE cancelled due to STOP priority.");
        }

        stopActive = true;
        stopStartPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < stopCheckDuration && !GameOver)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        stopActive = false;
        qteEnabled = previousQteEnabled;

        if (himScript != null)
        {
            himScript.HideTextBox();
        }

        stopCoroutine = null;
        Debug.Log("Climb: STOP window ended.");
    }

    private void MovePlayerUp(float amount)
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position += Vector3.up * amount;
        }
    }

    private void QTESuccess()
    {
        qteActive = false;
        Debug.Log("Climb: QTE Success! Moving up double!");
        MovePlayerUp(doubleMoveAmount);
    }

    private void QTEFailed()
    {
        qteActive = false;
        GameOver = true;
        Debug.Log("Climb: QTE Failed! Game Over!");
    }

    private void TriggerStopFailed()
    {
        stopActive = false;
        qteActive = false;
        qteEnabled = false;
        GameOver = true;
        Debug.Log("Climb: STOP failure - Player moved. Game Over!");
    }
}
