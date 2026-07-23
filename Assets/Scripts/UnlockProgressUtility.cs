using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UnlockProgressUtility
{
    public static int CountUnlockedMenusExcludingDefault(string saveFileName, string defaultMenuId = "0")
    {
        MenuUnlockSaveData saveData = LoadUnlockData(saveFileName);
        int count = 0;

        for (int i = 0; i < saveData.menus.Count; i++)
        {
            MenuUnlockState state = saveData.menus[i];
            if (state == null
                || state.menuID == defaultMenuId
                || !state.isUnlocked
                || state.level <= 0)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public static string GetUnlockSavePath(string saveFileName)
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private static MenuUnlockSaveData LoadUnlockData(string saveFileName)
    {
        string savePath = GetUnlockSavePath(saveFileName);
        if (!File.Exists(savePath))
        {
            return new MenuUnlockSaveData();
        }

        try
        {
            MenuUnlockSaveData saveData = JsonUtility.FromJson<MenuUnlockSaveData>(File.ReadAllText(savePath));
            if (saveData == null)
            {
                saveData = new MenuUnlockSaveData();
            }

            saveData.EnsureInitialized();
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(UnlockProgressUtility)}: Failed to load {savePath}. {exception.Message}");
            return new MenuUnlockSaveData();
        }
    }

    [Serializable]
    private class MenuUnlockSaveData
    {
        public List<MenuUnlockState> menus = new List<MenuUnlockState>();

        public void EnsureInitialized()
        {
            if (menus == null)
            {
                menus = new List<MenuUnlockState>();
            }
        }
    }

    [Serializable]
    private class MenuUnlockState
    {
        public string menuID;
        public bool isUnlocked;
        public int level;
    }
}
