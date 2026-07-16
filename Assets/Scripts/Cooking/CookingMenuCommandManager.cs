using System.Collections.Generic;
using UnityEngine;

public class CookingMenuCommandManager : MonoBehaviour
{
    [SerializeField] private List<MenuArrowCommandSetting> menuArrowCommands = new List<MenuArrowCommandSetting>();

    public bool TryGetCommandSequence(string menuName, out string commandSequence)
    {
        for (int i = 0; i < menuArrowCommands.Count; i++)
        {
            MenuArrowCommandSetting commandSetting = menuArrowCommands[i];
            if (commandSetting == null || commandSetting.MenuName != menuName)
            {
                continue;
            }

            commandSequence = commandSetting.CommandSequence;
            return !string.IsNullOrWhiteSpace(commandSequence);
        }

        commandSequence = string.Empty;
        return false;
    }
}
