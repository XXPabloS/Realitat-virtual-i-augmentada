using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject menuUI;
    public GameObject tutorialUI;

    [Header("Game Scene")]
    public string gameSceneName = "GameScene";

    [Header("Menu Music")]
    [SerializeField] private AudioSource menuMusic;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;

    private bool isStartingGame;

    public void StartGame()
    {
        if (isStartingGame) return;
        isStartingGame = true;
        StartCoroutine(FadeOutAndStartGame());
    }

    private IEnumerator FadeOutAndStartGame()
    {
        if (menuMusic != null && menuMusic.isPlaying)
        {
            float initialVolume = menuMusic.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                menuMusic.volume = Mathf.Lerp(initialVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            menuMusic.volume = 0f;
            menuMusic.Stop();
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowTutorial()
    {
        menuUI.SetActive(false);
        tutorialUI.SetActive(true);
    }

    public void HideTutorial()
    {
        tutorialUI.SetActive(false);
        menuUI.SetActive(true);
    }
}