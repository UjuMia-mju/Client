using UnityEngine;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logoObject; // 애니메이션을 적용할 게임 오브젝트
    [SerializeField] private UIPanelAnimator animator;

    [Header("Sequence Settings")]
    [SerializeField] private float stayTime = 2.0f;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Splash");

        if (animator == null) animator = UIPanelAnimator.Instance;
        
        if (logoObject != null && animator != null)
        {
            StartCoroutine(PlaySplashSequence());
        }
    }

    private IEnumerator PlaySplashSequence()
    {
        // 1. 로고 나타남 (FadeIn)
        // 로고는 보통 원래 크기(Vector3.one)로 나타나야 하므로 두 번째 인자로 전달
        yield return StartCoroutine(animator.FadeIn(logoObject, Vector3.one));

        // 2. 유지
        yield return new WaitForSeconds(stayTime);

        // 3. 로고 사라짐 (FadeOut)
        yield return StartCoroutine(animator.FadeOut(logoObject));

        // 4. 로고가 완전히 사라지면 다음 씬으로 이동
        SceneLoader.Instance.LoadScene(Define.Scene.LOGIN);
    }
}