using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject menuUI;
    public GameObject tutorialUI;

    [Header("Game Scene")]
    public string gameSceneName = "GameScene";

    // Called by the Start button
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Called by the Tutorial button
    public void ShowTutorial()
    {
        menuUI.SetActive(false);
        tutorialUI.SetActive(true);
    }

    // Optional: Call this from a "Back" button in the tutorial
    public void HideTutorial()
    {
        tutorialUI.SetActive(false);
        menuUI.SetActive(true);
    }
}