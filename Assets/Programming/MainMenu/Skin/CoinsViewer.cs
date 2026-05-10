using TMPro;
using UnityEngine;

public class CoinsViewer : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;

    public void Display(int coins)
    {
        if (coinsText != null)
            coinsText.text = coins.ToString();
    }
}
