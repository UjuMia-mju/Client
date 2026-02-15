using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
            fadeCanvasGroup.alpha = targetAlpha;
            if (targetAlpha <= 0f)
            {
                isFading = false;
                fadeCanvasGroup.blocksRaycasts = false;
                fadeInstance.SetActive(false);
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        // 씬 로더 자체가 파괴되었다면 다시 초기화 시도
        if (fadeInstance == null) InitFadeCanvas();
        
        StopAllCoroutines();
        StartCoroutine(LoadAsyncSequence(sceneName));
    }

    private IEnumerator LoadAsyncSequence(string sceneName)
    {
        fadeInstance.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        targetAlpha = 1.0f;
        isFading = true;

        while (fadeCanvasGroup.alpha < 1.0f) yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            if (op.progress >= 0.9f)
            {
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        targetAlpha = 0.0f;
    }
}