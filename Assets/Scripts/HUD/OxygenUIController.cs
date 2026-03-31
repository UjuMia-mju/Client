using UnityEngine;
using UnityEngine.UI;

public class OxygenUIController : MonoBehaviour
{
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Image oxygenImage;

    private void OnEnable()
    {
        playerStat.OnOxygenChanged += UpdateOxygenFill;
    }

    private void OnDisable()
    {
        playerStat.OnOxygenChanged -= UpdateOxygenFill;
    }

    private void UpdateOxygenFill(float amount)
    {
        if (oxygenImage != null)
        {
            oxygenImage.fillAmount = amount;
            
            // 추가 연출: 산소가 20% 이하일 때 빨간색으로 변경 등
            oxygenImage.color = (amount <= 0.2f) ? Color.red : Color.white;
        }
    }
}