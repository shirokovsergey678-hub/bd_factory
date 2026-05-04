using UnityEngine;
using TMPro;
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

    private ProjectNode currentNode;
    private DatabaseManager db;

    void Awake()
    {
        db = DatabaseManager.Instance;

        saveButton.onClick.AddListener(Save);

        panel.SetActive(false);
    }

    public void Show(ProjectNode node)
    {
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