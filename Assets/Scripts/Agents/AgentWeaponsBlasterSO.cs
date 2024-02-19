using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Agent", menuName = "ScriptableObjects/Blaster", order = 2)]
public class AgentWeaponsBlasterSO : ScriptableObject
{
    [SerializeField] float fireRate;
    [SerializeField] float damage;
    [SerializeField] float missleSpeed;
    [SerializeField] float missleLifetime;

    public float GetFirerate()
    {
        return fireRate;
    }
    public float GetDamage()
    {
        return fireRate;
    }
    public float GetMissleSpeed()
    {
        return missleSpeed;
    }
    public float GetMissleLifeTime()
    {
        return missleLifetime;
    }
}
