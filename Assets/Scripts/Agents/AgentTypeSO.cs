using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Agent", menuName = "ScriptableObjects/Agent", order = 1)]
public class AgentTypeSO : ScriptableObject
{
    [SerializeField, Min(0)] float mass;
    [SerializeField, Min(0)] float agentMaxHP;
    [SerializeField, Min(0)] float colliderSize;
    [SerializeField, Min(0)] float dragCoefficient;
    [SerializeField, Min(0)] float engineMaxStrenght;
    [SerializeField, Min(0)] float breakingStrenght;
    [SerializeField, Min(0)] float avoidenenceRange;

    [SerializeField, Min(0)] float maneuverabilityVertical;
    [SerializeField, Min(0)] float maneuverabilityHorizontal;
    [SerializeField, Min(0)] float rollSpeed;
    [SerializeField, Min(0)] float scannerRange;


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
    public float GetAvoidenenceRange()
    { 
        return avoidenenceRange;
    }
    public float GetBreakingStrenght()
    {
        return breakingStrenght;
    }

    public float GetManeuverabilityVertical()
    {
        return maneuverabilityVertical;
    }
    public float GetManeuverabilityHorizontal()
    {
        return maneuverabilityHorizontal;
    }
    public float GetRollSpeed()
    {
        return rollSpeed;
    }
    public float GetScannerRange()
    {
        return scannerRange;
    }
}
