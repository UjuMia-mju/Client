using System.Collections;
using UnityEngine;

public enum WormAnimState
{
    Idle = 0,
    BiteAttack = 1,
    TakeDamage = 2,
    Die = 3,
    Spawn = 4
}

public class DesertWormAnimator : MonoBehaviour
{
    private const string ANIM_PAR = "AnimPar";

    private WormAnimState state = WormAnimState.Idle;
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponent<Animator>();
        // 초기 파라미터 동기화
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }

    // 외부에서 상태 설정 및 즉시 애니메이터에 반영
    public void SetState(WormAnimState newState)
    {
        state = newState;
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }

    public WormAnimState GetAnimState()
    {
        return state;
    }

    /// <summary>현재 재생 중인 애니메이터 상태가 <paramref name="stateName"/>이고 한 번 재생이 끝날 때까지 대기.</summary>
    public IEnumerator WaitForStatePlaybackComplete(string stateName, float fallbackSeconds = 0.6f, int layerIndex = 0)
    {
        if (anim == null)
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        int stateHash = Animator.StringToHash(stateName);
        float timeout = Mathf.Max(2f, fallbackSeconds * 3f);
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(layerIndex);
            if (info.shortNameHash == stateHash || info.IsName(stateName))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (elapsed < timeout)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(layerIndex);
            if (info.shortNameHash == stateHash || info.IsName(stateName))
            {
                if (!info.loop && info.normalizedTime >= 1f)
                    yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(fallbackSeconds);
    }
}
