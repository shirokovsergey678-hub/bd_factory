using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SFB;
using SFB;
using System.IO;
using System;
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
    async void AddScheme()
    {
        if (currentNode == null)
            return;

        var extensions = new[]
        {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
    };

        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Выбери схему",
            "",
            extensions,
            false
        );

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

        // --------------------------
        // СОХРАНЯЕМ ПРОПОРЦИИ
        // --------------------------

        float width = 800f;

        float ratio = (float)tex.height / tex.width;

        float height = width * ratio;

        RectTransform rt = obj.GetComponent<RectTransform>();

        rt.sizeDelta = new Vector2(width, height);

        var le = obj.GetComponent<LayoutElement>();

        le.preferredWidth = width;
        le.preferredHeight = height;
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
        bool hasSchemes =
            currentNode != null &&
            currentNode.Schemes != null &&
            currentNode.Schemes.Count > 0;

        SetSchemesScrollVisible(hasSchemes);
    }
    void SetSchemesScrollVisible(bool visible)
    {
        if (schemesScrollView != null)
            schemesScrollView.SetActive(visible);
    }
    async void DeleteFile(NodeFile file)
    {
        FileManager.DeleteFile(file.FilePath);

        await db.DeleteNodeFileAsync(file.Id);

        LoadFiles(showVersion);
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
    async void AddFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Выбери файл", "", "", false);

        if (paths.Length == 0) return;

        string savedPath = FileManager.SaveFile(paths[0]);

        var file = new NodeFile
        {
            NodeId = currentNode.Id,
            FileName = System.IO.Path.GetFileName(paths[0]),
            FilePath = savedPath
        };

        await db.AddNodeFileAsync(file);

        LoadFiles(showVersion);
    }
    async void LoadFiles(int version)
    {
        if (currentNode == null) return; // 🔥

        foreach (Transform c in filesContainer)
            Destroy(c.gameObject);

        var files = await db.GetNodeFilesAsync(currentNode.Id);

        if (version != showVersion || currentNode == null)
            return;

        foreach (var f in files)
        {
            var obj = Instantiate(fileItemPrefab, filesContainer);
            var ui = obj.GetComponent<FileItemUI>();

            ui.Setup(f);
            ui.OnDelete += DeleteFile;
        }
    }

    async void Save()
    {
        if (currentNode == null)
            return;

        currentNode.Name = nameInput.text;
        currentNode.Description = descriptionInput.text;

        if (int.TryParse(quantityInput.text, out int q))
            currentNode.Quantity = q;

        await db.UpdateNodeAsync(currentNode);

        hierarchyUI.RefreshVisible();

        Debug.Log("Сохранено");
    }

    public void Hide()
    {
        showVersion++;
        panel.SetActive(false);
        currentNode = null;
        ClearSchemes();

        foreach (Transform c in filesContainer)
            Destroy(c.gameObject);
    }
}
