using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconOverlayUI : MonoBehaviour
{
    public static IconOverlayUI Instance { get; private set; }

    [SerializeField] GameObject pointObject;
    [SerializeField] GameObject squadronObject;
    [SerializeField] GameObject warpPointObject;

    [SerializeField] GameObject spawnerInfoPanel;

    [SerializeField] float maxMarkerMovment;

    List<GameObject> warpPoints = new List<GameObject>();

    Dictionary<int, UIOverlayMarker> squadronMarkers = new Dictionary<int, UIOverlayMarker>();
    Dictionary<int, UIOverlayMarker> warpMarkers = new Dictionary<int, UIOverlayMarker>();
    Camera cam;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two icon overlay controllers");
        }
    }

    private void Start()
    {
        cam = Camera.main;
        for (int i = 0; i < TeamManager.teams.Count; i++)
        {
            GameObject icon = Instantiate(warpPointObject, transform);
            warpMarkers.Add(i, icon.GetComponent<UIOverlayMarker>());
            warpPoints.Add(icon);
            warpMarkers[i].SetMarker(i, TeamManager.teams[i].teamColor);   
        }
    }

    private void Update()
    {
        UpdateWarpPoints();
        UpdateSquadronMarkers();
    }

    public void ClickedOnSquadron(int id)
    {
        AgentInfoUI.Instance.FollowSquadron(id);
    }

    public void ClickedOnWarpPoint(int id)
    {
        TeamManager.currentTeam = id;
        spawnerInfoPanel.SetActive(true);
        HighlightWarpPoint(id, true);
    }

    void UpdateWarpPoints()
    {
        for (int i = 0; i < warpPoints.Count; i++)
        {
            warpPoints[i].transform.position = cam.WorldToScreenPoint(TeamManager.teams[i].teamWarpPoint);
        }
    }

    void UpdateSquadronMarkers()
    {
        List<Squadron> squadrons = AgentManager.Instance.GetSquadrons();
        for (int i = 0; i < squadrons.Count; i++)
        {
            if(squadronMarkers.ContainsKey(squadrons[i].squdronID)) squadronMarkers[squadrons[i].squdronID].transform.position = cam.WorldToScreenPoint(squadrons[i].squadronCenter);
            //squadronMarkers[i].transform.position = Vector3.MoveTowards(squadronMarkers[i].transform.position, cam.WorldToScreenPoint(squadrons[i].squadronCenter), Screen.width * maxMarkerMovment * Time.deltaTime);
        }
    }

    public void CreateSquadronWaypoint(int id, int teamID)
    {
        GameObject icon = Instantiate(squadronObject, transform);
        squadronMarkers.Add(id, icon.GetComponent<UIOverlayMarker>());
        icon.GetComponent<UIOverlayMarker>().SetMarker(id, TeamManager.teams[teamID].teamColor);
    }

    public void RemoveSquadronWaypoint(int id)
    {
        AgentInfoUI.Instance.SquadDestroyed(id);
        squadronMarkers[id].DestroyMarker();
        squadronMarkers.Remove(id);
    }
    

    public void HighlightSquad(int squadID, bool value)
    {
        squadronMarkers[squadID].Select(value);
    }
    public void HighlightWarpPoint(int team, bool value)
    {
        warpMarkers[team].Select(value);
    }
}
