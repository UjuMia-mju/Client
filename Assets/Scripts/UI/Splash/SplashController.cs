using UnityEngine;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logoObject;

    [Header("Sequence Settings")]
    [SerializeField] private float stayTime = 2.0f;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Splash");

        var panelAnimator = UIPanelAnimator.Instance;
        if (logoObject != null && panelAnimator != null)
            StartCoroutine(PlaySplashSequence(panelAnimator));
    }

    private IEnumerator PlaySplashSequence(UIPanelAnimator panelAnimator)
    {
        yield return StartCoroutine(panelAnimator.FadeIn(logoObject, Vector3.one));

        yield return new WaitForSeconds(stayTime);

        yield return StartCoroutine(panelAnimator.FadeOut(logoObject));

        SceneLoader.Instance.LoadScene(Define.Scene.LOGIN);
    }
}
