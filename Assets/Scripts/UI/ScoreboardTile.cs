using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreboardTile : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI teamName;
    [SerializeField] TextMeshProUGUI teamPoints;


    public void Setup(string _teamName, Color teamColor)
    {
        teamName.text = _teamName;
        teamName.color = teamColor;
        teamPoints.text = "0";
    }

    public void UpdatePoints(float points)
    {
        teamPoints.text = Mathf.RoundToInt(points).ToString();
    }
}
