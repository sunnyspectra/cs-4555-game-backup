using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shard", menuName = "Inventory/Shard")]
public class Shard : ItemBase
{

    public override bool Use(Spirit spirit)
    {
        return true;
    }

    public override bool CanUseOutsideBattle => false;

}


