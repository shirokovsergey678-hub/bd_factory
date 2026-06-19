using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InputFieldScrollForwarder : MonoBehaviour, IScrollHandler
{
    [SerializeField] private SmartVerticalScrollbar smartScrollbar;
    [SerializeField] private ScrollRect fallbackScrollRect;

    void Awake()
    {
        ResolveTargets();
    }

    void Reset()
    {
        ResolveTargets();
    }

    public void OnScroll(PointerEventData eventData)
    {
        ResolveTargets();

        if (smartScrollbar != null)
        {
            smartScrollbar.OnScroll(eventData);
            return;
        }

        if (fallbackScrollRect != null)
        {
            ExecuteEvents.Execute(fallbackScrollRect.gameObject, eventData, ExecuteEvents.scrollHandler);
            eventData.Use();
        }
    }

    private void ResolveTargets()
    {
        if (smartScrollbar == null)
            smartScrollbar = GetComponentInParent<SmartVerticalScrollbar>();

        if (fallbackScrollRect == null)
            fallbackScrollRect = GetComponentInParent<ScrollRect>();
    }
}
