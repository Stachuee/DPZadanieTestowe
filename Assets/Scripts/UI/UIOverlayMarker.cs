using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIOverlayMarker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum MarkerType {WarpPoint, SquadronMarker, CaptureFlag };

    int id;
    Image marker;
    [SerializeField] bool clickable;
    [SerializeField] MarkerType markerType;
    [SerializeField] Image image;

    readonly float STANDARD_ALFA = 0.5f;
    readonly float HIGHLIGHT_ALFA = 1f;

    public void SetMarker(int _id, Color color)
    {
        color.a = STANDARD_ALFA;
        image.color = color; 
        id = _id;
    }

    public void OnPointerClick(PointerEventData eventData) 
    {
        IconOverlayUI.Instance.ClickedOnSquadron(markerType, id);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Color color = image.color;
        color.a = HIGHLIGHT_ALFA;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Color color = image.color;
        color.a = STANDARD_ALFA;
        image.color = color;
    }

}
