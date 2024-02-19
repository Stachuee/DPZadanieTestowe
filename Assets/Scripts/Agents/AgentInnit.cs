using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentInnit : MonoBehaviour
{
    [SerializeField]
    MeshRenderer[] myRenderers;

    public void Innit(int team, int warpMatIndex)
    {
        for(int i = 0; i < myRenderers.Length; i++)
        {
            myRenderers[i].material = TeamManager.GetTeamMat(team);
            TeamManager.SubscribeUnitToWarpMat(myRenderers[i], warpMatIndex);
        }
    }
}
