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
    struct DisplayTextBinding
    {
        public RectTransform Root;
        public TMP_Text Text;
        public float MinHeight;
    }

    struct AdminInputBinding
    {
        public TMP_InputField Input;
        public float MinHeight;
    }

    private const float ProductNameDisplayMinHeight = 42f;
    private const float ProductDescriptionDisplayMinHeight = 110f;

    private const string TextAddRoot = "Добавить раздел";
    private const string TextNewRoot = "Новый раздел";
    private const string TextNewChild = "Новый подраздел";
    private const string TextChooseLeft = "Выбери раздел слева.";
    private const string TextRootNotFound = "Раздел не найден.";
    private const string TextAddProduct = "Добавить продукт";
    private const string TextNoChildrenAdmin = "У этого раздела пока нет подразделов. Добавь их слева.";
    private const string TextNoChildrenUser = "В этом разделе пока нет данных.";
    private const string TextAddImage = "Добавить\nизображение\nпродукта";
    private const string TextNoImage = "Нет изображения";
    private const string TextPickImage = "Выбери изображение";
    private const string TextProductName = "Название продукта";
    private const string TextProductDescription = "Описание продукта";
    private const string TextSaveProduct = "Сохранить изменения";
    private const string TextUnnamed = "Без названия";
    private const string TextDescriptionEmpty = "Описание не заполнено";
    private const string TextTechDescription = "Техническое описание";
    private const string TextAddFile = "Добавить файл";
    private const string TextPickFile = "Выбери файл";
    private const string TextNoFiles = "Файлы не добавлены";
    private const string TextOpenInExplorer = "Путь в проводнике";
    private const string TextDelete = "Удалить";
    private const string TextDeleteProduct = "Удалить продукт";
    private const string TextEnterName = "Введи название";
    private const string TextCreate = "Создать";
    private const string TextCancel = "Отмена";

    [Header("Scene References")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private RectTransform leftContent;
    [SerializeField] private RectTransform rightContent;
    [SerializeField] private RectTransform modalRoot;
    [SerializeField] private ScrollRect leftScroll;
    [SerializeField] private ScrollRect rightScroll;

    [Header("Scene Originals")]
    [SerializeField] private Button leftAddRootOriginal;
    [SerializeField] private RectTransform leftRootOriginal;
    [SerializeField] private RectTransform rightSectionOriginal;

    private DatabaseManager db;
    private readonly Dictionary<int, bool> expandedRoots = new();
    private readonly Dictionary<int, RectTransform> childSectionAnchors = new();
    private readonly List<DisplayTextBinding> displayTextBindings = new();
    private readonly List<AdminInputBinding> adminInputBindings = new();
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
            Debug.LogError("CatalogMainPanelUI: scene references or originals are not configured.");
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
        PrepareOriginalsForRuntime();

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

        if (leftContent != null && !IsValidLeftAddRootButton(leftAddRootOriginal))
            leftAddRootOriginal = FindLeftAddRootOriginal();

        if (leftRootOriginal == null && leftContent != null)
            leftRootOriginal = leftContent.Find("Root_12") as RectTransform;

        if (rightSectionOriginal == null && rightContent != null)
            rightSectionOriginal = rightContent.Find("Section_15") as RectTransform;
    }

    bool HasRequiredReferences()
    {
        return bodyRoot != null
            && leftContent != null
            && rightContent != null
            && modalRoot != null
            && leftScroll != null
            && rightScroll != null
            && leftAddRootOriginal != null
            && leftRootOriginal != null
            && rightSectionOriginal != null;
    }

    bool IsValidLeftAddRootButton(Button button)
    {
        if (button == null || leftContent == null)
            return false;

        Transform buttonTransform = button.transform;
        if (buttonTransform.parent != leftContent)
            return false;

        string objectName = buttonTransform.name;
        return objectName == "Button_Добавить раздел" || objectName.Contains("Добавить раздел");
    }

    Button FindLeftAddRootOriginal()
    {
        if (leftContent == null)
            return null;

        Transform exact = leftContent.Find("Button_Добавить раздел");
        if (exact != null)
            return exact.GetComponent<Button>();

        for (int i = 0; i < leftContent.childCount; i++)
        {
            Transform child = leftContent.GetChild(i);
            if (child == null || child == leftRootOriginal)
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null && child.name.Contains("Добавить раздел"))
                return button;
        }

        for (int i = 0; i < leftContent.childCount; i++)
        {
            Transform child = leftContent.GetChild(i);
            if (child == null || child == leftRootOriginal)
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    void PrepareOriginalsForRuntime()
    {
        if (leftRootOriginal != null)
            leftRootOriginal.gameObject.SetActive(false);

        if (rightSectionOriginal != null)
            rightSectionOriginal.gameObject.SetActive(false);
    }

    async Task BuildLeftPanelAsync(List<CatalogRootCategory> roots)
    {
        ClearDynamicChildren(leftContent, leftAddRootOriginal?.transform as RectTransform, leftRootOriginal);

        if (leftAddRootOriginal != null)
        {
            leftAddRootOriginal.gameObject.SetActive(db.IsAdmin);
            if (db.IsAdmin)
            {
                BindButton(leftAddRootOriginal, TextAddRoot, () =>
                {
                    ShowPrompt(TextNewRoot, async name =>
                    {
                        await db.CreateCatalogRootAsync(name);
                        await RefreshAsync();
                    });
                });
            }
        }

        foreach (CatalogRootCategory root in roots)
        {
            root.Children = await db.GetCatalogChildrenAsync(root.Id);

            if (!expandedRoots.ContainsKey(root.Id))
                expandedRoots[root.Id] = true;

            RectTransform rootClone = CloneTemplate(leftRootOriginal, leftContent, $"Root_{root.Id}");
            RectTransform rows = FindRect(rootClone, "Rows");
            RectTransform childTemplate = FindRect(rootClone, "Rows/ChildRow");
            RectTransform addChildTemplate = FindRect(rootClone, "Rows/AddChildRow");

            if (childTemplate != null)
                childTemplate.gameObject.SetActive(false);
            if (addChildTemplate != null)
                addChildTemplate.gameObject.SetActive(false);

            Button expandButton = FindButton(rootClone, "Header/Button_Expand");
            SetExpandButtonState(expandButton, expandedRoots[root.Id]);
            BindButton(expandButton, null, () =>
            {
                expandedRoots[root.Id] = !expandedRoots[root.Id];
                _ = RefreshAsync();
            });

            BindButton(FindButton(rootClone, "Header/Button_Name"), root.Name, () =>
            {
                selectedRootId = root.Id;
                _ = BuildRightPanelAsync();
            });

            Button deleteRootButton = FindButton(rootClone, "Header/Button_Delete");
            if (deleteRootButton != null)
            {
                deleteRootButton.gameObject.SetActive(db.IsAdmin);
                if (db.IsAdmin)
                {
                    BindButton(deleteRootButton, "x", async () =>
                    {
                        await db.DeleteCatalogRootAsync(root.Id);
                        if (selectedRootId == root.Id)
                            selectedRootId = -1;
                        await RefreshAsync();
                    });
                }
            }

            if (rows == null)
                continue;

            ClearDynamicChildren(rows, childTemplate, addChildTemplate);
            rows.gameObject.SetActive(expandedRoots[root.Id]);

            if (!expandedRoots[root.Id])
                continue;

            foreach (CatalogChildCategory child in root.Children)
            {
                RectTransform childClone = CloneTemplate(childTemplate, rows, $"ChildRow_{child.Id}");

                BindButton(FindButton(childClone, "Button_Name"), child.Name, () =>
                {
                    selectedRootId = root.Id;
                    _ = BuildRightPanelAsync(child.Id);
                });

                Button deleteChildButton = FindButton(childClone, "Button_Delete");
                if (deleteChildButton != null)
                {
                    deleteChildButton.gameObject.SetActive(db.IsAdmin);
                    if (db.IsAdmin)
                    {
                        BindButton(deleteChildButton, "x", async () =>
                        {
                            await db.DeleteCatalogChildAsync(child.Id);
                            selectedRootId = root.Id;
                            await RefreshAsync();
                        });
                    }
                }
            }

            if (db.IsAdmin && addChildTemplate != null)
            {
                RectTransform addChildClone = CloneTemplate(addChildTemplate, rows, $"AddChildRow_{root.Id}");
                BindButton(FindButton(addChildClone, "Button_Add"), "+", () =>
                {
                    ShowPrompt(TextNewChild, async name =>
                    {
                        await db.CreateCatalogChildAsync(root.Id, name);
                        selectedRootId = root.Id;
                        await RefreshAsync();
                    });
                });
            }
        }
    }

    async Task BuildRightPanelAsync(int scrollToChildId = -1)
    {
        ClearDynamicChildren(rightContent, rightSectionOriginal);
        childSectionAnchors.Clear();
        displayTextBindings.Clear();
        adminInputBindings.Clear();

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
            RectTransform sectionClone = CloneTemplate(rightSectionOriginal, rightContent, $"Section_{child.Id}");
            childSectionAnchors[child.Id] = sectionClone;

            SetText(FindText(sectionClone, "TitleBar/TitleText"), child.Name);

            RectTransform productsRoot = FindRect(sectionClone, "Products");
            RectTransform productTemplate = FindRect(sectionClone, "Products/Product_17");
            if (productTemplate != null)
                productTemplate.gameObject.SetActive(false);

            if (productsRoot != null)
                ClearDynamicChildren(productsRoot, productTemplate);

            if (productsRoot != null && productTemplate != null)
            {
                foreach (CatalogProduct product in child.Products)
                    BuildProductCard(productsRoot, productTemplate, child, product);
            }

            Button addProductButton = FindButton(sectionClone, "Button_Добавить продукт");
            if (addProductButton != null)
            {
                addProductButton.gameObject.SetActive(db.IsAdmin);
                if (db.IsAdmin)
                {
                    BindButton(addProductButton, TextAddProduct, async () =>
                    {
                        await db.CreateProductAsync(child.Id);
                        await BuildRightPanelAsync(child.Id);
                    });
                }
            }
        }

        if (root.Children.Count == 0)
            CreateInfoText(rightContent, db.IsAdmin ? TextNoChildrenAdmin : TextNoChildrenUser);

        await Task.Yield();

        if (scrollToChildId > 0 && childSectionAnchors.TryGetValue(scrollToChildId, out RectTransform target))
            ScrollToTarget(target);
    }

    void BuildProductCard(RectTransform parent, RectTransform productTemplate, CatalogChildCategory child, CatalogProduct product)
    {
        RectTransform card = CloneTemplate(productTemplate, parent, $"Product_{product.Id}");

        RectTransform fileTemplate = FindRect(card, "FilesSection/FileList/FileRow");
        if (fileTemplate != null)
            fileTemplate.gameObject.SetActive(false);

        Button imageButton = FindButton(card, "TopRow/ImageArea");
        TMP_Text imageLabel = FindText(card, "TopRow/ImageArea/Label");
        Image previewImage = FindImage(card, "TopRow/ImageArea/Preview");
        SetProductImageState(product, imageLabel, previewImage);

        if (imageButton != null)
        {
            imageButton.interactable = db.IsAdmin;
            BindButton(imageButton, null, async () =>
            {
                if (!db.IsAdmin)
                    return;

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

        TMP_InputField nameInput = FindInput(card, "TopRow/InfoArea/NameInput");
        TMP_InputField descriptionInput = FindInput(card, "TopRow/InfoArea/DescriptionInput");
        RectTransform nameTextRoot = FindRect(card, "TopRow/InfoArea/NameText");
        RectTransform descriptionTextRoot = FindRect(card, "TopRow/InfoArea/DescriptionText");
        TMP_Text nameText = FindText(card, "TopRow/InfoArea/NameText/Text");
        TMP_Text descriptionText = FindText(card, "TopRow/InfoArea/DescriptionText/Text");

        if (db.IsAdmin)
        {
            SetActive(nameInput, true);
            SetActive(descriptionInput, true);
            SetActive(nameTextRoot, false);
            SetActive(descriptionTextRoot, false);

            ConfigureInput(nameInput, TextProductName, product.Name, true);
            ConfigureInput(descriptionInput, TextProductDescription, product.Description, true);
            RegisterAdminInputLayout(nameInput, ProductNameDisplayMinHeight);
            RegisterAdminInputLayout(descriptionInput, ProductDescriptionDisplayMinHeight);
        }
        else
        {
            SetActive(nameInput, false);
            SetActive(descriptionInput, false);
            SetActive(nameTextRoot, true);
            SetActive(descriptionTextRoot, true);
            SetActive(nameText, true);
            SetActive(descriptionText, true);

            SetText(nameText, string.IsNullOrWhiteSpace(product.Name) ? TextUnnamed : product.Name);
            SetText(descriptionText, string.IsNullOrWhiteSpace(product.Description) ? TextDescriptionEmpty : product.Description);
            UpdateDisplayTextLayout(nameTextRoot, nameText, ProductNameDisplayMinHeight);
            UpdateDisplayTextLayout(descriptionTextRoot, descriptionText, ProductDescriptionDisplayMinHeight);
            RegisterDisplayTextLayout(nameTextRoot, nameText, ProductNameDisplayMinHeight);
            RegisterDisplayTextLayout(descriptionTextRoot, descriptionText, ProductDescriptionDisplayMinHeight);
        }

        SetText(FindText(card, "FilesSection/FilesHeader/HeaderLabel"), TextTechDescription);

        Button addFileButton = FindButton(card, "FilesSection/FilesHeader/Button_Добавить файл");
        if (addFileButton != null)
        {
            addFileButton.gameObject.SetActive(db.IsAdmin);
            if (db.IsAdmin)
            {
                BindButton(addFileButton, TextAddFile, async () =>
                {
                    string[] paths = StandaloneFileBrowser.OpenFilePanel(TextPickFile, "", "", false);
                    if (paths.Length == 0)
                        return;

                    string savedPath = CatalogFileStorage.ImportFile(paths[0]);
                    await db.AddProductFileAsync(product.Id, Path.GetFileName(paths[0]), savedPath);
                    await BuildRightPanelAsync(child.Id);
                });
            }
        }

        RectTransform fileList = FindRect(card, "FilesSection/FileList");
        RectTransform emptyFilesLabel = FindRect(card, "FilesSection/EmptyFilesLabel");
        TMP_Text emptyFilesText = FindText(card, "FilesSection/EmptyFilesLabel/Text") ?? FindText(card, "FilesSection/EmptyFilesLabel");
        if (fileList != null)
            ClearDynamicChildren(fileList, fileTemplate);

        bool hasFiles = product.Files.Count > 0;
        if (emptyFilesLabel != null)
        {
            emptyFilesLabel.gameObject.SetActive(!hasFiles);
            if (!hasFiles && emptyFilesText != null)
                SetText(emptyFilesText, TextNoFiles);
        }

        if (hasFiles && fileList != null && fileTemplate != null)
        {
            foreach (CatalogProductFile file in product.Files)
                BuildFileRow(fileList, fileTemplate, child.Id, file);
        }

        RectTransform actionsRow = FindRect(card, "ActionsRow");
        if (actionsRow != null)
            actionsRow.gameObject.SetActive(db.IsAdmin);

        if (db.IsAdmin)
        {
            BindButton(FindButton(card, "ActionsRow/Button_Сохранить изменения"), TextSaveProduct, async () =>
            {
                product.Name = nameInput != null ? nameInput.text : product.Name;
                product.Description = descriptionInput != null ? descriptionInput.text : product.Description;
                await db.UpdateProductAsync(product);
                await BuildRightPanelAsync(child.Id);
            });

            BindButton(FindButton(card, "ActionsRow/Button_Удалить продукт"), TextDeleteProduct, async () =>
            {
                await db.DeleteProductAsync(product.Id);

                if (!string.IsNullOrWhiteSpace(product.ImagePath))
                    CatalogFileStorage.DeleteIfExists(product.ImagePath);

                foreach (CatalogProductFile productFile in product.Files)
                    CatalogFileStorage.DeleteIfExists(productFile.FilePath);

                await BuildRightPanelAsync(child.Id);
            });
        }
    }

    void BuildFileRow(RectTransform parent, RectTransform fileTemplate, int childId, CatalogProductFile file)
    {
        RectTransform row = CloneTemplate(fileTemplate, parent, $"FileRow_{file.Id}");
        TMP_Text fileNameText = FindText(row, "FileName/Text") ?? FindText(row, "FileName");
        string displayFileName = string.IsNullOrWhiteSpace(file.FileName) ? Path.GetFileName(file.FilePath) : file.FileName;
        SetText(fileNameText, displayFileName);
        NormalizeRuntimeFileRow(row, fileNameText);

        BindButton(FindButton(row, "Button_Путь в проводнике"), TextOpenInExplorer, () =>
        {
            CatalogFileStorage.OpenInFolder(file.FilePath);
        });

        Button deleteButton = FindButton(row, "Button_Удалить");
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(db.IsAdmin);
            if (db.IsAdmin)
            {
                BindButton(deleteButton, TextDelete, async () =>
                {
                    CatalogFileStorage.DeleteIfExists(file.FilePath);
                    await db.DeleteProductFileAsync(file.Id);
                    await BuildRightPanelAsync(childId);
                });
            }
        }
    }

    void NormalizeRuntimeFileRow(RectTransform row, TMP_Text fileNameText)
    {
        if (row == null)
            return;

        if (fileNameText != null)
        {
            fileNameText.gameObject.SetActive(true);
            fileNameText.enabled = true;
            fileNameText.enableWordWrapping = false;
            fileNameText.overflowMode = TextOverflowModes.Ellipsis;
            fileNameText.alignment = TextAlignmentOptions.TopLeft;
            fileNameText.margin = Vector4.zero;
            RectTransform textRect = fileNameText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.localScale = Vector3.one;
            textRect.localRotation = Quaternion.identity;
        }

        RectTransform fileNameRoot = FindRect(row, "FileName");
        if (fileNameRoot != null)
        {
            LayoutElement fileNameLayout = fileNameRoot.GetComponent<LayoutElement>();
            if (fileNameLayout == null)
                fileNameLayout = fileNameRoot.gameObject.AddComponent<LayoutElement>();

            fileNameLayout.minWidth = 220f;
            fileNameLayout.preferredWidth = 0f;
            fileNameLayout.flexibleWidth = 4f;
        }

        Button openButton = FindButton(row, "Button_РџСѓС‚СЊ РІ РїСЂРѕРІРѕРґРЅРёРєРµ");
        if (openButton != null)
        {
            LayoutElement openLayout = openButton.GetComponent<LayoutElement>();
            if (openLayout == null)
                openLayout = openButton.gameObject.AddComponent<LayoutElement>();

            openLayout.preferredWidth = 92f;
            openLayout.flexibleWidth = 0f;
        }

        Button deleteButton = FindButton(row, "Button_РЈРґР°Р»РёС‚СЊ");
        if (deleteButton != null)
        {
            LayoutElement deleteLayout = deleteButton.GetComponent<LayoutElement>();
            if (deleteLayout == null)
                deleteLayout = deleteButton.gameObject.AddComponent<LayoutElement>();

            deleteLayout.preferredWidth = 100f;
            deleteLayout.flexibleWidth = 0f;
        }

        RectTransform spacer = FindRect(row, "Spacer");
        if (spacer != null)
        {
            LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
            if (spacerLayout == null)
                spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();

            spacerLayout.preferredWidth = 0f;
            spacerLayout.flexibleWidth = 1f;
            spacer.SetSiblingIndex(Mathf.Max(0, row.childCount - 2));
        }

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
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

    void ConfigureInput(TMP_InputField input, string placeholder, string value, bool multiLine)
    {
        if (input == null)
            return;

        ApplyInputVisuals(input);
        input.scrollSensitivity = 0f;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.richText = false;
        input.verticalScrollbar = null;
        input.text = value ?? string.Empty;

        if (input.textComponent != null)
        {
            Stretch(input.textComponent.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            input.textComponent.rectTransform.localScale = Vector3.one;
            input.textComponent.enableWordWrapping = multiLine;
            input.textComponent.overflowMode = TextOverflowModes.Masking;
            input.textComponent.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        }

        if (input.placeholder is TMP_Text placeholderText)
        {
            Stretch(placeholderText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            placeholderText.rectTransform.localScale = Vector3.one;
            placeholderText.text = placeholder;
            placeholderText.enableWordWrapping = multiLine;
            placeholderText.overflowMode = TextOverflowModes.Masking;
            placeholderText.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        }

        if (input.verticalScrollbar != null)
            input.verticalScrollbar = null;

        Transform scrollbar = input.transform.Find("Scrollbar");
        if (scrollbar != null)
            scrollbar.gameObject.SetActive(false);

        EnsureInputScrollForwarder(input);
    }

    void SetProductImageState(CatalogProduct product, TMP_Text label, Image previewImage)
    {
        bool hasImage = !string.IsNullOrWhiteSpace(product.ImagePath) && File.Exists(product.ImagePath);

        if (previewImage != null)
            previewImage.gameObject.SetActive(hasImage);

        if (label != null)
            label.gameObject.SetActive(!hasImage);

        if (!hasImage)
        {
            SetText(label, db.IsAdmin ? TextAddImage : TextNoImage);
            return;
        }

        byte[] bytes = File.ReadAllBytes(product.ImagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);

        if (previewImage != null)
        {
            previewImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            previewImage.preserveAspect = true;
        }
    }

    T CloneTemplate<T>(T template, Transform parent, string objectName) where T : Component
    {
        if (template == null)
            return null;

        T clone = Instantiate(template, parent);
        clone.name = objectName;
        clone.gameObject.SetActive(true);
        return clone;
    }

    void BindButton(Button button, string label, Action action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(() => action.Invoke());

        if (label != null)
            SetButtonLabel(button, label);
    }

    void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
            text.text = label;
    }

    void SetExpandButtonState(Button button, bool expanded)
    {
        if (button == null)
            return;

        Transform arrow = button.transform.Find("collapseArrowImage");
        if (arrow != null)
        {
            arrow.localRotation = Quaternion.Euler(0f, 0f, expanded ? -90f : 0f);
            return;
        }

        SetButtonLabel(button, expanded ? "v" : ">");
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    void UpdateDisplayTextLayout(RectTransform root, TMP_Text text, float minHeight)
    {
        if (root == null || text == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null)
            layout = root.gameObject.AddComponent<LayoutElement>();

        float preferredHeight = text.preferredHeight + text.margin.y + text.margin.w + 4f;
        layout.minHeight = minHeight;
        layout.preferredHeight = Mathf.Max(minHeight, preferredHeight);

        RectTransform infoArea = root.parent as RectTransform;
        if (infoArea != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(infoArea);

        RectTransform topRow = infoArea != null ? infoArea.parent as RectTransform : null;
        if (topRow != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(topRow);

        RectTransform card = topRow != null ? topRow.parent as RectTransform : null;
        if (card != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    void RegisterDisplayTextLayout(RectTransform root, TMP_Text text, float minHeight)
    {
        if (root == null || text == null)
            return;

        displayTextBindings.Add(new DisplayTextBinding
        {
            Root = root,
            Text = text,
            MinHeight = minHeight
        });
    }

    void RefreshDisplayTextLayouts()
    {
        for (int i = 0; i < displayTextBindings.Count; i++)
        {
            DisplayTextBinding binding = displayTextBindings[i];
            if (binding.Root == null || binding.Text == null || !binding.Root.gameObject.activeInHierarchy)
                continue;

            UpdateDisplayTextLayout(binding.Root, binding.Text, binding.MinHeight);
        }
    }

    void UpdateInputLayout(TMP_InputField input, float minHeight)
    {
        if (input == null || input.textComponent == null)
            return;

        RectTransform root = input.transform as RectTransform;
        if (root == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(input.textComponent.rectTransform);

        float contentHeight = input.textComponent.preferredHeight;
        if (input.placeholder is TMP_Text placeholderText)
            contentHeight = Mathf.Max(contentHeight, placeholderText.preferredHeight);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null)
            layout = root.gameObject.AddComponent<LayoutElement>();

        float preferredHeight = Mathf.Max(minHeight, contentHeight + 10f);
        layout.minHeight = minHeight;
        layout.preferredHeight = preferredHeight;

        RectTransform infoArea = root.parent as RectTransform;
        if (infoArea != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(infoArea);

        RectTransform topRow = infoArea != null ? infoArea.parent as RectTransform : null;
        if (topRow != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(topRow);

        RectTransform card = topRow != null ? topRow.parent as RectTransform : null;
        if (card != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    void RegisterAdminInputLayout(TMP_InputField input, float minHeight)
    {
        if (input == null)
            return;

        input.onValueChanged.AddListener(_ => UpdateInputLayout(input, minHeight));

        adminInputBindings.Add(new AdminInputBinding
        {
            Input = input,
            MinHeight = minHeight
        });

        UpdateInputLayout(input, minHeight);
    }

    void RefreshAdminInputLayouts()
    {
        for (int i = 0; i < adminInputBindings.Count; i++)
        {
            AdminInputBinding binding = adminInputBindings[i];
            if (binding.Input == null || !binding.Input.gameObject.activeInHierarchy)
                continue;

            UpdateInputLayout(binding.Input, binding.MinHeight);
        }
    }

    void OnRectTransformDimensionsChange()
    {
        RefreshUserDisplayLayouts();
    }

    public void RefreshUserDisplayLayouts()
    {
        if (!isActiveAndEnabled || db == null)
            return;

        Canvas.ForceUpdateCanvases();
        RefreshAdminInputLayouts();
        RefreshDisplayTextLayouts();
        if (rightScroll != null && rightScroll.viewport != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rightScroll.viewport);

        if (rightContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rightContent);
    }

    void SetActive(Component component, bool value)
    {
        if (component != null)
            component.gameObject.SetActive(value);
    }

    RectTransform FindRect(Component root, string path)
    {
        return root != null ? root.transform.Find(path) as RectTransform : null;
    }

    Button FindButton(Component root, string path)
    {
        Transform target = root != null ? root.transform.Find(path) : null;
        return target != null ? target.GetComponent<Button>() : null;
    }

    TMP_InputField FindInput(Component root, string path)
    {
        Transform target = root != null ? root.transform.Find(path) : null;
        return target != null ? target.GetComponent<TMP_InputField>() : null;
    }

    TMP_Text FindText(Component root, string path)
    {
        Transform target = root != null ? root.transform.Find(path) : null;
        if (target == null)
            return null;

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text != null)
            return text;

        return target.GetComponentInChildren<TMP_Text>(true);
    }

    Image FindImage(Component root, string path)
    {
        Transform target = root != null ? root.transform.Find(path) : null;
        return target != null ? target.GetComponent<Image>() : null;
    }

    void ClearDynamicChildren(RectTransform parent, params RectTransform[] preserved)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            bool keep = false;
            for (int j = 0; j < preserved.Length; j++)
            {
                if (preserved[j] == child)
                {
                    keep = true;
                    break;
                }
            }

            if (!keep)
                Destroy(child.gameObject);
        }
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
        layout.minHeight = height;
        layout.preferredHeight = height;

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        ApplyInputVisuals(input);
        input.scrollSensitivity = 0f;
        input.lineType = multiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.richText = false;

        RectTransform viewport = CreatePanel("TextArea", root, Color.clear);
        Stretch(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 5f), new Vector2(-8f, -5f));
        viewport.gameObject.AddComponent<RectMask2D>();
        input.textViewport = viewport;

        TextMeshProUGUI text = CreateText(
            viewport,
            value ?? string.Empty,
            16,
            multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft,
            Color.white) as TextMeshProUGUI;
        Stretch(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        text.enableWordWrapping = multiLine;
        text.overflowMode = TextOverflowModes.Masking;
        text.raycastTarget = false;
        input.textComponent = text;

        TextMeshProUGUI placeholderText = CreateText(
            viewport,
            placeholder,
            16,
            multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft,
            new Color(1f, 1f, 1f, 0.7f)) as TextMeshProUGUI;
        Stretch(placeholderText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        placeholderText.enableWordWrapping = multiLine;
        placeholderText.overflowMode = TextOverflowModes.Masking;
        placeholderText.raycastTarget = false;
        input.placeholder = placeholderText;

        input.text = value ?? string.Empty;
        EnsureInputScrollForwarder(input);

        return input;
    }

    void ApplyInputVisuals(TMP_InputField input)
    {
        if (input == null)
            return;

        input.customCaretColor = true;
        input.caretColor = Color.black;
        input.selectionColor = new Color(0.16f, 0.47f, 0.78f, 0.45f);
        input.caretWidth = 2;
    }

    void EnsureInputScrollForwarder(TMP_InputField input)
    {
        if (input == null)
            return;

        InputFieldScrollForwarder forwarder = input.GetComponent<InputFieldScrollForwarder>();
        if (forwarder == null)
            forwarder = input.gameObject.AddComponent<InputFieldScrollForwarder>();
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

    Button CreatePrimaryButton(RectTransform parent, string label, Action action, float width = -1f, float height = 36f)
    {
        return CreateButton(parent, label, action, Hex("0A78C2"), Color.white, width, height, TextAlignmentOptions.Center);
    }

    Button CreateGhostButton(RectTransform parent, string label, Action action, float width = -1f, float height = 36f)
    {
        return CreateButton(parent, label, action, new Color(0.12f, 0.12f, 0.12f, 1f), new Color(0.96f, 0.89f, 0.55f), width, height, TextAlignmentOptions.Center);
    }

    Button CreateButton(
        RectTransform parent,
        string label,
        Action action,
        Color backgroundColor,
        Color textColor,
        float width,
        float height,
        TextAlignmentOptions alignment)
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

        TMP_Text text = CreateText(root, label, 15, alignment, textColor);
        text.margin = new Vector4(10f, 4f, 10f, 4f);

        return button;
    }

    void CreateInfoText(RectTransform parent, string textValue)
    {
        RectTransform panel = CreatePanel("InfoPanel", parent, Color.clear);
        panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        CreateText(panel, textValue, 16, TextAlignmentOptions.MidlineLeft, Color.black);
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

    void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString("#" + html, out Color color);
        return color;
    }
}
