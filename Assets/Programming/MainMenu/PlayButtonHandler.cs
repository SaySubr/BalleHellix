using UnityEngine;
using MainMenu;

/// <summary>
/// Обработчик кнопки "Играть" в MainMenu.
/// Запускает последний открытый уровень.
/// </summary>
public class PlayButtonHandler : MonoBehaviour
{
    /// <summary>
    /// Вызывается при клике на кнопку "Играть"
    /// </summary>
    public void OnClick_Play()
    {
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.LaunchLastUnlockedLevel();
        }
        else
        {
            Debug.LogError("PlayButtonHandler: GameLauncher.Instance не найден!");
        }
    }
}
