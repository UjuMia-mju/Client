using UnityEngine;


public class Monster : MovingObject
{
    [HideInInspector] public int monsterId;

    [Header("Stats")]
    [SerializeField] protected int maxHp = 3;
    [SerializeField] protected int attack = 1;

    protected int hp;

    protected DamageMeshTintController damageTint;

    [Header("Scene Placement")]
    [Tooltip("어떤 몬스터 종류인지. 씬 배치 동기화 시 프리팹 매칭용 키.")]
    [SerializeField] private Monsters monsterKey = Monsters.None;
    public Monsters MonsterKey => monsterKey;

    [Tooltip("씬에 미리 배치된 몬스터면 체크. 호스트가 피어에게 초기 ID/스폰을 동기화합니다.")]
    [SerializeField] private bool isScenePlacedMonster = false;
    public bool IsScenePlacedMonster => isScenePlacedMonster;

    protected override void Awake()
    {
        base.Awake();
        hp = maxHp;
        EnsureDamageTint();
    }

    protected void EnsureDamageTint()
    {
        if (damageTint != null)
            return;

        damageTint = GetComponent<DamageMeshTintController>();
        if (damageTint == null)
            damageTint = gameObject.AddComponent<DamageMeshTintController>();

        damageTint.SetMeshRoot(transform);
    }

    protected void PlayDamageTint(int damageAmount)
    {
        EnsureDamageTint();
        damageTint.PlayHitFlash(damageAmount);
    }

    void Update() { }

    // 호스트 전용: 외부(무기)에서 몬스터에 데미지 적용
    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        hp -= amount;
        if (hp < 0) hp = 0;
    }

    /// <summary>
    /// 피어 측에서 사망 패킷 수신 시 호출. 죽음 애니메이션을 재생하고 적절한 시간 뒤에
    /// 스스로를 파괴해야 한다. 기본 구현은 즉시 파괴.
    /// </summary>
    public virtual void PlayDeathAndDestroy()
    {
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    public void ApplyDamage(int damage)
    {
        hp -= damage;
    }
}