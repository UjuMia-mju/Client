using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialData tutorialData;
    [SerializeField] private GameObject tutorialPagePrefab;
    // [SerializeField] private Button tutorialButton;

    private readonly List<GameObject> _pages = new List<GameObject>();

    // private void Awake()
    // {
    //     if (tutorialButton == null)
    //         tutorialButton = transform.Find("TutorialButton")?.GetComponent<Button>();
    //
    //     if (tutorialButton != null)
    //         tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
    // }

    private void Start()
    {
        if (tutorialData == null || tutorialPagePrefab == null)
        {
            Debug.LogWarning("[TutorialManager] TutorialData 또는 Tutorial_Page 프리팹이 할당되지 않았습니다.", this);
            return;
        }

        if (!TryResolveChapterStage(out int chapter, out int stage))
        {
            Debug.LogWarning("[TutorialManager] 현재 씬의 chapter/stage를 확인할 수 없습니다.", this);
            return;
        }

        if (!tutorialData.TryGetSlides(chapter, stage, out IReadOnlyList<TutorialData.Slide> slides) ||
            slides == null || slides.Count == 0)
        {
            return;
        }

        BuildPages(slides);
        ShowPage(0);
    }

    private void BuildPages(IReadOnlyList<TutorialData.Slide> slides)
    {
        for (int i = 0; i < slides.Count; i++)
        {
            TutorialData.Slide slide = slides[i];
            if (slide == null || slide.sprite == null)
                continue;

            GameObject page = Instantiate(tutorialPagePrefab, transform);
            page.name = $"Tutorial_Page_{slide.spriteKey}";
            page.SetActive(false);

            Image image = page.transform.Find("Image")?.GetComponent<Image>();
            if (image != null)
                image.sprite = slide.sprite;

            Button closeButton = page.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnPageClosed(page));

            _pages.Add(page);
        }
    }

    // private void OnTutorialButtonClicked()
    // {
    //     if (_pages.Count == 0)
    //         return;
    //
    //     if (IsAnyPageActive())
    //         return;
    //
    //     ShowPage(0);
    // }

    private void OnPageClosed(GameObject page)
    {
        page.SetActive(false);

        int index = _pages.IndexOf(page);
        if (index < 0)
            return;

        int nextIndex = index + 1;
        if (nextIndex < _pages.Count)
            ShowPage(nextIndex);
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= _pages.Count)
            return;

        _pages[index].SetActive(true);
    }

    // private static bool IsAnyPageActive(IReadOnlyList<GameObject> pages)
    // {
    //     for (int i = 0; i < pages.Count; i++)
    //     {
    //         if (pages[i] != null && pages[i].activeSelf)
    //             return true;
    //     }
    //
    //     return false;
    // }
    //
    // private bool IsAnyPageActive() => IsAnyPageActive(_pages);

    private static bool TryResolveChapterStage(out int chapter, out int stage)
    {
        chapter = StageManager.LastLoadedChapter;
        stage = StageManager.LastLoadedStageNum;

        if (chapter > 0 && stage > 0)
            return true;

        return Define.Scene.TryGetChapterStageFromScene(
            SceneManager.GetActiveScene().name,
            out chapter,
            out stage);
    }
}
