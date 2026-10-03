using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject menuUI;
    public GameObject tutorialUI;

    [Header("Game Scene")]
    public string gameSceneName = "GameScene";

    public void StartGame()
    {
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