using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritParty : MonoBehaviour
{
    [SerializeField] List<Spirit> spirits;

    public event Action OnUpdated;

    public List<Spirit> Spirits
    {
        get
        {
            return spirits;
        }
        set
        {
            spirits = value;
            OnUpdated?.Invoke();
        }
    }

    private void Awake()
    {
        foreach (var spirit in spirits)
        {
            spirit.Init();
        }
    }

    private void Start()
    {

    }

    public Spirit GetHealthySpirits(List<Spirit> dontInclude = null)
    {
        var healthySpirits = spirits.Where(x => x.HP > 0);
        if (dontInclude != null)
            healthySpirits = healthySpirits.Where(p => !dontInclude.Contains(p));

        return healthySpirits.FirstOrDefault();
    }

    public Spirit GetHealthySpirit()
    {
        return spirits.Where(x => x.HP > 0).FirstOrDefault();
    }

    public List<Spirit> GetHealthySpirit(int unitCount)
    {
        return spirits.Where(x => x.HP > 0).Take(unitCount).ToList();
    }

    public void AddSpirit(Spirit newSpirit)
    {
        if (spirits.Count < 6)
        {
            spirits.Add(newSpirit);
            OnUpdated?.Invoke();
        }
        else
        {

        }
    }

    public void PartyUpdated()
    {
        OnUpdated?.Invoke();
    }

    public static SpiritParty GetPlayerParty()
    {
        return FindObjectOfType<PlayerController>().GetComponent<SpiritParty>();
    }
}
