using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Marco : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI marcoText;

    public void RandomNumber()
    {
        int number = Random.Range(0, 101);

        marcoText.text = number.ToString() + (number % 3 == 0 ? "\nMarko" : "") + (number % 5 == 0 ? "\nPolo" : "");
    }
}
