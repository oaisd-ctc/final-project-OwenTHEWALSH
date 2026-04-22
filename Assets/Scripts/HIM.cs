using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HIM : MonoBehaviour
{
    [Header("Visibility Settings")]
    [Tooltip("Renderer component to control visibility (shadow state).")]
    [SerializeField]
    private Renderer himRenderer;

    [Tooltip("Additional renderer to fade (e.g., Eyes or another child component).")]
    [SerializeField]
    private Renderer additionalRenderer;

    [Tooltip("Alpha value when hidden in shadows (0-1).")]
    [SerializeField]
    private float shadowAlpha = 0.1f;

    [Tooltip("Alpha value when visible/speaking (0-1).")]
    [SerializeField]
    private float visibleAlpha = 1f;

    [Tooltip("Time to fade in/out (seconds).")]
    [SerializeField]
    private float fadeDuration = 0.5f;

    [Header("Text Box Settings")]
    [Tooltip("Canvas or parent object for text boxes to appear.")]
    [SerializeField]
    private Transform textBoxParent;

    [Tooltip("Prefab for text box UI (optional).")]
    [SerializeField]
    private GameObject textBoxPrefab;

    private Coroutine fadeCoroutine;
    private bool isVisible = false;
    private Material himMaterial;
    private Material additionalMaterial;

    private void Start()
    {
        if (himRenderer == null)
        {
            himRenderer = GetComponent<Renderer>();
        }

        if (himRenderer != null)
        {
            himMaterial = himRenderer.material;
            SetAlpha(himMaterial, shadowAlpha);
            isVisible = false;
        }
        else
        {
            Debug.LogWarning("HIM: No Renderer found. Visibility control will not work.");
        }

        if (additionalRenderer != null)
        {
            additionalMaterial = additionalRenderer.material;
            SetAlpha(additionalMaterial, shadowAlpha);
        }
    }

    private void Update()
    {   
        
    }

    public void ShowHIM(string dialogueText = "")
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(visibleAlpha));
        isVisible = true;

        if (!string.IsNullOrEmpty(dialogueText))
        {
            ShowTextBox(dialogueText);
        }
    }

    public void HideHIM()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(shadowAlpha));
        isVisible = false;
        HideTextBox();
    }

    public void ShowTextBox(string text)
    {
        // Leave this for you to implement with your UI system
        Debug.Log($"HIM Says: {text}");
        // TODO: Instantiate or activate textBoxPrefab here with the dialogue text
    }

    public void HideTextBox()
    {
        // Leave this for you to implement with your UI system  
        Debug.Log("Text box hidden");
        // TODO: Deactivate or destroy text boxes here
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (himMaterial == null)
            yield break;

        Color startHIMColor = himMaterial.color;
        Color startAdditionalColor = additionalMaterial != null ? additionalMaterial.color : Color.white;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            float newAlpha = Mathf.Lerp(startHIMColor.a, targetAlpha, t);
            
            SetAlpha(himMaterial, newAlpha);
            
            if (additionalMaterial != null)
            {
                SetAlpha(additionalMaterial, newAlpha);
            }

            yield return null;
        }

        SetAlpha(himMaterial, targetAlpha);
        if (additionalMaterial != null)
        {
            SetAlpha(additionalMaterial, targetAlpha);
        }

        fadeCoroutine = null;
    }

    private void SetAlpha(Material material, float alpha)
    {
        if (material == null)
            return;

        Color color = material.color;
        color.a = alpha;
        material.color = color;
    }

    public bool IsVisible()
    {
        return isVisible;
    }
}
