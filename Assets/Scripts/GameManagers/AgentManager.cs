using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance;

    [SerializeField] int agentCount;
    
    [SerializeField]
    List<Agent> allAgents = new List<Agent>();
    List<Transform> allAgentTransforms = new List<Transform>();
    [SerializeField]
    List<SquadronOrders> orders = new List<SquadronOrders>();

    Dictionary<int, string> agentNames = new Dictionary<int, string>(); // remember to remove names after destroying agent

    [SerializeField] Vector3 playfieldCenter;
    [SerializeField] Vector3 playfieldSize;


    int nextAgentId = 0;

    NativeArray<Agent> agentsNativeArray;
    NativeArray<Agent> readOnlyAgentsNativeArray;
    NativeArray<SquadronOrders> ordersNativeArray;
    TransformAccessArray transformAccessArray;

    JobHandle agentSteeringHandle;


    [SerializeField] AgentTypeSO toSpawn;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two agent managers on one scene");
        }
    }

    private void Start()
    {
        AgentPooling.Instance.CreatePool(agentCount);
        for (int i = 0; i < 1; i++)
        {
            SpawnDivision(10, 0);
        }
    }


    private void Update()
    {
        SortAgents();

        agentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        readOnlyAgentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        ordersNativeArray = new NativeArray<SquadronOrders>(orders.ToArray(), Allocator.TempJob);
        transformAccessArray = new TransformAccessArray(allAgentTransforms.ToArray());

        AgentCollision agentCollisionJob = new AgentCollision(agentsNativeArray);
        agentCollisionJob.Run();


        for(int i = 0; i < allAgents.Count; i++)
        {
            transformAccessArray[i].position = agentsNativeArray[i].position; // not sure about that. Could be using this wrong
        }

        AgentSteering agentSteeringJob = new AgentSteering(agentsNativeArray, 
            readOnlyAgentsNativeArray,
            ordersNativeArray,
            Time.deltaTime,
            playfieldSize,
            playfieldCenter
            );

        agentSteeringHandle = agentSteeringJob.Schedule(transformAccessArray);

    }

    private void LateUpdate()
    {
        agentSteeringHandle.Complete();

        for(int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i] = agentsNativeArray[i];
            //allAgentTransforms[i].rotation = Quaternion.LookRotation(allAgents[i].flightDirection.normalized, Vector3.up);
            //if(allAgents[i].flightDirection != Vector3.zero) allAgentTransforms[i].forward = allAgents[i].flightDirection;
        }

        agentsNativeArray.Dispose();
        transformAccessArray.Dispose();
        readOnlyAgentsNativeArray.Dispose();
        ordersNativeArray.Dispose();
    }

    public void SpawnDivision(int unitCount, int divisionID)
    {
        for (int i = 0; i < unitCount; i++)
            SpawnAgent(toSpawn,
                new Vector3(playfieldCenter.x + Random.Range(-playfieldSize.x, playfieldSize.x) / 2, playfieldCenter.y + Random.Range(-playfieldSize.y, playfieldSize.y) / 2, playfieldCenter.z + Random.Range(-playfieldSize.z, playfieldSize.z) / 2),
                divisionID
                );
        SquadronOrders order = new SquadronOrders()
        {
            formation = SquadronOrders.Formation.Defensive,
            rallyPoint = new Vector3(0, 0, 0),
            squdronID = divisionID
        };
        orders.Add(order);
    }

    public void SpawnAgent(AgentTypeSO type, Vector3 spawnPoint, int squadron)
    {
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.transform.position = spawnPoint;
        Agent toAdd = new Agent() {
            position = spawnPoint,
            squadron = squadron,
            up = new Vector3(0, 1, 0),
            forward = new Vector3(1, 0, 0),
            colliderSize = type.GetColliderSize(),
            mass = type.GetMass(),
            dragCoefficient = type.GetDragCoefficient(),
            engineMaxStrenght = type.GetEngineStrenght(),
            breakingStrenght = type.GetBreakingStrenght(),
            maneuverabilityHorizontal = type.GetManeuverabilityHorizontal(),
            maneuverabilityVertical = type.GetManeuverabilityVertical(),
            rollSpeed = type.GetRollSpeed(),
            agentMaxHP = type.GetHp(),
            agentHP = type.GetHp(),
            avoidenenceRange = type.GetAvoidenenceRange(),
        };
        
        //string agentName = AgentNames.GetRandomName();
        toAdd.id = GetNextId();
        newAgent.transform.name = toAdd.id.ToString();
        agentNames.Add(toAdd.id, AgentNames.GetRandomName());

        allAgentTransforms.Add(newAgent.transform);
        allAgents.Add(toAdd);
    }

    void SortAgents()
    {
        for(int i = 1; i < allAgents.Count; i++)
        {
            for(int j = i - 1; j >= 0; j--)
            {
                if(allAgentTransforms[j].position.x - allAgents[j].colliderSize > allAgentTransforms[j + 1].position.x - allAgents[j + 1].colliderSize)
                {
                    Agent temp = allAgents[j];
                    allAgents[j] = allAgents[j + 1];
                    allAgents[j + 1] = temp;

                    Transform tempTransform = allAgentTransforms[j];
                    allAgentTransforms[j] = allAgentTransforms[j + 1];
                    allAgentTransforms[j + 1] = tempTransform;
                }
                else
                {
                    break;
                }
            }
        }
    }

    int GetNextId()
    {
        return nextAgentId++;
    }

    public Agent GetAgentById(int id)
    {
        return allAgents.Find(x => x.id == id);
    }

    public string GetAgentNameByID(int id)
    {
        return agentNames[id];
    }

    public int GetAgentCount()
    {
        return agentCount;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (Agent agent in allAgents)
        {
            Gizmos.DrawWireSphere(agent.position, agent.colliderSize);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(agent.position, agent.position + agent.steeringVector);
        }

        Gizmos.color = Color.white;

        Gizmos.DrawWireCube(playfieldCenter, new Vector3(playfieldSize.x, playfieldSize.y, playfieldSize.z));

        foreach (SquadronOrders order in orders)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(order.rallyPoint, 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(order.rallyPoint, SquadronOrders.DEFENSIVE_FORMATION_ACCEPTED_RADIUS);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(order.rallyPoint, SquadronOrders.OFFENSIVE_FORMATION_ACCEPTED_RADIUS);
        }
    }
}
