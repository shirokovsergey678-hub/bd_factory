using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SmartVerticalScrollbarHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private SmartVerticalScrollbar controller;

    void Reset()
    {
        controller = GetComponentInParent<SmartVerticalScrollbar>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        controller?.BeginHandleDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controller?.BeginHandleDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller?.DragHandle(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        controller?.EndHandleDrag();
    }
}
