using UnityEngine;
using Protocol;
using System.Collections.Generic;

public class SmeltingRecipeManager : MonoBehaviour
{
    public static SmeltingRecipeManager Instance { get; private set; }

    [SerializeField]
    private List<SmeltingRecipe> recipes; // 인스펙터에서 설정하거나 데이터 테이블에서 로드

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // itemStringKey를 기반으로 레시피를 조회
    public bool TryGetRecipe(string inputItemStringKey, out SmeltingRecipe recipe)
    {
        foreach (var r in recipes)
        {
            if (r.inputItemStringKey == inputItemStringKey)
            {
                recipe = r;
                return true;
            }
        }

        recipe = default;
        return false;
    }
}
