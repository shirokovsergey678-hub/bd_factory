using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogMainPanelUI : MonoBehaviour
{
    private const string TextAddRoot = "\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c \u0440\u0430\u0437\u0434\u0435\u043b";
    private const string TextNewRoot = "\u041d\u043e\u0432\u044b\u0439 \u0440\u0430\u0437\u0434\u0435\u043b";
    private const string TextNewChild = "\u041d\u043e\u0432\u044b\u0439 \u043f\u043e\u0434\u0440\u0430\u0437\u0434\u0435\u043b";
    private const string TextChooseLeft = "\u0412\u044b\u0431\u0435\u0440\u0438 \u0440\u0430\u0437\u0434\u0435\u043b \u0441\u043b\u0435\u0432\u0430.";
    private const string TextRootNotFound = "\u0420\u0430\u0437\u0434\u0435\u043b \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d.";
    private const string TextAddProduct = "\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c \u043f\u0440\u043e\u0434\u0443\u043a\u0442";
    private const string TextNoChildrenAdmin = "\u0423 \u044d\u0442\u043e\u0433\u043e \u0440\u0430\u0437\u0434\u0435\u043b\u0430 \u043f\u043e\u043a\u0430 \u043d\u0435\u0442 \u043f\u043e\u0434\u0440\u0430\u0437\u0434\u0435\u043b\u043e\u0432. \u0414\u043e\u0431\u0430\u0432\u044c \u0438\u0445 \u0441\u043b\u0435\u0432\u0430.";
    private const string TextNoChildrenUser = "\u0412 \u044d\u0442\u043e\u043c \u0440\u0430\u0437\u0434\u0435\u043b\u0435 \u043f\u043e\u043a\u0430 \u043d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445.";
    private const string TextAddImage = "\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c\n\u0438\u0437\u043e\u0431\u0440\u0430\u0436\u0435\u043d\u0438\u0435\n\u043f\u0440\u043e\u0434\u0443\u043a\u0442\u0430";
    private const string TextNoImage = "\u041d\u0435\u0442 \u0438\u0437\u043e\u0431\u0440\u0430\u0436\u0435\u043d\u0438\u044f";
    private const string TextPickImage = "\u0412\u044b\u0431\u0435\u0440\u0438 \u0438\u0437\u043e\u0431\u0440\u0430\u0436\u0435\u043d\u0438\u0435";
    private const string TextProductName = "\u041d\u0430\u0437\u0432\u0430\u043d\u0438\u0435 \u043f\u0440\u043e\u0434\u0443\u043a\u0442\u0430";
    private const string TextProductDescription = "\u041e\u043f\u0438\u0441\u0430\u043d\u0438\u0435 \u043f\u0440\u043e\u0434\u0443\u043a\u0442\u0430";
    private const string TextSaveProduct = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u044f";
    private const string TextUnnamed = "\u0411\u0435\u0437 \u043d\u0430\u0437\u0432\u0430\u043d\u0438\u044f";
    private const string TextDescriptionEmpty = "\u041e\u043f\u0438\u0441\u0430\u043d\u0438\u0435 \u043d\u0435 \u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d\u043e";
    private const string TextTechDescription = "\u0422\u0435\u0445\u043d\u0438\u0447\u0435\u0441\u043a\u043e\u0435 \u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435";
    private const string TextAddFile = "\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c \u0444\u0430\u0439\u043b";
    private const string TextPickFile = "\u0412\u044b\u0431\u0435\u0440\u0438 \u0444\u0430\u0439\u043b";
    private const string TextNoFiles = "\u0424\u0430\u0439\u043b\u044b \u043d\u0435 \u0434\u043e\u0431\u0430\u0432\u043b\u0435\u043d\u044b";
    private const string TextOpenInExplorer = "\u041f\u0443\u0442\u044c \u0432 \u043f\u0440\u043e\u0432\u043e\u0434\u043d\u0438\u043a\u0435";
    private const string TextDelete = "\u0423\u0434\u0430\u043b\u0438\u0442\u044c";
    private const string TextDeleteProduct = "\u0423\u0434\u0430\u043b\u0438\u0442\u044c \u043f\u0440\u043e\u0434\u0443\u043a\u0442";
    private const string TextEnterName = "\u0412\u0432\u0435\u0434\u0438 \u043d\u0430\u0437\u0432\u0430\u043d\u0438\u0435";
    private const string TextCreate = "\u0421\u043e\u0437\u0434\u0430\u0442\u044c";
    private const string TextCancel = "\u041e\u0442\u043c\u0435\u043d\u0430";

    [Header("Scene References")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform leftContent;
    [SerializeField] private RectTransform rightContent;
    [SerializeField] private RectTransform modalRoot;
    [SerializeField] private ScrollRect leftScroll;
    [SerializeField] private ScrollRect rightScroll;

    private DatabaseManager db;
    private readonly Dictionary<int, bool> expandedRoots = new();
    private readonly Dictionary<int, RectTransform> childSectionAnchors = new();
    private int selectedRootId = -1;

    void Awake()
    {
        db = DatabaseManager.Instance;
    }

    async void OnEnable()
    {
        if (db == null)
            db = DatabaseManager.Instance;

        ResolveSceneReferences();

        if (!HasRequiredReferences())
        {
            Debug.LogError("CatalogMainPanelUI: scene references are not configured.");
            return;
        }

        if (db?.CurrentUser != null)
            await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (db == null)
            return;

        await db.EnsureCatalogSchemaAsync();

        List<CatalogRootCategory> roots = await db.GetCatalogRootsAsync();

        if (selectedRootId <= 0 && roots.Count > 0)
            selectedRootId = roots[0].Id;

        if (selectedRootId > 0 && roots.Find(r => r.Id == selectedRootId) == null)
            selectedRootId = roots.Count > 0 ? roots[0].Id : -1;

        await BuildLeftPanelAsync(roots);
        await BuildRightPanelAsync();
    }

    void ResolveSceneReferences()
    {
        if (bodyRoot == null)
            bodyRoot = transform.Find("CatalogBody") as RectTransform;

        if (leftScroll == null && bodyRoot != null)
            leftScroll = bodyRoot.Find("LeftPanel/LeftScroll")?.GetComponent<ScrollRect>();

        if (rightScroll == null && bodyRoot != null)
            rightScroll = bodyRoot.Find("RightPanel/RightScroll")?.GetComponent<ScrollRect>();

        if (leftContent == null && leftScroll != null)
            leftContent = leftScroll.content;

        if (rightContent == null && rightScroll != null)
            rightContent = rightScroll.content;

        if (modalRoot == null)
            modalRoot = transform.Find("ModalRoot") as RectTransform;
    }

    bool HasRequiredReferences()
    {
        return bodyRoot != null
            && leftContent != null
            && rightContent != null
            && modalRoot != null
            && leftScroll != null
            && rightScroll != null;
    }

    async Task BuildLeftPanelAsync(List<CatalogRootCategory> roots)
    {
        ClearChildren(leftContent);

        if (db.IsAdmin)
        {
            CreateOutlineButton(leftContent, TextAddRoot, () =>
            {
                ShowPrompt(TextNewRoot, async name =>
                {
                    await db.CreateCatalogRootAsync(name);
                    await RefreshAsync();
                });
            });
        }

        foreach (CatalogRootCategory root in roots)
        {
            root.Children = await db.GetCatalogChildrenAsync(root.Id);

            if (!expandedRoots.ContainsKey(root.Id))
                expandedRoots[root.Id] = true;

            RectTransform rootCard = CreatePanel($"Root_{root.Id}", leftContent, Color.white);
            AddOutline(rootCard, new Color(0.88f, 0.88f, 0.88f, 1f));

            VerticalLayoutGroup cardLayout = rootCard.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;
            cardLayout.spacing = 6;
            cardLayout.padding = new RectOffset(8, 8, 8, 8);

            RectTransform header = CreatePanel("Header", rootCard, Color.clear);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;
            headerLayout.spacing = 4;

            CreateMiniButton(header, expandedRoots[root.Id] ? "v" : ">", () =>
            {
                expandedRoots[root.Id] = !expandedRoots[root.Id];
                _ = RefreshAsync();
            }, 22f);

            CreateLinkButton(header, root.Name, () =>
            {
                selectedRootId = root.Id;
                _ = BuildRightPanelAsync();
            });

            if (db.IsAdmin)
            {
                CreateMiniDangerButton(header, "x", async () =>
                {
                    await db.DeleteCatalogRootAsync(root.Id);
                    if (selectedRootId == root.Id)
                        selectedRootId = -1;
                    await RefreshAsync();
                }, 22f);
            }

            if (!expandedRoots[root.Id])
                continue;

            foreach (CatalogChildCategory child in root.Children)
            {
                RectTransform childRow = CreatePanel("ChildRow", rootCard, Color.clear);
                childRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

                HorizontalLayoutGroup childLayout = childRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                childLayout.childControlHeight = true;
                childLayout.childControlWidth = true;
                childLayout.childForceExpandHeight = false;
                childLayout.childForceExpandWidth = false;
                childLayout.padding = new RectOffset(24, 0, 0, 0);
                childLayout.spacing = 4;

                CreateLinkButton(childRow, child.Name, () =>
                {
                    selectedRootId = root.Id;
                    _ = BuildRightPanelAsync(child.Id);
                });

                if (db.IsAdmin)
                {
                    CreateMiniDangerButton(childRow, "x", async () =>
                    {
                        await db.DeleteCatalogChildAsync(child.Id);
                        selectedRootId = root.Id;
                        await RefreshAsync();
                    }, 22f);
                }
            }

            if (db.IsAdmin)
            {
                RectTransform addChildRow = CreatePanel("AddChildRow", rootCard, Color.clear);
                addChildRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
                HorizontalLayoutGroup addChildLayout = addChildRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                addChildLayout.childControlHeight = true;
                addChildLayout.childControlWidth = true;
                addChildLayout.childForceExpandHeight = false;
                addChildLayout.childForceExpandWidth = false;
                addChildLayout.padding = new RectOffset(24, 0, 0, 0);
                addChildLayout.spacing = 4;

                CreateMiniButton(addChildRow, "+", () =>
                {
                    ShowPrompt(TextNewChild, async name =>
                    {
                        await db.CreateCatalogChildAsync(root.Id, name);
                        selectedRootId = root.Id;
                        await RefreshAsync();
                    });
                }, 22f);
            }
        }
    }

    async Task BuildRightPanelAsync(int scrollToChildId = -1)
    {
        ClearChildren(rightContent);
        childSectionAnchors.Clear();

        if (selectedRootId <= 0)
        {
            CreateInfoText(rightContent, TextChooseLeft);
            return;
        }

        CatalogRootCategory root = await db.GetCatalogRootDetailsAsync(selectedRootId);
        if (root == null)
        {
            CreateInfoText(rightContent, TextRootNotFound);
            return;
        }

        foreach (CatalogChildCategory child in root.Children)
        {
            RectTransform section = CreatePanel($"Section_{child.Id}", rightContent, Color.white);
            childSectionAnchors[child.Id] = section;

            VerticalLayoutGroup sectionLayout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            sectionLayout.childControlHeight = true;
            sectionLayout.childControlWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.spacing = 8;
            sectionLayout.padding = new RectOffset(0, 0, 0, 8);

            RectTransform titleBar = CreatePanel("TitleBar", section, Hex("0A78C2"));
            titleBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
            TMP_Text titleText = CreateText(titleBar, child.Name, 17, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
            titleText.margin = new Vector4(12, 0, 12, 0);

            foreach (CatalogProduct product in child.Products)
                BuildProductCard(section, child, product);

            if (db.IsAdmin)
            {
                CreateOutlineButton(section, TextAddProduct, async () =>
                {
                    await db.CreateProductAsync(child.Id);
                    await BuildRightPanelAsync(child.Id);
                });
            }
        }

        if (root.Children.Count == 0)
            CreateInfoText(rightContent, db.IsAdmin ? TextNoChildrenAdmin : TextNoChildrenUser);

        await Task.Yield();

        if (scrollToChildId > 0 && childSectionAnchors.TryGetValue(scrollToChildId, out RectTransform target))
            ScrollToTarget(target);
    }

    void BuildProductCard(RectTransform parent, CatalogChildCategory child, CatalogProduct product)
    {
        RectTransform card = CreatePanel($"Product_{product.Id}", parent, Color.white);
        AddOutline(card, new Color(0.85f, 0.85f, 0.85f, 1f));
        TMP_InputField nameInput = null;
        TMP_InputField descriptionInput = null;

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.childControlHeight = true;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = true;
        cardLayout.spacing = 8;
        cardLayout.padding = new RectOffset(8, 8, 8, 8);

        RectTransform topRow = CreatePanel("TopRow", card, Color.clear);
        HorizontalLayoutGroup topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.childControlHeight = true;
        topLayout.childControlWidth = true;
        topLayout.childForceExpandHeight = false;
        topLayout.childForceExpandWidth = false;
        topLayout.spacing = 12;

        RectTransform imageArea = CreatePanel("ImageArea", topRow, new Color(0.55f, 0.55f, 0.55f, 1f));
        LayoutElement imageLayout = imageArea.gameObject.AddComponent<LayoutElement>();
        imageLayout.preferredWidth = 180;
        imageLayout.preferredHeight = 160;

        if (!string.IsNullOrWhiteSpace(product.ImagePath) && File.Exists(product.ImagePath))
            CreateImagePreview(imageArea, product.ImagePath);
        else
            CreateText(imageArea, db.IsAdmin ? TextAddImage : TextNoImage, 14, TextAlignmentOptions.Center, Color.white);

        if (db.IsAdmin)
        {
            Button imageButton = imageArea.gameObject.AddComponent<Button>();
            imageButton.onClick.AddListener(async () =>
            {
                ExtensionFilter[] extensions = { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };
                string[] paths = StandaloneFileBrowser.OpenFilePanel(TextPickImage, "", extensions, false);
                if (paths.Length == 0)
                    return;

                if (!string.IsNullOrWhiteSpace(product.ImagePath))
                    CatalogFileStorage.DeleteIfExists(product.ImagePath);

                product.ImagePath = CatalogFileStorage.ImportImage(paths[0]);
                await db.UpdateProductAsync(product);
                await BuildRightPanelAsync(child.Id);
            });
        }

        RectTransform infoArea = CreatePanel("InfoArea", topRow, Color.clear);
        infoArea.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        VerticalLayoutGroup infoLayout = infoArea.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.childControlHeight = true;
        infoLayout.childControlWidth = true;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childForceExpandWidth = true;
        infoLayout.spacing = 8;

        if (db.IsAdmin)
        {
            nameInput = CreateInputField(infoArea, TextProductName, product.Name);
            descriptionInput = CreateInputField(infoArea, TextProductDescription, product.Description, 110, true);
        }
        else
        {
            TMP_Text nameText = CreateText(
                infoArea,
                string.IsNullOrWhiteSpace(product.Name) ? TextUnnamed : product.Name,
                18,
                TextAlignmentOptions.TopLeft,
                Hex("0A78C2"),
                FontStyles.Bold);
            nameText.margin = Vector4.zero;

            CreateText(
                infoArea,
                string.IsNullOrWhiteSpace(product.Description) ? TextDescriptionEmpty : product.Description,
                15,
                TextAlignmentOptions.TopLeft,
                Color.black);
        }

        RectTransform filesSection = CreatePanel("FilesSection", card, Hex("222222"));
        VerticalLayoutGroup filesLayout = filesSection.gameObject.AddComponent<VerticalLayoutGroup>();
        filesLayout.childControlHeight = true;
        filesLayout.childControlWidth = true;
        filesLayout.childForceExpandHeight = false;
        filesLayout.childForceExpandWidth = true;
        filesLayout.spacing = 6;
        filesLayout.padding = new RectOffset(6, 6, 6, 6);

        RectTransform filesHeader = CreatePanel("FilesHeader", filesSection, Color.clear);
        filesHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
        HorizontalLayoutGroup filesHeaderLayout = filesHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        filesHeaderLayout.childControlHeight = true;
        filesHeaderLayout.childControlWidth = true;
        filesHeaderLayout.childForceExpandHeight = false;
        filesHeaderLayout.childForceExpandWidth = false;
        filesHeaderLayout.spacing = 8;

        CreateRowLabel(filesHeader, TextTechDescription, 14, Color.white);

        if (db.IsAdmin)
        {
            CreatePrimaryButton(filesHeader, TextAddFile, async () =>
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel(TextPickFile, "", "", false);
                if (paths.Length == 0)
                    return;

                string savedPath = CatalogFileStorage.ImportFile(paths[0]);
                await db.AddProductFileAsync(product.Id, Path.GetFileName(paths[0]), savedPath);
                await BuildRightPanelAsync(child.Id);
            }, 130f, 30f);
        }

        RectTransform fileList = CreatePanel("FileList", filesSection, Color.clear);
        VerticalLayoutGroup fileListLayout = fileList.gameObject.AddComponent<VerticalLayoutGroup>();
        fileListLayout.childControlHeight = true;
        fileListLayout.childControlWidth = true;
        fileListLayout.childForceExpandHeight = false;
        fileListLayout.childForceExpandWidth = true;
        fileListLayout.spacing = 4;

        if (product.Files.Count == 0)
        {
            CreateText(fileList, TextNoFiles, 13, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.85f, 1f));
        }
        else
        {
            foreach (CatalogProductFile file in product.Files)
                BuildFileRow(fileList, child.Id, file);
        }

        if (db.IsAdmin)
        {
            RectTransform actionsRow = CreatePanel("ActionsRow", card, Color.clear);
            actionsRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;

            HorizontalLayoutGroup actionsLayout = actionsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.childControlHeight = true;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandHeight = false;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.spacing = 2;

            CreatePrimaryButton(actionsRow, TextSaveProduct, async () =>
            {
                product.Name = nameInput.text;
                product.Description = descriptionInput.text;
                await db.UpdateProductAsync(product);
                await BuildRightPanelAsync(child.Id);
            }, -1f, 42f);

            CreateDangerButton(actionsRow, TextDeleteProduct, async () =>
            {
                await db.DeleteProductAsync(product.Id);

                if (!string.IsNullOrWhiteSpace(product.ImagePath))
                    CatalogFileStorage.DeleteIfExists(product.ImagePath);

                foreach (CatalogProductFile productFile in product.Files)
                    CatalogFileStorage.DeleteIfExists(productFile.FilePath);

                await BuildRightPanelAsync(child.Id);
            }, -1f, 42f);
        }
    }

    void BuildFileRow(RectTransform parent, int childId, CatalogProductFile file)
    {
        RectTransform row = CreatePanel("FileRow", parent, new Color(0.12f, 0.12f, 0.12f, 1f));
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.spacing = 6;
        rowLayout.padding = new RectOffset(8, 8, 6, 6);

        TMP_Text fileName = CreateText(row, file.FileName, 13, TextAlignmentOptions.MidlineLeft, Color.white);
        LayoutElement fileNameLayout = fileName.gameObject.AddComponent<LayoutElement>();
        fileNameLayout.preferredWidth = 260;

        CreateGhostButton(row, TextOpenInExplorer, () => CatalogFileStorage.OpenInFolder(file.FilePath), 130f, 28f);

        if (db.IsAdmin)
        {
            CreateDangerButton(row, TextDelete, async () =>
            {
                CatalogFileStorage.DeleteIfExists(file.FilePath);
                await db.DeleteProductFileAsync(file.Id);
                await BuildRightPanelAsync(childId);
            }, 82f, 28f);
        }
    }

    void ShowPrompt(string title, Action<string> onConfirm)
    {
        modalRoot.gameObject.SetActive(true);
        ClearChildren(modalRoot);

        RectTransform dialog = CreatePanel("Dialog", modalRoot, Color.white);
        Stretch(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -90f), new Vector2(210f, 90f));

        VerticalLayoutGroup dialogLayout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
        dialogLayout.childControlHeight = true;
        dialogLayout.childControlWidth = true;
        dialogLayout.childForceExpandHeight = false;
        dialogLayout.childForceExpandWidth = true;
        dialogLayout.spacing = 10;
        dialogLayout.padding = new RectOffset(14, 14, 14, 14);

        CreateText(dialog, title, 18, TextAlignmentOptions.MidlineLeft, Color.black, FontStyles.Bold);
        TMP_InputField input = CreateInputField(dialog, TextEnterName, string.Empty);

        RectTransform buttons = CreatePanel("Buttons", dialog, Color.clear);
        HorizontalLayoutGroup buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.spacing = 8;

        CreatePrimaryButton(buttons, TextCreate, () =>
        {
            if (string.IsNullOrWhiteSpace(input.text))
                return;

            modalRoot.gameObject.SetActive(false);
            onConfirm?.Invoke(input.text.Trim());
        }, 110f, 34f);

        CreateGhostButton(buttons, TextCancel, () => modalRoot.gameObject.SetActive(false), 110f, 34f);
    }

    ScrollRect CreateScrollArea(RectTransform parent, string name, out RectTransform content)
    {
        RectTransform root = CreatePanel(name, parent, Color.clear);
        ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = CreatePanel("Viewport", root, new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        content = CreatePanel("Content", viewport, Color.clear);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        return scrollRect;
    }

    TMP_Text CreateText(
        RectTransform parent,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Color color,
        FontStyles fontStyle = FontStyles.Normal)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
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

    TMP_InputField CreateInputField(RectTransform parent, string placeholder, string value, float height = 42f, bool multiLine = false)
    {
        RectTransform root = CreatePanel("InputField", parent, new Color(0.42f, 0.42f, 0.42f, 1f));
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.16f, 0.47f, 0.78f, 0.45f);
        input.caretWidth = 2;
        input.scrollSensitivity = 20f;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.richText = false;

        RectTransform viewport = CreatePanel("TextArea", root, Color.clear);
        float rightPadding = multiLine ? 24f : 8f;
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 5f), new Vector2(-rightPadding, -5f));
        viewport.gameObject.AddComponent<RectMask2D>();
        input.textViewport = viewport;

        TextMeshProUGUI text = CreateText(
            viewport,
            value ?? string.Empty,
            16,
            multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft,
            Color.white) as TextMeshProUGUI;
        text.enableWordWrapping = multiLine;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        input.textComponent = text;

        TextMeshProUGUI placeholderText = CreateText(
            viewport,
            placeholder,
            16,
            multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft,
            new Color(1f, 1f, 1f, 0.7f)) as TextMeshProUGUI;
        placeholderText.enableWordWrapping = multiLine;
        placeholderText.overflowMode = TextOverflowModes.Overflow;
        placeholderText.raycastTarget = false;
        input.placeholder = placeholderText;

        input.text = value ?? string.Empty;

        if (multiLine)
            AttachInputScrollbar(root, input);

        return input;
    }

    void AttachInputScrollbar(RectTransform inputRoot, TMP_InputField input)
    {
        RectTransform scrollbarRoot = CreatePanel("Scrollbar", inputRoot, new Color(0.24f, 0.24f, 0.24f, 1f));
        Stretch(scrollbarRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-14f, 4f), new Vector2(-4f, -4f));
        scrollbarRoot.pivot = new Vector2(1f, 0.5f);

        Image scrollbarImage = scrollbarRoot.GetComponent<Image>();
        scrollbarImage.raycastTarget = true;

        Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = CreatePanel("Sliding Area", scrollbarRoot, Color.clear);
        Stretch(slidingArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f));

        RectTransform handle = CreatePanel("Handle", slidingArea, new Color(0.82f, 0.82f, 0.82f, 1f));
        Stretch(handle, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 16f));

        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.size = 0.25f;

        input.verticalScrollbar = scrollbar;
    }

    TMP_Text CreateRowLabel(RectTransform parent, string value, float size, Color color)
    {
        RectTransform root = CreatePanel("Label", parent, Color.clear);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 30f;

        TMP_Text text = CreateText(root, value, size, TextAlignmentOptions.MidlineLeft, color);
        text.margin = new Vector4(4f, 0f, 4f, 0f);
        text.enableWordWrapping = false;
        return text;
    }

    void CreateImagePreview(RectTransform parent, string imagePath)
    {
        byte[] bytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        GameObject go = new GameObject("Image", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        Stretch(go.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f));
    }

    RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        if (color != Color.clear)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
        }

        return go.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    void AddOutline(RectTransform rect, Color color)
    {
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    Button CreatePrimaryButton(RectTransform parent, string label, Action action, float width = -1f, float height = 36f)
    {
        return CreateButton(parent, label, action, Hex("0A78C2"), Color.white, width, height, TextAlignmentOptions.Center);
    }

    Button CreateGhostButton(RectTransform parent, string label, Action action, float width = -1f, float height = 36f)
    {
        return CreateButton(parent, label, action, new Color(0.12f, 0.12f, 0.12f, 1f), new Color(0.96f, 0.89f, 0.55f), width, height, TextAlignmentOptions.Center);
    }

    Button CreateDangerButton(RectTransform parent, string label, Action action, float width = -1f, float height = 36f)
    {
        return CreateButton(parent, label, action, new Color(0.15f, 0.08f, 0.08f, 1f), new Color(1f, 0.65f, 0.65f), width, height, TextAlignmentOptions.Center);
    }

    Button CreateOutlineButton(RectTransform parent, string label, Action action)
    {
        Button button = CreateButton(parent, label, action, Color.white, Color.black, -1f, 38f, TextAlignmentOptions.Center);
        AddOutline(button.GetComponent<RectTransform>(), Color.black);
        return button;
    }

    Button CreateLinkButton(RectTransform parent, string label, Action action)
    {
        Button button = CreateButton(parent, label, action, Color.clear, Color.black, -1f, 26f, TextAlignmentOptions.MidlineLeft, true);
        TMP_Text text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        return button;
    }

    Button CreateMiniButton(RectTransform parent, string label, Action action, float size)
    {
        return CreateButton(parent, label, action, new Color(0.82f, 0.82f, 0.82f, 1f), Color.black, size, size, TextAlignmentOptions.Center);
    }

    Button CreateMiniDangerButton(RectTransform parent, string label, Action action, float size)
    {
        return CreateButton(parent, label, action, new Color(0.55f, 0.17f, 0.17f, 1f), Color.white, size, size, TextAlignmentOptions.Center);
    }

    Button CreateButton(
        RectTransform parent,
        string label,
        Action action,
        Color backgroundColor,
        Color textColor,
        float width,
        float height,
        TextAlignmentOptions alignment,
        bool underline = false)
    {
        RectTransform root = CreatePanel("Button_" + label, parent, backgroundColor);

        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
            layout.preferredWidth = width;
        else
            layout.flexibleWidth = 1f;

        Button button = root.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => action?.Invoke());

        TMP_Text text = CreateText(root, label, 15, alignment, textColor, underline ? FontStyles.Underline : FontStyles.Normal);
        text.margin = new Vector4(10f, 4f, 10f, 4f);

        return button;
    }

    void CreateInfoText(RectTransform parent, string text)
    {
        RectTransform panel = CreatePanel("InfoPanel", parent, Color.clear);
        panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        CreateText(panel, text, 16, TextAlignmentOptions.MidlineLeft, Color.black);
    }

    void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    void ScrollToTarget(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        float contentHeight = rightContent.rect.height;
        float viewportHeight = rightScroll.viewport.rect.height;
        if (contentHeight <= viewportHeight)
            return;

        float targetY = Mathf.Abs(target.localPosition.y);
        float normalized = Mathf.Clamp01(1f - targetY / Mathf.Max(1f, contentHeight - viewportHeight));
        rightScroll.verticalNormalizedPosition = normalized;
    }

    Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString("#" + html, out Color color);
        return color;
    }
}
