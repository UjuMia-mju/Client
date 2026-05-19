using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HPUIController : MonoBehaviour
{
    public PlayerStat playerStat;
    [SerializeField] private List<Image> hpImageList = new List<Image>();
    
    private List<Coroutine> fadeCoroutines = new List<Coroutine>();
    private Color originalColor;
    public float FADE_DURATION = 1.5f;
    public float DISPLAY_DURATION = 1.0f;

    public void SetPlayerStat(PlayerStat stat)
    {
        if (playerStat == stat) return;
        Unhook();
        playerStat = stat;
        if (isActiveAndEnabled)
            Hook();
    }

    private void Awake()
    {
        if (hpImageList.Count > 0) originalColor = hpImageList[0].color;
    }

    private void Hook()
    {
        if (playerStat == null) return;
        playerStat.OnHpChanged -= UpdateHPUI;
        playerStat.OnHpChanged += UpdateHPUI;
        UpdateHPUI(playerStat.GetHp());
    }

    private void Unhook()
    {
        if (playerStat == null) return;
        playerStat.OnHpChanged -= UpdateHPUI;
    }

    private void OnEnable()
    {
        Hook();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void UpdateHPUI(int currentHp)
    {
        StopAllCoroutines();
        fadeCoroutines.Clear();

        for (int i = 0; i < hpImageList.Count; i++)
        {
            hpImageList[i].gameObject.SetActive(i < currentHp);
            SetAlpha(hpImageList[i], 1f);
        }

        StartCoroutine(WaitAndFadeOut()); // ← 페이드 트리거 살아있음
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