using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class SchemeItemUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image image;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Dropdown markerNodeDropdown;
    [SerializeField] private RectTransform markerLayer;
    [SerializeField] private GameObject markerPrefab;

    public event Action<SchemeItemUI> OnDelete;

    public string FilePath { get; private set; }

    private ProjectNode currentNode;
    private HierarchyUI hierarchyUI;
    private DatabaseManager db;
    private Canvas canvas;
    private readonly List<ProjectNode> markerTargets = new();
    private readonly List<SchemeMarker> schemeMarkers = new();

    void Awake()
    {
        db = DatabaseManager.Instance;
        canvas = GetComponentInParent<Canvas>();

        if (markerLayer == null)
        {
            Transform layer = transform.Find("MarkerLayer");

            if (layer != null)
                markerLayer = layer.GetComponent<RectTransform>();
        }

        if (markerNodeDropdown == null)
        {
            Transform dropdown = transform.Find("MarkerNodeDropdown");

            if (dropdown != null)
                markerNodeDropdown = dropdown.GetComponent<TMP_Dropdown>();
        }
    }

    public void Setup(Sprite sprite, string path, ProjectNode node, HierarchyUI hierarchy)
    {
        image.sprite = sprite;
        FilePath = path;
        currentNode = node;
        hierarchyUI = hierarchy;

        if (markerLayer == null)
            markerLayer = (RectTransform)transform;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            OnDelete?.Invoke(this);
        });

        FillMarkerNodeDropdown();
        LoadMarkers();
    }

    void FillMarkerNodeDropdown()
    {
        markerTargets.Clear();

        if (currentNode != null && currentNode.Children != null)
        {
            foreach (var child in currentNode.Children)
            {
                if (!HasMarkerForNode(child.Id))
                    markerTargets.Add(child);
            }
        }

        if (markerNodeDropdown == null)
            return;

        markerNodeDropdown.ClearOptions();
        
        if (markerTargets.Count == 0)
        {
            markerNodeDropdown.interactable = false;
            markerNodeDropdown.AddOptions(new List<string> { "All child nodes added" });
            return;
        }

        markerNodeDropdown.interactable = true;

        var names = new List<string>();

        foreach (var child in markerTargets)
            names.Add(child.Name);

        markerNodeDropdown.AddOptions(names);
        markerNodeDropdown.value = 0;
        markerNodeDropdown.RefreshShownValue();
    }

    async void LoadMarkers()
    {
        if (db == null || markerPrefab == null || markerLayer == null)
            return;

        foreach (Transform child in markerLayer)
        {
            if (child.GetComponent<SchemeMarkerUI>() != null)
                Destroy(child.gameObject);
        }

        var markers = await db.GetSchemeMarkersAsync(FilePath);
        schemeMarkers.Clear();
        schemeMarkers.AddRange(markers);
        FillMarkerNodeDropdown();

        foreach (var marker in markers)
            CreateMarkerUI(marker);
    }

    public async void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (currentNode == null || markerPrefab == null || markerLayer == null)
            return;

        ProjectNode targetNode = GetSelectedMarkerTarget();

        if (targetNode == null)
        {
            Debug.Log("Selected node has no child nodes for marker");
            return;
        }

        if (HasMarkerForNode(targetNode.Id))
        {
            Debug.Log("Marker for this node already exists on this scheme");
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markerLayer,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out Vector2 localPoint
        );

        var marker = new SchemeMarker
        {
            SchemePath = FilePath,
            TargetNodeId = targetNode.Id,
            TargetNodeName = targetNode.Name,
            ButtonX = localPoint.x + 120f,
            ButtonY = localPoint.y + 40f,
            TargetX = localPoint.x,
            TargetY = localPoint.y
        };

        marker.Id = await db.SaveSchemeMarkerAsync(marker);
        schemeMarkers.Add(marker);
        CreateMarkerUI(marker);
        FillMarkerNodeDropdown();
    }

    bool HasMarkerForNode(int nodeId)
    {
        foreach (var marker in schemeMarkers)
        {
            if (marker.TargetNodeId == nodeId)
                return true;
        }

        return false;
    }

    ProjectNode GetSelectedMarkerTarget()
    {
        if (markerTargets.Count == 0)
            return null;

        if (markerNodeDropdown == null)
            return markerTargets[0];

        int index = markerNodeDropdown.value;

        if (index < 0 || index >= markerTargets.Count)
            return null;

        return markerTargets[index];
    }

    void CreateMarkerUI(SchemeMarker marker)
    {
        var obj = Instantiate(markerPrefab, markerLayer);
        RectTransform rt = (RectTransform)obj.transform;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var ui = obj.GetComponent<SchemeMarkerUI>();

        if (ui == null)
            return;

        ui.Setup(marker);
        ui.OnChanged += SaveMarker;
        ui.OnButtonClicked += OpenMarkerNode;
        ui.OnDeleteClicked += DeleteMarker;
    }

    async void SaveMarker(SchemeMarkerUI markerUI)
    {
        if (db == null || markerUI.Marker == null)
            return;

        await db.SaveSchemeMarkerAsync(markerUI.Marker);
    }

    async void DeleteMarker(SchemeMarkerUI markerUI)
    {
        if (db == null || markerUI.Marker == null)
            return;

        await db.DeleteSchemeMarkerAsync(markerUI.Marker.Id);
        schemeMarkers.Remove(markerUI.Marker);
        Destroy(markerUI.gameObject);
        FillMarkerNodeDropdown();
    }

    void OpenMarkerNode(SchemeMarkerUI markerUI)
    {
        if (hierarchyUI == null || markerUI.Marker == null)
            return;

        hierarchyUI.OpenNode(markerUI.Marker.TargetNodeId);
    }
}
