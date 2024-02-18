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
    public enum AgentState {Iddle, Pursue }

    public int id;
    public float agentHP;
    public float agentMaxHP;
    //public int agentTeam;
    public AgentState agentState;

    public Vector3 targetVector;

    public int squadron;

    public Vector3 position;
    public Vector3 forward;
    public Vector3 up;

    public Vector3 flightDirection;
    public Vector3 prevFrameFlightDirection;

    public float scannerRange;

    public float avoidenenceRange;
    public Vector3 collisionAvoidance;
    public float colliderSize;

    public float mass;
    public float dragCoefficient;
    public float engineMaxStrenght;
    public float breakingStrenght;

    public float maneuverabilityVertical;
    public float maneuverabilityHorizontal;
    public float rollSpeed;


    public float acceleration;
    public float velocity;
    public float throttle;
    public float breaks;

    public Vector3 steeringVector;
    public Vector3 target;
}

[System.Serializable]
public struct SquadronOrders
{
    public static readonly float DEFENSIVE_FORMATION_ACCEPTED_RADIUS = 10;
    public static readonly float OFFENSIVE_FORMATION_ACCEPTED_RADIUS = 25;

    public enum Formation { Defensive, Offensive };
    public int squdronID;
    public Vector3 rallyPoint;
    public Formation formation;
}




[BurstCompile]
public struct AgentSteering : IJobParallelForTransform
{
    [ReadOnly] float deltaTime;
    [ReadOnly] Vector3 playfieldSize;
    [ReadOnly] Vector3 playfieldCenter;
    [ReadOnly] float fluidDensity;
    [ReadOnly] NativeArray<Agent> readOnlyAgents;
    [ReadOnly] NativeArray<SquadronOrders> squadronOrders;
    NativeArray<Agent> agents;


    public AgentSteering(NativeArray<Agent> _agents, NativeArray<Agent> _readOnlyAgents, NativeArray<SquadronOrders> _squadronOrders,  float _deltaTime, Vector3 _playfieldSize, Vector3 _playfieldCenter)
    {
        squadronOrders = _squadronOrders;
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

        agent = Steer(agent, index);


        float agentspeed = agent.flightDirection.magnitude;
        float agentAcceleration = (agent.flightDirection - agent.prevFrameFlightDirection).magnitude ;
        Vector3 agentFlightNormilized = agent.flightDirection.normalized;

        float flightForce = agentAcceleration * agent.mass;
        float steeringForce = agent.engineMaxStrenght;
        float dragForce = fluidDensity * math.pow(agentspeed, 2) * agent.dragCoefficient * (2 * math.PI * agent.colliderSize) / 2;

        Vector3 finalMoveForce = agentFlightNormilized * flightForce +
            agent.forward * steeringForce * agent.throttle 
            + -agent.flightDirection * agent.breakingStrenght * agent.breaks
            + -agentFlightNormilized * dragForce;
        
        agent.prevFrameFlightDirection = agent.flightDirection;

        Vector3 acceleration = finalMoveForce / agent.mass;

        agent.flightDirection += acceleration * deltaTime;

        agent.acceleration = acceleration.magnitude;
        agent.velocity = agent.flightDirection.magnitude;


        transform.position += agent.flightDirection  * deltaTime;
        if(agent.flightDirection.x != 0 || agent.flightDirection.y != 0) transform.rotation = Quaternion.LookRotation(agent.forward, agent.up);
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

    Vector3 KeepFormation(NativeArray<SquadronOrders> orders, Agent agent)
    {
        Vector3 formationVector = Vector3.zero;

        for(int i = 0; i < orders.Length; i++)
        {
            if(orders[i].squdronID == agent.squadron)
            {
                SquadronOrders myOrders = orders[i];

                Vector3 rallyPointDirectionVector = (myOrders.rallyPoint - agent.position);
                float distance = rallyPointDirectionVector.magnitude;

                if (myOrders.formation == SquadronOrders.Formation.Defensive && distance > SquadronOrders.DEFENSIVE_FORMATION_ACCEPTED_RADIUS)
                {
                    formationVector = rallyPointDirectionVector.normalized;
                    agent.target = myOrders.rallyPoint;
                }
                else if(myOrders.formation == SquadronOrders.Formation.Offensive && agent.agentState == Agent.AgentState.Iddle && distance > SquadronOrders.OFFENSIVE_FORMATION_ACCEPTED_RADIUS)
                {
                    formationVector = rallyPointDirectionVector.normalized;
                    agent.target = myOrders.rallyPoint;
                }


                break;
            }
        }


        return formationVector;
    }

    Agent Steer(Agent agent, int index)
    {

        Vector3 avoidenenceSteering = Vector3.zero;
        Vector3 targetSteering = Vector3.zero;

        avoidenenceSteering += AvoidBounds(agent);
        avoidenenceSteering += AvoidAgents(readOnlyAgents, index);
        targetSteering += KeepFormation(squadronOrders, agent);
        agent.targetVector = targetSteering;

        if (avoidenenceSteering.magnitude > 0.05f)
        {
            // try to not ram into the target
            agent.throttle = 1;
            agent.breaks = 0;
        }
        else if(targetSteering.magnitude > 0.05f)
        {
            //speed up to not hit anything
            bool breaks = BreakingRequired(agent, Vector3.Distance(agent.position, agent.target));
            agent.breaks = breaks ? 1 : 0;
            agent.throttle = breaks ? 0 : 1;
        }
        else
        {
            agent.throttle = 0;
            agent.breaks = 1;
        }

        Vector3 steering = avoidenenceSteering + targetSteering;

        if (steering.magnitude < 0.05f)
        {
            agent.up = Vector3.RotateTowards(agent.up, Vector3.up, agent.rollSpeed * deltaTime, 0);
            return agent;
        }

        steering = steering.normalized;

        bool targetInFront = Vector3.Dot(agent.forward, steering) >= 0;
        Vector3 steerProjected = Vector3.ProjectOnPlane(steering, agent.forward);
        Vector3 steerProjectedNormalized = steerProjected.normalized;

        agent.up = Vector3.RotateTowards(agent.up, steerProjectedNormalized, agent.rollSpeed * deltaTime, 0) * (Vector3.Dot(steerProjectedNormalized, agent.up) >= 0 ? 1 : -1);

        float horizontalPower = math.abs(Vector3.Angle(agent.up, steerProjectedNormalized) % 180 - 90) / 90;

        Vector3 steerPower = (targetInFront ? steerProjected : steerProjectedNormalized ) * (agent.maneuverabilityHorizontal * horizontalPower + agent.maneuverabilityVertical * (1 - horizontalPower));

        Vector3 steerPowerNormalized = agent.forward * math.sqrt(1 - steerPower.magnitude) + steerPower;

        agent.forward = Vector3.RotateTowards(agent.forward, steerPowerNormalized, agent.rollSpeed * deltaTime, 0);
        
        if(!targetInFront)
        {
            agent.throttle = 0;
            agent.breaks = 1;
        }

        agent.steeringVector = steerPowerNormalized;

        return agent;
    }

    /// <summary>
    /// Check if breaking is needed in order to prevent ramming.
    /// </summary>
    bool BreakingRequired(Agent agent, float distance)
    {
        float acceleration = agent.breakingStrenght / agent.mass;
        float timeToStop = agent.velocity / acceleration;
        float timeRemain = distance / agent.velocity;
        return timeToStop >= timeRemain;
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
        coefficientOfRestitution = -0.5f;
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
