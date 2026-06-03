using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "HUD/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Serializable]
    public class Slide
    {
        [Tooltip("Tutorial_(이름) 형식에서 이름 부분 (예: Stone_Glow)")]
        public string spriteKey;

        public Sprite sprite;
    }

    [Serializable]
    public class StageEntry
    {
        public int chapter;
        public int stage;
        public List<Slide> slides = new List<Slide>();
    }

    [SerializeField] private List<StageEntry> entries = new List<StageEntry>();

    public IReadOnlyList<StageEntry> Entries => entries;

    public bool TryGetSlides(int chapter, int stage, out IReadOnlyList<Slide> slides)
    {
        foreach (StageEntry entry in entries)
        {
            if (entry.chapter == chapter && entry.stage == stage)
            {
                slides = entry.slides;
                return true;
            }
        }

        slides = null;
        return false;
    }
}
