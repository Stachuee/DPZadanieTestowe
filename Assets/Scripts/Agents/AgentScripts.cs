using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Burst;

[System.Serializable]
public struct Agent
{
    public Vector3 position;
    public Vector3 flightDirection;
    public Vector3 steeringDirection; // normalized
    public float steerPower;
    public float colliderSize;
    public float mass;
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
        agent.position = transform.position;

        agents[index] = agent;
    }
}

[BurstCompile]
public struct AgentCollision : IJob
{
    NativeArray<Agent> agents;
    [ReadOnly] float coefficientOfRestitution;

    public AgentCollision(NativeArray<Agent> _agents)
    {
        agents = _agents;
        coefficientOfRestitution = 0;
    }

    public void Execute()
    {
        for(int i = 0; i < agents.Length; i++)
        {
            Agent agentOne = agents[i];
            for (int j = i + 1; j < agents.Length; j++)
            {
                Agent agentTwo = agents[j];
                if(agentTwo.position.x - agentTwo.colliderSize > agentOne.position.x + agentOne.colliderSize)
                {
                    break;
                }
                else
                {
                    Vector3 delta = agentTwo.position - agentOne.position;
                    float agentDistance = delta.magnitude;
                    if(agentDistance < agentOne.colliderSize + agentTwo.colliderSize)
                    {
                        Vector3 deltaNormalized = delta.normalized;
                        Vector3 moveVector = deltaNormalized * (agentDistance - (agentOne.colliderSize + agentTwo.colliderSize)) *.5f;
                        agentOne.position += moveVector;
                        agentTwo.position += -moveVector;

                        float systemMass = 1 / ((1 / agentOne.mass) + (1 / agentTwo.mass));
                        float impactSpeed = Vector3.Dot(deltaNormalized, (agentOne.flightDirection - agentTwo.flightDirection));
                        float impulseMagnitude = (1 + coefficientOfRestitution) * systemMass * impactSpeed;

                        agentOne.flightDirection = -(impulseMagnitude / agentOne.mass) * deltaNormalized;
                        agentTwo.flightDirection = (impulseMagnitude / agentTwo.mass) * deltaNormalized;

                        agents[i] = agentOne;
                        agents[j] = agentTwo;
                    }
                }
            }
        }
    }
}
