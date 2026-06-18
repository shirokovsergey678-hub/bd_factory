using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CatalogMainPanelSceneSetup
{
    private const string ScenePath = "Assets/Scenes/LogReg.unity";
    private const string ResizeCursorTexturePath = "Assets/Materials/cursor_resize_horizontal.png";

    [MenuItem("Tools/Catalog/Setup MainPanel Scene")]
    public static void SetupFromMenu()
    {
        SetupScene(saveScene: true);
    }

    public static void RunBatch()
    {
        SetupScene(saveScene: true);
        AssetDatabase.SaveAssets();
    }

    private static void SetupScene(bool saveScene)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (sceneAsset == null)
        {
            Debug.LogError("Scene not found: " + ScenePath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject mainPanel = FindRequiredObject("MainPanel");
        if (mainPanel == null)
            return;

        RectTransform mainPanelRect = mainPanel.GetComponent<RectTransform>();
        CatalogMainPanelUI catalogUi = mainPanel.GetComponent<CatalogMainPanelUI>();
        if (catalogUi == null)
            catalogUi = mainPanel.AddComponent<CatalogMainPanelUI>();

        RectTransform bodyRoot = EnsurePanel(mainPanelRect, "CatalogBody", null, Color.clear);
        Stretch(bodyRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -72f));
        bodyRoot.SetSiblingIndex(0);

        RectTransform leftPanel = EnsurePanel(bodyRoot, "LeftPanel", typeof(Image), ParseColor("F7F7F7"));
        Stretch(leftPanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(295f, 0f));

        RectTransform rightPanel = EnsurePanel(bodyRoot, "RightPanel", typeof(Image), ParseColor("FCFCFC"));
        Stretch(rightPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(298f, 0f), new Vector2(0f, 0f));

        RectTransform resizeHandle = EnsurePanel(bodyRoot, "HierarchyResizeHandle", typeof(Image), new Color(0f, 0f, 0f, 0.08f));
        SetupResizeHandle(resizeHandle, bodyRoot, leftPanel, rightPanel);

        ScrollRect leftScroll = EnsureScrollArea(leftPanel, "LeftScroll", out RectTransform leftContent, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        ScrollRect rightScroll = EnsureScrollArea(rightPanel, "RightScroll", out RectTransform rightContent, new Vector2(8f, 6f), new Vector2(-8f, -6f));

        SetupVerticalContent(leftContent, 8, new RectOffset(8, 8, 8, 8));
        SetupVerticalContent(rightContent, 10, new RectOffset(4, 4, 8, 8));

        RectTransform modalRoot = EnsurePanel(mainPanelRect, "ModalRoot", typeof(Image), new Color(0f, 0f, 0f, 0.45f));
        Stretch(modalRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        modalRoot.gameObject.SetActive(false);
        modalRoot.SetAsLastSibling();

        AssignReferences(catalogUi, bodyRoot, leftContent, rightContent, modalRoot, leftScroll, rightScroll);

        EditorUtility.SetDirty(mainPanel);
        EditorSceneManager.MarkSceneDirty(scene);

        if (saveScene)
            EditorSceneManager.SaveScene(scene);
    }

    private static GameObject FindRequiredObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
            Debug.LogError("Required object not found: " + objectName);
        return obj;
    }

    private static RectTransform EnsurePanel(RectTransform parent, string name, System.Type imageType, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (imageType != null)
        {
            Image image = go.GetComponent<Image>();
            if (image == null)
                image = go.AddComponent<Image>();
            image.color = color;
        }

        return rect;
    }

    private static ScrollRect EnsureScrollArea(RectTransform parent, string name, out RectTransform content, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform scrollRoot = EnsurePanel(parent, name, null, Color.clear);
        Stretch(scrollRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax);

        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = EnsurePanel(scrollRoot, "Viewport", typeof(Image), new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null)
            mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = EnsurePanel(viewport, "Content", null, Color.clear);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        return scrollRect;
    }

    private static void SetupVerticalContent(RectTransform content, int spacing, RectOffset padding)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = spacing;
        layout.padding = padding;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private static void SetupResizeHandle(RectTransform handle, RectTransform bodyRoot, RectTransform leftPanel, RectTransform rightPanel)
    {
        handle.SetSiblingIndex(1);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(10f, 0f);
        handle.anchoredPosition = new Vector2(296.5f, 0f);

        Image image = handle.GetComponent<Image>();
        image.raycastTarget = true;

        HierarchyPanelResizer resizer = handle.GetComponent<HierarchyPanelResizer>();
        if (resizer == null)
            resizer = handle.gameObject.AddComponent<HierarchyPanelResizer>();

        Texture2D resizeCursorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResizeCursorTexturePath);

        SerializedObject so = new SerializedObject(resizer);
        so.FindProperty("bodyRoot").objectReferenceValue = bodyRoot;
        so.FindProperty("leftPanel").objectReferenceValue = leftPanel;
        so.FindProperty("rightPanel").objectReferenceValue = rightPanel;
        so.FindProperty("handleRect").objectReferenceValue = handle;
        so.FindProperty("resizeCursorTexture").objectReferenceValue = resizeCursorTexture;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignReferences(
        CatalogMainPanelUI catalogUi,
        RectTransform bodyRoot,
        RectTransform leftContent,
        RectTransform rightContent,
        RectTransform modalRoot,
        ScrollRect leftScroll,
        ScrollRect rightScroll)
    {
        SerializedObject so = new SerializedObject(catalogUi);
        so.FindProperty("bodyRoot").objectReferenceValue = bodyRoot;
        so.FindProperty("leftContent").objectReferenceValue = leftContent;
        so.FindProperty("rightContent").objectReferenceValue = rightContent;
        so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        so.FindProperty("leftScroll").objectReferenceValue = leftScroll;
        so.FindProperty("rightScroll").objectReferenceValue = rightScroll;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;
    }

    private static Color ParseColor(string html)
    {
        ColorUtility.TryParseHtmlString("#" + html, out Color color);
        return color;
    }
}
