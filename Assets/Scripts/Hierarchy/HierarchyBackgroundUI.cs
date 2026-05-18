using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HierarchyBackgroundUI : MonoBehaviour, IPointerClickHandler
{
    public Action<Vector2> OnRightClickEmpty;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnRightClickEmpty?.Invoke(Input.mousePosition);
    }
}
