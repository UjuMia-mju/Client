using UnityEngine;

/// <summary>
/// 플레이어 머리 위 월드 Canvas(HP·산소 등) 표시 on/off.
/// </summary>
public static class PlayerOverheadUI
{
    public static void SetWorldCanvasActive(Transform playerRoot, bool active)
    {
        if (playerRoot == null)
            return;

        Transform canvas = playerRoot.Find("Canvas");
        if (canvas != null)
            canvas.gameObject.SetActive(active);
    }
}
