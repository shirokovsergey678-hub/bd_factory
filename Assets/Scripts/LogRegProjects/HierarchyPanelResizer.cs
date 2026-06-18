using UnityEngine;
using UnityEngine.EventSystems;

public class HierarchyPanelResizer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
#if UNITY_EDITOR
    private const string ResizeCursorAssetPath = "Assets/Materials/cursor_resize_horizontal.png";
#endif

    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform leftPanel;
    [SerializeField] private RectTransform rightPanel;
    [SerializeField] private RectTransform handleRect;
    [SerializeField] private Texture2D resizeCursorTexture;
    [SerializeField] private Vector2 resizeCursorHotspot = new Vector2(16f, 16f);
    [SerializeField] private float minWidth = 220f;
    [SerializeField] private float maxWidth = 520f;
    [SerializeField] private float panelGap = 3f;

    private Canvas rootCanvas;
    private bool pointerInside;

    void Awake()
    {
        ResolveReferences();
        EnsureCursorTextureReference();
        rootCanvas = GetComponentInParent<Canvas>();
        ApplyCurrentWidth();
    }

    void OnEnable()
    {
        ResolveReferences();
        EnsureCursorTextureReference();
        ApplyCurrentWidth();
    }

    void OnValidate()
    {
        ResolveReferences();
        EnsureCursorTextureReference();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();
        ApplyResizeCursor();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (bodyRoot == null || leftPanel == null || rightPanel == null)
            return;

        Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? eventData.pressEventCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bodyRoot, eventData.position, eventCamera, out Vector2 localPoint))
            return;

        float pivotOffset = bodyRoot.rect.width * bodyRoot.pivot.x;
        float desiredWidth = Mathf.Clamp(localPoint.x + pivotOffset, minWidth, GetEffectiveMaxWidth());
        ApplyWidth(desiredWidth);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pointerInside)
            ApplyResizeCursor();
        else
            ResetCursor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyResizeCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ResetCursor();
    }

    void ResolveReferences()
    {
        if (bodyRoot == null)
            bodyRoot = transform.parent as RectTransform;

        if (bodyRoot == null)
            return;

        if (leftPanel == null)
            leftPanel = bodyRoot.Find("LeftPanel") as RectTransform;

        if (rightPanel == null)
            rightPanel = bodyRoot.Find("RightPanel") as RectTransform;

        if (handleRect == null)
            handleRect = transform as RectTransform;
    }

    void ApplyCurrentWidth()
    {
        if (leftPanel == null)
            return;

        ApplyWidth(leftPanel.offsetMax.x);
    }

    void ApplyWidth(float width)
    {
        if (leftPanel == null || rightPanel == null || handleRect == null)
            return;

        leftPanel.offsetMin = new Vector2(0f, leftPanel.offsetMin.y);
        leftPanel.offsetMax = new Vector2(width, leftPanel.offsetMax.y);

        rightPanel.offsetMin = new Vector2(width + panelGap, rightPanel.offsetMin.y);

        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = new Vector2(width + panelGap * 0.5f, 0f);
    }

    float GetEffectiveMaxWidth()
    {
        if (bodyRoot == null)
            return maxWidth;

        float bodyWidth = bodyRoot.rect.width;
        float hardMax = Mathf.Max(minWidth, bodyWidth - 360f);
        return Mathf.Min(maxWidth, hardMax);
    }

    void ApplyResizeCursor()
    {
        Cursor.SetCursor(GetResizeCursorTexture(), resizeCursorHotspot, CursorMode.Auto);
    }

    void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    Texture2D GetResizeCursorTexture()
    {
        EnsureCursorTextureReference();

        if (resizeCursorTexture != null)
            return resizeCursorTexture;

        resizeCursorTexture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
        resizeCursorTexture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color dark = new Color(0.12f, 0.12f, 0.12f, 1f);
        Color light = Color.white;

        Color[] pixels = new Color[24 * 24];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        DrawHorizontalLine(pixels, 24, 6, 17, 12, dark);
        DrawHorizontalLine(pixels, 24, 7, 16, 11, light);
        DrawHorizontalLine(pixels, 24, 7, 16, 13, light);

        DrawArrow(pixels, 24, 4, 12, -1, dark, light);
        DrawArrow(pixels, 24, 19, 12, 1, dark, light);

        resizeCursorTexture.SetPixels(pixels);
        resizeCursorTexture.Apply();
        return resizeCursorTexture;
    }

    void DrawHorizontalLine(Color[] pixels, int width, int fromX, int toX, int y, Color color)
    {
        for (int x = fromX; x <= toX; x++)
            pixels[y * width + x] = color;
    }

    void DrawArrow(Color[] pixels, int width, int centerX, int centerY, int direction, Color outline, Color fill)
    {
        for (int offset = 0; offset < 4; offset++)
        {
            int x = centerX + offset * direction;
            Plot(pixels, width, x, centerY, outline);
            Plot(pixels, width, x, centerY - offset, outline);
            Plot(pixels, width, x, centerY + offset, outline);
        }

        for (int offset = 1; offset < 3; offset++)
        {
            int x = centerX + offset * direction;
            Plot(pixels, width, x, centerY, fill);
            Plot(pixels, width, x, centerY - offset + 1, fill);
            Plot(pixels, width, x, centerY + offset - 1, fill);
        }
    }

    void Plot(Color[] pixels, int width, int x, int y, Color color)
    {
        if (x < 0 || x >= width || y < 0 || y >= width)
            return;

        pixels[y * width + x] = color;
    }

    void EnsureCursorTextureReference()
    {
#if UNITY_EDITOR
        if (resizeCursorTexture != null)
            return;

        Texture2D assetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ResizeCursorAssetPath);
        if (assetTexture == null)
            return;

        resizeCursorTexture = assetTexture;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
