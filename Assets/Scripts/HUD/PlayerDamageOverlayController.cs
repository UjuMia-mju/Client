using UnityEngine;

/// <summary>
/// Player / OtherPlayers HP 감소 시 캐릭터 메시에 짧은 붉은 틴트.
/// </summary>
public class PlayerDamageOverlayController : MonoBehaviour
{
    public PlayerStat playerStat;

    [SerializeField] private DamageMeshTintController meshTint;

    private int trackedHp = -1;

    private void Awake()
    {
        EnsureMeshTint();
    }

    public void SetPlayerStat(PlayerStat stat)
    {
        if (playerStat == stat)
            return;

        Unhook();
        playerStat = stat;
        if (isActiveAndEnabled)
            Hook();
    }

    public void SetMeshRoot(Transform root)
    {
        EnsureMeshTint();
        meshTint.SetMeshRoot(root);
    }

    private void OnEnable()
    {
        Hook();
    }

    private void OnDisable()
    {
        Unhook();
    }

    void EnsureMeshTint()
    {
        if (meshTint != null)
            return;

        meshTint = GetComponent<DamageMeshTintController>();
        if (meshTint == null)
            meshTint = gameObject.AddComponent<DamageMeshTintController>();
    }

    private void Hook()
    {
        if (playerStat == null)
            return;

        playerStat.OnHpChanged -= OnHpChanged;
        playerStat.OnHpChanged += OnHpChanged;
        trackedHp = playerStat.GetHp();
    }

    private void Unhook()
    {
        if (playerStat == null)
            return;

        playerStat.OnHpChanged -= OnHpChanged;
    }

    private void OnHpChanged(int newHp)
    {
        if (trackedHp < 0)
        {
            trackedHp = newHp;
            return;
        }

        if (newHp >= trackedHp)
        {
            trackedHp = newHp;
            return;
        }

        int damage = trackedHp - newHp;
        trackedHp = newHp;
        EnsureMeshTint();
        meshTint.PlayHitFlash(damage);
    }
}
