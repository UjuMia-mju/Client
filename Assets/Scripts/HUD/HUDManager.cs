using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// 게임 내 HUD 및 UI 패널들의 생성, 제거, 연출을 관리합니다.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject pausePanel; 

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private Vector3 targetScale = Vector3.one; 

    private UIPanelAnimator animator;
    private GameObject currentPanel;
    
    // 상태 관리 변수
    private GameObject currentActivePanel;
    private bool isTransitioning = false; // 애니메이션 중 중복 입력 방지
    private Key closeKey = Key.Escape;
    
    private void Awake() => animator = GetComponent<UIPanelAnimator>();

    public bool IsPanelOpen => currentActivePanel != null; // 패널 오픈 여부 확인용
    
    private void Update()
    {
        // Escape 키 입력 처리
        if (Keyboard.current[closeKey].wasPressedThisFrame && !isTransitioning)
        {
            HandleTogglePanel();
        }
    }

    private void HandleTogglePanel()
    {
        if (currentActivePanel == null)
        {
            OpenPanel(pausePanel);
        }
        else
        {
            ClosePanel();
        }
    }
    
    /// <summary>
    /// 새로운 HUD 패널을 생성하고 나타나는 연출을 실행합니다.
    /// </summary>
    public void OpenPanel(GameObject prefab)
    {
        if (prefab == null) return;
        
        if (currentActivePanel != null)
        {
            Destroy(currentActivePanel); 
        }

        currentActivePanel = Instantiate(prefab, transform);
        StartCoroutine(FadeInSequence(currentActivePanel));
    }

    /// <summary>
    /// 현재 활성화된 패널을 연출과 함께 제거합니다.
    /// </summary>
    public void ClosePanel()
    {
        if (currentActivePanel != null && !isTransitioning)
        {
            StartCoroutine(FadeOutSequence(currentActivePanel));
        }
    }

    #region UI Animations (Coroutines)

    private IEnumerator FadeInSequence(GameObject panel)
    {
        isTransitioning = true;

        // Alpha 조절을 위해 CanvasGroup 확인 및 추가
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        // 초기 상태 설정
        cg.alpha = 0f;
        panel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / animDuration);

            cg.alpha = Mathf.Lerp(0f, 1f, t);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        cg.alpha = 1f;
        panel.transform.localScale = targetScale;
        isTransitioning = false;
    }

    private IEnumerator FadeOutSequence(GameObject panel)
    {
        isTransitioning = true;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / animDuration);

            cg.alpha = Mathf.Lerp(1f, 0f, t);
            
            yield return null;
        }

        Destroy(panel);
        currentActivePanel = null;
        isTransitioning = false;
    }

    #endregion
}