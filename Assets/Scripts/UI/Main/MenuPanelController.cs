using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// MenuPanel의 버튼들의 Hover 및 Click 이벤트 관리
/// </summary>
public class MenuPanelController : MonoBehaviour
{
    [System.Serializable]
    public class MenuSet
    {
        public Button button;
        public GameObject panelPrefab;
    }

    [Header("Menu Sets")]
    [SerializeField] private Button singlePlayButton;
    [SerializeField] private Button multiPlayButton;
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
        _allMenuSets = new List<MenuSet> { settings, custom, store };

        // 1. Tag가 달린 모든 오브젝트 저장 (복구용)
        GameObject[] mainButtons = GameObject.FindGameObjectsWithTag(Define.Tag.MAINBUTTON);
        foreach (GameObject go in mainButtons)
        {
            _mainButtonObjects.Add(go);
        }

        // 2. 씬 이동 버튼 개별 초기화
        if (singlePlayButton != null)
        {
            _buttonOriginScales[singlePlayButton] = singlePlayButton.transform.localScale;
            InitSceneButton(singlePlayButton);
        }

        if (multiPlayButton != null)
        {
            _buttonOriginScales[multiPlayButton] = multiPlayButton.transform.localScale;
            InitSceneButton(multiPlayButton);
        }

        // 3. 패널 오픈 버튼 초기화 (기존 MenuSet)
        foreach (var set in _allMenuSets)
        {
            if (set.button == null) continue;
            _buttonOriginScales[set.button] = set.button.transform.localScale;
            InitPanelButton(set); 
        }
    }
    
    /// <summary>
    /// 씬 이동 전용 버튼 초기화 (Single, Multi)
    /// </summary>
    private void InitSceneButton(Button btn)
    {
        btn.onClick.AddListener(() => {
            SoundManager.Instance.PlaySFX("Click2");
            // Lobby 씬 완성 시 Define.Scene.Lobby 로 변경
            SceneLoader.Instance.LoadScene(Define.Scene.GAME); 
        });

        AddHoverEvents(btn); // 공통 호버 이벤트 연결
    }

    /// <summary>
    /// 패널 오픈 전용 버튼 초기화 (Settings, Custom, Store)
    /// </summary>
    private void InitPanelButton(MenuSet set)
    {
        set.button.onClick.AddListener(() => {
            SoundManager.Instance.PlaySFX("Click2");
            OnButtonClicked(set.button, set.panelPrefab);
        });

        AddHoverEvents(set.button); // 공통 호버 이벤트 연결
    }

    /// <summary>
    /// 호버(Hover) 이벤트 관리
    /// </summary>
    private void AddHoverEvents(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        
        AddEvent(trigger, EventTriggerType.PointerEnter, () => {
            SoundManager.Instance.PlaySFX("Hover");
            if (btn.interactable) 
                btn.transform.localScale = _buttonOriginScales[btn] * _hoverScale;
        });

        AddEvent(trigger, EventTriggerType.PointerExit, () => {
            if (btn.interactable) 
                btn.transform.localScale = _buttonOriginScales[btn];
        });
    }

    /// <summary>
    /// 클릭 이벤트
    /// </summary>
    private void OnButtonClicked(Button clickedBtn, GameObject panelPrefab)
    {
        DisableAllHovers();
        
        foreach (GameObject go in _mainButtonObjects)
        {
            if (go == null) continue;

            if (go != clickedBtn.gameObject)
            {
                go.SetActive(false);
            }
            else
            {
                foreach (Transform child in go.transform) child.gameObject.SetActive(false);
                if (go.TryGetComponent<Image>(out var img)) img.enabled = false;
            }
        }

        menuManager.StartZoomSequence(clickedBtn.transform, panelPrefab);
    }

    public void ResetAllButtons()
    {
        foreach (GameObject go in _mainButtonObjects)
        {
            if (go == null) continue;

            go.SetActive(true); 
            foreach (Transform child in go.transform) child.gameObject.SetActive(true);

            if (go.TryGetComponent<Button>(out var btn))
            {
                btn.interactable = true;
                if (go.TryGetComponent<Image>(out var img)) img.enabled = true;
                
                // _buttonOriginScales에 등록된 원본 크기가 있으면 복구
                if (_buttonOriginScales.ContainsKey(btn))
                    btn.transform.localScale = _buttonOriginScales[btn];
            }
        }
    }

    /// <summary>
    /// 모든 버튼의 호버 해제
    /// </summary>
    private void DisableAllHovers()
    {
        // 딕셔너리에 등록된 '모든' 버튼을 순회하며 비활성화합니다.
        foreach (var btn in _buttonOriginScales.Keys)
        {
            if (btn == null) continue;
            btn.interactable = false;
            btn.transform.localScale = _buttonOriginScales[btn];
        }
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }
}