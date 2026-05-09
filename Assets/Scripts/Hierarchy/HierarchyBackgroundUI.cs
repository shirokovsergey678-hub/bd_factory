using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class HierarchyBackgroundUI : MonoBehaviour, IPointerClickHandler
{
    public Action<Vector2> OnRightClickEmpty;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log("ПКМ по пустоте");
            OnRightClickEmpty?.Invoke(Input.mousePosition);
        }
    }
}