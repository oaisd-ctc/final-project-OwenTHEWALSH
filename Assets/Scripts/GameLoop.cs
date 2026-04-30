using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameLoop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject himGameObject;
    [SerializeField] private GameObject textBoxGameObject;
    [SerializeField] private GameObject qteTextGameObject;

    [Header("QTE Settings")]
    [SerializeField] private float qteChance = 0.25f;
    [SerializeField] private float qteWindow = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float normalMoveAmount = 5f;
    [SerializeField] private float doubleMoveAmount = 10f;

    [Header("STOP Settings")]
    [SerializeField] private float stopMinDelay = 5f;
    [SerializeField] private float stopMaxDelay = 10f;
    [SerializeField] private float stopCheckDuration = 2f;
    [SerializeField] private string stopDialogue = "STOP";

    [Header("Detection Settings")]
    [SerializeField] private float moveThreshold = 0.1f;

    // Private state
    private HIM himScript;
    private GameObject textBox;
    private GameObject qteText;
    private Vector3 lastPlayerPosition;
    public bool GameOver { get; private set; } = false;

    // Active phase tracking
    private GamePhase currentPhase = GamePhase.Idle;
    private float phaseTimer = 0f;

    // QTE state
    private float qteTimeRemaining = 0f;

    // Coroutines
    private Coroutine stopCoroutine;    

    private enum GamePhase
    {
        Idle,
        IntroText,
        QTEWindow,
        QTEActive,
        Stop,
        GameOverPhase
    }

    private void Start()
    {
        himScript = himGameObject != null ? himGameObject.GetComponent<HIM>() : FindObjectOfType<HIM>();
        textBox = textBoxGameObject;
        qteText = qteTextGameObject;

        if (textBox != null) textBox.SetActive(false);
        if (qteText != null) qteText.SetActive(false);
    }

    private void Update()
    {
        if (GameOver)
            return;

        phaseTimer += Time.deltaTime;

        switch (currentPhase)
        {
            case GamePhase.IntroText:
                HandleIntroTextPhase();
                break;
            case GamePhase.QTEWindow:
                HandleQTEWindowPhase();
                break;
            case GamePhase.QTEActive:
                HandleQTEActivePhase();
                break;
            case GamePhase.Stop:
                HandleStopPhase();
                break;
        }
    }

    private void HandleIntroTextPhase()
    {
        if (HasPlayerMoved())
        {
            EndGame("Player moved during intro text");
            return;
        }

        if (phaseTimer >= 3f)
        {
            ExitIntroTextPhase();
        }
    }

    private void HandleQTEWindowPhase()
    {
        if (HasPlayerMoved())
        {
            EndGame("Player moved during climb attempt");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AttemptClimb();
        }
    }

    private void HandleQTEActivePhase()
    {
        if (HasPlayerMoved())
        {
            EndGame("Player moved during QTE");
            return;
        }

        qteTimeRemaining -= Time.deltaTime;

        if (qteTimeRemaining <= 0f)
        {
            QTETimeout();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            QTESuccess();
        }
    }

    private void HandleStopPhase()
    {
        if (HasPlayerMoved())
        {
            EndGame("Player moved during STOP");
            return;
        }

        if (phaseTimer >= stopCheckDuration)
        {
            ExitStopPhase();
        }
    }

    private bool HasPlayerMoved()
    {
        if (Camera.main == null)
            return false;

        return Vector3.Distance(Camera.main.transform.position, lastPlayerPosition) > moveThreshold;
    }

    public void ActivateClimbSequence(GameObject displayTextBox, string displayText, float delayBeforeClimb)
    {
        textBox = displayTextBox;
        lastPlayerPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        EnterIntroTextPhase(displayText, delayBeforeClimb);
        StartSTOPTimer();
    }

    // ========== INTRO TEXT PHASE ==========
    private void EnterIntroTextPhase(string text, float delayBeforeQTE)
    {
        currentPhase = GamePhase.IntroText;
        phaseTimer = 0f;

        if (textBox != null)
        {
            textBox.SetActive(true);
            TextMeshProUGUI textComponent = textBox.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = text;
        }

        // Schedule QTE start
        Invoke(nameof(EnterQTEWindowPhase), 3f + delayBeforeQTE);
    }

    private void ExitIntroTextPhase()
    {
        if (textBox != null)
            textBox.SetActive(false);
    }

    // ========== QTE WINDOW PHASE ==========
    private void EnterQTEWindowPhase()
    {
        if (GameOver)
            return;

        currentPhase = GamePhase.QTEWindow;
        phaseTimer = 0f;

        if (qteText != null)
            qteText.SetActive(true);

        Debug.Log("GameLoop: QTE Window open! Press Space to climb!");
    }

    private void ExitQTEWindowPhase()
    {
        if (qteText != null)
            qteText.SetActive(false);

        currentPhase = GamePhase.Idle;
    }

    private void AttemptClimb()
    {
        if (Random.value < qteChance)
        {
            // QTE triggered
            StartQTE();
        }
        else
        {
            // Normal climb
            ExitQTEWindowPhase();
            MovePlayerUp(normalMoveAmount);
            Debug.Log("GameLoop: Normal climb!");
        }
    }

    // ========== QTE ACTIVE PHASE ==========
    private void StartQTE()
    {
        currentPhase = GamePhase.QTEActive;
        phaseTimer = 0f;
        qteTimeRemaining = qteWindow;

        if (qteText != null)
            qteText.SetActive(true);

        Debug.Log($"GameLoop: QTE Active! Press E in {qteWindow} seconds!");
    }

    private void QTESuccess()
    {
        if (qteText != null)
            qteText.SetActive(false);

        currentPhase = GamePhase.Idle;

        Debug.Log("GameLoop: QTE Success! Moving up double!");
        MovePlayerUp(doubleMoveAmount);
    }

    private void QTETimeout()
    {
        EndGame("QTE Failed - Time expired");
    }

    // ========== STOP PHASE ==========
    private void StartSTOPTimer()
    {
        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);

        stopCoroutine = StartCoroutine(STOPTimerRoutine());
    }

    private IEnumerator STOPTimerRoutine()
    {
        float delay = Random.Range(stopMinDelay, stopMaxDelay);
        Debug.Log($"GameLoop: STOP in {delay:F2} seconds");

        yield return new WaitForSeconds(delay);

        if (GameOver)
            yield break;

        EnterStopPhase();
    }

    private void EnterStopPhase()
    {
        currentPhase = GamePhase.Stop;
        phaseTimer = 0f;
        lastPlayerPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        if (himScript != null)
            himScript.ShowHIM(stopDialogue);

        Debug.Log("GameLoop: STOP! Don't move!");
    }

    private void ExitStopPhase()
    {
        if (himScript != null)
            himScript.HideTextBox();

        currentPhase = GamePhase.Idle;
    }

    // ========== MOVEMENT & GAME OVER ==========
    private void MovePlayerUp(float amount)
    {
        if (Camera.main != null)
            Camera.main.transform.position += Vector3.up * amount;
    }

    public void LockPlayerMovement(bool isLocked)
    {
        // Implement in Player script if needed
    }

    private void EndGame(string reason)
    {
        if (GameOver)
            return;

        GameOver = true;
        currentPhase = GamePhase.GameOverPhase;

        CancelInvoke();
        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);

        if (qteText != null)
            qteText.SetActive(false);

        if (textBox != null)
            textBox.SetActive(false);

        Debug.Log($"GameLoop: GAME OVER - {reason}");
    }
}