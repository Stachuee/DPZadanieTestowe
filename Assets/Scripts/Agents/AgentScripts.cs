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
    public int agentTeam;
    public float captureRate;

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

    public Vector3 test;
    public Blaster blaster;

    //public Rocket rocket;
}

[System.Serializable]
public struct Blaster
{
    public bool enabled;
    public float fireRate;
    public float damage;
    public float missleSpeed;
    public float missleLifetime;


    public bool fired;
    public float lastShot;
    public Vector3 barrelOrientation;

}
//[System.Serializable]
//public struct Rocket
//{
//    public bool enabled;
//    public float fireRate;
//    public float damage;
//    public float explosionRadius;
//    public float missleSpeed;
//}


[System.Serializable]
public struct Squadron
{
    public static readonly float DEFENSIVE_FORMATION_ACCEPTED_RADIUS = 5;
    public static readonly float OFFENSIVE_FORMATION_ACCEPTED_RADIUS = 10;

    public enum Formation { Defensive, Offensive };
    public int squdronID;
    public Vector3 rallyPoint;
    public Formation formation;

    public Vector3 squadronCenter;
    public int squadronUnitCount;
    public int squadronMaxUnitCount;

    public float squadronMaxHp;
    public float squadronCurrentHp;
}

[BurstCompile]
public struct SquadronUtil : IJob
{
    NativeArray<Squadron> squadrons;
    [ReadOnly] NativeArray<Agent> agents;

    public SquadronUtil(NativeArray<Squadron> _squadron, NativeArray<Agent> _agents)
    {
        squadrons = _squadron;
        agents = _agents;
    }

    public void Execute() //very bad fix pls
    {

        for (int index = 0; index < squadrons.Length; index++)
        {
            Squadron squadron = squadrons[index];

            squadron.squadronCenter = Vector3.zero;
            squadron.squadronUnitCount = 0;
            squadron.squadronCurrentHp = 0;
            
            squadrons[index] = squadron;
        }

        for (int i = 0; i < agents.Length; i++)
        {
            Squadron squadron = squadrons[agents[i].squadron];
            
            squadron.squadronUnitCount++;
            squadron.squadronCurrentHp += agents[i].agentHP;
            squadron.squadronCenter += agents[i].position;

            squadrons[agents[i].squadron] = squadron;
        }

        for (int index = 0; index < squadrons.Length; index++)
        {
            Squadron squadron = squadrons[index];

            if (squadron.squadronUnitCount > 0)
            {
                squadron.squadronCenter = squadron.squadronCenter / squadron.squadronUnitCount;
            }

            squadrons[index] = squadron;
        }


    }
}



[BurstCompile]
public struct AgentSteering : IJobParallelForTransform
{
    [ReadOnly] float deltaTime;
    [ReadOnly] float time;
    [ReadOnly] Vector3 playfieldSize;
    [ReadOnly] Vector3 playfieldCenter;
    [ReadOnly] float fluidDensity;
    [ReadOnly] NativeArray<Agent> readOnlyAgents;
    [ReadOnly] NativeArray<Squadron> squadron;
    NativeArray<Agent> agents;


    public AgentSteering(NativeArray<Agent> _agents, NativeArray<Agent> _readOnlyAgents, NativeArray<Squadron> _squadron,  float _deltaTime, float _time, Vector3 _playfieldSize, Vector3 _playfieldCenter)
    {
        time = _time;
        squadron = _squadron;
        agents = _agents;
        readOnlyAgents = _readOnlyAgents;
        deltaTime = _deltaTime;
        playfieldSize = _playfieldSize;
        playfieldCenter = _playfieldCenter;
        fluidDensity = 1.2f; //air resistance
    }

