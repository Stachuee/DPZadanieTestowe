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

    [SerializeField] Image agentHp;
    [SerializeField] TextMeshProUGUI agentName;

    [SerializeField] LayerMask agentMask;


    Camera cam;


    int currentAgent;
    bool trackingAgent;

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

        UpdateUI();
    }

    public void FollowSquadron(int squadronID)
    {
        Debug.Log("following");
        agentInfoPanel.SetActive(true);
    }

    public void HideUI()
    {
        agentInfoPanel.SetActive(false);

    }

    void UpdateUI()
    {
        if (!trackingAgent) return;
        Agent agent = AgentManager.Instance.GetAgentById(currentAgent); // searching list every frame is not optimal, but worsk for now
        agentHp.fillAmount = agent.agentHP / agent.agentMaxHP;
    }
}
