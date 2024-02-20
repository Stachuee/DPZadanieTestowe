using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scoreboard : MonoBehaviour
{
    [SerializeField] GameObject scoreboardTeamTilePrefab;

    Dictionary<int, ScoreboardTile> scoreboard = new Dictionary<int, ScoreboardTile> ();


    private void Start()
    {
        for(int i = 0; i < TeamManager.teams.Count; i++)
        {
            scoreboard.Add(i, Instantiate(scoreboardTeamTilePrefab, transform).GetComponent<ScoreboardTile>());
            scoreboard[i].Setup(TeamManager.teams[i].teamName, TeamManager.teams[i].teamColor);
        }
    }


    private void Update()
    {
        for (int i = 0; i < TeamManager.teams.Count; i++)
        {
            scoreboard[i].UpdatePoints(TeamManager.teams[i].teamPoints);
        }
    }
}
