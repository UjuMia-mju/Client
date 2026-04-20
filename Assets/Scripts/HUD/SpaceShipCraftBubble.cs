using UnityEngine;

/// <summary>
/// Rocket의 Trigger Collider에 Player가 들어오면 CraftBubble 프리팹을 만들고 벗어나면 제거
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
        if (_playerOverlapCount != 1 || bubblePrefab == null || _bubbleInstance != null)
            return;
        
        _bubbleInstance = Instantiate(bubblePrefab);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        if (_playerOverlapCount == 0)
            DestroyBubble();
    }

    private void OnDisable()
    {
        DestroyBubble();
        _playerOverlapCount = 0;
    }

    private static bool IsPlayer(Collider other)
    {
        if (other == null || !other.CompareTag(Define.Tag.PLAYER))
            return false;
        return other.GetComponentInParent<Player>() != null;
    }

    private void DestroyBubble()
    {
        if (_bubbleInstance == null)
            return;
        Destroy(_bubbleInstance);
        _bubbleInstance = null;
    }
}
