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

    private Coroutine fadeOutCoroutine = null;

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

    //TODO : 패킷을 받도록 구현 필요
    public void SetStat(int hpData, float oxygenData)
    {
        hp = hpData;
        oxygen = oxygenData;
    }
}
