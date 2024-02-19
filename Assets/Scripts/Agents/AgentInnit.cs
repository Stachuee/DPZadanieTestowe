using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentInnit : MonoBehaviour
{
    [SerializeField]
    MeshRenderer myRenderer;

    public void Innit(int team, int warpMatIndex)
    {
        myRenderer.material = TeamManager.GetTeamMat(team);
        TeamManager.SubscribeUnitToWarpMat(myRenderer, warpMatIndex);
    }
}
