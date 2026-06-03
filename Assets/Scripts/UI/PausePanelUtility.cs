using UnityEngine;

/// <summary>
/// <see cref="Define.Tag.PAUSE_PANEL"/> 태그가 붙은 일시정지 UI를 일괄 제거합니다.
/// </summary>
public static class PausePanelUtility
{
    public static void DestroyAllOpen()
    {
        if (ScenePauseMenuController.Instance != null)
            ScenePauseMenuController.Instance.DismissPausePanelCompletely();

        if (StageManager.Instance != null)
            StageManager.Instance.DismissStagePausePanelCompletely();

        foreach (var hud in Object.FindObjectsByType<HUDManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            hud.DismissPausePanelCompletely();

        DestroyRemainingTaggedPanels();
    }

    static void DestroyRemainingTaggedPanels()
    {
        foreach (var controller in Object.FindObjectsByType<PausePanelController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (controller == null)
                continue;

            var go = controller.gameObject;
            if (go == null || !go.CompareTag(Define.Tag.PAUSE_PANEL))
                continue;

            if (!go.scene.IsValid())
                continue;

            Object.Destroy(go);
        }
    }
}
