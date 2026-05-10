using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : SceneSingleton<LoginManager>
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField pwInputField;
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        if (pwInputField != null)
        {
            pwInputField.contentType = TMP_InputField.ContentType.Password;
            pwInputField.asteriskChar = '*';
        }
    }

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Intro");
        loginButton.onClick.AddListener(OnLoginButton);
    }

    public string inputId { get; private set; }
    public string inputPw { get; private set; }

    public void OnLoginButton()
    {
        SoundManager.Instance.PlaySFX("Click2");

        inputId = idInputField.text;
        inputPw = pwInputField.text;

        // 빈 값 체크
        if (string.IsNullOrEmpty(inputId) || string.IsNullOrEmpty(inputPw))
        {
            MessageManager.Instance.Show("아이디와 비밀번호를 입력해 주세요.");
            return;
        }

        ConnectManager.Instance.SendLogin(inputId, inputPw);
    }
}