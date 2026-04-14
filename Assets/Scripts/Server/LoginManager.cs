using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : SceneSingleton<LoginManager>
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField pwInputField;
    [SerializeField] private Button loginButton;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Intro");
        loginButton.onClick.AddListener(OnLoginButton);
    }

    public string inputId { get; private set; }
    public string inputPw { get; private set; }

    public void OnLoginButton()
    {
        inputId = idInputField.text;
        inputPw = pwInputField.text;

        // 빈 값 체크
        if (string.IsNullOrEmpty(inputId) || string.IsNullOrEmpty(inputPw))
        {
            Debug.LogWarning("ID나 PW를 입력하세요!");
            return;
        }

        Debug.Log($"저장됨 - ID: {inputId}, PW: {inputPw}");

        //TODO: 서버로 로그인 요청 보내기
        ConnectManager.Instance.SendLogin(inputId, inputPw);
    }
}