using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HPUIController : MonoBehaviour
{
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private List<Image> hpImageList = new List<Image>();
    
    private List<Coroutine> fadeCoroutines = new List<Coroutine>();
    private Color originalColor;
    public float FADE_DURATION = 1.5f;
    public float DISPLAY_DURATION = 1.0f;

    private void Awake()
    {
        if (hpImageList.Count > 0) originalColor = hpImageList[0].color;
    }

    private void OnEnable()
    {
        playerStat.OnHpChanged += UpdateHPUI;
    }

    private void OnDisable()
    {
        playerStat.OnHpChanged -= UpdateHPUI;
    }

    private void UpdateHPUI(int currentHp)
    {
        StopAllCoroutines();
        fadeCoroutines.Clear();

        // 1. 체력 개수만큼 활성화 및 투명도 복구
        for (int i = 0; i < hpImageList.Count; i++)
        {
            hpImageList[i].gameObject.SetActive(i < currentHp);
            SetAlpha(hpImageList[i], 1f);
        }

        // 2. 일정 시간 뒤 페이드 아웃 시작
        StartCoroutine(WaitAndFadeOut());
    }

    private IEnumerator WaitAndFadeOut()
    {
        yield return new WaitForSeconds(DISPLAY_DURATION);
        foreach (Image img in hpImageList)
        {
            if (img.gameObject.activeSelf)
                StartCoroutine(FadeOut(img));
        }
    }

    private IEnumerator FadeOut(Image img)
    {
        float elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            SetAlpha(img, Mathf.Lerp(1f, 0f, elapsed / FADE_DURATION));
            yield return null;
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}