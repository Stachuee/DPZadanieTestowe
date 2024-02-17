using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Burst;

public struct Agent
{
    public Vector3 flightDirection;
    public Vector3 steeringDirection; // normalized
    public float steerPower;
}

[BurstCompile]
public struct AgentSteering : IJobParallelForTransform
{
    [ReadOnly] float deltaTime;
    NativeArray<Agent> agents;

    public AgentSteering(NativeArray<Agent> _agents, float _deltaTime)
    {
        agents = _agents;
        deltaTime = _deltaTime;
    }

    public void Execute(int index, TransformAccess transform)
    {
        Agent agent = agents[index];

        agent.flightDirection += agent.steeringDirection * agent.steerPower * deltaTime;

        transform.position += agent.flightDirection * deltaTime;

        agents[index] = agent;
    }
}

[BurstCompile]
public struct AgentCollision : IJob
{
    NativeArray<Agent> agents;

    public AgentCollision(NativeArray<Agent> _agents)
    {
        agents = _agents;
    }

    public void Execute()
    {
        
    }
}
