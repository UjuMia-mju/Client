using UnityEngine;
using System.Collections;

public class StageUIManager : MonoBehaviour
{
    [Header("UI Navigation Buttons")] 
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;

    [Header("UI Pop-Up Settings")] 
    [SerializeField] private float popUpDuration = 0.2f;
    [SerializeField] private Vector3 finalPanelScale = new Vector3(1f, 1f, 1f);

    private GameObject _currentPanel;

    public void ToggleNavButtons(bool isVisible)
    {
        if (leftButton != null) leftButton.SetActive(isVisible);
        if (rightButton != null) rightButton.SetActive(isVisible);
    }

    public IEnumerator OpenPanel(GameObject panelPrefab, string stageName, int difficulty, string description, int estimatedClearTimeSeconds)
    {
        if (panelPrefab == null) yield break;

        _currentPanel = Instantiate(panelPrefab);
        
        SelectPanelController panelInfo = _currentPanel.GetComponent<SelectPanelController>();
        if (panelInfo != null)
        {
            panelInfo.SetInfo(stageName, difficulty, description, estimatedClearTimeSeconds);
        }

        RectTransform rect = _currentPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero; 
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.zero; 
        }

        Canvas canvas = _currentPanel.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
        }

        yield return StartCoroutine(DynamicPopUpPanel(_currentPanel));
    }

    public IEnumerator ClosePanel()
    {
        if (_currentPanel != null)
        {
            yield return StartCoroutine(DynamicClosePanel(_currentPanel));
            Destroy(_currentPanel);
            _currentPanel = null;
        }
    }

    private IEnumerator DynamicPopUpPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, finalPanelScale, t);
            yield return null;
        }
        cg.alpha = 1f;
        panel.transform.localScale = finalPanelScale;
    }

    private IEnumerator DynamicClosePanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        Vector3 startScale = panel.transform.localScale;
        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / popUpDuration);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            panel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
    }
}