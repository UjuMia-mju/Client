using UnityEngine;
using System;
using System.Collections;
using Protocol;

public class PeerPlayerStat : PlayerStat
{
    // TODO: NetManager.Instance._playerId는 Awake() 시점에 아직 0임.
    // _playerId가 확정되는 시점(S_PLAYER_ENTER 수신 후)보다 산소 감소 루프가 먼저 시작되므로
    // 올바른 해결책은 GetMyPlayerId()처럼 호출 시점마다 읽어오는 것이나,
    // 현재 구조상 _playerId 확정 전에 루프가 돌아 여전히 0이 반환될 수 있음.
    // 임시로 1로 하드코딩. 추후 _playerId 확정 이후 산소 루프를 시작하는 구조로 수정 필요.
    #region HP 증/감소 로직
    public override void DecreaseHp(int damage)
    {
        base.DecreaseHp(damage);
        PeerStatManager.Instance.DecreaseHp(GetMyPlayerId(), damage);
    }

    public override void IncreaseHp(int amount)
    {
        base.IncreaseHp(amount);
        PeerStatManager.Instance.IncreaseHp(GetMyPlayerId(), amount);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator DecreaseOxygen()
    {
        while (statData.oxygen > 0)
        {
            statData.DecreaseOxygen(0.01f);
            CallOnOxygenChanged();

            PeerStatManager.Instance.DecreaseOxygen(GetMyPlayerId());

            yield return new WaitForSeconds(1.0f);
        }

        if (!isRespawning)
        {
            if (oxygenHpDrainRoutine == null)
                oxygenHpDrainRoutine = StartCoroutine(OxygenHpDrainCoroutine());
        }
    }

    public override IEnumerator IncreaseOxygen()
    {
        while (statData.oxygen < 1f)
        {
            PeerStatManager.Instance.IncreaseOxygen(GetMyPlayerId());
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override IEnumerator OxygenHpDrainCoroutine()
    {
        while (statData.oxygen <= 0f && !isRespawning && statData.hp > 0)
        {
            DecreaseHp(1);
            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }
    #endregion
}
