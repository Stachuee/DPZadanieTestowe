using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] ParticleSystem particle;
    public void OnParticleSystemStopped()
    {
        AgentPooling.Instance.ReturnExplosionToPool(particle);
    }
}
