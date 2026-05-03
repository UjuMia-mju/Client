using UnityEngine;

/// <summary>
/// Rocket Trigger 안에 로컬 Player가 있을 때만 CraftBubble을 활성화합니다.
/// Enter / Stay 동안 SetActive(true), Exit 및 비활성화 시 SetActive(false).
/// </summary>
public class SpaceShipCraftBubble : MonoBehaviour
{
    [Header("CraftBubble")]
    [SerializeField] private GameObject bubblePrefab;

    private GameObject _bubbleInstance;
    private int _playerOverlapCount;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerOverlapCount++;
        EnsureBubbleInstance();
        SetBubbleActive(true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayer(other))
            return;

        EnsureBubbleInstance();
        SetBubbleActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        if (_playerOverlapCount == 0)
            SetBubbleActive(false);
    }

    private void OnDisable()
    {
        SetBubbleActive(false);
        _playerOverlapCount = 0;
    }

    private static bool IsPlayer(Collider other)
    {
        if (other == null || !other.CompareTag(Define.Tag.PLAYER))
            return false;
        return other.GetComponentInParent<Player>() != null;
    }

    private void EnsureBubbleInstance()
    {
        if (bubblePrefab == null || _bubbleInstance != null)
            return;

        _bubbleInstance = Instantiate(bubblePrefab);
        _bubbleInstance.SetActive(false);
    }

    private void SetBubbleActive(bool active)
    {
        if (_bubbleInstance == null)
            return;
        _bubbleInstance.SetActive(active);
    }
}
