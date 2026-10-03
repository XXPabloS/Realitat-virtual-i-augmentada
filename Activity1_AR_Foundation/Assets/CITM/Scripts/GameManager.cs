using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    [Header("AR Reference")]
    public CardFlipDetector flipDetector;

    [Header("UI Panels")]
    public GameObject setupPanel;
    public GameObject gamePanel;
    public GameObject winPanel;

    [Header("UI Texts")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI instructionsText;

    private int totalPairs = 0;
    private int matchedPairs = 0;

    // Variables para guardar los IDs de las cartas que se han levantado
    private string firstCardId = null;
    private string firstCardName = null;
    private string secondCardId = null;
    private string secondCardName = null;

    // Lista para guardar las cartas que ya tienen pareja
    private HashSet<string> matchedCards = new HashSet<string>();

    // Bloquea el turno si fallas y te obliga a girar las dos cartas
    private bool isWaitingForReset = false;

    private void OnEnable()
    {
        // Suscribirse al evento del flipDetector
        if (flipDetector != null)
        {
            flipDetector.OnCardFaceUp += HandlePhysicalCardUp;
            flipDetector.OnCardFaceDown += HandlePhysicalCardDown;
        }
    }

    private void OnDisable()
    {
        // Desuscribirse del evento al apagar el objeto
        if (flipDetector != null)
        {
            flipDetector.OnCardFaceUp -= HandlePhysicalCardUp;
            flipDetector.OnCardFaceDown -= HandlePhysicalCardDown;
        }
    }

    private void Start()
    {
        setupPanel.SetActive(true);
        gamePanel.SetActive(false);
        winPanel.SetActive(false);
    }

    public void StartGameWithCards(int totalCards)
    {
        // Botones de UI metodos
        totalPairs = totalCards / 2;
        matchedPairs = 0;
        matchedCards.Clear();
        firstCardId = null;
        secondCardId = null;
        isWaitingForReset = false;

        setupPanel.SetActive(false);
        gamePanel.SetActive(true);

        UpdateHUD();
        instructionsText.text = "Flip the cards and find the matches!";
    }

    public void RestartGame()
    {
        // Resetear contadores y HUD
        totalPairs = 0;
        matchedPairs = 0;
        matchedCards.Clear();
        firstCardId = null;
        secondCardId = null;
        isWaitingForReset = false;

        // Borrar si habian cartas virtuales, modo debug del ordenador
        VirtualCardDebugger debugger = FindAnyObjectByType<VirtualCardDebugger>();
        if (debugger != null)
        {
            debugger.ClearVirtualCards();
        }

        // Pantalla incial default
        winPanel.SetActive(false);
        gamePanel.SetActive(false);
        setupPanel.SetActive(true);
    }

    public void GoToMenu()
    {
        // Por si metemos un boton que vaya al menu
        SceneManager.LoadScene("MenuScene");
    }

    private void HandlePhysicalCardUp(ARTrackedImage img)
    {
        // Si las gafas o el movil detecta la imagen fisica
        ProcessCardFlippedUp(img.trackableId.ToString(), img.referenceImage.name);
    }

    private void HandlePhysicalCardDown(ARTrackedImage img)
    {
        ProcessCardFlippedDown(img.trackableId.ToString());
    }

    // Comprobar si se permite girar una carta
    public bool CanFlipNewCard(string cardId)
    {
        if (totalPairs == 0) return false;
        if (matchedCards.Contains(cardId)) return false;

        // Bloquea que se pueda girar nada si esta esperando
        if (isWaitingForReset) return false;

        // Si ya hay dos levantadas, NO se puede levantar una tercera.
        if (firstCardId != null && secondCardId != null) return false;

        return true;
    }

    // Detectar si son pareja
    public void ProcessCardFlippedUp(string cardId, string cardName)
    {
        // Si aun no ha empezado el juego pero hay dos cartas ya levantadas se ignora
        if (totalPairs == 0 || matchedCards.Contains(cardId)) return;
        // Si ya hay dos cartas levantadas no pilla la tercera para hacer el check
        if (isWaitingForReset || (firstCardId != null && secondCardId != null))
        {
            // Avisa al jugador de que no sirve de nada girar cartas porque esta esperando que gires esas dos cartas
            instructionsText.text = "Turn the previous cards face down first!";
            return;
        }

        // Entra aqui si es la primera carta que se levanta en el turno o si es la logica normal
        if (firstCardId == null)
        {
            firstCardId = cardId;
            firstCardName = cardName;
            instructionsText.text = "Where is its match?";
        }
        // Si es la segunda carta del turno y además no es la misma id que la primera que la ha girado y vuelto a levantar
        else if (secondCardId == null && cardId != firstCardId)
        {
            secondCardId = cardId;
            secondCardName = cardName;

            // Ver si son iguales
            CheckForMatch();
        }
    }

    // Girar la carta hacia abajo, parte de atrás hacia arriba
    public void ProcessCardFlippedDown(string cardId)
    {
        // Limpiar el hueco de esa carta en la memoria
        if (firstCardId == cardId) firstCardId = null;
        if (secondCardId == cardId) secondCardId = null;

        // Si el jugador ha girado AMBAS cartas entonces se levanta el bloquea y deja girar de nuevo
        if (firstCardId == null && secondCardId == null)
        {
            isWaitingForReset = false;

            if (matchedPairs < totalPairs)
            {
                instructionsText.text = "Flip a card...";
            }
        }
    }

    private void CheckForMatch()
    {
        if (firstCardName == secondCardName)
        {
            // Si son iguales se meten en la lista de resueltas para que no vuelvan a contar
            matchedCards.Add(firstCardId);
            matchedCards.Add(secondCardId);
            matchedPairs++;
            // Aumentar el marcador en el HUD
            UpdateHUD();

            // Limpiar el hueco de memoria
            firstCardId = null;
            secondCardId = null;
            isWaitingForReset = false;

            // Comporbar si ya has ganado
            if (matchedPairs >= totalPairs)
            {
                WinGame();
            }
            else
            {
                instructionsText.text = "Match found! Keep searching.";
            }
        }
        else
        {
            // Si no son iguales
            instructionsText.text = "Mismatch! Turn both face down.";
            isWaitingForReset = true;
        }
    }

    private void UpdateHUD()
    {
        // Actualizar el Score
        scoreText.text = $"Matches: {matchedPairs} / {totalPairs}";
    }

    private void WinGame()
    {
        gamePanel.SetActive(false);
        winPanel.SetActive(true);
    }
}