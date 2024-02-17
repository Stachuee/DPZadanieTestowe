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
    public int id;
    public float agentHP;
    public float agentMaxHP;

    public Vector3 position;
    public Vector3 flightDirection;
    public Vector3 prevFrameFlightDirection;

    public Vector3 steeringDirection;
    public float colliderSize;

    public float mass;
    public float dragCoefficient;
    public float engineMaxStrenght;

    public float acceleration;
    public float speed;

}

[BurstCompile]
public struct AgentSteering : IJobParallelForTransform
{
    [ReadOnly] float deltaTime;
    [ReadOnly] Vector3 playfieldSize;
    [ReadOnly] Vector3 playfieldCenter;
    [ReadOnly] float fluidDensity;
    NativeArray<Agent> agents;

    public AgentSteering(NativeArray<Agent> _agents, float _deltaTime, Vector3 _playfieldSize, Vector3 _playfieldCenter)
    {
        agents = _agents;
        deltaTime = _deltaTime;
        playfieldSize = _playfieldSize;
        playfieldCenter = _playfieldCenter;
        fluidDensity = 1.2f; //air resistance
    }

    public void Execute(int index, TransformAccess transform)
    {
        Agent agent = agents[index];

        if (agent.position.x > playfieldCenter.x + playfieldSize.x ||
            agent.position.x < playfieldCenter.x - playfieldSize.x ||
            agent.position.y > playfieldCenter.y + playfieldSize.y ||
            agent.position.y < playfieldCenter.y - playfieldSize.y ||
            agent.position.z > playfieldCenter.z + playfieldSize.z ||
            agent.position.z < playfieldCenter.z - playfieldSize.z)
        {
            agent.steeringDirection = (playfieldCenter - agent.position).normalized;
        }

        float agentspeed = agent.flightDirection.magnitude;
        float agentAcceleration = (agent.flightDirection - agent.prevFrameFlightDirection).magnitude ;
        Vector3 agentFlightNormilized = agent.flightDirection.normalized;

        float flightForce = agentAcceleration * agent.mass;
        float steeringForce = agent.engineMaxStrenght;
        float dragForce = fluidDensity * math.pow(agentspeed, 2) * agent.dragCoefficient * (2 * math.PI * agent.colliderSize) / 2;

        Vector3 finalMoveForce = agentFlightNormilized * flightForce + agent.steeringDirection * steeringForce + -agentFlightNormilized * dragForce;
        
        agent.prevFrameFlightDirection = agent.flightDirection;

        Vector3 acceleration = finalMoveForce / agent.mass;

        agent.flightDirection += acceleration * deltaTime;

        agent.acceleration = acceleration.magnitude;
        agent.speed = agent.flightDirection.magnitude;


        transform.position += agent.flightDirection  * deltaTime;

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

                        agentOne.agentHP -= 0.1f;
                        agentTwo.agentHP -= 0.1f;

                        agents[i] = agentOne;
                        agents[j] = agentTwo;
                    }
                }
            }
        }
    }
}
