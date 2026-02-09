using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MenuPanelController : MonoBehaviour
{
    [System.Serializable]
    public class MenuSet
    {
        public Button button;
        public GameObject panelPrefab;
    }

    [Header("Menu Assignments")]
    [SerializeField] private MenuSet singlePlay;
    [SerializeField] private MenuSet multiPlay;
    [SerializeField] private MenuSet settings;
    [SerializeField] private MenuSet custom;
    [SerializeField] private MenuSet store;

    private Dictionary<Button, Vector3> _buttonOriginScales = new Dictionary<Button, Vector3>();
    private List<MenuSet> _allMenuSets = new List<MenuSet>();
    private float _hoverScale = 1.1f;

    void Start()
    {
        // 리스트에 담아 관리 편의성 확보
        _allMenuSets = new List<MenuSet> { singlePlay, multiPlay, settings, custom, store };

        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;

            _buttonOriginScales[set.button] = set.button.transform.localScale;
            InitButtonEvents(set);
        }
    }

    private void InitButtonEvents(MenuSet set)
    {
        // 클릭 시: 할당된 프리팹으로 줌인 연출 요청
        set.button.onClick.AddListener(() => {
            OnButtonClicked(set.button, set.panelPrefab);
        });

        // 호버 효과
        EventTrigger trigger = set.button.gameObject.GetComponent<EventTrigger>() ?? set.button.gameObject.AddComponent<EventTrigger>();
        
        AddEvent(trigger, EventTriggerType.PointerEnter, () => {
            if (set.button.interactable) 
                set.button.transform.localScale = _buttonOriginScales[set.button] * _hoverScale;
        });

        AddEvent(trigger, EventTriggerType.PointerExit, () => {
            if (set.button.interactable) 
                set.button.transform.localScale = _buttonOriginScales[set.button];
        });
    }

    private void OnButtonClicked(Button clickedBtn, GameObject panelPrefab)
    {
        // 1. 모든 Hover 상태 즉시 종료 및 잠금
        DisableAllHovers();

        // 2. 다른 버튼들 페이드 아웃 (코루틴)
        StartCoroutine(FadeOutOtherButtons(clickedBtn));

        // 3. MenuManager에게 줌인 연출 요청
        MenuManager.Instance.StartZoomSequence(clickedBtn.transform, panelPrefab);
    }

    public void ResetAllButtons()
    {
        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;

            CanvasGroup cg = set.button.GetComponent<CanvasGroup>() ?? set.button.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            set.button.interactable = true;
            set.button.transform.localScale = _buttonOriginScales[set.button];
        }
    }

    private void DisableAllHovers()
    {
        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;
            set.button.interactable = false;
            set.button.transform.localScale = _buttonOriginScales[set.button];
        }
    }

    private IEnumerator FadeOutOtherButtons(Button clickedBtn)
    {
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            foreach (var set in _allMenuSets)
            {
                if (set.button != clickedBtn && set.button != null)
                {
                    CanvasGroup cg = set.button.GetComponent<CanvasGroup>() ?? set.button.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = alpha;
                    cg.blocksRaycasts = false;
                }
            }
            yield return null;
        }
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }
}