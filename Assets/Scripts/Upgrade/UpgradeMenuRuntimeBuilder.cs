using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuRuntimeBuilder : MonoBehaviour
{
    [SerializeField] private string recipeResourcePath = "temp_recipe";
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject upgradeButtonPrefab;
    [SerializeField, Min(0f)] private float buttonSpacing = 95f;
    [SerializeField] private string generatedButtonPrefix = "UpgradeButton_";

    private void Awake()
    {
        BuildButtons();
    }

    public void BuildButtons()
    {
        if (contentRoot == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Content root is not assigned.");
            return;
        }

        if (upgradeButtonPrefab == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Upgrade button prefab is not assigned.");
            return;
        }

        ApplyButtonSpacing();
        ClearExistingButtons();

        foreach (string menuName in LoadMenuNames())
        {
            GameObject buttonObject = Instantiate(upgradeButtonPrefab, contentRoot);
            buttonObject.name = $"{generatedButtonPrefix}{menuName}";
            buttonObject.SetActive(true);

            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = menuName;
            }
        }
    }

    private void OnValidate()
    {
        ApplyButtonSpacing();
    }

    private void ApplyButtonSpacing()
    {
        if (contentRoot == null)
        {
            return;
        }

        GridLayoutGroup layoutGroup = contentRoot.GetComponent<GridLayoutGroup>();
        if (layoutGroup == null)
        {
            return;
        }

        layoutGroup.spacing = new Vector2(buttonSpacing, layoutGroup.spacing.y);
    }

    private void ClearExistingButtons()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child.name.StartsWith(generatedButtonPrefix, StringComparison.Ordinal))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private List<string> LoadMenuNames()
    {
        List<string> menuNames = new List<string>();
        TextAsset recipeAsset = Resources.Load<TextAsset>(recipeResourcePath);
        if (recipeAsset == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Resources/{recipeResourcePath} was not found.");
            return menuNames;
        }

        string[] lines = recipeAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            string menuName = GetFirstCsvCell(rawLine).Trim().TrimStart('\uFEFF');
            if (!string.IsNullOrEmpty(menuName))
            {
                menuNames.Add(menuName);
            }
        }

        return menuNames;
    }

    private static string GetFirstCsvCell(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        if (line[0] != '"')
        {
            int commaIndex = line.IndexOf(',');
            int tabIndex = line.IndexOf('\t');
            int separatorIndex = GetFirstSeparatorIndex(commaIndex, tabIndex);
            return separatorIndex >= 0 ? line.Substring(0, separatorIndex) : line;
        }

        for (int i = 1; i < line.Length; i++)
        {
            if (line[i] != '"')
            {
                continue;
            }

            bool escapedQuote = i + 1 < line.Length && line[i + 1] == '"';
            if (escapedQuote)
            {
                i++;
                continue;
            }

            return line.Substring(1, i - 1).Replace("\"\"", "\"");
        }

        return line.Trim('"');
    }

    private static int GetFirstSeparatorIndex(int commaIndex, int tabIndex)
    {
        if (commaIndex < 0)
        {
            return tabIndex;
        }

        if (tabIndex < 0)
        {
            return commaIndex;
        }

        return Mathf.Min(commaIndex, tabIndex);
    }
}
