using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HierarchyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content; // Content из ScrollView
    [SerializeField] private GameObject nodePrefab;

    private DatabaseManager db;

    private List<ProjectNode> tree = new();        // настоящее дерево
    private List<ProjectNode> visibleNodes = new(); // что сейчас видно

    void Awake()
    {
        db = DatabaseManager.Instance;
    }

    // вызываешь при открытии проекта
    public async void LoadHierarchy(int projectId)
    {
        Clear();

        var flat = await db.GetNodesAsync(projectId);

        tree = BuildTree(flat);

        BuildVisibleList();
        Draw();
    }

    // --------------------------
    // 1. СТРОИМ ДЕРЕВО
    // --------------------------
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
            {
                lookup[node.ParentId.Value].Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    // --------------------------
    // 2. СТРОИМ ВИДИМЫЙ СПИСОК
    // --------------------------
    void BuildVisibleList()
    {
        visibleNodes.Clear();

        foreach (var root in tree)
            AddNodeRecursive(root, 0);
    }

    void AddNodeRecursive(ProjectNode node, int level)
    {
        node.Level = level;
        visibleNodes.Add(node);

        if (!node.IsExpanded)
            return;

        foreach (var child in node.Children)
            AddNodeRecursive(child, level + 1);
    }

    // --------------------------
    // 3. РИСУЕМ UI
    // --------------------------
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
            ui.OnToggle += OnNodeToggled;
        }
    }
    void OnNodeSelected(ProjectNode node)
    {
        Debug.Log($"Выбран: {node.Name}");

    }
    void OnNodeToggled(ProjectNode node)
    {
        node.IsExpanded = !node.IsExpanded;

        BuildVisibleList();
        Draw();
    }

    // --------------------------
    // 4. КЛИК
    // --------------------------
    void OnNodeClicked(ProjectNode node, NodeUI ui)
    {
        // раскрытие / закрытие
        if (node.Children.Count > 0)
        {
            node.IsExpanded = !node.IsExpanded;

            BuildVisibleList();
            Draw();
        }

        Debug.Log($"Выбран: {node.Name}");
    }

    // --------------------------
    void Clear()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
    }
}