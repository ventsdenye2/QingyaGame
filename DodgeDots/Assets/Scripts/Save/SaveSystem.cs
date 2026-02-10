using System;
using System.IO;
using UnityEngine;

namespace DodgeDots.Save
{
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private static SaveData _current;

        public static SaveData Current => _current;

        public static bool HasSave => File.Exists(GetSavePath());

        public static void LoadOrCreate()
        {
            if (HasSave)
            {
                Load();
                return;
            }
            _current = new SaveData();
        }

        public static void NewGame()
        {
            _current = new SaveData();
        }

        public static void Load()
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                _current = new SaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                _current = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch
            {
                _current = new SaveData();
            }
        }

        public static void Save()
        {
            if (_current == null)
            {
                _current = new SaveData();
            }

            _current.savedAtUtc = DateTime.UtcNow.ToString("o");

            try
            {
                string json = JsonUtility.ToJson(_current, prettyPrint: true);
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Save failed: {ex.Message}");
            }
        }

        public static void SetFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            LoadOrCreate();

            if (!_current.gameFlags.Contains(flagKey))
            {
                _current.gameFlags.Add(flagKey);
                Save(); // ×Ô¶¯±£´æ
                Debug.Log($"[SaveSystem] Flag Set: {flagKey}");
            }
        }

        public static void RemoveFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            LoadOrCreate();

            if (_current.gameFlags.Contains(flagKey))
            {
                _current.gameFlags.Remove(flagKey);
                Save();
            }
        }

        public static bool HasFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return false;
            LoadOrCreate();
            return _current.gameFlags.Contains(flagKey);
        }

        public static void Clear()
        {
            _current = new SaveData();
            string path = GetSavePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }
    }
}
