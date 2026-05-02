using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ContextMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private Action onCreate;
    private Action onDelete;

    void Update()
    {
        if (!panel.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = panel.GetComponentInParent<Canvas>().worldCamera;

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                panel.GetComponent<RectTransform>(),
                Input.mousePosition,
                cam
            ))
            {
                Hide();
            }
        }
    }

    public void Show(Vector2 screenPos, Action create, Action delete)
    {
        panel.SetActive(true);

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera cam = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            cam,
            out Vector2 localPos
        );

        panel.GetComponent<RectTransform>().localPosition = localPos;

        onCreate = create;
        onDelete = delete;
    }

    public void OnCreateClicked()
    {
        Debug.Log("CREATE CLICK");
        onCreate?.Invoke();
        Hide();
    }

    public void OnDeleteClicked()
    {
        Debug.Log("DELETE CLICK");
        onDelete?.Invoke();
        Hide();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}