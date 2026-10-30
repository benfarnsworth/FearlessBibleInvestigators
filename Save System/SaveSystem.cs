using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"savegame_slot_{slotIndex}.json");
    }

    private static string GetTempFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"savegame_slot_{slotIndex}.tmp");
    }

    public static void SaveGame(SaveData data, int slotIndex = 0)
    {
        try
        {
            string finalPath = GetSaveFilePath(slotIndex);
            string tempPath = GetTempFilePath(slotIndex);

            string json = JsonUtility.ToJson(data, true);

            // Write to temporary file first to prevent file corruption on crash
            File.WriteAllText(tempPath, json);

            // Safe atomic-like replacement using File.Copy over finalPath instead of deleting first
            File.Copy(tempPath, finalPath, true);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            Debug.Log($"[SAVE SYSTEM] Game saved successfully to Slot {slotIndex}: {finalPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE SYSTEM] Failed to save game (Slot {slotIndex}): {e.Message}");
        }
    }

    public static SaveData LoadGame(int slotIndex = 0)
    {
        string path = GetSaveFilePath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SAVE SYSTEM] No save file found for Slot {slotIndex}!");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SAVE SYSTEM] Game loaded successfully from Slot {slotIndex}!");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE SYSTEM] Failed to load save file (Slot {slotIndex}): {e.Message}");
            return null;
        }
    }

    public static bool HasSaveFile(int slotIndex = 0)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    public static void DeleteSaveFile(int slotIndex = 0)
    {
        string path = GetSaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SAVE SYSTEM] Save file deleted for Slot {slotIndex}.");
        }
    }
}