using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SmartVerticalScrollbar : MonoBehaviour, IScrollHandler
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform handleRect;

    [Header("Handle")]
    [SerializeField] private float minHandleHeight = 28f;
    [SerializeField] private float trackPaddingTop = 2f;
    [SerializeField] private float trackPaddingBottom = 2f;
    [SerializeField] private bool hideWhenNotScrollable = true;

    [Header("Scroll Feel")]
    [SerializeField] [Range(0.02f, 1f)] private float wheelViewportStep = 0.16f;
    [SerializeField] private bool manageMouseWheel = true;

    private bool isDraggingHandle;
    private float dragPointerOffsetFromHandleTop;
    private float lastViewportHeight = -1f;
    private float lastContentHeight = -1f;
    private float lastNormalizedPosition = -1f;
    private float cachedScrollSensitivity = -1f;

    public ScrollRect ScrollRect => scrollRect;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        ResolveReferences();
        RefreshVisuals(true);
    }

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        ResolveReferences();

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            if (manageMouseWheel)
            {
                cachedScrollSensitivity = scrollRect.scrollSensitivity;
                scrollRect.scrollSensitivity = 0f;
            }
        }

        RefreshVisuals(true);
    }

    void OnDisable()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);

            if (manageMouseWheel && cachedScrollSensitivity >= 0f)
                scrollRect.scrollSensitivity = cachedScrollSensitivity;
        }

        isDraggingHandle = false;
    }

    void LateUpdate()
    {
        if (scrollRect == null || trackRect == null || handleRect == null)
            return;

        float viewportHeight = GetViewportHeight();
        float contentHeight = GetContentHeight();
        float normalized = GetNormalizedPosition();

        if (!Mathf.Approximately(viewportHeight, lastViewportHeight) ||
            !Mathf.Approximately(contentHeight, lastContentHeight) ||
            !Mathf.Approximately(normalized, lastNormalizedPosition))
        {
            RefreshVisuals();
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!manageMouseWheel || scrollRect == null || !CanScroll())
            return;

        float delta = eventData.scrollDelta.y;
        if (Mathf.Approximately(delta, 0f))
            return;

        float hiddenHeight = Mathf.Max(0f, GetContentHeight() - GetViewportHeight());
        if (hiddenHeight <= 0.001f)
            return;

        float viewportStep = GetViewportHeight() * wheelViewportStep * Mathf.Max(1f, Mathf.Abs(delta));
        float normalizedStep = viewportStep / hiddenHeight;

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + normalizedStep * Mathf.Sign(delta));

        RefreshVisuals(true);
        eventData.Use();
    }

    public void BeginHandleDrag(PointerEventData eventData)
    {
        if (scrollRect == null || trackRect == null || handleRect == null || !CanScroll())
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(handleRect, eventData.position, eventData.pressEventCamera))
            return;

        if (!TryGetPointerYFromTrackTop(eventData, out float pointerYFromTop))
            return;

        isDraggingHandle = true;
        dragPointerOffsetFromHandleTop = GetPointerYInsideTrack(pointerYFromTop) - GetHandleTopOffset();
        scrollRect.StopMovement();
    }

    public void DragHandle(PointerEventData eventData)
    {
        if (!isDraggingHandle || scrollRect == null || trackRect == null || handleRect == null)
            return;

        if (!TryGetPointerYFromTrackTop(eventData, out float pointerYFromTop))
            return;

        float travel = GetHandleTravel();
        if (travel <= 0.001f)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            RefreshVisuals(true);
            return;
        }

        float newHandleTop = Mathf.Clamp(
            GetPointerYInsideTrack(pointerYFromTop) - dragPointerOffsetFromHandleTop,
            0f,
            travel);

        scrollRect.verticalNormalizedPosition = 1f - (newHandleTop / travel);
        RefreshVisuals(true);
    }

    public void EndHandleDrag()
    {
        isDraggingHandle = false;
    }

    public void RefreshVisuals(bool forceLayout = false)
    {
        ResolveReferences();

        if (scrollRect == null || trackRect == null || handleRect == null)
            return;

        if (forceLayout)
            Canvas.ForceUpdateCanvases();

        bool canScroll = CanScroll();

        if (hideWhenNotScrollable)
            trackRect.gameObject.SetActive(canScroll);
        else if (!trackRect.gameObject.activeSelf)
            trackRect.gameObject.SetActive(true);

        if (!canScroll)
        {
            lastViewportHeight = GetViewportHeight();
            lastContentHeight = GetContentHeight();
            lastNormalizedPosition = 1f;
            return;
        }

        float trackInnerHeight = GetTrackInnerHeight();
        float viewportHeight = GetViewportHeight();
        float contentHeight = GetContentHeight();
        float sizeRatio = Mathf.Clamp01(viewportHeight / Mathf.Max(viewportHeight, contentHeight));
        float handleHeight = Mathf.Clamp(trackInnerHeight * sizeRatio, minHandleHeight, trackInnerHeight);
        float travel = Mathf.Max(0f, trackInnerHeight - handleHeight);
        float normalized = GetNormalizedPosition();
        float handleTopOffset = (1f - normalized) * travel;

        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.anchoredPosition = new Vector2(0f, -trackPaddingTop - handleTopOffset);
        handleRect.sizeDelta = new Vector2(0f, handleHeight);
        handleRect.localScale = Vector3.one;
        handleRect.localRotation = Quaternion.identity;

        lastViewportHeight = viewportHeight;
        lastContentHeight = contentHeight;
        lastNormalizedPosition = normalized;
    }

    private void OnScrollRectValueChanged(Vector2 _)
    {
        RefreshVisuals();
    }

    private void ResolveReferences()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect != null)
        {
            if (viewport == null)
                viewport = scrollRect.viewport;

            if (content == null)
                content = scrollRect.content;
        }
    }

    private float GetViewportHeight()
    {
        return viewport != null ? viewport.rect.height : 0f;
    }

    private float GetContentHeight()
    {
        return content != null ? content.rect.height : 0f;
    }

    private float GetNormalizedPosition()
    {
        return scrollRect != null ? Mathf.Clamp01(scrollRect.verticalNormalizedPosition) : 1f;
    }

    private bool CanScroll()
    {
        return scrollRect != null &&
               viewport != null &&
               content != null &&
               trackRect != null &&
               handleRect != null &&
               GetContentHeight() > GetViewportHeight() + 0.5f;
    }

    private float GetTrackInnerHeight()
    {
        return Mathf.Max(0f, trackRect.rect.height - trackPaddingTop - trackPaddingBottom);
    }

    private float GetHandleTravel()
    {
        return Mathf.Max(0f, GetTrackInnerHeight() - handleRect.rect.height);
    }

    private float GetHandleTopOffset()
    {
        return (1f - GetNormalizedPosition()) * GetHandleTravel();
    }

    private bool TryGetPointerYFromTrackTop(PointerEventData eventData, out float value)
    {
        value = 0f;

        if (trackRect == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                trackRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        value = trackRect.rect.yMax - localPoint.y;
        return true;
    }

    private float GetPointerYInsideTrack(float pointerYFromTop)
    {
        return Mathf.Clamp(pointerYFromTop - trackPaddingTop, 0f, GetTrackInnerHeight());
    }
}
