using UnityEngine;
using Data;

/// <summary>
/// Отладка сохранений.
/// Вешается на любой GameObject в сцене.
/// </summary>
public class SaveDebug : MonoBehaviour
{
    
    [Tooltip("Нажми чтобы открыть папку с сохранениями")]
    [ContextMenu("Открыть папку с сохранениями")]
    public void OpenFolder()
    {
        SaveHelper.OpenSaveFolder();
    }

    [Tooltip("Нажми чтобы открыть JSON в блокноте")]
    [ContextMenu("Открыть JSON в блокноте")]
    public void OpenJson()
    {
        SaveHelper.OpenSaveFile();
    }

    [Tooltip("Нажми чтобы вывести JSON в консоль")]
    [ContextMenu("Вывести JSON в консоль")]
    public void PrintJson()
    {
        SaveHelper.PrintSaveToConsole();
    }

    [Tooltip("Нажми чтобы удалить сохранение")]
    [ContextMenu("Удалить сохранение")]
    public void DeleteSave()
    {
        if (DataController.Instance != null)
        {
            DataController.Instance.DeleteSave();
        }
    }

    private void OnGUI()
    {
        // Кнопки в игре для быстрого доступа
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("💾 Отладка сохранений");

        if (GUILayout.Button("Открыть папку"))
        {
            SaveHelper.OpenSaveFolder();
        }

        if (GUILayout.Button("Открыть JSON"))
        {
            SaveHelper.OpenSaveFile();
        }

        if (GUILayout.Button("В консоль"))
        {
            SaveHelper.PrintSaveToConsole();
        }

        if (GUILayout.Button("Удалить"))
        {
            if (DataController.Instance != null)
            {
                DataController.Instance.DeleteSave();
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
