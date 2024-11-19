using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name = "Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Spirit spirit) =>
                {
                    spirit.DecreaseHP(spirit.MaxHp / 8);
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has been slept",
                OnAfterTurn = (Spirit spirit) =>
                {
                    spirit.DecreaseHP(spirit.MaxHp / 16);
                }
            }
        },
    };



    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
       
        else if (condition.Id == ConditionID.psn)
            return 1.5f;

        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, slp, par, frz,
    confusion
}
