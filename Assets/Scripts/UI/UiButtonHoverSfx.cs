using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI <see cref="Button"/>에 포인터가 올라갈 때 효과음을 한 번만 등록합니다(중복 EventTrigger 방지).
/// </summary>
public static class UiButtonHoverSfx
{
    public sealed class HoverSfxRegistered : MonoBehaviour { }

    public static void Register(Button btn, string sfxName = "Hover")
    {
        if (btn == null) return;
        if (btn.GetComponent<HoverSfxRegistered>() != null) return;

        btn.gameObject.AddComponent<HoverSfxRegistered>();

        var trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => SoundManager.Instance.PlaySFX(sfxName));
        trigger.triggers.Add(entry);
    }
}
