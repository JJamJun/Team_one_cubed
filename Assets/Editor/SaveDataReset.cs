using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDataResetTool
{
    private static readonly string WalletFilePath = Path.Combine(Application.persistentDataPath, "player_wallet.json");
    private static readonly string UpgradesFilePath = Path.Combine(Application.persistentDataPath, "player_upgrades.json");

    [MenuItem("Tools/Save Reset/Wallet")]
    public static void ResetWallet()
    {
        DeleteFile(WalletFilePath, "Wallet");
    }

    [MenuItem("Tools/Save Reset/Upgrades")]
    public static void ResetUpgrades()
    {
        DeleteFile(UpgradesFilePath, "Upgrades");
    }

    [MenuItem("Tools/Save Reset/All")]
    public static void ResetAll()
    {
        Debug.Log("--- Starting Full Save Reset ---");
        ResetWallet();
        ResetUpgrades();
        Debug.Log("<b>All save data</b> has been completely wiped.");
    }


    private static void DeleteFile(string path, string saveName)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<b>{saveName}</b> file deleted at {path}");
        }
        else
        {
            Debug.LogWarning($"No file called <b>{saveName}</b> exists at {path}");
        }
    }
}