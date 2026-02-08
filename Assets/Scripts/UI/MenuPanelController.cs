using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MenuPanelController : MonoBehaviour
{
    // 버튼별 원래 크기를 저장할 딕셔너리
    private Dictionary<GameObject, Vector3> _originScales = new Dictionary<GameObject, Vector3>();
    private float _hoverScale = 1.1f;

    void Start()
    {
        // 게임 시작 시 MainButton 태그가 달린 모든 오브젝트를 찾아 초기화
        GameObject[] mainButtons = GameObject.FindGameObjectsWithTag(Define.Tag.MAINBUTTON);

        foreach (GameObject obj in mainButtons)
        {
            // 1. 초기 크기 저장
            if (!_originScales.ContainsKey(obj))
                _originScales[obj] = obj.transform.localScale;

            // 2. 버튼 컴포넌트가 있다면 이벤트 연결
            Button btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                InitButtonEvents(btn);
            }
        }
    }

    private void InitButtonEvents(Button btn)
    {
        // 클릭 이벤트
        btn.onClick.AddListener(() => {
            // 버튼 이름으로 패널 프리팹 찾기 (예: SinglePlayButton -> SinglePlayPanel)
            string panelPath = btn.gameObject.name.Replace("Button", "Panel");
            
            // 프리팹 로드 (Resources 폴더 기준) - *만약 인스펙터 할당 방식을 쓴다면 이 부분은 수정 필요
            GameObject panelPrefab = Resources.Load<GameObject>($"Panels/{panelPath}");

            OnButtonClicked(btn.gameObject, panelPrefab);
        });

        // 호버 이벤트 (EventTrigger)
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        
        AddEvent(trigger, EventTriggerType.PointerEnter, () => {
            if (btn.interactable) 
                btn.transform.localScale = _originScales[btn.gameObject] * _hoverScale;
        });

        AddEvent(trigger, EventTriggerType.PointerExit, () => {
            if (btn.interactable) 
                btn.transform.localScale = _originScales[btn.gameObject];
        });
    }

    private void OnButtonClicked(GameObject clickedObj, GameObject panelPrefab)
    {
        // 1. 태그로 모든 MainButton 찾기
        GameObject[] allUi = GameObject.FindGameObjectsWithTag(Define.Tag.MAINBUTTON);

        foreach (GameObject ui in allUi)
        {
            // 클릭한 버튼은 상호작용만 끄고(크기 원복), 나머지는 숨김
            if (ui == clickedObj)
            {
                Button btn = ui.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
                ui.transform.localScale = _originScales[ui];
            }
            else
            {
                // 나머지는 페이드 아웃으로 숨기기
                StartCoroutine(FadeOutUI(ui));
            }
        }

        // 2. 매니저에게 줌 연출 요청
        if (panelPrefab != null)
        {
            MenuManager.Instance.StartZoomSequence(clickedObj.transform, panelPrefab);
        }
    }

    // UI 숨기기 (CanvasGroup 활용)
    private IEnumerator FadeOutUI(GameObject ui)
    {
        CanvasGroup cg = ui.GetComponent<CanvasGroup>();

        float duration = 0.4f;
        float elapsed = 0f;

        // 클릭 방지
        cg.blocksRaycasts = false;
        Button btn = ui.GetComponent<Button>();
        if(btn != null) btn.interactable = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0f;
    }

    // ESC 복구 시 호출
    public void ResetAllButtons()
    {
        // 태그로 모든 UI 다시 찾아서 복구
        GameObject[] allUi = GameObject.FindGameObjectsWithTag(Define.Tag.MAINBUTTON);

        foreach (GameObject ui in allUi)
        {
            CanvasGroup cg = ui.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }

            Button btn = ui.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
            }

            if (_originScales.ContainsKey(ui))
            {
                ui.transform.localScale = _originScales[ui];
            }
        }
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }
}