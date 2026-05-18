using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SchemeMarkerUI : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Button dragButton;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Image line;
    [SerializeField] private RectTransform targetPoint;

    [Header("Line")]
    [SerializeField] private float lineThickness = 3f;

    private RectTransform rectTransform;
    private RectTransform buttonRect;
    private Canvas canvas;
    private SchemeMarker marker;
    private bool buttonWasDragged;

    public event Action<SchemeMarkerUI> OnButtonClicked;
    public event Action<SchemeMarkerUI> OnDeleteClicked;
    public event Action<SchemeMarkerUI> OnChanged;

    public SchemeMarker Marker => marker;

    void Awake()
    {
        AutoBind();

        rectTransform = (RectTransform)transform;

        if (dragButton != null)
            buttonRect = (RectTransform)dragButton.transform;

        canvas = GetComponentInParent<Canvas>();

        SetupDrag(dragButton != null ? dragButton.gameObject : null, buttonRect);
        SetupDrag(targetPoint != null ? targetPoint.gameObject : null, targetPoint);

        if (dragButton != null)
        {
            dragButton.onClick.RemoveAllListeners();
            dragButton.onClick.AddListener(() =>
            {
                if (buttonWasDragged)
                {
                    buttonWasDragged = false;
                    return;
                }

                OnButtonClicked?.Invoke(this);
            });
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke(this));
        }
    }

    void AutoBind()
    {
        if (dragButton == null)
        {
            Transform button = transform.Find("DragButton");

            if (button != null)
                dragButton = button.GetComponent<Button>();
        }

        if (buttonLabel == null && dragButton != null)
            buttonLabel = dragButton.GetComponentInChildren<TMP_Text>();

        if (deleteButton == null)
        {
            Transform delete = transform.Find("DeleteButton");

            if (delete != null)
                deleteButton = delete.GetComponent<Button>();
        }

        if (line == null)
        {
            Transform lineTransform = transform.Find("Line");

            if (lineTransform != null)
                line = lineTransform.GetComponent<Image>();
        }

        if (targetPoint == null)
        {
            Transform point = transform.Find("TargetPoint");

            if (point != null)
                targetPoint = point.GetComponent<RectTransform>();
        }
    }

    void LateUpdate()
    {
        UpdateLine();
    }

    public void Setup(SchemeMarker marker)
    {
        this.marker = marker;

        if (buttonLabel != null)
            buttonLabel.text = marker.TargetNodeName;

        if (buttonRect != null)
            buttonRect.anchoredPosition = new Vector2(marker.ButtonX, marker.ButtonY);

        if (targetPoint != null)
            targetPoint.anchoredPosition = new Vector2(marker.TargetX, marker.TargetY);

        UpdateLine();
    }

    public void SetInitialPositions(Vector2 buttonPosition, Vector2 targetPosition)
    {
        if (buttonRect != null)
            buttonRect.anchoredPosition = buttonPosition;

        if (targetPoint != null)
            targetPoint.anchoredPosition = targetPosition;

        SaveCurrentPositions();
        UpdateLine();
    }

    void SetupDrag(GameObject target, RectTransform draggedRect)
    {
        if (target == null || draggedRect == null)
            return;

        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        AddTrigger(trigger, EventTriggerType.Drag, data =>
        {
            var eventData = (PointerEventData)data;
            
            if (draggedRect == buttonRect)
                buttonWasDragged = true;

            DragToPointer(draggedRect, eventData);
        });

        AddTrigger(trigger, EventTriggerType.EndDrag, _ =>
        {
            SaveCurrentPositions();
            OnChanged?.Invoke(this);
        });
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    void DragToPointer(RectTransform draggedRect, PointerEventData eventData)
    {
        RectTransform dragParent = draggedRect.parent as RectTransform;

        if (dragParent == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragParent,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out Vector2 localPoint
        );

        draggedRect.anchoredPosition = localPoint;
        SaveCurrentPositions();
        UpdateLine();
    }

    void SaveCurrentPositions()
    {
        if (marker == null || buttonRect == null || targetPoint == null)
            return;

        marker.ButtonX = buttonRect.anchoredPosition.x;
        marker.ButtonY = buttonRect.anchoredPosition.y;
        marker.TargetX = targetPoint.anchoredPosition.x;
        marker.TargetY = targetPoint.anchoredPosition.y;
    }

    void UpdateLine()
    {
        if (line == null || buttonRect == null || targetPoint == null)
            return;

        RectTransform lineRect = line.rectTransform;
        Vector2 start = buttonRect.anchoredPosition;
        Vector2 end = targetPoint.anchoredPosition;
        Vector2 direction = end - start;

        lineRect.anchoredPosition = start + direction * 0.5f;
        lineRect.sizeDelta = new Vector2(direction.magnitude, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        rectTransform.SetAsLastSibling();
        lineRect.SetAsFirstSibling();
    }
}
