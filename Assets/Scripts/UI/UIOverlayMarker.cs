using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIOverlayMarker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum MarkerType {WarpPoint, SquadronMarker, CaptureFlag };

    [SerializeField]
    int id;
    [SerializeField] bool clickable;
    [SerializeField] MarkerType markerType;
    [SerializeField] Image image;

    [SerializeField] GameObject selected;

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
        if (!clickable) return;
        switch (markerType)
        {
            case MarkerType.SquadronMarker:
                IconOverlayUI.Instance.ClickedOnSquadron(id);
                break;
            case MarkerType.WarpPoint:
                IconOverlayUI.Instance.ClickedOnWarpPoint(id);
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!clickable) return;
        Color color = image.color;
        color.a = HIGHLIGHT_ALFA;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!clickable) return;
        Color color = image.color;
        color.a = STANDARD_ALFA;
        image.color = color;
    }

    public void DestroyMarker()
    {
        Destroy(gameObject);
    }

    public void Select(bool state)
    {
        selected.SetActive(state);
    }
}
