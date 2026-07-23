using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MenuInfoProgramButtonBuilder : MonoBehaviour
{
    [SerializeField] private string menuInfoResourcePath = "menu_info";
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private CounterOrderController orderController;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Vector2 buttonSize = new Vector2(90f, 90f);
    [SerializeField] private float verticalSpacing = 8f;
    [SerializeField] private RectOffset padding = new RectOffset(0, 0, 0, 0);
    [SerializeField] private string generatedButtonPrefix = "ProgramMenuButton_";

    private void Start()
    {
        BuildButtons();
    }

    public void BuildButtons()
    {
        if (buttonPrefab == null)
        {
            Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: Button prefab is not assigned.");
            return;
        }

        if (orderController == null)
        {
            orderController = FindCounterOrderController();
        }

        RectTransform targetRoot = GetContentRoot();
        List<string> menuNames = LoadMenuNames();
        ClearGeneratedButtons(targetRoot);

        for (int i = 0; i < menuNames.Count; i++)
        {
            CreateButton(targetRoot, menuNames[i], i);
        }

        UpdateContentSize(targetRoot, menuNames.Count);
    }

    private void CreateButton(RectTransform targetRoot, string menuName, int index)
    {
        GameObject buttonObject = Instantiate(buttonPrefab, targetRoot);
        buttonObject.name = $"{generatedButtonPrefix}{index}_{menuName}";

        RectTransform buttonRect = buttonObject.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.localScale = Vector3.one;
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, buttonSize.x);
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonSize.y);

            int columns = GetColumnCount(targetRoot);
            Vector2Int gridPosition = new Vector2Int(index % columns, index / columns);
            buttonRect.anchoredPosition = GetAnchoredPosition(targetRoot, gridPosition, columns);
        }

        CounterMenuButton menuButton = buttonObject.GetComponent<CounterMenuButton>();
        if (menuButton != null)
        {
            menuButton.Initialize(orderController, menuName);
        }
        else
        {
            Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: Generated prefab '{buttonPrefab.name}' does not have a {nameof(CounterMenuButton)} component.");
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: Generated prefab '{buttonPrefab.name}' does not have a Button component.");
            return;
        }

        CounterOrderController targetOrderController = orderController;
        string targetMenuName = menuName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (targetOrderController == null)
            {
                targetOrderController = FindCounterOrderController();
            }

            if (targetOrderController != null)
            {
                targetOrderController.RegisterMenuClick(targetMenuName);
            }
        });
    }

    private RectTransform GetContentRoot()
    {
        if (contentRoot != null)
        {
            return contentRoot;
        }

        ScrollRect scrollRect = FindScrollRectByName("POS_Buttons");
        if (scrollRect != null && scrollRect.content != null)
        {
            contentRoot = scrollRect.content;
            return contentRoot;
        }

        contentRoot = transform as RectTransform;
        return contentRoot;
    }

    private int GetColumnCount(RectTransform targetRoot)
    {
        float availableWidth = targetRoot != null
            ? Mathf.Max(0f, targetRoot.rect.width - padding.left - padding.right)
            : buttonSize.x;

        return Mathf.Max(1, Mathf.FloorToInt(availableWidth / Mathf.Max(1f, buttonSize.x)));
    }

    private Vector2 GetAnchoredPosition(RectTransform targetRoot, Vector2Int gridPosition, int columns)
    {
        float horizontalSpacing = GetAutoHorizontalSpacing(targetRoot, columns);
        return new Vector2(
            padding.left + gridPosition.x * (buttonSize.x + horizontalSpacing),
            -padding.top - gridPosition.y * (buttonSize.y + verticalSpacing));
    }

    private float GetAutoHorizontalSpacing(RectTransform targetRoot, int columns)
    {
        if (columns <= 1 || targetRoot == null)
        {
            return 0f;
        }

        float availableWidth = Mathf.Max(0f, targetRoot.rect.width - padding.left - padding.right);
        float remainingWidth = Mathf.Max(0f, availableWidth - columns * buttonSize.x);
        return remainingWidth / (columns - 1);
    }

    private void UpdateContentSize(RectTransform targetRoot, int buttonCount)
    {
        if (targetRoot == null || buttonCount <= 0)
        {
            return;
        }

        int columns = GetColumnCount(targetRoot);
        int lastRow = (buttonCount - 1) / columns;
        float targetHeight = padding.top + padding.bottom + (lastRow + 1) * buttonSize.y + lastRow * verticalSpacing;
        targetRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(targetRoot.rect.height, targetHeight));
    }

    private List<string> LoadMenuNames()
    {
        List<string> menuNames = new List<string>();
        TextAsset menuInfoAsset = Resources.Load<TextAsset>(menuInfoResourcePath);
        if (menuInfoAsset == null)
        {
            Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: Resources/{menuInfoResourcePath} was not found.");
            return menuNames;
        }

        string[] lines = menuInfoAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return menuNames;
        }

        List<string> headerCells = ParseCsvLine(lines[0]);
        int menuNameIndex = FindColumnIndex(headerCells, "MenuName");
        if (menuNameIndex < 0)
        {
            Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: MenuName column was not found in {menuInfoResourcePath}.");
            return menuNames;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> cells = ParseCsvLine(lines[i]);
            if (menuNameIndex >= cells.Count)
            {
                continue;
            }

            string menuName = cells[menuNameIndex].Trim();
            if (!string.IsNullOrEmpty(menuName))
            {
                menuNames.Add(menuName);
            }
        }

        return menuNames;
    }

    private void ClearGeneratedButtons(RectTransform targetRoot)
    {
        if (targetRoot == null)
        {
            return;
        }

        for (int i = targetRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = targetRoot.GetChild(i);
            if (child.name.StartsWith(generatedButtonPrefix, StringComparison.Ordinal))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static int FindColumnIndex(List<string> headerCells, string columnName)
    {
        for (int i = 0; i < headerCells.Count; i++)
        {
            string header = headerCells[i].Trim().TrimStart('\uFEFF');
            if (string.Equals(header, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> cells = new List<string>();
        if (string.IsNullOrEmpty(line))
        {
            return cells;
        }

        StringBuilder cell = new StringBuilder();
        bool insideQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char current = line[i];
            if (current == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (current == ',' && !insideQuotes)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else
            {
                cell.Append(current);
            }
        }

        cells.Add(cell.ToString());
        return cells;
    }

    private static ScrollRect FindScrollRectByName(string targetName)
    {
        ScrollRect[] scrollRects = Resources.FindObjectsOfTypeAll<ScrollRect>();
        for (int i = 0; i < scrollRects.Length; i++)
        {
            ScrollRect scrollRect = scrollRects[i];
            if (scrollRect.gameObject.scene.IsValid() && scrollRect.name == targetName)
            {
                return scrollRect;
            }
        }

        return null;
    }

    private static CounterOrderController FindCounterOrderController()
    {
        CounterOrderController activeController = FindFirstObjectByType<CounterOrderController>();
        if (activeController != null)
        {
            return activeController;
        }

        CounterOrderController[] controllers = Resources.FindObjectsOfTypeAll<CounterOrderController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            CounterOrderController controller = controllers[i];
            if (controller.gameObject.scene.IsValid())
            {
                return controller;
            }
        }

        return null;
    }
}
