using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomPanUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDragHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Canvas canvas;

    private bool hovered;

    private float zoom = 1f;

    public float zoomSpeed = 0.1f;
    public float minZoom = 0.3f;
    public float maxZoom = 5f;

    void Update()
    {
        if (!hovered)
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Zoom(scroll);
    }

    void Zoom(float scroll)
    {
        Vector2 mouseLocalBefore;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            Input.mousePosition,
            canvas.worldCamera,
            out mouseLocalBefore
        );

        float oldZoom = zoom;

        zoom += scroll * zoomSpeed;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        float scaleFactor = zoom / oldZoom;

        target.localScale = Vector3.one * zoom;

        Vector2 mouseLocalAfter;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            Input.mousePosition,
            canvas.worldCamera,
            out mouseLocalAfter
        );

        Vector2 difference = mouseLocalAfter - mouseLocalBefore;

        target.anchoredPosition += difference * zoom;
    }

    public void OnDrag(PointerEventData eventData)
    {
        target.anchoredPosition += eventData.delta;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }
}