using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AgentPooling : MonoBehaviour
{
    public static AgentPooling Instance;

    [SerializeField] GameObject agentPrefab;
    [SerializeField] ParticleSystem explosionPrefab;

    ObjectPool<GameObject> agentPool;
    ObjectPool<ParticleSystem> explosionPool;

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
    public void CreateAgentPool(int poolSize)
    {
        agentPool = new ObjectPool<GameObject>(CreateAgent, GetNewAgent, ReturnAgent, DestroyExcessAgent, true, poolSize, poolSize * 2);
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
        Destroy(agent);
    }


    public void CreateExplosionPool(int poolSize)
    {
        explosionPool = new ObjectPool<ParticleSystem>(CreateExplosion, GetNewExplosion, ReturnExplosion, DestroyExcessExplosion, true, poolSize, poolSize * 2);
    }

    private ParticleSystem CreateExplosion()
    {
        return Instantiate(explosionPrefab);
    }

    private void GetNewExplosion(ParticleSystem explosion)
    {
        explosion.gameObject.SetActive(true);
    }

    private void ReturnExplosion(ParticleSystem explosion)
    {
        explosion.gameObject.SetActive(false);
    }

    private void DestroyExcessExplosion(ParticleSystem explosion)
    {
        Destroy(explosion);
    }



    /// <summary>
    /// Get new agent.
    /// </summary>
    public GameObject NewAgentFromPool()
    {
        return agentPool.Get();
    }

    public ParticleSystem NewExplosionFromPool()
    {
        return explosionPool.Get();
    }


    /// <summary>
    /// Return agent to pool.
    /// </summary>
    public void ReturnAgentToPool(GameObject toReturn)
    {
        agentPool.Release(toReturn);
    }

    public void ReturnExplosionToPool(ParticleSystem toReturn)
    {
        explosionPool.Release(toReturn);
    }
}
