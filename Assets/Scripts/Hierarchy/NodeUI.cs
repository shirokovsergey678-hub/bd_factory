using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NodeUI : MonoBehaviour
{
    [SerializeField] private Button mainButton;
    [SerializeField] private Button arrowButton;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image arrow;
    [SerializeField] private LayoutElement indent;

    private ProjectNode node;

    public event Action<ProjectNode> OnClick;
    public event Action<ProjectNode> OnToggle;

    public void Setup(ProjectNode node, int level)
    {
        this.node = node;

        label.text = node.Name;
        indent.preferredWidth = level * 20;

        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(() => OnClick?.Invoke(node));

        arrowButton.onClick.RemoveAllListeners();
        arrowButton.onClick.AddListener(() => OnToggle?.Invoke(node));
    }

    public void SetArrow(bool expanded, bool hasChildren)
    {
        // если нет детей — просто делаем прозрачной
        if (!hasChildren)
        {
            arrow.color = new Color(1, 1, 1, 0);
            arrowButton.interactable = false;
            return;
        }

        arrow.color = Color.white;
        arrowButton.interactable = true;

        arrow.rectTransform.localRotation =
            Quaternion.Euler(0, 0, expanded ? -90 : 0);
    }
}