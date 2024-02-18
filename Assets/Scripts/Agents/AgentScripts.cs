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

    public float scannerRange;

    public float avoidenenceRange;
    public Vector3 collisionAvoidance;
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
    [ReadOnly] NativeArray<Agent> readOnlyAgents;
    NativeArray<Agent> agents;


    public AgentSteering(NativeArray<Agent> _agents, NativeArray<Agent> _readOnlyAgents, float _deltaTime, Vector3 _playfieldSize, Vector3 _playfieldCenter)
    {
        agents = _agents;
        readOnlyAgents = _readOnlyAgents;
        deltaTime = _deltaTime;
        playfieldSize = _playfieldSize;
        playfieldCenter = _playfieldCenter;
        fluidDensity = 1.2f; //air resistance
    }

    public void Execute(int index, TransformAccess transform)
    {
        Agent agent = agents[index];

        Vector3 steering = Vector3.zero;

        steering += AvoidBounds(agent);
        steering += AvoidAgents(readOnlyAgents, index);


        steering = steering.normalized;


        float agentspeed = agent.flightDirection.magnitude;
        float agentAcceleration = (agent.flightDirection - agent.prevFrameFlightDirection).magnitude ;
        Vector3 agentFlightNormilized = agent.flightDirection.normalized;

        float flightForce = agentAcceleration * agent.mass;
        float steeringForce = agent.engineMaxStrenght;
        float dragForce = fluidDensity * math.pow(agentspeed, 2) * agent.dragCoefficient * (2 * math.PI * agent.colliderSize) / 2;

        Vector3 finalMoveForce = agentFlightNormilized * flightForce + steering * steeringForce + -agentFlightNormilized * dragForce;
        
        agent.prevFrameFlightDirection = agent.flightDirection;

        Vector3 acceleration = finalMoveForce / agent.mass;

        agent.flightDirection += acceleration * deltaTime;

        agent.acceleration = acceleration.magnitude;
        agent.speed = agent.flightDirection.magnitude;


        transform.position += agent.flightDirection  * deltaTime;
        transform.rotation = LookRotation(agent.flightDirection.normalized, Vector3.up);
        agent.position = transform.position;

        agents[index] = agent;
    }

    Vector3 AvoidBounds(Agent agent)
    {
        if (agent.position.x > playfieldCenter.x + playfieldSize.x ||
            agent.position.x < playfieldCenter.x - playfieldSize.x ||
            agent.position.y > playfieldCenter.y + playfieldSize.y ||
            agent.position.y < playfieldCenter.y - playfieldSize.y ||
            agent.position.z > playfieldCenter.z + playfieldSize.z ||
            agent.position.z < playfieldCenter.z - playfieldSize.z)
        {
            return  (playfieldCenter - agent.position).normalized;
        }
        return Vector3.zero;
    }

    Vector3 AvoidAgents(NativeArray<Agent> agents, int id)
    {
        Vector3 avoidenence = Vector3.zero;
        Agent agentOne = agents[id];

        for(int i = 0; i < agents.Length; i++)
        {
            Agent agentTwo = agents[i];
            if(agentTwo.position.x > agentOne.position.x + agentOne.avoidenenceRange)
            {
                break;
            }
            else if(id != i)
            {
                Vector3 delta = agentTwo.position - agentOne.position;
                float agentDistance = delta.magnitude;
                if (agentDistance < agentOne.avoidenenceRange)
                {
                    avoidenence += -delta.normalized * (agentOne.avoidenenceRange / agentDistance);
                }
            }
        }

        return avoidenence;
    }

    Quaternion LookRotation(Vector3 forward, Vector3 up)
    {

        forward = Vector3.Normalize(forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Cross(forward, right);
        var m00 = right.x;
        var m01 = right.y;
        var m02 = right.z;
        var m10 = up.x;
        var m11 = up.y;
        var m12 = up.z;
        var m20 = forward.x;
        var m21 = forward.y;
        var m22 = forward.z;


        float num8 = (m00 + m11) + m22;
        Quaternion quaternion = new Quaternion();
        if (num8 > 0f)
        {
            var num = (float)math.sqrt(num8 + 1f);
            quaternion.w = num * 0.5f;
            num = 0.5f / num;
            quaternion.x = (m12 - m21) * num;
            quaternion.y = (m20 - m02) * num;
            quaternion.z = (m01 - m10) * num;
            return quaternion;
        }
        if ((m00 >= m11) && (m00 >= m22))
        {
            var num7 = (float)math.sqrt(((1f + m00) - m11) - m22);
            var num4 = 0.5f / num7;
            quaternion.x = 0.5f * num7;
            quaternion.y = (m01 + m10) * num4;
            quaternion.z = (m02 + m20) * num4;
            quaternion.w = (m12 - m21) * num4;
            return quaternion;
        }
        if (m11 > m22)
        {
            var num6 = (float)math.sqrt(((1f + m11) - m00) - m22);
            var num3 = 0.5f / num6;
            quaternion.x = (m10 + m01) * num3;
            quaternion.y = 0.5f * num6;
            quaternion.z = (m21 + m12) * num3;
            quaternion.w = (m20 - m02) * num3;
            return quaternion;
        }
        var num5 = (float)math.sqrt(((1f + m22) - m00) - m11);
        var num2 = 0.5f / num5;
        quaternion.x = (m20 + m02) * num2;
        quaternion.y = (m21 + m12) * num2;
        quaternion.z = 0.5f * num5;
        quaternion.w = (m01 - m10) * num2;
        return quaternion;
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
