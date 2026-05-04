using UnityEngine;
using System.Collections.Generic;

public class HierarchyUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject nodePrefab;

    [SerializeField] private ContextMenuUI contextMenu;
    [SerializeField] private CreateNodePanel createPanel;
    [SerializeField] private HierarchyBackgroundUI background;
    [SerializeField] private NodeDetailsUI nodeDetailsUI;
    private DatabaseManager db;

    private List<ProjectNode> tree = new();
    private List<ProjectNode> visibleNodes = new();

    private int currentProjectId;


    void Awake()
    {
        background.OnRightClickEmpty += OnEmptyRightClick;
        db = DatabaseManager.Instance;
    }
    void OnEmptyRightClick(Vector2 pos)
    {
        contextMenu.Show(
            pos,
            () => ShowCreate(null), // 👈 теперь через панель
            null
        );
    }
    public async void LoadHierarchy(int projectId)
    {
        currentProjectId = projectId;

        Clear();

        var flat = await db.GetNodesAsync(projectId);
        tree = BuildTree(flat);

        BuildVisibleList();
        Draw();
    }

    List<ProjectNode> BuildTree(List<ProjectNode> flat)
    {
        var lookup = new Dictionary<int, ProjectNode>();
        var roots = new List<ProjectNode>();

        foreach (var n in flat)
        {
            n.Children = new List<ProjectNode>();
            n.IsExpanded = false;
            lookup[n.Id] = n;
        }

        foreach (var n in flat)
        {
            if (n.ParentId.HasValue && lookup.ContainsKey(n.ParentId.Value))
                lookup[n.ParentId.Value].Children.Add(n);
            else
                roots.Add(n);
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

        if (!node.IsExpanded) return;

        foreach (var c in node.Children)
            AddRecursive(c, level + 1);
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
    void OnNodeSelected(ProjectNode node)
    {
        nodeDetailsUI.Show(node);
    }
    void OnToggle(ProjectNode node)
    {
        node.IsExpanded = !node.IsExpanded;

        BuildVisibleList();
        Draw();
    }

    void OnRightClick(ProjectNode node, Vector2 pos)
    {
        contextMenu.Show(
            pos,
            () => ShowCreate(node),
            () => DeleteNode(node)
        );
    }

    void ShowCreate(ProjectNode parent)
    {
        contextMenu.Hide(); // 🔥 ВАЖНО

        createPanel.Show(async (name) =>
        {
            int? parentId = parent?.Id;

            int newId = await db.CreateNodeAsync(currentProjectId, parentId, name);

            var newNode = new ProjectNode
            {
                Id = newId,
                Name = name,
                ParentId = parentId,
                Children = new List<ProjectNode>()
            };

            if (parent == null)
                tree.Add(newNode);
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