    public void Execute(int index, TransformAccess transform)
    {
        Steer(index);
        
        Agent agent = agents[index];

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

    Vector3 AvoidBounds(int id)
    {
        Agent agent = readOnlyAgents[id];
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

    Vector3 AvoidAgents(int id)
    {
        Vector3 avoidenence = Vector3.zero;
        Agent agentOne = readOnlyAgents[id];

        for(int i = 0; i < readOnlyAgents.Length; i++)
        {
            Agent agentTwo = readOnlyAgents[i];
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

    Vector3 KeepFormation(int id)
    {
        Agent agent = readOnlyAgents[id];
        Vector3 formationVector = Vector3.zero;

        for(int i = 0; i < squadron.Length; i++)
        {
            if(squadron[i].squdronID == agent.squadron)
            {
                Squadron mySquadron = squadron[i];

                Vector3 rallyPointDirectionVector = (mySquadron.rallyPoint - agent.position);
                float distance = rallyPointDirectionVector.magnitude;

                if (mySquadron.formation == Squadron.Formation.Defensive)
                {
                    if (distance > Squadron.DEFENSIVE_FORMATION_ACCEPTED_RADIUS)
                    {
                        formationVector = rallyPointDirectionVector.normalized;
                        agent.target = mySquadron.rallyPoint;
                    }
                }
                else if(mySquadron.formation == Squadron.Formation.Offensive)
                {
                    if(distance > Squadron.OFFENSIVE_FORMATION_ACCEPTED_RADIUS)
                    {
                        formationVector = rallyPointDirectionVector.normalized;
                        agent.target = mySquadron.rallyPoint;
                    }
                }


                break;
            }
        }
        return formationVector;
    }

    Vector3 PursueTarget(int id)
    {
        Agent agent = readOnlyAgents[id];

        Vector3 follow = Vector3.zero;
        bool inRange;
        float distance;
        int targetID = GetClosestEnemyAgentID(id, out inRange, out distance);

        if (inRange)
        {
            follow = (readOnlyAgents[targetID].position - agent.position).normalized;

            ShootBlaster(id, targetID, distance);
        }
        return follow;
    }

    void ShootBlaster(int id, int targetID, float distance)
    {
        Agent agentOne = agents[id];
        Blaster blasterOne = agentOne.blaster;

        if (blasterOne.lastShot + 1 / blasterOne.fireRate > time) return;

        Agent agentTwo = readOnlyAgents[targetID];
        float approximatedTravelTime = distance / blasterOne.missleSpeed;
        Vector3 approximatedEnemyPosition = agentTwo.position + agentTwo.flightDirection * approximatedTravelTime;
        blasterOne.barrelOrientation = (approximatedEnemyPosition - agentOne.position).normalized;
        blasterOne.fired = true;
        blasterOne.lastShot = time;

        agentOne.blaster = blasterOne;
        agents[id] = agentOne;
    }

    void Steer(int index)
    {

        Vector3 avoidenenceSteering = Vector3.zero;
        Vector3 targetSteering = Vector3.zero;

        avoidenenceSteering += AvoidBounds(index);
        avoidenenceSteering += AvoidAgents(index);
        targetSteering += KeepFormation(index);
        targetSteering += PursueTarget(index);

        Agent agent = agents[index];


        if (avoidenenceSteering.magnitude > 0.05f)
        {
            agent.throttle = 1;
            agent.breaks = 0;
        }
        else if (targetSteering.magnitude > 0.05f)
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
            agents[index] = agent;
            return;
        }

        steering = steering.normalized;

        bool targetInFront = Vector3.Dot(agent.forward, steering) >= 0;
        Vector3 steerProjected = Vector3.ProjectOnPlane(steering, agent.forward);
        Vector3 steerProjectedNormalized = steerProjected.normalized;

        agent.up = Vector3.RotateTowards(agent.up, steerProjectedNormalized, agent.rollSpeed * deltaTime, 0) * (Vector3.Dot(steerProjectedNormalized, agent.up) >= 0 ? 1 : -1);

        float horizontalPower = math.abs(Vector3.Angle(agent.up, steerProjectedNormalized) % 180 - 90) / 90;

        Vector3 steerPower = (targetInFront ? steerProjected : steerProjectedNormalized) * (agent.maneuverabilityHorizontal * horizontalPower + agent.maneuverabilityVertical * (1 - horizontalPower));

        Vector3 steerPowerNormalized = agent.forward * math.sqrt(1 - steerPower.magnitude) + steerPower;

        agent.forward = Vector3.RotateTowards(agent.forward, steerPowerNormalized, agent.rollSpeed * deltaTime, 0);

        if (!targetInFront)
        {
            agent.throttle = .5f;
            agent.breaks = .5f;
        }

        agent.steeringVector = steerPowerNormalized;

        agents[index] = agent;
        return;
    }


    int GetClosestEnemyAgentID(int id, out bool inRange, out float distance)
    {
        Agent agentOne = readOnlyAgents[id];

        float closestDistance = float.PositiveInfinity;

        int closest = -1;
        inRange = false;
        distance = 0f;

        for (int i = 0; i < readOnlyAgents.Length; i++)
        {
            Agent agentTwo = readOnlyAgents[i];
            if (agentTwo.position.x > agentOne.position.x + agentOne.scannerRange)
            {
                break;
            }
            else if (id != i && agentOne.agentTeam != agentTwo.agentTeam)
            {
                float currentDistance = Vector3.SqrMagnitude(agentOne.position - agentTwo.position);
                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closest = i;
                }
            }
        }
        if (closestDistance < float.PositiveInfinity)
        {
            distance = math.sqrt(closestDistance);
            inRange = distance < agentOne.scannerRange;
        }
        return closest;
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
    [ReadOnly] bool takeContactDamage;
    [ReadOnly] float collisionDamageMultiplier;
    public AgentCollision(NativeArray<Agent> _agents, bool _takeContactDamage, float _collisionDamageMultiplier)
    {
        agents = _agents;
        coefficientOfRestitution = -0.5f;
        takeContactDamage = _takeContactDamage;
        collisionDamageMultiplier = _collisionDamageMultiplier;
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

                        if(takeContactDamage)
                        {
                            agentOne.agentHP -= ((agentTwo.mass * impactSpeed) / agentOne.mass) * collisionDamageMultiplier;
                            agentTwo.agentHP -= ((agentOne.mass * impactSpeed) / agentTwo.mass) * collisionDamageMultiplier;
                        }

                        agents[i] = agentOne;
                        agents[j] = agentTwo;
                    }
                }
            }
        }
    }
}
