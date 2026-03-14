using UnityEngine;
using TMPro;

/// <summary>
/// 메인 화면에서 "내 이름#태그"를 표시한다.
/// 상대방이 나를 초대할 때 이 정보를 입력하면 된다.
/// </summary>
public class MainPlayerInfoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;
    [Tooltip("예: \"내 정보 (초대받을 때 상대가 입력): {0}\"")]
    [SerializeField] private string format = "내 정보: {0}";

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (displayText == null) return;

        string name = NetManager.Instance != null ? NetManager.Instance.PlayerName : "";
        int tag = NetManager.Instance != null ? NetManager.Instance.PlayerTag : 0;
        string combined = $"{name}#{tag}";

        displayText.text = string.Format(format, combined);
    }
}
