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

    NativeArray<Agent> agentsNativeArray;
    TransformAccessArray transformAccessArray;

    JobHandle agentSteeringHandle;

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
        SpawnAgent();
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
            Time.deltaTime);

        agentSteeringHandle = agentSteeringJob.Schedule(transformAccessArray);

    }

    private void LateUpdate()
    {
        agentSteeringHandle.Complete();

        for(int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i] = agentsNativeArray[i];
        }

        agentsNativeArray.Dispose();
        transformAccessArray.Dispose();
    }

    public void SpawnAgent()
    {
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.transform.position = new Vector3(2, 0, 0);
        Agent toAdd = new Agent() { 
            flightDirection = new Vector3 (-1, 0, 0),
            colliderSize = .5f,
            position = newAgent.transform.position,
        };
        allAgentTransforms.Add(newAgent.transform);
        allAgents.Add(toAdd);


        newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.transform.position = new Vector3(-2, 0, 0);
        toAdd = new Agent()
        {
            flightDirection = new Vector3(1, 0, 0),
            colliderSize = .5f,
            position = newAgent.transform.position,
        };
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
    }
}
