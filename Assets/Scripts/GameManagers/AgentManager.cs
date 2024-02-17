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
    
    List<Agent> allAgents = new List<Agent>();
    List<Transform> allAgentTransforms = new List<Transform>();

    NativeArray<Agent> agentsNativeArray;
    TransformAccessArray accessArray;

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
        agentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        accessArray = new TransformAccessArray(allAgentTransforms.ToArray());

        AgentSteering agentSteeringJob = new AgentSteering(agentsNativeArray, 
            Time.deltaTime);

        agentSteeringHandle = agentSteeringJob.Schedule(accessArray);
    }

    private void LateUpdate()
    {
        agentSteeringHandle.Complete();


        agentsNativeArray.Dispose();
        accessArray.Dispose();
    }

    public void SpawnAgent()
    {
        Agent toAdd = new Agent() { 
            flightDirection = new Vector3 (1, 0, 0),
        };
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        allAgentTransforms.Add(newAgent.transform);
        allAgents.Add(toAdd);
    }


    public int GetAgentCount()
    {
        return agentCount;
    }
}
