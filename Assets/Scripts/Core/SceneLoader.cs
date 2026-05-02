using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Scene 변경과 변경 시 Fade 연출 (UIPanelAnimator 활용)
/// </summary>
public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    [Header("페이드 설정")]
    [SerializeField] private GameObject fadePrefab;
    [SerializeField] private UIPanelAnimator animator; // 인스펙터에서 할당하거나 자동 할당

    private GameObject fadeInstance;
    private CanvasGroup fadeCanvasGroup;

    protected override void Awake()
    {
        base.Awake();
        InitFadeCanvas();
    }

    private void InitFadeCanvas()
    {
        if (fadeInstance == null && fadePrefab != null)
        {
            fadeInstance = Instantiate(fadePrefab);
            fadeInstance.transform.SetParent(null);
            DontDestroyOnLoad(fadeInstance);
            
            fadeCanvasGroup = fadeInstance.GetComponent<CanvasGroup>();
            
            // 초기 상태 설정
            fadeInstance.SetActive(false);
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }

            // 프리팹에 없으면: DontDestroy로 유지된 싱글톤 우선(중복 AddComponent 시 파괴 방지)
            if (animator == null)
            {
                animator = UIPanelAnimator.Instance
                    ?? fadeInstance.GetComponent<UIPanelAnimator>()
                    ?? fadeInstance.AddComponent<UIPanelAnimator>();
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        if (fadeInstance == null) InitFadeCanvas();
        
        StopAllCoroutines();
        if (CanRunFadeLoad())
        {
            StartCoroutine(LoadAsyncSequenceWithFade(sceneName));
        }
        else
        {
            StartCoroutine(LoadAsyncSequenceNoFade(sceneName));
        }
    }

    private bool CanRunFadeLoad()
    {
        return fadeInstance != null && fadeCanvasGroup != null && animator != null;
    }

    /// <summary>fadePrefab 미배정 등 — 페이드 없이 비동기 씬 전환만 수행 (모든 경로를 SceneLoader로 맞출 때용)</summary>
    private IEnumerator LoadAsyncSequenceNoFade(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            if (op.progress >= 0.9f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private IEnumerator LoadAsyncSequenceWithFade(string sceneName)
    {
        // 1. 페이드 인 시작 (검은 화면 채우기)
        fadeInstance.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        // UIPanelAnimator의 FadeIn 기능을 사용 (Scale 연출이 필요 없다면 Vector3.one 고정)
        yield return StartCoroutine(animator.FadeIn(fadeInstance, Vector3.one));

        yield return null; 

        // 2. 비동기 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            if (op.progress >= 0.9f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        // 3. 씬 전환 후 잠시 대기했다가 페이드 아웃
        yield return new WaitForSecondsRealtime(0.2f);
        
        // UIPanelAnimator의 FadeOut 기능을 사용
        yield return StartCoroutine(animator.FadeOut(fadeInstance));

        // 4. 마무리
        fadeCanvasGroup.blocksRaycasts = false;
        fadeInstance.SetActive(false);
    }
}