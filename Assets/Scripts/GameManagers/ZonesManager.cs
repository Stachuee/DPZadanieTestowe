using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonesManager : MonoBehaviour
{
    public static ZonesManager Instance { get; private set; }

    [SerializeField] List<Material> zoneMats;

    [SerializeField] GameObject zoneObject;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two zone managers");
        }
    }


    private void Start()
    {
        List<CaptureZone> zones = AgentManager.Instance.GetZones();

        for (int i = 0; i < zones.Count; i++)
        {
            GameObject temp = Instantiate(zoneObject, zones[i].position, Quaternion.identity);
            temp.transform.localScale = Vector3.one * zones[i].size * 2;
            temp.GetComponent<MeshRenderer>().material = zoneMats[i];
        }

    }

    private void Update()
    {
        List<CaptureZone> zones = AgentManager.Instance.GetZones();    
        for(int i = 0; i < zones.Count; i++)
        {
            zoneMats[i].SetColor("_CaptureColor", TeamManager.GetTeamColor(zones[i].teamControlling));
            zoneMats[i].SetFloat("_Controll", zones[i].controll);
        }
    }

}
