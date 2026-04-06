using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// 게임 내 HUD 및 UI 패널들의 생성, 제거, 관리를 담당 (연출은 UIPanelAnimator에 위임)
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject pausePanel; 

    [Header("Dependencies")]
    [SerializeField] private UIPanelAnimator animator;
    
    // 상태 관리 변수
    private GameObject currentActivePanel;
    private bool isTransitioning = false; 
    private Key closeKey = Key.Escape;
    
    private void Awake() 
    {
        // 인스펙터에서 할당하지 않았을 경우를 대비한 자동 할당
        if (animator == null) animator = GetComponent<UIPanelAnimator>();
    }

    public bool IsPanelOpen => currentActivePanel != null; 
    
    private void Update()
    {
        if (Keyboard.current[closeKey].wasPressedThisFrame && !isTransitioning)
        {
            HandleTogglePanel();
        }
    }

    private void HandleTogglePanel()
    {
        if (currentActivePanel == null)
            OpenPanel(pausePanel);
        else
            ClosePanel();
    }

    public void OpenPanel(GameObject prefab, Vector3 customScale)
    {
        if (prefab == null || isTransitioning) return;

        if (currentActivePanel != null)
        {
            Destroy(currentActivePanel);
        }

        currentActivePanel = Instantiate(prefab, transform);
        StartCoroutine(OpenSequence(currentActivePanel, customScale));
    }

    public void OpenPanel(GameObject prefab)
    {
        OpenPanel(prefab, Vector3.one);
    }

    public void ClosePanel()
    {
        if (currentActivePanel != null && !isTransitioning)
        {
            StartCoroutine(CloseSequence());
        }
    }

    #region Wrapper Coroutines
    // Animator의 코루틴을 실행하고 상태(isTransitioning)를 관리하는 래퍼 함수

    private IEnumerator OpenSequence(GameObject panel, Vector3 target)
    {
        isTransitioning = true;
        
        // UIPanelAnimator의 FadeIn 코루틴이 끝날 때까지 대기
        yield return StartCoroutine(animator.FadeIn(panel, target));
        
        isTransitioning = false;
    }

    private IEnumerator CloseSequence()
    {
        isTransitioning = true;
        
        // UIPanelAnimator의 FadeOut 코루틴이 끝날 때까지 대기
        yield return StartCoroutine(animator.FadeOut(currentActivePanel));
        
        currentActivePanel = null;
        isTransitioning = false;
    }
    #endregion
}