using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [SerializeField] List<SquadronTypeSO> squadronTypes = new List<SquadronTypeSO>();

    [SerializeField] bool spawning;

    [SerializeField] Vector2 waveCooldown;

    [SerializeField] int unitLimitPerTeam;

    private void Start()
    {
        StartCoroutine(StartSpawning());
    }

    IEnumerator StartSpawning()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(waveCooldown.x, waveCooldown.y));
            if (spawning)
            {
                List<Squadron> squadrons = AgentManager.Instance.GetSquadrons();
                for (int i = 0; i < TeamManager.teams.Count; i++)
                {
                    int unitCount = 0;
                    squadrons.ForEach(squadron =>
                    {
                        if (squadron.teamID == i)
                        {
                            unitCount += squadron.squadronUnitCount;
                        }
                    });

                    if (unitCount < unitLimitPerTeam)
                    {
                        AgentManager.Instance.WarpSquadron(squadronTypes[Random.Range(0, squadronTypes.Count)], i, true);
                    }
                }
            }
        }
    }

}
