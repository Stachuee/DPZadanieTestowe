using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AgentInfoUI : MonoBehaviour
{
    [SerializeField] GameObject agentInfoPanel;

    [SerializeField] Image agentHp;
    [SerializeField] TextMeshProUGUI agentName;

    [SerializeField] LayerMask agentMask;


    Camera cam;


    int currentAgent;
    bool trackingAgent;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) GetClickedAgent();

        UpdateUI();
    }

    void GetClickedAgent()
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, agentMask))
        {
            agentName.text = hit.collider.name;

            int id;
            if(int.TryParse(hit.collider.name, out id))
            {
                currentAgent = id;
                agentName.text = AgentManager.Instance.GetAgentNameByID(id);
            }
            else
            {
                Debug.LogError("Agent ID in name incorrect");
            }

            trackingAgent = true;
            agentInfoPanel.SetActive(true);
        }
        else
        {
            trackingAgent = false;
            agentInfoPanel.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (!trackingAgent) return;
        Agent agent = AgentManager.Instance.GetAgentById(currentAgent); // searching list every frame is not optimal, but worsk for now
        agentHp.fillAmount = agent.agentHP / agent.agentMaxHP;
    }
}
