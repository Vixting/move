using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private Transform spinnerTransform;
    
    [Header("Loading Tips")]
    [SerializeField] private TMP_Text tipsText;
    [SerializeField] private string[] loadingTips;
    [SerializeField] private float tipChangeInterval = 5f;
    
    private Coroutine tipChangeCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        // Make sure this starts hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Reset progress bar
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }
        
        // Start tip cycling
        if (tipsText != null && loadingTips.Length > 0)
        {
            SetRandomTip();
            tipChangeCoroutine = StartCoroutine(CycleTipsRoutine());
        }
    }

    private void OnDisable()
    {
        if (tipChangeCoroutine != null)
        {
            StopCoroutine(tipChangeCoroutine);
            tipChangeCoroutine = null;
        }
    }

    private void Update()
    {
        // Rotate loading spinner if available
        if (spinnerTransform != null)
        {
            spinnerTransform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    public void SetProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(progress);
        }
        
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {(progress * 100):0}%";
        }
    }

    private void SetRandomTip()
    {
        if (tipsText != null && loadingTips.Length > 0)
        {
            int randomIndex = Random.Range(0, loadingTips.Length);
            tipsText.text = loadingTips[randomIndex];
        }
    }

    private IEnumerator CycleTipsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tipChangeInterval);
            SetRandomTip();
        }
    }
}