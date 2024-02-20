using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Agent", menuName = "ScriptableObjects/Squadron", order = 3)]
public class SquadronTypeSO : ScriptableObject
{
    [System.Serializable]
    public struct UnitType
    {
        public AgentTypeSO type;
        public int ammount;
    }

    [SerializeField] string squadronName;

    [SerializeField] Sprite squadronImage;
    [SerializeField] List<UnitType> units;

    public string GenerateSquadronName()
    {
        int number = Random.Range(20, 1000);
        return number + (number % 10 == 1 ? "st" : (number % 10 == 2 ? "nd" : (number % 10 == 3 ? "rd" : "th"))) + " " +  squadronName + " squadron";
    }
    public List<UnitType> ToSpawn()
    {
        return units;
    }

    public Sprite GetImage()
    {
        return squadronImage;
    }
}
