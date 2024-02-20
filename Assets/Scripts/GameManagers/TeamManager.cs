using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    [System.Serializable]
    public struct Team
    {
        public string teamName;
        public float teamPoints;
        public Material teamMat;
        public Color teamColor;
        public Vector3 teamWarpPoint;
        public float teamWarpRadius;
        public Vector3 warpDirection;
    }

    public static List<Team> teams { get; private set; }
    [SerializeField] List<Team> _teams;

    [System.Serializable]
    struct WarpMaterial
    {
        [HideInInspector] public Vector3 warpPosition;
        [HideInInspector] public Vector3 warpNormal;
        public float warpProgress;
        [HideInInspector] public float warpSpeed;
        [HideInInspector] public int teamWarp;

        public Material mat;

        [HideInInspector] public List<Renderer> renderersUsingMat;

        public bool inProgress;
    }

    [SerializeField] List<WarpMaterial> _warpMaterials;
    static List<WarpMaterial> warpMaterials = new List<WarpMaterial>();

    public static int currentTeam;

    static readonly float WARP_SPEED = 20;
    static readonly float WARP_STREACH = 10;

    private void Awake()
    {
        warpMaterials = _warpMaterials;
        for(int i = 0; i < _teams.Count; i++)
        {
            Team team = _teams[i];
            team.teamMat.SetColor("_BaseColor", team.teamColor);
            team.warpDirection = team.warpDirection.normalized;
            _teams[i] = team;
        }
        teams = _teams;
    }

    private void Update()
    {
        UpdateWarpMaterials();
    }

    public static Color GetTeamColor(int teamID)
    {
        return teams[teamID].teamColor;
    }

    public static Material GetTeamMat(int team)
    {
        if (team >= 0 && team < teams.Count)
        {
            return teams[team].teamMat;
        }
        else
        {
            Debug.LogError("Team " + team.ToString() + " doesnt exist");
            return null;
        }
    }

    public static bool GetTeamWarpMaterialID(int index, out int matIndex)
    {
        int newWarpIndex = warpMaterials.FindIndex(mat => !mat.inProgress);
        if (newWarpIndex == -1)
        {
            Debug.LogError("No free warp mats avalible");
            matIndex = -1;
            return false;
        }

        Team team = teams[index];


        WarpMaterial warpMaterial = warpMaterials[newWarpIndex];
        warpMaterial.inProgress = true;
        warpMaterial.teamWarp = index;

        warpMaterial.warpPosition = team.teamWarpPoint - team.warpDirection * team.teamWarpRadius;
        warpMaterial.warpNormal = -team.warpDirection;
        warpMaterial.warpSpeed = WARP_SPEED;
        warpMaterial.warpProgress = team.teamWarpRadius * 2;
        warpMaterial.renderersUsingMat = new List<Renderer>();

        warpMaterial.mat.SetVector("_WarpPosition", warpMaterial.warpPosition);
        warpMaterial.mat.SetVector("_WarpNormal", warpMaterial.warpNormal);
        warpMaterial.mat.SetFloat("_StrechPerUnit", WARP_STREACH);
        warpMaterial.mat.SetFloat("_warpProgress", warpMaterial.warpProgress);
        warpMaterial.mat.SetColor("_BaseColor", team.teamColor);


        warpMaterials[newWarpIndex] = warpMaterial;
        matIndex = newWarpIndex;
        return true;
    }

    public static void SubscribeUnitToWarpMat(Renderer unit, int matID)
    {
        WarpMaterial warpMat = warpMaterials[matID];

        List<Material> mats = new List<Material>();

        unit.material = warpMat.mat;

        warpMat.renderersUsingMat.Add(unit);

        warpMaterials[matID] = warpMat;
    }

    void UpdateWarpMaterials()
    {
        for(int i = 0; i < warpMaterials.Count; i++)
        {
            if(warpMaterials[i].inProgress)
            {
                WarpMaterial warpMaterial = warpMaterials[i];
                warpMaterial.warpProgress -= Time.deltaTime * warpMaterial.warpSpeed;
                warpMaterial.mat.SetFloat("_warpProgress", warpMaterial.warpProgress);

                if(warpMaterial.warpProgress <= 0)
                {
                    warpMaterial.inProgress = false;

                    warpMaterial.renderersUsingMat.ForEach(render =>
                    {
                        render.material = teams[warpMaterial.teamWarp].teamMat;
                    });
                }

                warpMaterials[i] = warpMaterial;
            }
        }
    }

    public static Vector3 GetRandomTeamWarpPosition(int teamID)
    {
        return teams[teamID].teamWarpPoint + Random.insideUnitSphere * Random.Range(0, teams[teamID].teamWarpRadius);
    }

    public static Vector3 GetTeamWarpRotation(int teamID)
    {
        return teams[teamID].warpDirection;
    }

    public static void TeamGainPoints(int id, float value)
    {
        Team team = teams[id];

        team.teamPoints += value;

        teams[id] = team;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        _teams.ForEach(team =>
        {
            Gizmos.DrawWireSphere(team.teamWarpPoint, team.teamWarpRadius);
            Gizmos.DrawLine(team.teamWarpPoint, team.teamWarpPoint + team.warpDirection * 2);
        });
    }
}
