using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameLoop : MonoBehaviour
{
    [Header("References")]
    [Tooltip("HIM script reference for tracking.")]
    [SerializeField]
    private GameObject himGameObject;

    [Tooltip("Text box GameObject for displaying intro text.")]
    [SerializeField]
    private GameObject textBoxGameObject;

    [Tooltip("QTE text GameObject to display during QTE.")]
    [SerializeField]
    private GameObject qteTextGameObject;

    [Header("QTE Settings")]
    [Tooltip("Probability of QTE triggering (0-1).")]
    [SerializeField]
    private float qteChance = 0.25f;

    [Tooltip("Time window to press E during QTE.")]
    [SerializeField]
    private float qteWindow = 2f;

    [Header("Movement Settings")]
    [Tooltip("Normal upward movement amount.")]
    [SerializeField]
    private float normalMoveAmount = 5f;

    [Tooltip("Double upward movement amount on QTE success.")]
    [SerializeField]
    private float doubleMoveAmount = 10f;

    [Header("STOP Settings")]
    [Tooltip("Minimum delay (seconds) before HIM says STOP.")]
    [SerializeField]
    private float stopMinDelay = 5f;

    [Tooltip("Maximum delay (seconds) before HIM says STOP.")]
    [SerializeField]
    private float stopMaxDelay = 10f;

    [Tooltip("Duration (seconds) the STOP check is active. If player moves during this window it's game over.")]
    [SerializeField]
    private float stopCheckDuration = 2f;

    [Header("HIM Dialogue")]
    [Tooltip("What HIM says during the STOP phase.")]
    [SerializeField]
    private string stopDialogue = "STOP";

    [Header("Detection Settings")]
    [Tooltip("Movement threshold to detect player movement (meters).")]
    [SerializeField]
    private float moveThreshold = 0.1f;

    private HIM himScript;
    private GameObject textBox;
    private GameObject qteText;
    private Vector3 lastPlayerPosition;
    private bool textActive;
    private bool qteActive;
    private float qteTimer;
    private bool qteEnabled = false;
    private bool movementLocked = false;
    public bool GameOver = false;

    // STOP state
    private bool stopActive = false;
    private Vector3 stopStartPosition;
    private Coroutine stopCoroutine;
    private Coroutine climbSequenceCoroutine;

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

        if (qteTextGameObject != null)
        {
            qteText = qteTextGameObject;
            qteText.SetActive(false);
        }
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
                Debug.Log("GameLoop: Player moved during intro text! Game Over.");
                TriggerGameOver("Player moved during intro text");
                return;
            }
        }

        // STOP takes priority: if STOP is active and player moves -> Game Over
        if (stopActive && Camera.main != null)
        {
            if (Vector3.Distance(Camera.main.transform.position, stopStartPosition) > moveThreshold)
            {
                Debug.Log("GameLoop: Player moved during STOP! Game Over by STOP priority.");
                TriggerGameOver("Player moved during STOP");
                return;
            }
        }

        // Handle Space key to attempt climb (only after QTE is enabled and movement not locked)
        if (!GameOver && qteEnabled && !movementLocked && Input.GetKeyDown(KeyCode.Space))
        {
            if (stopActive)
            {
                Debug.Log("GameLoop: Attempt ignored because STOP is active.");
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
                if (stopActive)
                {
                    Debug.Log("GameLoop: QTE input ignored due to STOP priority.");
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

    public void ActivateClimbSequence(GameObject displayTextBox, string displayText, float delayBeforeClimb)
    {
        textBox = displayTextBox;
        lastPlayerPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        if (textBox != null)
        {
            textBox.SetActive(true);
        }

        if (climbSequenceCoroutine != null)
        {
            StopCoroutine(climbSequenceCoroutine);
        }
        climbSequenceCoroutine = StartCoroutine(TextBoxRoutine(displayText, delayBeforeClimb));

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

        Debug.Log($"GameLoop: Waiting {delayBeforeClimb} seconds before QTE starts...");
        yield return new WaitForSeconds(delayBeforeClimb);

        if (GameOver)
            yield break;

        movementLocked = false;
        qteEnabled = true;
        Debug.Log("GameLoop: QTE enabled! Press Space to climb!");
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
            Debug.Log("GameLoop: Normal climb! No QTE this time.");
        }
    }

    private IEnumerator QTERoutine()
    {
        qteActive = true;
        qteTimer = qteWindow;
        Debug.Log($"GameLoop: QTE started! Press E in {qteWindow} seconds!");

        if (qteText != null)
        {
            qteText.SetActive(true);
            Debug.Log("GameLoop: QTE text activated!");
        }

        yield return new WaitForSeconds(qteWindow);

        if (qteActive)
        {
            QTEFailed();
        }
    }

    private IEnumerator StopRoutine()
    {
        float delay = Random.Range(stopMinDelay, stopMaxDelay);
        Debug.Log($"GameLoop: STOP will be announced in {delay:F2} seconds.");
        yield return new WaitForSeconds(delay);

        if (GameOver)
            yield break;

        if (himScript != null)
        {
            himScript.ShowHIM(stopDialogue);
        }
        else
        {
            Debug.Log("GameLoop: HIM not assigned, but STOP announced internally.");
        }

        bool previousQteEnabled = qteEnabled;
        qteEnabled = false;

        if (qteActive)
        {
            qteActive = false;
            Debug.Log("GameLoop: Active QTE cancelled due to STOP priority.");
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
        Debug.Log("GameLoop: STOP window ended.");
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

        if (qteText != null)
        {
            qteText.SetActive(false);
        }

        Debug.Log("GameLoop: QTE Success! Moving up double!");
        MovePlayerUp(doubleMoveAmount);
    }

    private void QTEFailed()
    {
        qteActive = false;
        TriggerGameOver("QTE Failed");
    }

    private void TriggerGameOver(string reason)
    {
        stopActive = false;
        qteActive = false;
        qteEnabled = false;
        GameOver = true;

        if (qteText != null)
        {
            qteText.SetActive(false);
        }

        if (textBox != null)
        {
            textBox.SetActive(false);
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }

        if (climbSequenceCoroutine != null)
        {
            StopCoroutine(climbSequenceCoroutine);
            climbSequenceCoroutine = null;
        }

        Debug.Log($"GameLoop: Game Over! Reason: {reason}");
    }
}
 