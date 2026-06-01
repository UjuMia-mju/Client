using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 플레이어 사망 시 부활까지 남은 시간을 표시하는 DeadPanel을 제어합니다.
/// </summary>
public class DeadPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float respawnDelaySeconds = PlayerLifeServerManager.RespawnDelaySeconds;

    private Canvas panelCanvas;
    private PlayerStat playerStat;
    private Coroutine countdownCoroutine;

    private void Awake()
    {
        panelCanvas = GetComponent<Canvas>();
        ResolveCountdownTextIfNeeded();
        transform.localScale = Vector3.one;
        HideImmediate();
    }

    private void Start()
    {
        StartCoroutine(BindLocalPlayerWhenReady());
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void OnDestroy()
    {
        Unhook();
    }

    public void SetPlayerStat(PlayerStat stat)
    {
        if (playerStat == stat) return;
        Unhook();
        playerStat = stat;
        if (isActiveAndEnabled)
            Hook();
    }

    private IEnumerator BindLocalPlayerWhenReady()
    {
        while (playerStat == null)
        {
            var player = FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);
            if (player != null)
            {
                SetPlayerStat(player.GetComponent<PlayerStat>());
                yield break;
            }

            yield return null;
        }
    }

    private void ResolveCountdownTextIfNeeded()
    {
        if (countdownText != null) return;

        Transform textTransform = transform.Find("Text (TMP)");
        if (textTransform != null)
            countdownText = textTransform.GetComponent<TMP_Text>();
    }

    private void Hook()
    {
        if (playerStat == null) return;

        playerStat.OnPlayerDead -= HandlePlayerDead;
        playerStat.OnPlayerDead += HandlePlayerDead;
        playerStat.OnPlayerRevive -= HandlePlayerRevive;
        playerStat.OnPlayerRevive += HandlePlayerRevive;
    }

    private void Unhook()
    {
        if (playerStat == null) return;

        playerStat.OnPlayerDead -= HandlePlayerDead;
        playerStat.OnPlayerRevive -= HandlePlayerRevive;
    }

    private void HandlePlayerDead()
    {
        Show();
        StopCountdown();
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private void HandlePlayerRevive()
    {
        HideImmediate();
    }

    private IEnumerator CountdownRoutine()
    {
        float remaining = respawnDelaySeconds;

        while (remaining > 0f)
        {
            UpdateCountdownText(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }

        UpdateCountdownText(0f);
        countdownCoroutine = null;
    }

    private void UpdateCountdownText(float remainingSeconds)
    {
        if (countdownText == null) return;
        countdownText.text = Mathf.CeilToInt(remainingSeconds).ToString();
    }

    private void StopCountdown()
    {
        if (countdownCoroutine == null) return;
        StopCoroutine(countdownCoroutine);
        countdownCoroutine = null;
    }

    private void Show()
    {
        if (panelCanvas != null)
            panelCanvas.enabled = true;
    }

    private void HideImmediate()
    {
        StopCountdown();
        if (panelCanvas != null)
            panelCanvas.enabled = false;
    }
}
