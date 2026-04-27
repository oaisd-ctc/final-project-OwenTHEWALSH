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
        // Check if player moved while text is active
        if (textActive)
        {
            if (Vector3.Distance(Camera.main.transform.position, lastPlayerPosition) > 0.1f)
            {
                Debug.Log("Climb: Player moved during text!");
                textActive = false;
                if (textBox != null)
                {
                    textBox.SetActive(false);
                }
            }
        }

        // Handle Space key to attempt climb (only after QTE is enabled and movement not locked)
        if (!GameOver && qteEnabled && !movementLocked && Input.GetKeyDown(KeyCode.Space))
        {
            AttemptClimb();
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
                QTESuccess();
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
        lastPlayerPosition = Camera.main.transform.position;
        
        if (textBox != null)
        {
            textBox.SetActive(true);
        }
        
        StartCoroutine(TextBoxRoutine(displayText, delayBeforeClimb));
    }

    private IEnumerator TextBoxRoutine(string text, float delayBeforeClimb)
    {
        textActive = true;
        if (textBox != null)
        {
            // Update text box with the display text
            TextMeshProUGUI textComponent = textBox.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }   
        yield return new WaitForSeconds(3f);
        textActive = false;
        if (textBox != null)
        {
            textBox.SetActive(false);
        }

        // Delay before enabling QTE
        Debug.Log($"Climb: Waiting {delayBeforeClimb} seconds before QTE starts...");
        yield return new WaitForSeconds(delayBeforeClimb);

        // Unlock player movement and enable QTE
        movementLocked = false;
        qteEnabled = true;
        Debug.Log("Climb: QTE enabled! Press Space to climb!");
    }

    private void AttemptClimb()
    {
        // 25% chance to trigger QTE
        float randomValue = Random.value;
        
        if (randomValue < qteChance)
        {
            // QTE triggered
            StartCoroutine(QTERoutine());
        }
        else
        {
            // Normal climb without QTE
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
}
