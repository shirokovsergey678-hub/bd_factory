using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SFB;

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

    private ProjectNode currentNode;
    private DatabaseManager db;

    void Awake()
    {
        db = DatabaseManager.Instance;

        saveButton.onClick.AddListener(Save);
        addFileButton.onClick.AddListener(AddFile);
        panel.SetActive(false);
    }
    async void DeleteFile(NodeFile file)
    {
        FileManager.DeleteFile(file.FilePath);

        await db.DeleteNodeFileAsync(file.Id);

        LoadFiles();
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

        LoadFiles();
    }
    async void LoadFiles()
    {
        foreach (Transform c in filesContainer)
            Destroy(c.gameObject);

        var files = await db.GetNodeFilesAsync(currentNode.Id);

        foreach (var f in files)
        {
            var obj = Instantiate(fileItemPrefab, filesContainer);
            var ui = obj.GetComponent<FileItemUI>();

            ui.Setup(f);

            ui.OnDelete += DeleteFile;
        }
    }

    public void Show(ProjectNode node)
    {
        LoadFiles();
        currentNode = node;

        panel.SetActive(true);

        nameInput.text = node.Name;
        descriptionInput.text = node.Description ?? "";
        quantityInput.text = node.Quantity <= 0 ? "1" : node.Quantity.ToString();
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

        hierarchyUI.LoadHierarchy(currentNode.ProjectId);

        Debug.Log("Сохранено");
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentNode = null;
    }
}