using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static List<Material> teamMats;
    [SerializeField] List<Material> _teamMats;


    private void Awake()
    {
        teamMats = _teamMats;
    }

    public static Material GetTeamMat(int team)
    {
        if (team >= 0 && team < teamMats.Count)
        {
            return teamMats[team];
        }
        else
        {
            Debug.LogError("Team " + team.ToString() + " doesnt exist");
            return null;
        }
    }
}
