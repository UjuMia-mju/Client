using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OtherPlayerStats : MonoBehaviour
{
    private float oxygen = 1;
    private int hp = 5;

    //public GameObject hpParent;
    private List<Image> hpImageList = new List<Image>();

    private Image oxygenImage;
    private Color originalColor;

    private const float FADE_DURATION = 1.5f;
    private const float OXYGEN_DECREASE_INTERVAL = 1f;
    private const float HP_DISPLAY_DURATION = 1f;

    private const string HP = "HP";
    private const string OXYGEN = "Oxygen";

    private List<Coroutine> fadeCoroutines = new List<Coroutine>();

    private void Start()
    {
        hpImageList = new List<Image>();
        foreach (Image img in GetComponentsInChildren<Image>(true))
        {
            if (img.name.StartsWith(HP))
            {
                hpImageList.Add(img);
                StartCoroutine(FadeOutCoroutine(img, FADE_DURATION));
            }
            else if (img.name.StartsWith(OXYGEN))
            {
                oxygenImage = img;
            }
        }
    }

    private void Update()
    {
        oxygenImage.fillAmount = oxygen;
    }

    private IEnumerator FadeOutCoroutine(Image img, float duration)
    {
        originalColor = img.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            img.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }

    private void ReturnHPImageAlpha(Image img)
    {
        img.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
    }

    private IEnumerator DecreaseHpUI()
    {
        Debug.Log("체력 줄어듬 : " + hp);

        foreach (Coroutine c in fadeCoroutines)
        {
            if (c != null)
            {
                StopCoroutine(c);
            }
        }

        fadeCoroutines.Clear();


        foreach (Image img in hpImageList)
        {
            if (img != null)
            {
                ReturnHPImageAlpha(img);
            }
        }

        // 가장 오른쪽에 있는 이미지를 비활성화
        for (int i = hpImageList.Count - 1; i >= 0; i--)
        {
            if (hpImageList[i].IsActive())
            {
                hpImageList[i].gameObject.SetActive(false);
                break;
            }
        }

        yield return new WaitForSeconds(HP_DISPLAY_DURATION); // 체력 이미지가 보이는 시간

        foreach (Image img in hpImageList)
        {
            if (img != null)
            {
                Coroutine c = StartCoroutine(FadeOutCoroutine(img, FADE_DURATION));
                fadeCoroutines.Add(c);
            }
        }
    }

    //TODO : 패킷을 받도록 구현 필요
    public void SetStat(int hpData, float oxygenData)
    {
        // 이전 값과 비교해서 hp가 줄었는지 확인
        if (hpData < hp)
        {
            StartCoroutine(DecreaseHpUI());
        }

        hp = hpData;
        oxygen = oxygenData;
    }
}
