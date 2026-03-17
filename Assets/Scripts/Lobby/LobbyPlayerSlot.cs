using TMPro;
using UnityEngine;

/// <summary>
/// LobbyAstronut 프리팹에 붙여, 플레이어 이름과 레디 상태를 표시합니다.
/// Name / ReadyState 자식 오브젝트의 TMP_Text를 찾아 설정합니다.
/// </summary>
public class LobbyPlayerSlot : MonoBehaviour
{
    [Header("비어 있으면 자식에서 이름으로 찾습니다")]
    [SerializeField] private TMP_Text nameLabel;       // 플레이어 이름 표시용 (오브젝트명 "Name")
    [SerializeField] private TMP_Text readyStateLabel; // "Ready" 텍스트 (오브젝트명 "ReadyState")

    private void Awake()
    {
        if (nameLabel == null || readyStateLabel == null)
            ResolveLabels();
        // 기본은 레디 미표시. S_READY 등 패킷 수신 시 SetReady(true) 호출 (2번 구현)
        SetReady(false);
    }

    /// <summary>자식 오브젝트 중 "Name", "ReadyState" 이름의 TMP_Text를 찾아 캐시합니다.</summary>
    void ResolveLabels()
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.gameObject.name == "Name")
                nameLabel = t;
            else if (t.gameObject.name == "ReadyState")
                readyStateLabel = t;
        }
    }

    /// <summary>표시할 플레이어 이름을 설정합니다.</summary>
    public void SetPlayerName(string playerName)
    {
        if (nameLabel == null) ResolveLabels();
        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
    }

    /// <summary>레디 상태 표시를 켜거나 끕니다. (2번 구현 시 사용)</summary>
    public void SetReady(bool ready)
    {
        if (readyStateLabel == null) ResolveLabels();
        if (readyStateLabel != null)
            readyStateLabel.gameObject.SetActive(ready);
    }
}
