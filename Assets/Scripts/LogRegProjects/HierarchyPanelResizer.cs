using UnityEngine;
using UnityEngine.EventSystems;

public class HierarchyPanelResizer : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform leftPanel;
    [SerializeField] private RectTransform rightPanel;
    [SerializeField] private RectTransform handleRect;
    [SerializeField] private float minWidth = 220f;
    [SerializeField] private float maxWidth = 520f;
    [SerializeField] private float panelGap = 3f;

    private Canvas rootCanvas;

    void Awake()
    {
        ResolveReferences();
        rootCanvas = GetComponentInParent<Canvas>();
        ApplyCurrentWidth();
    }

    void OnEnable()
    {
        ResolveReferences();
        ApplyCurrentWidth();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (bodyRoot == null || leftPanel == null || rightPanel == null)
            return;

        Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? eventData.pressEventCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bodyRoot, eventData.position, eventCamera, out Vector2 localPoint))
            return;

        float pivotOffset = bodyRoot.rect.width * bodyRoot.pivot.x;
        float desiredWidth = Mathf.Clamp(localPoint.x + pivotOffset, minWidth, GetEffectiveMaxWidth());
        ApplyWidth(desiredWidth);
    }

    void ResolveReferences()
    {
        if (bodyRoot == null)
            bodyRoot = transform.parent as RectTransform;

        if (bodyRoot == null)
            return;

        if (leftPanel == null)
            leftPanel = bodyRoot.Find("LeftPanel") as RectTransform;

        if (rightPanel == null)
            rightPanel = bodyRoot.Find("RightPanel") as RectTransform;

        if (handleRect == null)
            handleRect = transform as RectTransform;
    }

    void ApplyCurrentWidth()
    {
        if (leftPanel == null)
            return;

        ApplyWidth(leftPanel.offsetMax.x);
    }

    void ApplyWidth(float width)
    {
        if (leftPanel == null || rightPanel == null || handleRect == null)
            return;

        leftPanel.offsetMin = new Vector2(0f, leftPanel.offsetMin.y);
        leftPanel.offsetMax = new Vector2(width, leftPanel.offsetMax.y);

        rightPanel.offsetMin = new Vector2(width + panelGap, rightPanel.offsetMin.y);

        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = new Vector2(width + panelGap * 0.5f, 0f);
    }

    float GetEffectiveMaxWidth()
    {
        if (bodyRoot == null)
            return maxWidth;

        float bodyWidth = bodyRoot.rect.width;
        float hardMax = Mathf.Max(minWidth, bodyWidth - 360f);
        return Mathf.Min(maxWidth, hardMax);
    }
}
