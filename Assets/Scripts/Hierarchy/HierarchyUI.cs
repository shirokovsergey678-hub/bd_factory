using System.Collections.Generic;
using UnityEngine;

public class HierarchyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private ContextMenuUI contextMenu;
    [SerializeField] private CreateNodePanel createPanel;
    [SerializeField] private HierarchyBackgroundUI background;
    [SerializeField] private NodeDetailsUI nodeDetailsUI;

    private DatabaseManager db;
    private readonly List<ProjectNode> tree = new();
    private readonly List<ProjectNode> visibleNodes = new();

    private int currentProjectId;
    private int loadVersion;
    private ProjectNode selectedNode;
    private bool waitingForParent;

    void Awake()
    {
        db = DatabaseManager.Instance;
        background.OnRightClickEmpty += OnEmptyRightClick;
    }

    public async void LoadHierarchy(int projectId)
    {
        int version = ++loadVersion;

        currentProjectId = projectId;
        selectedNode = null;
        waitingForParent = false;

        Clear();
        nodeDetailsUI.Hide();

        var flat = await db.GetNodesAsync(projectId);

        if (version != loadVersion)
            return;

        tree.Clear();
        tree.AddRange(BuildTree(flat));

        BuildVisibleList();
        Draw();
    }

    List<ProjectNode> BuildTree(List<ProjectNode> flat)
    {
        var lookup = new Dictionary<int, ProjectNode>();
        var roots = new List<ProjectNode>();

        foreach (var node in flat)
        {
            node.Children = new List<ProjectNode>();
            node.IsExpanded = false;
            lookup[node.Id] = node;
        }

        foreach (var node in flat)
        {
            if (node.ParentId.HasValue && lookup.ContainsKey(node.ParentId.Value))
                lookup[node.ParentId.Value].Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }

    void BuildVisibleList()
    {
        visibleNodes.Clear();

        foreach (var root in tree)
            AddRecursive(root, 0);
    }

    void AddRecursive(ProjectNode node, int level)
    {
        node.Level = level;
        visibleNodes.Add(node);

        if (!node.IsExpanded)
            return;

        foreach (var child in node.Children)
            AddRecursive(child, level + 1);
    }

    void Draw()
    {
        Clear();

        foreach (var node in visibleNodes)
        {
            var obj = Instantiate(nodePrefab, content);
            var ui = obj.GetComponent<NodeUI>();

            ui.Setup(node, node.Level);
            ui.SetArrow(node.IsExpanded, node.Children.Count > 0);

            ui.OnClick += OnNodeSelected;
            ui.OnToggle += OnToggle;
            ui.OnRightClick += OnRightClick;
        }
    }

    public void RefreshVisible()
    {
        BuildVisibleList();
        Draw();
    }

    async void OnNodeSelected(ProjectNode node)
    {
        if (waitingForParent)
        {
            waitingForParent = false;

            if (selectedNode == null || selectedNode.Id == node.Id || IsChildOf(selectedNode, node))
                return;

            selectedNode.ParentId = node.Id;
            await db.UpdateNodeParentAsync(selectedNode.Id, node.Id);
            LoadHierarchy(currentProjectId);
            return;
        }

        selectedNode = node;
        nodeDetailsUI.Show(node);
    }

    public bool OpenNode(int nodeId)
    {
        ProjectNode node = FindNode(tree, nodeId);

        if (node == null)
            return false;

        selectedNode = node;
        ExpandParents(tree, nodeId);
        BuildVisibleList();
        Draw();
        nodeDetailsUI.Show(node);

        return true;
    }

    ProjectNode FindNode(List<ProjectNode> nodes, int nodeId)
    {
        foreach (var node in nodes)
        {
            if (node.Id == nodeId)
                return node;

            ProjectNode found = FindNode(node.Children, nodeId);

            if (found != null)
                return found;
        }

        return null;
    }

    bool ExpandParents(List<ProjectNode> nodes, int nodeId)
    {
        foreach (var node in nodes)
        {
            if (node.Id == nodeId)
                return true;

            if (ExpandParents(node.Children, nodeId))
            {
                node.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    bool IsChildOf(ProjectNode parent, ProjectNode possibleChild)
    {
        foreach (var child in parent.Children)
        {
            if (child.Id == possibleChild.Id || IsChildOf(child, possibleChild))
                return true;
        }

        return false;
    }

    void OnToggle(ProjectNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        BuildVisibleList();
        Draw();
    }

    void OnRightClick(ProjectNode node, Vector2 pos)
    {
        selectedNode = node;
        nodeDetailsUI.Show(node);

        contextMenu.Show(
            pos,
            () => ShowCreate(node),
            () => DeleteNode(node),
            () => waitingForParent = true
        );
    }

    async void OnEmptyRightClick(Vector2 pos)
    {
        if (waitingForParent)
        {
            waitingForParent = false;

            if (selectedNode != null)
            {
                selectedNode.ParentId = null;
                await db.UpdateNodeParentAsync(selectedNode.Id, null);
                LoadHierarchy(currentProjectId);
            }

            return;
        }

        contextMenu.Show(pos, () => ShowCreate(null), null, null);
    }

    void ShowCreate(ProjectNode parent)
    {
        contextMenu.Hide();

        createPanel.Show(async (name) =>
        {
            int? parentId = parent?.Id;
            int newId = await db.CreateNodeAsync(currentProjectId, parentId, name);

            var newNode = new ProjectNode
            {
                Id = newId,
                ProjectId = currentProjectId,
                ParentId = parentId,
                Name = name,
                Description = "",
                Quantity = 1,
                Children = new List<ProjectNode>()
            };

            if (parent == null)
            {
                tree.Add(newNode);
            }
            else
            {
                parent.Children.Add(newNode);
                parent.IsExpanded = true;
            }

            BuildVisibleList();
            Draw();
        });
    }

    async void DeleteNode(ProjectNode node)
    {
        await db.DeleteNodeAsync(node.Id);
        RemoveNode(tree, node);
        BuildVisibleList();
        Draw();
    }

    bool RemoveNode(List<ProjectNode> list, ProjectNode target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == target)
            {
                list.RemoveAt(i);
                return true;
            }

            if (RemoveNode(list[i].Children, target))
                return true;
        }

        return false;
    }

    void Clear()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
    }
}
