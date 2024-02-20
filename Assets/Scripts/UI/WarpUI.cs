using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpUI : MonoBehaviour
{
    
    public void WarpSquadron(SquadronTypeSO type)
    {
        AgentManager.Instance.WarpSquadron(type, TeamManager.currentTeam, true);
    }
}
