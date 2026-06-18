using System;
using System.IO;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeDetailsUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private HierarchyUI hierarchyUI;
    [SerializeField] private Button saveButton;
    [SerializeField] private Transform filesContainer;
    [SerializeField] private GameObject fileItemPrefab;
    [SerializeField] private Button addFileButton;

    [Header("Schemes")]
    [SerializeField] private Transform schemesContainer;
    [SerializeField] private GameObject schemesScrollView;
    [SerializeField] private GameObject schemeItemPrefab;
    [SerializeField] private Button addSchemeButton;

    private ProjectNode currentNode;
    private DatabaseManager db;
    private int showVersion;

    void Awake()
    {
        db = DatabaseManager.Instance;

        if (schemesScrollView == null && schemesContainer != null)
        {
            var scrollRect = schemesContainer.GetComponentInParent<ScrollRect>(true);

            if (scrollRect != null)
                schemesScrollView = scrollRect.gameObject;
        }

        addSchemeButton.onClick.AddListener(AddScheme);
        saveButton.onClick.AddListener(Save);
        addFileButton.onClick.AddListener(AddFile);
        panel.SetActive(false);
    }

    public async void Show(ProjectNode node)
    {
        int version = ++showVersion;

        currentNode = node;
        ClearSchemes();

        var schemes = await db.GetNodeSchemesAsync(node.Id);

        if (version != showVersion || currentNode != node)
            return;

        currentNode.Schemes = schemes;

        foreach (var path in node.Schemes)
        {
            if (File.Exists(path))
                CreateSchemeUI(path);
        }

        panel.SetActive(true);

        nameInput.text = node.Name;
        descriptionInput.text = node.Description ?? "";
        quantityInput.text = node.Quantity.ToString();
        LoadFiles(version);
    }

    public void Hide()
    {
        showVersion++;
        panel.SetActive(false);
        currentNode = null;
        ClearSchemes();

        foreach (Transform child in filesContainer)
            Destroy(child.gameObject);
    }

    async void AddScheme()
    {
        if (currentNode == null)
            return;

        var extensions = new[]
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Выбери схему", "", extensions, false);

        if (paths.Length == 0)
            return;

        string sourcePath = paths[0];
        string folder = Path.Combine(Application.persistentDataPath, "Schemes");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
        string destPath = Path.Combine(folder, fileName);

        File.Copy(sourcePath, destPath, true);

        currentNode.Schemes.Add(destPath);
        await db.AddNodeSchemeAsync(currentNode.Id, destPath);

        CreateSchemeUI(destPath);
        UpdateSchemesScrollVisibility();
    }

    void CreateSchemeUI(string path)
    {
        SetSchemesScrollVisible(true);

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        var obj = Instantiate(schemeItemPrefab, schemesContainer);
        var ui = obj.GetComponent<SchemeItemUI>();

        ui.Setup(sprite, path, currentNode, hierarchyUI);
        ui.OnDelete += DeleteScheme;

        float width = 800f;
        float height = width * tex.height / tex.width;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var layout = obj.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    async void DeleteScheme(SchemeItemUI item)
    {
        currentNode.Schemes.Remove(item.FilePath);

        if (File.Exists(item.FilePath))
            File.Delete(item.FilePath);

        Destroy(item.gameObject);
        await db.DeleteNodeSchemeAsync(currentNode.Id, item.FilePath);
        UpdateSchemesScrollVisibility();
    }

    void ClearSchemes()
    {
        foreach (Transform child in schemesContainer)
            Destroy(child.gameObject);

        SetSchemesScrollVisible(false);
    }

    void UpdateSchemesScrollVisibility()
    {
        bool hasSchemes = currentNode != null &&
                          currentNode.Schemes != null &&
                          currentNode.Schemes.Count > 0;

        SetSchemesScrollVisible(hasSchemes);
    }

    void SetSchemesScrollVisible(bool visible)
    {
        if (schemesScrollView != null)
            schemesScrollView.SetActive(visible);
    }

    async void AddFile()
    {
        if (currentNode == null)
            return;

        var paths = StandaloneFileBrowser.OpenFilePanel("Выбери файл", "", "", false);

        if (paths.Length == 0)
            return;

        string sourcePath = paths[0];

        var file = new NodeFile
        {
            NodeId = currentNode.Id,
            FileName = Path.GetFileName(sourcePath),
            FilePath = sourcePath,
            FileData = File.ReadAllBytes(sourcePath)
        };

        await db.AddNodeFileAsync(file);
        LoadFiles(showVersion);
    }

    async void LoadFiles(int version)
    {
        if (currentNode == null)
            return;

        int nodeId = currentNode.Id;

        foreach (Transform child in filesContainer)
            Destroy(child.gameObject);

        var files = await db.GetNodeFilesAsync(nodeId);

        if (version != showVersion || currentNode == null || currentNode.Id != nodeId)
            return;

        foreach (var file in files)
        {
            var obj = Instantiate(fileItemPrefab, filesContainer);
            var ui = obj.GetComponent<FileItemUI>();

            ui.Setup(file);
            ui.OnDelete += DeleteFile;
        }
    }

    async void DeleteFile(NodeFile file)
    {
        await db.DeleteNodeFileAsync(file.Id);
        LoadFiles(showVersion);
    }

    async void Save()
    {
        if (currentNode == null)
            return;

        currentNode.Name = nameInput.text;
        currentNode.Description = descriptionInput.text;

        if (int.TryParse(quantityInput.text, out int quantity))
            currentNode.Quantity = quantity;

        await db.UpdateNodeAsync(currentNode);
        hierarchyUI.RefreshVisible();
    }
}
