using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Agent", menuName = "ScriptableObjects/Agent", order = 1)]
public class AgentTypeSO : ScriptableObject
{
    [SerializeField] float mass;
    [SerializeField] float agentMaxHP;
    [SerializeField] float colliderSize;
    [SerializeField] float dragCoefficient;
    [SerializeField] float engineMaxStrenght;


    public float GetMass()
    { 
        return mass;
    }
    public float GetHp()
    {
        return agentMaxHP;
    }
    public float GetColliderSize()
    {
        return colliderSize;
    }
    public float GetDragCoefficient()
    {
        return dragCoefficient;
    }
    public float GetEngineStrenght()
    {
        return engineMaxStrenght;
    }
}
