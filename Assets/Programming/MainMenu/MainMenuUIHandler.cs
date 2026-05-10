using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIHandler : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int indexGameScene = 3;
    [SerializeField] private int indexStore = 3;

    [Header("Anim Settings")]
    [SerializeField] private Animator vfxAnimator;
    [SerializeField] private float animStartDuration;

    private Coroutine _loadSceneCoroutine;

    public void OnClick_StartGame()
    {
        StartLoad(indexGameScene);
    }

    public void OnClick_ToStore()
    {
        StartLoad(indexStore);
    }

    private void StartLoad(int indexLevel)
    {
        vfxAnimator.SetTrigger("StartGame");

        if (_loadSceneCoroutine != null)
            StopCoroutine(_loadSceneCoroutine);

        _loadSceneCoroutine = StartCoroutine(LoadSceneRoutine(indexLevel));
    }

    private IEnumerator LoadSceneRoutine(int indexLevel)
    {
        yield return new WaitForSeconds(animStartDuration);
        SceneManager.LoadScene(indexLevel);
    }
}
