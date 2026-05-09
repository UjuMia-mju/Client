using UnityEngine;
using TMPro;

/// <summary>
/// 메인 화면에서 "이름#태그"를 표시한다.
/// </summary>
public class MainPlayerInfoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;
    [Tooltip("{0}에 이름#Tag가 들어갑니다. 접두어 없이만 쓰려면 \"{0}\" 그대로 두면 됩니다.")]
    [SerializeField] private string format = "{0}";

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
