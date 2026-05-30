/// <summary>
/// 런타임에 활성화된 <see cref="MessageCatalog"/> 를 통해 문구를 조회합니다.
/// <see cref="MessageManager"/> 가 Awake 시 카탈로그를 등록합니다.
/// </summary>
public static class MessageTexts
{
    static MessageCatalog _catalog;

    public static void Initialize(MessageCatalog catalog)
    {
        _catalog = catalog;
    }

    public static string Get(string key)
    {
        return _catalog != null ? _catalog.Get(key) : key;
    }

    public static string Format(string key, params object[] args)
    {
        return _catalog != null ? _catalog.Format(key, args) : key;
    }
}
