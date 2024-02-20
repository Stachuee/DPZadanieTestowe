using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AgentInfoUI : MonoBehaviour
{
    public static AgentInfoUI Instance { get; private set; }

    [SerializeField] GameObject agentInfoPanel;

    [SerializeField] Image squadronHp;
    [SerializeField] TextMeshProUGUI squadronName;
    [SerializeField] TextMeshProUGUI squadronUnitCount;

    [SerializeField] GameObject arrow;

    Camera cam;
    [SerializeField] LayerMask orderLayer;

    int currentSquad;
    bool trackingSquad;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two agent info ui controllers");
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) HideUI();
        else if(Input.GetMouseButtonDown(1) && trackingSquad)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, float.PositiveInfinity, orderLayer))
            {
                AgentManager.Instance.SendOrderToSquadron(currentSquad, hit.point);
            }
        }
        UpdateUI();
    }

    public void FollowSquadron(int squadronID)
    {
        agentInfoPanel.SetActive(true);
        trackingSquad = true;
        currentSquad = squadronID;
        arrow.SetActive(true);
        squadronName.text = AgentManager.Instance.GetSquadronNameById(currentSquad);
        IconOverlayUI.Instance.HighlightSquad(currentSquad, true);
    }

    public void HideUI()
    {
        agentInfoPanel.SetActive(false);
        trackingSquad = false;
        arrow.SetActive(false);
        IconOverlayUI.Instance.HighlightSquad(currentSquad, false);
    }

    void UpdateUI()
    {
        if (!trackingSquad) return;
        Squadron squadron = AgentManager.Instance.GetSquadronById(currentSquad);
        squadronHp.fillAmount = squadron.squadronCurrentHp / squadron.squadronMaxHp;
        squadronUnitCount.text = squadron.squadronUnitCount + "\n" + squadron.squadronMaxUnitCount;
        arrow.transform.position = squadron.rallyPoint;
    }
}
