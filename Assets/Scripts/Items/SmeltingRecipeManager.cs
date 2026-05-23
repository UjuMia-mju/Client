using UnityEngine;
using Protocol;

public class SmeltingRecipeManager : MonoBehaviour
{
    public static SmeltingRecipeManager Instance { get; private set; }

    [Header("제련 카탈로그 (레시피·결과 프리팹 단일 관리)")]
    [SerializeField] private SmeltingCatalog smeltingCatalog;

    public SmeltingCatalog Catalog => smeltingCatalog;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // itemStringKey를 기반으로 레시피를 조회
    public bool TryGetRecipe(string inputItemStringKey, out SmeltingRecipe recipe)
    {
        if (smeltingCatalog != null &&
            smeltingCatalog.TryGetByInputKey(inputItemStringKey, out SmeltingCatalog.Entry e))
        {
            recipe = new SmeltingRecipe
            {
                inputItemStringKey = e.inputItemStringKey,
                outputItemID = e.outputItemID,
                smeltingTime = e.smeltingTime
            };
            return true;
        }

        recipe = default;
        return false;
    }
}
