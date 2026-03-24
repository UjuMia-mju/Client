using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStat : MonoBehaviour
{
    private float oxygen = 1;
    private int hp = 5;

    //public GameObject hpParent;
    private List<Image> hpImageList = new List<Image>();

    private Image oxygenImage;
    private Color originalHPColor;

    private const float FADE_DURATION = 1.5f;
    private const float OXYGEN_DECREASE_INTERVAL = 1f;
    private const float HP_DISPLAY_DURATION = 1f;

    private const string HP = "HP";
    private const string OXYGEN = "Oxygen";

    private List<Coroutine> fadeCoroutines = new List<Coroutine>();


    private Coroutine oxygenCoroutine;


    private void Start()
    {
        hpImageList = new List<Image>();
        foreach (Image img in GetComponentsInChildren<Image>(true))
        {
            if (img.name.StartsWith(HP))
            {
                originalHPColor = img.color;
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
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            img.color = new Color(originalHPColor.r, originalHPColor.g, originalHPColor.b, alpha);
            yield return null;
        }
    }

    private void ReturnHPImageAlpha(Image img)
    {
        img.color = new Color(originalHPColor.r, originalHPColor.g, originalHPColor.b, 1);
    }

    public IEnumerator OxygenDecrease()
    {
        while (true)
        {
            oxygen -= 0.01f;
            yield return new WaitForSeconds(OXYGEN_DECREASE_INTERVAL);
        }
    }

    public void StartOxygenRecover()
    {
        if (oxygenCoroutine == null)
        {
            oxygenCoroutine = StartCoroutine(OxygenIncrease());
        }
    }

    public void StopOxygenRecover()
    {
        if (oxygenCoroutine != null)
        {
            StopCoroutine(oxygenCoroutine);
            oxygenCoroutine = null;
        }
    }

    public IEnumerator OxygenIncrease()
    {
        while (true)
        {
            oxygen = Mathf.Min(oxygen + 0.06f, 1f);
            Debug.Log("산소 늘어남 : " + oxygen);
            yield return new WaitForSeconds(OXYGEN_DECREASE_INTERVAL);
        }
    }

    // TODO : 체력이 줄어들면, 잠깐 체력 이미지를 보여주고, 1초 후에 다시 사라지도록 합니다.
    // 또 현재 체력 수치에 맞게 이미지를 없애야 합니다.
    // 공격에 따라 데미지를 더 받을 수도 있는지 정해야 합니다.
    public IEnumerator DecreaseHp(int damage)
    {
        hp -= damage;
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

    public float GetOxygen()
    {
        return oxygen; 
    }

    public int GetHp()
    {
        return hp;
    }
}