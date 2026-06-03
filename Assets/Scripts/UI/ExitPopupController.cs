using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ExitPopup 프리팹 루트. 예/아니요 및 <see cref="PanelTweenPresentation"/> 등장·퇴장 연출.
/// </summary>
public class ExitPopupController : MonoBehaviour
{
    [SerializeField] private float showDuration = PanelTweenPresentation.DefaultShowDuration;
    [SerializeField] private float hideDuration = PanelTweenPresentation.DefaultHideDuration;
    [SerializeField] private float showScaleFrom = PanelTweenPresentation.DefaultShowScaleFrom;

    Canvas _canvas;
    Button _acceptButton;
    Button _declineButton;

    Coroutine _transitionRoutine;
    bool _visible;
    bool _pauseHoldActive;

    public bool IsVisible => _visible;

    void Awake()
    {
        BindHierarchy();
        gameObject.SetActive(false);
        _visible = false;
    }

    void OnDestroy()
    {
        PanelTweenPresentation.Kill(gameObject);
        ReleasePauseHold();
    }

    public void InitializeCanvasSortOrder(int sortOrder)
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();

        if (_canvas == null)
            return;

        _canvas.overrideSorting = true;
        _canvas.sortingOrder = sortOrder;
    }

    void BindHierarchy()
    {
        _canvas = GetComponent<Canvas>();

        var messageBox = transform.Find("Panel/MessageBox");
        if (messageBox != null)
        {
            _acceptButton = messageBox.Find("Buttons/AcceptButton")?.GetComponent<Button>();
            _declineButton = messageBox.Find("Buttons/DeclineButton")?.GetComponent<Button>();
        }

        if (_acceptButton != null)
        {
            _acceptButton.onClick.RemoveListener(OnAcceptClicked);
            _acceptButton.onClick.AddListener(OnAcceptClicked);
        }

        if (_declineButton != null)
        {
            _declineButton.onClick.RemoveListener(OnDeclineClicked);
            _declineButton.onClick.AddListener(OnDeclineClicked);
        }
    }

    public void ShowAnimated()
    {
        if (_visible)
            return;

        if (_transitionRoutine != null && gameObject.activeInHierarchy)
            StopCoroutine(_transitionRoutine);

        gameObject.SetActive(true);
        transform.localScale = Vector3.one;

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = ExitPopupManager.CanvasSortOrder;
        }

        AcquirePauseHold();
        _visible = true;
        _transitionRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        PanelTweenPresentation.Kill(gameObject);
        yield return PanelTweenPresentation.Show(
            gameObject,
            Vector3.one,
            showDuration,
            PanelTweenPresentation.DefaultDimFadeDuration,
            PanelTweenPresentation.DefaultBodyFadeDuration,
            showScaleFrom);
        _transitionRoutine = null;
    }

    public void HideAnimated()
    {
        if (!_visible)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _visible = false;
        _transitionRoutine = StartCoroutine(HideRoutine());
    }

    IEnumerator HideRoutine()
    {
        PanelTweenPresentation.Kill(gameObject);
        yield return PanelTweenPresentation.Hide(gameObject, destroyOnEnd: false, hideDuration, showScaleFrom);
        ReleasePauseHold();
        gameObject.SetActive(false);
        _transitionRoutine = null;
    }

    void OnAcceptClicked()
    {
        SoundManager.Instance?.PlaySFX("Click3");
        HideAnimated();
        ExitPopupManager.ConfirmQuitApplication();
    }

    void OnDeclineClicked()
    {
        SoundManager.Instance?.PlaySFX("Click2");
        PausePanelUtility.DestroyAllOpen();
        HideAnimated();
    }

    void AcquirePauseHold()
    {
        if (_pauseHoldActive)
            return;

        _pauseHoldActive = true;
        InputManager.PushPauseMenuHold();
    }

    void ReleasePauseHold()
    {
        if (!_pauseHoldActive)
            return;

        _pauseHoldActive = false;
        InputManager.PopPauseMenuHold();
    }
}
