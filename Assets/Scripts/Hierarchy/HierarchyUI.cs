using UnityEngine;
using System.Collections.Generic;

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

    private List<ProjectNode> tree = new();
    private List<ProjectNode> visibleNodes = new();

    private int currentProjectId;

    // -----------------------------
    // ВЫБРАННАЯ НОДА
    // -----------------------------
    private ProjectNode selectedNode;

    // -----------------------------
    // РЕЖИМ ПЕРЕНОСА
    // -----------------------------
    private bool waitingForParent = false;

    // =====================================================
    // INIT
    // =====================================================

    void Awake()
    {
        db = DatabaseManager.Instance;

        background.OnRightClickEmpty += OnEmptyRightClick;
    }

    // =====================================================
    // LOAD
    // =====================================================

    public async void LoadHierarchy(int projectId)
    {
        currentProjectId = projectId;

        Clear();

        var flat = await db.GetNodesAsync(projectId);

        tree = BuildTree(flat);

        BuildVisibleList();
        Draw();
    }

    // =====================================================
    // TREE
    // =====================================================

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
            if (node.ParentId.HasValue &&
                lookup.ContainsKey(node.ParentId.Value))
            {
                lookup[node.ParentId.Value]
                    .Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
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

    // =====================================================
    // DRAW
    // =====================================================

    void Draw()
    {
        Clear();

        foreach (var node in visibleNodes)
        {
            var obj = Instantiate(nodePrefab, content);

            var ui = obj.GetComponent<NodeUI>();

            ui.Setup(node, node.Level);

            ui.SetArrow(
                node.IsExpanded,
                node.Children.Count > 0
            );

            ui.OnClick += OnNodeSelected;
            ui.OnToggle += OnToggle;
            ui.OnRightClick += OnRightClick;
        }
    }

    // =====================================================
    // SELECT
    // =====================================================

    async void OnNodeSelected(ProjectNode node)
    {
        // -------------------------------------------------
        // РЕЖИМ ПЕРЕНОСА
        // -------------------------------------------------
        if (waitingForParent)
        {
            waitingForParent = false;

            if (selectedNode == null)
                return;

            // нельзя в самого себя
            if (selectedNode.Id == node.Id)
            {
                Debug.Log("Нельзя вложить объект в самого себя");
                return;
            }

            // нельзя в потомка
            if (IsChildOf(selectedNode, node))
            {
                Debug.Log("Нельзя вложить родителя в потомка");
                return;
            }

            selectedNode.ParentId = node.Id;

            await db.UpdateNodeParentAsync(
                selectedNode.Id,
                node.Id
            );

            LoadHierarchy(currentProjectId);

            Debug.Log(
                $"{selectedNode.Name} теперь внутри {node.Name}"
            );

            return;
        }

        // -------------------------------------------------
        // ОБЫЧНЫЙ ВЫБОР
        // -------------------------------------------------
        selectedNode = node;

        nodeDetailsUI.Show(node);

        Debug.Log($"Выбрано: {node.Name}");
    }

    // =====================================================
    // CHECK CHILD
    // =====================================================

    bool IsChildOf(ProjectNode parent, ProjectNode possibleChild)
    {
        foreach (var child in parent.Children)
        {
            if (child.Id == possibleChild.Id)
                return true;

            if (IsChildOf(child, possibleChild))
                return true;
        }

        return false;
    }

    // =====================================================
    // TOGGLE
    // =====================================================

    void OnToggle(ProjectNode node)
    {
        node.IsExpanded = !node.IsExpanded;

        BuildVisibleList();
        Draw();
    }

    // =====================================================
    // RIGHT CLICK NODE
    // =====================================================

    void OnRightClick(ProjectNode node, Vector2 pos)
    {
        // 🔥 ПКМ сразу выбирает объект
        selectedNode = node;

        nodeDetailsUI.Show(node);

        contextMenu.Show(
            pos,

            // СОЗДАТЬ
            () => ShowCreate(node),

            // УДАЛИТЬ
            () => DeleteNode(node),

            // СДЕЛАТЬ ДОЧЕРНИМ
            () =>
            {
                waitingForParent = true;

                Debug.Log(
                    $"Выбери нового родителя для {selectedNode.Name}"
                );
            }
        );
    }

    // =====================================================
    // RIGHT CLICK EMPTY
    // =====================================================

    async void OnEmptyRightClick(Vector2 pos)
    {
        // -------------------------------------------------
        // РЕЖИМ ПЕРЕНОСА
        // -------------------------------------------------
        if (waitingForParent)
        {
            waitingForParent = false;

            if (selectedNode != null)
            {
                selectedNode.ParentId = null;

                await db.UpdateNodeParentAsync(
                    selectedNode.Id,
                    null
                );

                LoadHierarchy(currentProjectId);

                Debug.Log(
                    $"{selectedNode.Name} теперь корневой объект"
                );
            }

            return;
        }

        // -------------------------------------------------
        // ОБЫЧНОЕ МЕНЮ
        // -------------------------------------------------
        contextMenu.Show(
            pos,
            () => ShowCreate(null),
            null,
            null
        );
    }

    // =====================================================
    // CREATE
    // =====================================================

    void ShowCreate(ProjectNode parent)
    {
        contextMenu.Hide();

        createPanel.Show(async (name) =>
        {
            int? parentId = parent?.Id;

            int newId = await db.CreateNodeAsync(
                currentProjectId,
                parentId,
                name
            );

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

    // =====================================================
    // DELETE
    // =====================================================

    async void DeleteNode(ProjectNode node)
    {
        await db.DeleteNodeAsync(node.Id);

        RemoveNode(tree, node);

        BuildVisibleList();
        Draw();

        Debug.Log($"Удалено: {node.Name}");
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

    // =====================================================
    // CLEAR
    // =====================================================

    void Clear()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
    }
}