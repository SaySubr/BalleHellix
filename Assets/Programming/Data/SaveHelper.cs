using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace Data
{
    /// <summary>
    /// Помощник для работы с сохранениями.
    /// </summary>
    public static class SaveHelper
    {
        /// <summary>
        /// Открыть папку с сохранениями в проводнике
        /// </summary>
        public static void OpenSaveFolder()
        {
            string savePath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (Directory.Exists(savePath))
            {
                // Открываем папку в проводнике
                Process.Start(savePath);
                UnityEngine.Debug.Log($"📂 Открыта папка: {savePath}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"❌ Папка не найдена: {savePath}");
            }
        }

        /// <summary>
        /// Открыть JSON файл в блокноте
        /// </summary>
        public static void OpenSaveFile()
        {
            string savePath = Path.Combine(Application.persistentDataPath, "Saves", "savegame.json");
            
            if (File.Exists(savePath))
            {
                // Открываем в блокноте
                Process.Start("notepad.exe", savePath);
                UnityEngine.Debug.Log($"📄 Открыт файл: {savePath}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"❌ Файл не найден: {savePath}");
            }
        }

        /// <summary>
        /// Прочитать JSON и вывести в консоль
        /// </summary>
        public static void PrintSaveToConsole()
        {
            string savePath = Path.Combine(Application.persistentDataPath, "Saves", "savegame.json");
            
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                UnityEngine.Debug.Log("📄 JSON сохранение:\n" + json);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"❌ Файл не найден: {savePath}");
            }
        }
    }
}
