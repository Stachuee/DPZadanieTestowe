using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AgentPooling : MonoBehaviour
{
    public static AgentPooling Instance;

    [SerializeField] GameObject agentPrefab;

    ObjectPool<GameObject> agentPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two agents pooler in one scene");
        }
    }

    /// <summary>
    /// Create pool with size.
    /// </summary>
    /// <param name="poolSize"> Pool size.</param>
    public void CreatePool(int poolSize)
    {
        agentPool = new ObjectPool<GameObject>(CreateAgent, GetNewAgent, ReturnAgent, DestroyExcessAgent, true, poolSize, 1000);
    }

    private GameObject CreateAgent()
    {
        return Instantiate(agentPrefab);
    }

    private void GetNewAgent(GameObject agent)
    {
        agent.SetActive(true);
    }

    private void ReturnAgent(GameObject agent)
    {
        agent.SetActive(false);
    }

    private void DestroyExcessAgent(GameObject agent)
    {

    }

    /// <summary>
    /// Get new agent.
    /// </summary>
    public GameObject NewAgentFromPool()
    {
        return agentPool.Get();
    }


    /// <summary>
    /// Return agent to pool.
    /// </summary>
    public void ReturnAgentToPool(GameObject toReturn)
    {
        agentPool.Release(toReturn);
    }
}
