using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Skinned/메시에 HP 피격과 같은 짧은 붉은 틴트. Player·Monster 공통.
/// </summary>
public class DamageMeshTintController : MonoBehaviour
{
    [SerializeField] private Transform meshRoot;

    [Header("Tint")]
    [SerializeField] private Color hurtTint = new Color(1f, 0.22f, 0.18f, 1f);
    [SerializeField, Range(0f, 1f)] private float peakBlend = 0.82f;
    [SerializeField] private float fadeInDuration = 0.025f;
    [SerializeField] private float fadeOutDuration = 0.09f;
    [SerializeField] private float cooldownSeconds = 0.07f;
    [SerializeField] private float extraBlendPerDamage = 0.08f;
    [SerializeField, Range(0f, 1f)] private float maxPeakBlend = 0.95f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] _renderers;
    private readonly List<Color[]> _baseColors = new List<Color[]>();
    private readonly List<int[]> _colorPropertyIds = new List<int[]>();
    private MaterialPropertyBlock _mpb;
    private float lastFlashTime = -999f;
    private float _blend;
    private Sequence _flashSequence;

    public void SetMeshRoot(Transform root)
    {
        if (meshRoot == root && _renderers != null && _renderers.Length > 0)
            return;

        meshRoot = root;
        CacheRenderers();
        ClearTint();
    }

    public void PlayHitFlash(int damageAmount = 1)
    {
        if (!EnsureRenderers())
            return;

        if (Time.unscaledTime - lastFlashTime < cooldownSeconds)
            return;

        lastFlashTime = Time.unscaledTime;
        KillTween();

        float peak = Mathf.Min(
            maxPeakBlend,
            peakBlend + Mathf.Max(0, damageAmount - 1) * extraBlendPerDamage);

        _blend = 0f;
        ApplyBlend(0f);

        _flashSequence = DOTween.Sequence().SetUpdate(true);
        _flashSequence.Append(
            DOTween.To(() => _blend, SetBlendAndApply, peak, fadeInDuration).SetEase(Ease.OutQuad));
        _flashSequence.Append(
            DOTween.To(() => _blend, SetBlendAndApply, 0f, fadeOutDuration).SetEase(Ease.InQuad));
    }

    private void OnDisable()
    {
        KillTween();
        ClearTint();
    }

    private void SetBlendAndApply(float value)
    {
        _blend = value;
        ApplyBlend(value);
    }

    private void ApplyBlend(float blend)
    {
        if (_renderers == null || _mpb == null)
            return;

        for (int r = 0; r < _renderers.Length; r++)
        {
            Renderer renderer = _renderers[r];
            if (renderer == null)
                continue;

            Color[] bases = _baseColors[r];
            int[] propIds = _colorPropertyIds[r];

            for (int i = 0; i < bases.Length; i++)
            {
                Color c = Color.Lerp(bases[i], hurtTint, blend);
                _mpb.SetColor(propIds[i], c);
                renderer.SetPropertyBlock(_mpb, i);
            }
        }
    }

    private bool EnsureRenderers()
    {
        if (_renderers != null && _renderers.Length > 0)
            return true;

        CacheRenderers();
        return _renderers != null && _renderers.Length > 0;
    }

    private void CacheRenderers()
    {
        _renderers = null;
        _baseColors.Clear();
        _colorPropertyIds.Clear();

        Transform root = meshRoot != null ? meshRoot : transform;

        _renderers = root.GetComponentsInChildren<Renderer>(true);
        if (_renderers == null || _renderers.Length == 0)
            return;

        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        for (int r = 0; r < _renderers.Length; r++)
        {
            Renderer renderer = _renderers[r];
            Material[] mats = renderer.sharedMaterials;
            var bases = new Color[mats.Length];
            var propIds = new int[mats.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat != null && mat.HasProperty(BaseColorId))
                {
                    propIds[i] = BaseColorId;
                    bases[i] = mat.GetColor(BaseColorId);
                }
                else if (mat != null && mat.HasProperty(ColorId))
                {
                    propIds[i] = ColorId;
                    bases[i] = mat.GetColor(ColorId);
                }
                else
                {
                    propIds[i] = BaseColorId;
                    bases[i] = Color.white;
                }
            }

            _baseColors.Add(bases);
            _colorPropertyIds.Add(propIds);
        }
    }

    private void ClearTint()
    {
        if (_renderers == null)
            return;

        for (int r = 0; r < _renderers.Length; r++)
        {
            if (_renderers[r] != null)
                _renderers[r].SetPropertyBlock(null);
        }

        _blend = 0f;
    }

    private void KillTween()
    {
        _flashSequence?.Kill();
        _flashSequence = null;
    }
}
