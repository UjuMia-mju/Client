using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Scene 변경과 변경 시 Fade 연출
/// </summary>
public class SceneLoader : MonoBehaviorSingleton<SceneLoader>
{
    [Header("페이드 설정")]
    public GameObject fadePrefab;
    public float fadeDuration = 1.0f;

    private CanvasGroup fadeCanvasGroup;
    private GameObject fadeInstance;
    
    private bool isFading = false;
    private float targetAlpha = 0f;

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
            fadeInstance.SetActive(false);
            fadeCanvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (!isFading || fadeCanvasGroup == null) return;

        fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime / fadeDuration);

        if (Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            if (targetAlpha <= 0.01f) // 부동소수점 오차 고려
            {
                fadeCanvasGroup.alpha = 0f;
                isFading = false;
                fadeCanvasGroup.blocksRaycasts = false;
                if (fadeInstance != null) fadeInstance.SetActive(false);
            }
            else if (targetAlpha >= 0.99f)
            {
                fadeCanvasGroup.alpha = 1f;
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        if (fadeInstance == null) InitFadeCanvas();
        
        StopAllCoroutines();
        StartCoroutine(LoadAsyncSequence(sceneName));
    }

    private IEnumerator LoadAsyncSequence(string sceneName)
    {
        // 1. 페이드 인 시작
        if (fadeInstance != null) fadeInstance.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        targetAlpha = 1.0f;
        isFading = true;

        // 페이드가 완전히 찰 때까지 대기
        while (fadeCanvasGroup.alpha < 0.99f) yield return null;

        // [중요 수정 포인트] 
        // 씬을 넘기기 전 딱 한 프레임을 쉬어줍니다. 
        // 이 한 프레임 사이에 Splash 씬에 새로 배치된 다른 매니저들의 Awake가 실행되어 
        // DontDestroyOnLoad가 안전하게 처리됩니다.
        yield return null; 

        // 2. 비동기 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // progress가 0.9일 때 로딩 완료로 간주
            if (op.progress >= 0.9f)
            {
                // 약간의 여유를 주고 씬 전환 허용
                yield return new WaitForSecondsRealtime(0.1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        // 3. 씬 전환 후 잠시 대기했다가 페이드 아웃
        yield return new WaitForSecondsRealtime(0.2f);
        targetAlpha = 0.0f;
        
        // 페이드 아웃이 끝날 때까지 대기 (Update에서 isFading을 false로 바꿈)
        while (isFading) yield return null;
    }
}