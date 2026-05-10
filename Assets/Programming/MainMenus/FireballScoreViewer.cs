using System.Collections;
using MainGame;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireballScoreViewer : MonoBehaviour
{
    [SerializeField] private TowerGenerator towerGenerator;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text towerBlocksText;
    [SerializeField] private string towerBlocksFormat = "{0}/{1}";

    [Header("Auto Find")]
    [SerializeField] private bool autoFindMissingReferences = true;
    [SerializeField] private string scoreTextName = "Score";
    [SerializeField] private string towerBlocksTextName = "TowerBlocks";

    private TowerGenerator subscribedTowerGenerator;
    private Coroutine rebindCoroutine;

    private void Awake()
    {
        RebindReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RebindAndDisplay();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromTower();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (rebindCoroutine != null)
            StopCoroutine(rebindCoroutine);

        rebindCoroutine = StartCoroutine(RebindAfterSceneStart());
    }

    public void Display(int score)
    {
        EnsureTexts();

        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public void DisplayTowerProgress()
    {
        int totalBlocks = GetTotalBlocks();
        int remainingBlocks = GetRemainingBlocks(totalBlocks);

        DisplayTowerProgress(remainingBlocks, totalBlocks);
    }

    private IEnumerator RebindAfterSceneStart()
    {
        yield return null;
        RebindAndDisplay();

        yield return null;
        RebindAndDisplay();

        rebindCoroutine = null;
    }

    private void RebindAndDisplay()
    {
        RebindReferences();
        SubscribeToTower();

        Display(towerGenerator != null ? towerGenerator.GetCurrentScore() : 0);
        DisplayTowerProgress();
    }

    private void RebindReferences()
    {
        if (!autoFindMissingReferences)
            return;

        if (towerGenerator == null)
            towerGenerator = FindFirstObjectByType<TowerGenerator>();

        EnsureTexts();
    }

    private void EnsureTexts()
    {
        if (!autoFindMissingReferences)
            return;

        if (scoreText == null)
            scoreText = FindText(scoreTextName, "ScoreText", "Score Text");

        if (towerBlocksText == null)
            towerBlocksText = FindText(towerBlocksTextName, "Tower Blocks", "TowerBlocksText", "Tower Blocks Text", "TowerProgress", "Tower Progress", "BlocksText");
    }

    private TMP_Text FindText(params string[] names)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (string targetName in names)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                continue;

            foreach (TMP_Text text in texts)
            {
                if (text != null && string.Equals(text.gameObject.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                    return text;
            }
        }

        return null;
    }

    private void SubscribeToTower()
    {
        if (subscribedTowerGenerator == towerGenerator)
            return;

        UnsubscribeFromTower();

        if (towerGenerator == null)
            return;

        towerGenerator.OnScoreChanged += Display;
        towerGenerator.OnTowerGenerated += DisplayTowerGenerated;
        towerGenerator.OnBlockDestroyed += DisplayBlocksDestroyed;
        subscribedTowerGenerator = towerGenerator;
    }

    private void UnsubscribeFromTower()
    {
        if (subscribedTowerGenerator == null)
            return;

        subscribedTowerGenerator.OnScoreChanged -= Display;
        subscribedTowerGenerator.OnTowerGenerated -= DisplayTowerGenerated;
        subscribedTowerGenerator.OnBlockDestroyed -= DisplayBlocksDestroyed;
        subscribedTowerGenerator = null;
    }

    private void DisplayTowerGenerated(int totalBlocks)
    {
        DisplayTowerProgress(totalBlocks, totalBlocks);
    }

    private void DisplayBlocksDestroyed(int destroyedBlocks)
    {
        int totalBlocks = GetTotalBlocks();
        int remainingBlocks = Mathf.Max(0, totalBlocks - destroyedBlocks);

        DisplayTowerProgress(remainingBlocks, totalBlocks);
    }

    private void DisplayTowerProgress(int remainingBlocks, int totalBlocks)
    {
        EnsureTexts();

        if (towerBlocksText == null)
            return;

        towerBlocksText.text = string.Format(towerBlocksFormat, remainingBlocks, totalBlocks);
    }

    private int GetTotalBlocks()
    {
        if (towerGenerator != null)
        {
            int totalBlocks = towerGenerator.GetTotalBlocks();
            if (totalBlocks > 0)
                return totalBlocks;
        }

        return SessionManager.Instance != null ? SessionManager.Instance.towerHeight : 0;
    }

    private int GetRemainingBlocks(int totalBlocks)
    {
        if (towerGenerator != null)
        {
            int remainingBlocks = towerGenerator.GetRemainingBlocks();
            if (remainingBlocks > 0 || totalBlocks > 0)
                return remainingBlocks;
        }

        return totalBlocks;
    }
}
