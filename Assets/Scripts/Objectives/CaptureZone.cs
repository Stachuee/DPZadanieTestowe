using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[System.Serializable]
public struct CaptureZone
{
    public Vector3 position;
    public float size;
    public int teamControlling;
    public float controll;
}


public struct CheckCaptureZone : IJobParallelFor
{
    [ReadOnly] NativeArray<Agent> checkCapture;
    [ReadOnly] float deltaTime;
    NativeArray<CaptureZone> zones;

    public CheckCaptureZone(NativeArray<Agent> _checkCapture, float _deltaTime, NativeArray<CaptureZone> _zones)
    {
        checkCapture = _checkCapture;
        zones = _zones;
        deltaTime = _deltaTime;
    }

    public void Execute(int index)
    {
        CaptureZone zone = zones[index];

        for(int i = 0; i < checkCapture.Length; i++)
        {
            Agent agent = checkCapture[i];

            if (agent.position.x - agent.colliderSize > zone.position.x + zone.size)
            {
                break;
            }

            if((agent.position - zone.position).magnitude < zone.size)
            {
                if(zone.teamControlling == agent.agentTeam)
                {
                    zone.controll += agent.captureRate * deltaTime;
                }
                else
                {
                    zone.controll -= agent.captureRate * deltaTime;
                    if(zone.controll < 0)
                    {
                        zone.teamControlling = agent.agentTeam;
                        zone.controll = -zone.controll;
                    }
                }
                zone.controll = Mathf.Clamp01(zone.controll);
            }

        }
        zones[index] = zone;
    }
}