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

    [Header("Menu Sets")]
    [SerializeField] private MenuSet singlePlay;
    [SerializeField] private MenuSet multiPlay;
    [SerializeField] private MenuSet settings;
    [SerializeField] private MenuSet custom;
    [SerializeField] private MenuSet store;
    
    [Header(" ")]
    [SerializeField] private MenuManager menuManager;
    // Hover
    private Dictionary<Button, Vector3> _buttonOriginScales = new Dictionary<Button, Vector3>();
    private float _hoverScale = 1.1f;
    
    // Reset용 리스트
    private List<MenuSet> _allMenuSets = new List<MenuSet>();
    private List<GameObject> _mainButtonObjects = new List<GameObject>();
    
    void Start()
    {
        _allMenuSets = new List<MenuSet> { singlePlay, multiPlay, settings, custom, store };

        // 1. 시작 시점에 Tag가 달린 모든 오브젝트를 리스트에 미리 저장
        GameObject[] mainButtons = GameObject.FindGameObjectsWithTag(Define.Tag.MAINBUTTON);
        foreach (GameObject go in mainButtons)
        {
            _mainButtonObjects.Add(go);
        }

        // 2. 버튼별 이벤트 초기화
        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;

            _buttonOriginScales[set.button] = set.button.transform.localScale;
            InitButtonEvents(set); 
        }
    }
    
    /// <summary>
    /// 버튼 클릭, 호버 이벤트 등록
    /// </summary>
    private void InitButtonEvents(MenuSet set)
    {
        // 버튼 클릭 시 연출 실행
        set.button.onClick.AddListener(() => {
            OnButtonClicked(set.button, set.panelPrefab);
        });

        // 호버(마우스 올림/내림) 효과
        EventTrigger trigger = set.button.gameObject.GetComponent<EventTrigger>() ?? set.button.gameObject.AddComponent<EventTrigger>();
        
        // PointerEnter (커짐)
        AddEvent(trigger, EventTriggerType.PointerEnter, () => {
            if (set.button.interactable) 
                set.button.transform.localScale = _buttonOriginScales[set.button] * _hoverScale;
        });

        // PointerExit (복구)
        AddEvent(trigger, EventTriggerType.PointerExit, () => {
            if (set.button.interactable) 
                set.button.transform.localScale = _buttonOriginScales[set.button];
        });
    }

    /// <summary>
    /// 클릭 이벤트 시 대상 외 모든 버튼을 비활성화
    /// </summary>
    private void OnButtonClicked(Button clickedBtn, GameObject panelPrefab)
    {
        DisableAllHovers();

        foreach (GameObject go in _mainButtonObjects)
        {
            if (go == null) continue;

            if (go != clickedBtn.gameObject)
            {
                // 다른 버튼들은 통째로 비활성화
                go.SetActive(false);
            }
            else
            {
                // MainButton(Tag) 외의 모든 자식개체 비활성화
                foreach (Transform child in go.transform)
                {
                    child.gameObject.SetActive(false);
                }
            
                // 버튼 자체의 이미지(배경)도 있다면 숨김
                if (go.TryGetComponent<Image>(out var img)) img.enabled = false;
            }
        }

        menuManager.StartZoomSequence(clickedBtn.transform, panelPrefab);
    }

    /// <summary>
    /// 모든 버튼 활성화
    /// </summary>
    public void ResetAllButtons()
    {
        foreach (GameObject go in _mainButtonObjects)
        {
            if (go == null) continue;

            // 클릭 버튼 활성화
            go.SetActive(true); 
        
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetActive(true);
            }

            if (go.TryGetComponent<Button>(out var btn))
            {
                btn.interactable = true;
                if (go.TryGetComponent<Image>(out var img)) img.enabled = true;
                btn.transform.localScale = _buttonOriginScales[btn];
            }
        }
    }

    /// <summary>
    /// Hover 기능 해제
    /// </summary>
    private void DisableAllHovers()
    {
        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;
            set.button.interactable = false;
            set.button.transform.localScale = _buttonOriginScales[set.button];
        }
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }
}