using UnityEngine;
public enum WormAnimState
{
    Idle,
    BiteAttack,
    TakeDamage,
    Die
}

public class DesertWormAnimator : MonoBehaviour
{
    private const string ANIM_PAR = "AnimPar";

    private WormAnimState state = new WormAnimState();
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponent<Animator>();
    }

    public void WormAnimation(bool isBiting, bool isTakeDamage, bool isDie)
    {
        if (isBiting)
        {
            state = WormAnimState.BiteAttack;
        }
        else if (isTakeDamage)
        {
            state = WormAnimState.TakeDamage;
        }
        else if (isDie)
        {
            state = WormAnimState.Die;
        }
        else
        {
            state = WormAnimState.Idle;
        }

        anim.SetInteger(ANIM_PAR, (int)state);
    }

    public WormAnimState GetAnimState()
    {
        return state;
    }
}
