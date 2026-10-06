using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// mira que cartas se ven y avisar cuando una pasa de tapada a girada o al reves
/// Solo lanza eventos: los efectos de aparición/desaparición y la lógica de parejas
/// se añaden suscribiéndose a ellos.
/// Va en el mismo GameObject que el ARTrackedImageManager.

[RequireComponent(typeof(ARTrackedImageManager))]
public class CardFlipDetector : MonoBehaviour
{
    [SerializeField] private float hideDelay = 0.5f;

    /// EVENTO Se lanza una vez cuando la carta se gira
    public event Action<ARTrackedImage> OnCardFaceUp;

    /// EVENTO Se lanza una vez cuando la carta vuelve a estar boca abajo
    public event Action<ARTrackedImage> OnCardFaceDown;

    private ARTrackedImageManager manager;
    private readonly Dictionary<TrackableId, ARTrackedImage> faceUp = new();
    private readonly Dictionary<TrackableId, float> lastSeen = new();
    private readonly List<TrackableId> toHide = new();

    private void Awake() => manager = GetComponent<ARTrackedImageManager>();

    private void Update()
    {
        foreach (var img in manager.trackables)
        {
            if (img.trackingState != TrackingState.Tracking) continue;

            // Espera a que TrackedImages haya instanciado el prefab (hijo de la imagen),esto en vd se puede quitar facil cuando sepamos que efectos y como pero lo dejo puesto ahora
            if (img.transform.childCount == 0) continue;

            lastSeen[img.trackableId] = Time.time;

            if (!faceUp.ContainsKey(img.trackableId))
            {
                faceUp[img.trackableId] = img;
                OnCardFaceUp?.Invoke(img);
            }
        }

        toHide.Clear();
        foreach (var kv in faceUp)
            if (kv.Value == null || Time.time - lastSeen[kv.Key] > hideDelay)
                toHide.Add(kv.Key);

        foreach (var id in toHide)
        {
            var img = faceUp[id];
            faceUp.Remove(id);
            lastSeen.Remove(id);
            OnCardFaceDown?.Invoke(img);
        }
    }

    public void ResetDetector()
    {
        faceUp.Clear();
        lastSeen.Clear();
        toHide.Clear();
    }
}