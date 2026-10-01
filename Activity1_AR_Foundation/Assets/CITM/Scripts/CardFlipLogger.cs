using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// SOLO PARA PRUEBAS: escribe en la consola cuándo se gira / tapa una carta.
/// Se puede borrar cuando el detector esté verificado.
/// </summary>
public class CardFlipLogger : MonoBehaviour
{
    [SerializeField] private CardFlipDetector detector;

    private void Awake()
    {
        if (detector == null) detector = FindAnyObjectByType<CardFlipDetector>();
    }

    private void OnEnable()
    {
        detector.OnCardFaceUp += HandleUp;
        detector.OnCardFaceDown += HandleDown;
    }

    private void OnDisable()
    {
        detector.OnCardFaceUp -= HandleUp;
        detector.OnCardFaceDown -= HandleDown;
    }

    private void HandleUp(ARTrackedImage img) =>
        Debug.Log($"[TEST] GIRADA: {img.referenceImage.name}");

    private void HandleDown(ARTrackedImage img) =>
        Debug.Log($"[TEST] BOCA ABAJO: {img.referenceImage.name}");
}