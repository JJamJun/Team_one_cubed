using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MenuInfoProgramButtonBuilder : MonoBehaviour
{
    [SerializeField] private string menuInfoResourcePath = "menu_info";
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private CounterOrderController orderController;
    [SerializeField] private Vector2 spacing = new Vector2(18f, 14f);
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
            orderController = FindFirstObjectByType<CounterOrderController>();
        }

        List<string> menuNames = LoadMenuNames();
        ClearGeneratedButtons();

        for (int i = 0; i < menuNames.Count; i++)
        {
            CreateButton(menuNames[i], i);
        }
    }

    private void CreateButton(string menuName, int index)
    {
        GameObject buttonObject = Instantiate(buttonPrefab, transform);
        buttonObject.name = $"{generatedButtonPrefix}{index}_{menuName}";

        RectTransform buttonRect = buttonObject.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.localScale = Vector3.one;

            Vector2 prefabSize = GetPrefabButtonSize(buttonRect);
            Vector2Int gridPosition = GetGridPosition(index, prefabSize);
            buttonRect.anchoredPosition = GetAnchoredPosition(gridPosition, prefabSize);
        }

        CounterMenuButton menuButton = buttonObject.GetComponent<CounterMenuButton>();
        if (menuButton != null)
        {
            menuButton.Initialize(orderController, menuName);
            return;
        }

        Debug.LogWarning($"{nameof(MenuInfoProgramButtonBuilder)}: Generated prefab '{buttonPrefab.name}' does not have a {nameof(CounterMenuButton)} component.");
    }

    private Vector2 GetPrefabButtonSize(RectTransform buttonRect)
    {
        Vector2 size = buttonRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = buttonRect.sizeDelta;
        }

        return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    }

    private Vector2Int GetGridPosition(int index, Vector2 buttonSize)
    {
        RectTransform rootRect = transform as RectTransform;
        float availableWidth = rootRect != null
            ? Mathf.Max(0f, rootRect.rect.width - padding.left - padding.right)
            : buttonSize.x;

        float strideX = buttonSize.x + spacing.x;
        int columns = strideX > 0f
            ? Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing.x) / strideX))
            : 1;

        return new Vector2Int(index % columns, index / columns);
    }

    private Vector2 GetAnchoredPosition(Vector2Int gridPosition, Vector2 buttonSize)
    {
        return new Vector2(
            padding.left + gridPosition.x * (buttonSize.x + spacing.x),
            -padding.top - gridPosition.y * (buttonSize.y + spacing.y));
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

    private void ClearGeneratedButtons()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
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
}
