using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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

        RectTransform bodyRoot = EnsurePanel(mainPanelRect, "CatalogBody", false, Color.clear);
        Stretch(bodyRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -72f));
        bodyRoot.SetSiblingIndex(0);

        RectTransform leftPanel = EnsurePanel(bodyRoot, "LeftPanel", true, ParseColor("F7F7F7"));
        Stretch(leftPanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(295f, 0f));

        RectTransform rightPanel = EnsurePanel(bodyRoot, "RightPanel", true, ParseColor("FCFCFC"));
        Stretch(rightPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(298f, 0f), new Vector2(0f, 0f));

        RectTransform resizeHandle = EnsurePanel(bodyRoot, "HierarchyResizeHandle", true, new Color(0f, 0f, 0f, 0.08f));
        SetupResizeHandle(resizeHandle, bodyRoot, leftPanel, rightPanel);

        ScrollRect leftScroll = EnsureScrollArea(leftPanel, "LeftScroll", out RectTransform leftContent, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        ScrollRect rightScroll = EnsureScrollArea(rightPanel, "RightScroll", out RectTransform rightContent, new Vector2(8f, 6f), new Vector2(-8f, -6f));

        SetupVerticalContent(leftContent, 8, new RectOffset(8, 8, 8, 8));
        SetupVerticalContent(rightContent, 10, new RectOffset(4, 4, 8, 8));

        Button addRootOriginal = EnsureLeftAddRootOriginal(leftContent);
        RectTransform leftRootOriginal = EnsureLeftRootOriginal(leftContent);
        RectTransform rightSectionOriginal = EnsureRightSectionOriginal(rightContent);

        RectTransform modalRoot = EnsurePanel(mainPanelRect, "ModalRoot", true, new Color(0f, 0f, 0f, 0.45f));
        Stretch(modalRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        modalRoot.gameObject.SetActive(false);
        modalRoot.SetAsLastSibling();

        AssignReferences(catalogUi, bodyRoot, leftContent, rightContent, modalRoot, leftScroll, rightScroll, addRootOriginal, leftRootOriginal, rightSectionOriginal);

        EditorUtility.SetDirty(mainPanel);
        EditorSceneManager.MarkSceneDirty(scene);

        if (saveScene)
            EditorSceneManager.SaveScene(scene);
    }

    private static Button EnsureLeftAddRootOriginal(RectTransform parent)
    {
        Transform existing = parent.Find("Button_Р”РѕР±Р°РІРёС‚СЊ СЂР°Р·РґРµР»");
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            NormalizeButtonLabel(existingButton, false);
            return existingButton;
        }

        Button button = CreateButton(parent, "Button_Р”РѕР±Р°РІРёС‚СЊ СЂР°Р·РґРµР»", "Р”РѕР±Р°РІРёС‚СЊ СЂР°Р·РґРµР»", Color.white, Color.black, 38f, FontStyles.Normal);
        AddOutline(button.GetComponent<RectTransform>(), Color.black);
        return button;
    }

    private static RectTransform EnsureLeftRootOriginal(RectTransform parent)
    {
        Transform existing = parent.Find("Root_12");
        if (existing != null)
        {
            RectTransform existingRoot = existing as RectTransform;
            NormalizeLeftRootOriginal(existingRoot);
            return existingRoot;
        }

        RectTransform card = CreatePanel(parent, "Root_12", Color.white);
        AddOutline(card, new Color(0.88f, 0.88f, 0.88f, 1f));
        VerticalLayoutGroup cardLayout = EnsureVerticalLayout(card, 6, new RectOffset(8, 8, 8, 8));
        cardLayout.childForceExpandWidth = true;

        RectTransform header = CreatePanel(card, "Header", Color.clear);
        EnsureLayoutElement(header).preferredHeight = 30f;
        HorizontalLayoutGroup headerLayout = EnsureHorizontalLayout(header, 4, new RectOffset(0, 0, 0, 0));
        headerLayout.childForceExpandWidth = false;

        CreateButton(header, "Button_Expand", "v", new Color(0.82f, 0.82f, 0.82f, 1f), Color.black, 22f, FontStyles.Normal, 22f);
        CreateButton(header, "Button_Name", "Р Р°Р·РґРµР»", Color.clear, Color.black, 26f, FontStyles.Underline, -1f, TextAlignmentOptions.MidlineLeft);
        CreateButton(header, "Button_Delete", "x", new Color(0.55f, 0.17f, 0.17f, 1f), Color.white, 22f, FontStyles.Normal, 22f);

        RectTransform rows = CreatePanel(card, "Rows", Color.clear);
        EnsureVerticalLayout(rows, 6, new RectOffset(0, 0, 0, 0));
        ContentSizeFitter rowsFitter = rows.gameObject.AddComponent<ContentSizeFitter>();
        rowsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform childRow = CreatePanel(rows, "ChildRow", Color.clear);
        EnsureLayoutElement(childRow).preferredHeight = 28f;
        HorizontalLayoutGroup childLayout = EnsureHorizontalLayout(childRow, 4, new RectOffset(24, 0, 0, 0));
        childLayout.childForceExpandWidth = false;
        CreateButton(childRow, "Button_Name", "РџРѕРґСЂР°Р·РґРµР»", Color.clear, Color.black, 26f, FontStyles.Underline, -1f, TextAlignmentOptions.MidlineLeft);
        CreateButton(childRow, "Button_Delete", "x", new Color(0.55f, 0.17f, 0.17f, 1f), Color.white, 22f, FontStyles.Normal, 22f);

        RectTransform addChildRow = CreatePanel(rows, "AddChildRow", Color.clear);
        EnsureLayoutElement(addChildRow).preferredHeight = 24f;
        HorizontalLayoutGroup addChildLayout = EnsureHorizontalLayout(addChildRow, 4, new RectOffset(24, 0, 0, 0));
        addChildLayout.childForceExpandWidth = false;
        CreateButton(addChildRow, "Button_Add", "+", new Color(0.82f, 0.82f, 0.82f, 1f), Color.black, 22f, FontStyles.Normal, 22f);

        return card;
    }

    private static RectTransform EnsureRightSectionOriginal(RectTransform parent)
    {
        Transform existing = parent.Find("Section_15");
        if (existing != null)
        {
            RectTransform existingSection = existing as RectTransform;
            NormalizeRightSectionOriginal(existingSection);
            return existingSection;
        }

        RectTransform section = CreatePanel(parent, "Section_15", Color.white);
        VerticalLayoutGroup sectionLayout = EnsureVerticalLayout(section, 8, new RectOffset(0, 0, 0, 8));
        sectionLayout.childForceExpandWidth = true;

        RectTransform titleBar = CreatePanel(section, "TitleBar", ParseColor("0A78C2"));
        EnsureLayoutElement(titleBar).preferredHeight = 28f;
        TMP_Text titleText = CreateText(titleBar, "TitleText", "РќР°Р·РІР°РЅРёРµ РїРѕРґСЂР°Р·РґРµР»Р°", 17, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
        titleText.margin = new Vector4(12f, 0f, 12f, 0f);

        RectTransform products = CreatePanel(section, "Products", Color.clear);
        EnsureVerticalLayout(products, 8, new RectOffset(0, 0, 0, 0));
        ContentSizeFitter productsFitter = products.gameObject.AddComponent<ContentSizeFitter>();
        productsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        productsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        EnsureProductOriginal(products);

        CreateButton(section, "Button_Р”РѕР±Р°РІРёС‚СЊ РїСЂРѕРґСѓРєС‚", "Р”РѕР±Р°РІРёС‚СЊ РїСЂРѕРґСѓРєС‚", Color.white, Color.black, 38f, FontStyles.Normal);
        AddOutline(section.Find("Button_Р”РѕР±Р°РІРёС‚СЊ РїСЂРѕРґСѓРєС‚") as RectTransform, Color.black);

        return section;
    }

    private static RectTransform EnsureProductOriginal(RectTransform parent)
    {
        Transform existing = parent.Find("Product_17");
        if (existing != null)
        {
            RectTransform existingProduct = existing as RectTransform;
            NormalizeProductOriginal(existingProduct);
            return existingProduct;
        }

        RectTransform card = CreatePanel(parent, "Product_17", Color.white);
        AddOutline(card, new Color(0.85f, 0.85f, 0.85f, 1f));
        VerticalLayoutGroup cardLayout = EnsureVerticalLayout(card, 8, new RectOffset(8, 8, 8, 8));
        cardLayout.childForceExpandWidth = true;

        RectTransform topRow = CreatePanel(card, "TopRow", Color.clear);
        HorizontalLayoutGroup topLayout = EnsureHorizontalLayout(topRow, 12, new RectOffset(0, 0, 0, 0));
        topLayout.childForceExpandWidth = false;
        ContentSizeFitter topRowFitter = topRow.GetComponent<ContentSizeFitter>();
        if (topRowFitter == null)
            topRowFitter = topRow.gameObject.AddComponent<ContentSizeFitter>();
        topRowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        topRowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button imageAreaButton = CreateButton(topRow, "ImageArea", TextAddImageMultiline(), new Color(0.55f, 0.55f, 0.55f, 1f), Color.white, 160f, FontStyles.Normal, 180f);
        RectTransform imageArea = imageAreaButton.GetComponent<RectTransform>();
        LayoutElement imageLayout = EnsureLayoutElement(imageArea);
        imageLayout.minWidth = 180f;
        imageLayout.preferredWidth = 180f;
        imageLayout.flexibleWidth = 0f;
        imageLayout.minHeight = 160f;
        imageLayout.preferredHeight = 160f;
        NormalizeButtonLabel(imageAreaButton, false, Vector4.zero, 14f, true);
        Image preview = CreatePanel(imageArea, "Preview", Color.white).GetComponent<Image>();
        Stretch(preview.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f));
        preview.preserveAspect = true;
        preview.gameObject.SetActive(false);

        RectTransform infoArea = CreatePanel(topRow, "InfoArea", Color.clear);
        EnsureLayoutElement(infoArea).flexibleWidth = 1f;
        VerticalLayoutGroup infoLayout = EnsureVerticalLayout(infoArea, 8, new RectOffset(0, 0, 0, 0));
        infoLayout.childForceExpandWidth = true;
        ContentSizeFitter infoAreaFitter = infoArea.GetComponent<ContentSizeFitter>();
        if (infoAreaFitter == null)
            infoAreaFitter = infoArea.gameObject.AddComponent<ContentSizeFitter>();
        infoAreaFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        infoAreaFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateInputField(infoArea, "NameInput", "РќР°Р·РІР°РЅРёРµ РїСЂРѕРґСѓРєС‚Р°", 42f, true);
        CreateInputField(infoArea, "DescriptionInput", "РћРїРёСЃР°РЅРёРµ РїСЂРѕРґСѓРєС‚Р°", 110f, true);
        TMP_Text nameText = CreateDisplayText(infoArea, "NameText", "РќР°Р·РІР°РЅРёРµ РїСЂРѕРґСѓРєС‚Р°", 18, ParseColor("0A78C2"), FontStyles.Bold, 42f);
        TMP_Text descriptionText = CreateDisplayText(infoArea, "DescriptionText", "РћРїРёСЃР°РЅРёРµ РїСЂРѕРґСѓРєС‚Р°", 15, Color.black, FontStyles.Normal, 110f);
        nameText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);

        RectTransform filesSection = CreatePanel(card, "FilesSection", ParseColor("222222"));
        VerticalLayoutGroup filesLayout = EnsureVerticalLayout(filesSection, 6, new RectOffset(6, 6, 6, 6));
        filesLayout.childForceExpandWidth = true;

        RectTransform filesHeader = CreatePanel(filesSection, "FilesHeader", Color.clear);
        EnsureLayoutElement(filesHeader).preferredHeight = 34f;
        HorizontalLayoutGroup filesHeaderLayout = EnsureHorizontalLayout(filesHeader, 8, new RectOffset(0, 0, 0, 0));
        filesHeaderLayout.childForceExpandWidth = false;

        RectTransform headerLabel = CreatePanel(filesHeader, "HeaderLabel", Color.clear);
        LayoutElement labelLayout = EnsureLayoutElement(headerLabel);
        labelLayout.flexibleWidth = 1f;
        labelLayout.preferredHeight = 30f;
        TMP_Text headerText = CreateText(headerLabel, "Text", "РўРµС…РЅРёС‡РµСЃРєРѕРµ РѕРїРёСЃР°РЅРёРµ", 14, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Normal);
        headerText.margin = new Vector4(4f, 0f, 4f, 0f);

        CreateButton(filesHeader, "Button_Р”РѕР±Р°РІРёС‚СЊ С„Р°Р№Р»", "Р”РѕР±Р°РІРёС‚СЊ С„Р°Р№Р»", ParseColor("0A78C2"), Color.white, 30f, FontStyles.Normal, 130f);

        RectTransform fileList = CreatePanel(filesSection, "FileList", Color.clear);
        EnsureVerticalLayout(fileList, 4, new RectOffset(0, 0, 0, 0));
        ContentSizeFitter fileListFitter = fileList.gameObject.AddComponent<ContentSizeFitter>();
        fileListFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fileListFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        EnsureFileRowOriginal(fileList);
        CreateDisplayText(filesSection, "EmptyFilesLabel", "Р¤Р°Р№Р»С‹ РЅРµ РґРѕР±Р°РІР»РµРЅС‹", 13, new Color(0.85f, 0.85f, 0.85f, 1f), FontStyles.Normal, 18f);

        RectTransform actionsRow = CreatePanel(card, "ActionsRow", Color.clear);
        EnsureLayoutElement(actionsRow).preferredHeight = 42f;
        HorizontalLayoutGroup actionsLayout = EnsureHorizontalLayout(actionsRow, 2, new RectOffset(0, 0, 0, 0));
        actionsLayout.childForceExpandWidth = true;

        CreateButton(actionsRow, "Button_РЎРѕС…СЂР°РЅРёС‚СЊ РёР·РјРµРЅРµРЅРёСЏ", "РЎРѕС…СЂР°РЅРёС‚СЊ РёР·РјРµРЅРµРЅРёСЏ", ParseColor("0A78C2"), Color.white, 42f, FontStyles.Normal);
        CreateButton(actionsRow, "Button_РЈРґР°Р»РёС‚СЊ РїСЂРѕРґСѓРєС‚", "РЈРґР°Р»РёС‚СЊ РїСЂРѕРґСѓРєС‚", new Color(0.15f, 0.08f, 0.08f, 1f), new Color(1f, 0.65f, 0.65f), 42f, FontStyles.Normal);

        return card;
    }

    private static RectTransform EnsureFileRowOriginal(RectTransform parent)
    {
        Transform existing = parent.Find("FileRow");
        if (existing != null)
            return existing as RectTransform;

        RectTransform row = CreatePanel(parent, "FileRow", new Color(0.12f, 0.12f, 0.12f, 1f));
        HorizontalLayoutGroup layout = EnsureHorizontalLayout(row, 6, new RectOffset(8, 8, 6, 6));
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;

        RectTransform fileName = CreatePanel(row, "FileName", Color.clear);
        LayoutElement fileNameLayout = EnsureLayoutElement(fileName);
        fileNameLayout.preferredWidth = 260f;
        CreateText(fileName, "Text", "Р”РѕРєСѓРјРµРЅС‚.pdf", 13, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Normal);

        CreateButton(row, "Button_РџСѓС‚СЊ РІ РїСЂРѕРІРѕРґРЅРёРєРµ", "РџСѓС‚СЊ РІ РїСЂРѕРІРѕРґРЅРёРєРµ", new Color(0.12f, 0.12f, 0.12f, 1f), new Color(0.96f, 0.89f, 0.55f), 28f, FontStyles.Normal, 130f);
        CreateButton(row, "Button_РЈРґР°Р»РёС‚СЊ", "РЈРґР°Р»РёС‚СЊ", new Color(0.15f, 0.08f, 0.08f, 1f), new Color(1f, 0.65f, 0.65f), 28f, FontStyles.Normal, 82f);
        return row;
    }

    private static void NormalizeLeftRootOriginal(RectTransform root)
    {
        NormalizeButtonLabel(root.Find("Header/Button_Expand")?.GetComponent<Button>(), true);
        NormalizeButtonLabel(root.Find("Header/Button_Name")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(root.Find("Header/Button_Delete")?.GetComponent<Button>(), true);
        NormalizeButtonLabel(root.Find("Rows/ChildRow/Button_Name")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(root.Find("Rows/ChildRow/Button_Delete")?.GetComponent<Button>(), true);
        NormalizeButtonLabel(root.Find("Rows/AddChildRow/Button_Add")?.GetComponent<Button>(), true);
    }

    private static void NormalizeRightSectionOriginal(RectTransform section)
    {
        NormalizeProductOriginal(section.Find("Products/Product_17") as RectTransform);
        NormalizeButtonLabel(section.Find("Button_Р”РѕР±Р°РІРёС‚СЊ РїСЂРѕРґСѓРєС‚")?.GetComponent<Button>(), false);
    }

    private static void NormalizeProductOriginal(RectTransform product)
    {
        if (product == null)
            return;

        Button imageButton = product.Find("TopRow/ImageArea")?.GetComponent<Button>();
        NormalizeButtonLabel(imageButton, false, Vector4.zero, 14f, true);
        RemoveDuplicateLabelChildren(product.Find("TopRow/ImageArea"));
        RectTransform topRow = product.Find("TopRow") as RectTransform;
        if (topRow != null)
        {
            ContentSizeFitter topRowFitter = topRow.GetComponent<ContentSizeFitter>();
            if (topRowFitter == null)
                topRowFitter = topRow.gameObject.AddComponent<ContentSizeFitter>();
            topRowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            topRowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        RectTransform infoArea = product.Find("TopRow/InfoArea") as RectTransform;
        if (infoArea != null)
        {
            ContentSizeFitter infoAreaFitter = infoArea.GetComponent<ContentSizeFitter>();
            if (infoAreaFitter == null)
                infoAreaFitter = infoArea.gameObject.AddComponent<ContentSizeFitter>();
            infoAreaFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            infoAreaFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        if (imageButton != null)
        {
            RectTransform imageArea = imageButton.GetComponent<RectTransform>();
            LayoutElement imageLayout = EnsureLayoutElement(imageArea);
            imageLayout.minWidth = 180f;
            imageLayout.preferredWidth = 180f;
            imageLayout.flexibleWidth = 0f;
            imageLayout.minHeight = 160f;
            imageLayout.preferredHeight = 160f;
        }
        NormalizeExistingInputField(product.Find("TopRow/InfoArea/NameInput") as RectTransform, true);
        NormalizeExistingInputField(product.Find("TopRow/InfoArea/DescriptionInput") as RectTransform, true);

        Transform nameTextRoot = product.Find("TopRow/InfoArea/NameText");
        Transform descriptionTextRoot = product.Find("TopRow/InfoArea/DescriptionText");
        if (nameTextRoot != null)
        {
            nameTextRoot.gameObject.SetActive(false);
            Transform nameTextChild = nameTextRoot.Find("Text");
            if (nameTextChild != null)
                nameTextChild.gameObject.SetActive(true);
        }
        if (descriptionTextRoot != null)
        {
            descriptionTextRoot.gameObject.SetActive(false);
            Transform descriptionTextChild = descriptionTextRoot.Find("Text");
            if (descriptionTextChild != null)
                descriptionTextChild.gameObject.SetActive(true);
        }
        NormalizeButtonLabel(product.Find("FilesSection/FilesHeader/Button_Р”РѕР±Р°РІРёС‚СЊ С„Р°Р№Р»")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(product.Find("ActionsRow/Button_РЎРѕС…СЂР°РЅРёС‚СЊ РёР·РјРµРЅРµРЅРёСЏ")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(product.Find("ActionsRow/Button_РЈРґР°Р»РёС‚СЊ РїСЂРѕРґСѓРєС‚")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(product.Find("FilesSection/FileList/FileRow/Button_РџСѓС‚СЊ РІ РїСЂРѕРІРѕРґРЅРёРєРµ")?.GetComponent<Button>(), false);
        NormalizeButtonLabel(product.Find("FilesSection/FileList/FileRow/Button_РЈРґР°Р»РёС‚СЊ")?.GetComponent<Button>(), false);
    }

    private static void NormalizeExistingInputField(RectTransform inputRoot, bool multiLine)
    {
        if (inputRoot == null)
            return;

        TMP_InputField input = inputRoot.GetComponent<TMP_InputField>();
        if (input == null)
            return;

        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.16f, 0.47f, 0.78f, 0.45f);
        input.caretWidth = 2;
        input.scrollSensitivity = 20f;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.richText = false;

        if (input.textViewport == null)
            input.textViewport = inputRoot.Find("TextArea") as RectTransform;

        if (input.textViewport != null)
        {
            float rightPadding = multiLine ? 24f : 8f;
            Stretch(input.textViewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 5f), new Vector2(-rightPadding, -5f));

            if (input.textViewport.GetComponent<RectMask2D>() == null)
                input.textViewport.gameObject.AddComponent<RectMask2D>();
        }

        if (input.textComponent != null)
        {
            Stretch(input.textComponent.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            input.textComponent.rectTransform.localScale = Vector3.one;
            input.textComponent.enableWordWrapping = multiLine;
            input.textComponent.overflowMode = TextOverflowModes.Masking;
            input.textComponent.raycastTarget = false;
            input.textComponent.margin = Vector4.zero;
        }

        if (input.placeholder is TMP_Text placeholderText)
        {
            Stretch(placeholderText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            placeholderText.rectTransform.localScale = Vector3.one;
            placeholderText.enableWordWrapping = multiLine;
            placeholderText.overflowMode = TextOverflowModes.Masking;
            placeholderText.raycastTarget = false;
            placeholderText.margin = Vector4.zero;
        }

        AttachInputScrollbar(inputRoot, input, multiLine);
    }

    private static void RemoveDuplicateLabelChildren(Transform parent)
    {
        if (parent == null)
            return;

        Transform primary = null;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name != "Label")
                continue;

            if (primary == null)
            {
                primary = child;
                continue;
            }

            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void NormalizeButtonLabel(Button button, bool compact, Vector4? marginOverride = null, float? fontSize = null, bool allowWrap = false)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        text.margin = marginOverride ?? (compact ? new Vector4(0f, 0f, 0f, 0f) : new Vector4(8f, 2f, 8f, 2f));
        if (fontSize.HasValue)
            text.fontSize = fontSize.Value;

        text.enableWordWrapping = allowWrap;
        text.overflowMode = allowWrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
    }

    private static string TextAddImageMultiline()
    {
        return "Р”РѕР±Р°РІРёС‚СЊ\nРёР·РѕР±СЂР°Р¶РµРЅРёРµ\nРїСЂРѕРґСѓРєС‚Р°";
    }

    private static TMP_Text CreateDisplayText(RectTransform parent, string name, string textValue, float fontSize, Color color, FontStyles style, float minHeight)
    {
        RectTransform root = CreatePanel(parent, name, Color.clear);
        LayoutElement layout = EnsureLayoutElement(root);
        layout.minHeight = minHeight;
        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = root.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text text = CreateText(root, "Text", textValue, fontSize, TextAlignmentOptions.TopLeft, color, style);
        text.margin = new Vector4(0f, 2f, 0f, 0f);
        return text;
    }

    private static TMP_InputField CreateInputField(RectTransform parent, string name, string placeholder, float height, bool multiLine)
    {
        RectTransform root = CreatePanel(parent, name, new Color(0.42f, 0.42f, 0.42f, 1f));
        EnsureLayoutElement(root).preferredHeight = height;

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.16f, 0.47f, 0.78f, 0.45f);
        input.caretWidth = 2;
        input.scrollSensitivity = 20f;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.richText = false;

        RectTransform viewport = CreatePanel(root, "TextArea", Color.clear);
        float rightPadding = multiLine ? 24f : 8f;
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 5f), new Vector2(-rightPadding, -5f));
        viewport.gameObject.AddComponent<RectMask2D>();
        input.textViewport = viewport;

        TextMeshProUGUI text = CreateText(viewport, "Text", string.Empty, 16, multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Normal) as TextMeshProUGUI;
        Stretch(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        text.enableWordWrapping = multiLine;
        text.overflowMode = TextOverflowModes.Masking;
        text.raycastTarget = false;
        input.textComponent = text;

        TextMeshProUGUI placeholderText = CreateText(viewport, "Placeholder", placeholder, 16, multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft, new Color(1f, 1f, 1f, 0.7f), FontStyles.Normal) as TextMeshProUGUI;
        Stretch(placeholderText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        placeholderText.enableWordWrapping = multiLine;
        placeholderText.overflowMode = TextOverflowModes.Masking;
        placeholderText.raycastTarget = false;
        input.placeholder = placeholderText;

        AttachInputScrollbar(root, input, multiLine);

        return input;
    }

    private static void AttachInputScrollbar(RectTransform inputRoot, TMP_InputField input, bool enabled)
    {
        RectTransform scrollbarRoot = inputRoot.Find("Scrollbar") as RectTransform;
        if (scrollbarRoot == null)
            scrollbarRoot = CreatePanel(inputRoot, "Scrollbar", Color.clear);

        Stretch(scrollbarRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-14f, 4f), new Vector2(-4f, -4f));
        scrollbarRoot.pivot = new Vector2(1f, 0.5f);
        scrollbarRoot.gameObject.SetActive(enabled);

        Image scrollbarImage = scrollbarRoot.GetComponent<Image>();
        if (scrollbarImage != null)
            scrollbarImage.color = new Color(0f, 0f, 0f, 0f);

        Scrollbar scrollbar = scrollbarRoot.GetComponent<Scrollbar>();
        if (scrollbar == null)
            scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = scrollbarRoot.Find("Sliding Area") as RectTransform;
        if (slidingArea == null)
            slidingArea = CreatePanel(scrollbarRoot, "Sliding Area", Color.clear);
        Stretch(slidingArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f));

        RectTransform handle = slidingArea.Find("Handle") as RectTransform;
        if (handle == null)
            handle = CreatePanel(slidingArea, "Handle", Color.clear);
        Stretch(handle, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 16f));

        Image handleImage = handle.GetComponent<Image>();
        if (handleImage != null)
            handleImage.color = new Color(0f, 0f, 0f, 0f);

        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.25f;
        input.verticalScrollbar = enabled ? scrollbar : null;
    }

    private static Button CreateButton(
        RectTransform parent,
        string name,
        string label,
        Color backgroundColor,
        Color textColor,
        float height,
        FontStyles fontStyle,
        float width = -1f,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        RectTransform root = CreatePanel(parent, name, backgroundColor);
        LayoutElement layout = EnsureLayoutElement(root);
        layout.preferredHeight = height;
        if (width > 0f)
            layout.preferredWidth = width;
        else
            layout.flexibleWidth = 1f;

        Button button = root.gameObject.AddComponent<Button>();
        TMP_Text text = CreateText(root, "Label", label, 15, alignment, textColor, fontStyle);
        text.margin = new Vector4(10f, 4f, 10f, 4f);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return button;
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Color color,
        FontStyles fontStyle)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = true;
        Stretch(go.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        return text;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();

        if (color != Color.clear)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
        }

        return rect;
    }

    private static RectTransform EnsurePanel(RectTransform parent, string name, bool addImage, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        Image image = go.GetComponent<Image>();
        if (addImage)
        {
            if (image == null)
                image = go.AddComponent<Image>();
            image.color = color;
        }

        return rect;
    }

    private static ScrollRect EnsureScrollArea(RectTransform parent, string name, out RectTransform content, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform scrollRoot = EnsurePanel(parent, name, false, Color.clear);
        Stretch(scrollRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax);

        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = EnsurePanel(scrollRoot, "Viewport", true, new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null)
            mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = EnsurePanel(viewport, "Content", false, Color.clear);
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
        VerticalLayoutGroup layout = EnsureVerticalLayout(content, spacing, padding);
        layout.childForceExpandWidth = true;

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
        ScrollRect rightScroll,
        Button addRootOriginal,
        RectTransform leftRootOriginal,
        RectTransform rightSectionOriginal)
    {
        SerializedObject so = new SerializedObject(catalogUi);
        so.FindProperty("bodyRoot").objectReferenceValue = bodyRoot;
        so.FindProperty("leftContent").objectReferenceValue = leftContent;
        so.FindProperty("rightContent").objectReferenceValue = rightContent;
        so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        so.FindProperty("leftScroll").objectReferenceValue = leftScroll;
        so.FindProperty("rightScroll").objectReferenceValue = rightScroll;
        so.FindProperty("leftAddRootOriginal").objectReferenceValue = addRootOriginal;
        so.FindProperty("leftRootOriginal").objectReferenceValue = leftRootOriginal;
        so.FindProperty("rightSectionOriginal").objectReferenceValue = rightSectionOriginal;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindRequiredObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
            Debug.LogError("Required object not found: " + objectName);
        return obj;
    }

    private static VerticalLayoutGroup EnsureVerticalLayout(RectTransform rect, float spacing, RectOffset padding)
    {
        VerticalLayoutGroup layout = rect.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = spacing;
        layout.padding = padding;
        return layout;
    }

    private static HorizontalLayoutGroup EnsureHorizontalLayout(RectTransform rect, float spacing, RectOffset padding)
    {
        HorizontalLayoutGroup layout = rect.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = spacing;
        layout.padding = padding;
        return layout;
    }

    private static LayoutElement EnsureLayoutElement(RectTransform rect)
    {
        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();
        return layout;
    }

    private static void AddOutline(RectTransform rect, Color color)
    {
        Outline outline = rect.GetComponent<Outline>();
        if (outline == null)
            outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
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
