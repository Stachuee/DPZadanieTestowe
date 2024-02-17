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

    Dictionary<int, string> agentNames = new Dictionary<int, string>(); // remember to remove names after destroying agent

    [SerializeField] Vector3 playfieldCenter;
    [SerializeField] Vector3 playfieldSize;


    int nextAgentId = 0;

    NativeArray<Agent> agentsNativeArray;
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
        SpawnAgent(toSpawn, new Vector3(playfieldCenter.x + Random.Range(-playfieldSize.x, playfieldSize.x)/2, playfieldCenter.y + Random.Range(-playfieldSize.y, playfieldSize.y) / 2, playfieldCenter.z + Random.Range(-playfieldSize.z, playfieldSize.z) / 2));
    }


    private void Update()
    {
        SortAgents();

        agentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        transformAccessArray = new TransformAccessArray(allAgentTransforms.ToArray());

        AgentCollision agentCollisionJob = new AgentCollision(agentsNativeArray);
        agentCollisionJob.Run();


        for(int i = 0; i < allAgents.Count; i++)
        {
            transformAccessArray[i].position = agentsNativeArray[i].position; // not sure about that. Could be using this wrong
        }

        AgentSteering agentSteeringJob = new AgentSteering(agentsNativeArray, 
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
            allAgentTransforms[i].forward = allAgents[i].flightDirection;
        }

        agentsNativeArray.Dispose();
        transformAccessArray.Dispose();
    }

    public void SpawnAgent(AgentTypeSO type, Vector3 spawnPoint)
    {
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.transform.position = spawnPoint;
        Agent toAdd = new Agent() {
            position = spawnPoint,
            colliderSize = type.GetColliderSize(),
            mass = type.GetMass(),
            dragCoefficient = type.GetDragCoefficient(),
            engineMaxStrenght = type.GetEngineStrenght(),
            agentMaxHP = type.GetHp(),
            agentHP = type.GetHp(),
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
        }

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(playfieldCenter, new Vector3(playfieldSize.x, playfieldSize.y, playfieldSize.z));

    }
}
