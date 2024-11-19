using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new recovery item")]
public class RecoveryItem : ItemBase
{
    [Header("HP")]
    [SerializeField] int hpAmount;
    [SerializeField] bool restoreMaxHP;

    [Header("Status Conditions")]
    [SerializeField] ConditionID status;
    [SerializeField] bool recoverAllStatus;
    

    public override bool Use(Spirit spirit)
    {
        
        if (spirit.HP == 0)
            return false;

        if (restoreMaxHP || hpAmount > 0)
        {
            if (spirit.HP == spirit.MaxHp)
                return false;

            if (restoreMaxHP)
                spirit.IncreaseHP(spirit.MaxHp);
            else
                spirit.IncreaseHP(hpAmount);
        }

        if (recoverAllStatus || status != ConditionID.none)
        {
            if (spirit.Status == null && spirit.VolatileStatus == null)
                return false;

            if (recoverAllStatus)
            {
                spirit.CureStatus();
                spirit.CureVolatileStatus();
            }
            else
            {
                if (spirit.Status.Id == status)
                    spirit.CureStatus();
                else if (spirit.VolatileStatus.Id == status)
                    spirit.CureVolatileStatus();
                else
                    return false;
            }
        }

        return true;
    }
}
