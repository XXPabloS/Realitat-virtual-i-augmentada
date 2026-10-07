using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Spawns a prefab on every detected image and hides it when the image
/// is not actively tracked. AR Foundation 6 API (trackablesChanged).
/// </summary>
public class TrackedImages : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager m_TrackedImageManager;
    [SerializeField] GameObject[] prefabsToSpawn;

    void Awake()
    {
        // Fallback if the reference was not assigned in the Inspector
        if (m_TrackedImageManager == null)
            m_TrackedImageManager = FindAnyObjectByType<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        if (m_TrackedImageManager != null)
            m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);
    }

    void OnDisable()
    {
        if (m_TrackedImageManager != null)
            m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
    }

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            string fullImageName = GetImageName(newImage);
            Debug.Log($"Image added: {fullImageName}");

            // Extraer el nombre de la base del archivo, convierte "Image1_A" a "Image1"
            string baseName = fullImageName;
            if (fullImageName.Contains("_"))
            {
                baseName = fullImageName.Split('_')[0];
            }

            // Calcular el indice del prefab
            int pkmIndex = -1;
            if (baseName.StartsWith("Image"))
            {
                string numberString = baseName.Replace("Image", "");
                if (int.TryParse(numberString, out int num))
                {
                    pkmIndex = num - 1; // "Image1" es indice 0
                }
            }

            // Instanciar el modelo
            if (pkmIndex >= 0 && pkmIndex < prefabsToSpawn.Length)
            {
                GameObject prefabToSpawn = prefabsToSpawn[pkmIndex];
                if (prefabToSpawn != null)
                {
                    var content = Instantiate(prefabToSpawn, newImage.transform);
                    content.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));

                    // Pongo esto aqui, se podria borrar si da error, pero se supone que sin esta linea
                    // al detectar la carta el pokemon aparece al 100 por cien de su tamano al detectar la imagen
                    // y luego el CardFlipDetector hace su animacion de crecer, con esto el pokemon nace invisible y hace lo de crecer.
                    content.gameObject.SetActive(false);
                }
            }
        }
    }

    // referenceImage.name is empty when the detected image does not match any
    // entry in the Reference Image Library (e.g. a Simulated Tracked Image with no texture)
    static string GetImageName(ARTrackedImage image)
    {
        return string.IsNullOrEmpty(image.referenceImage.name)
            ? $"<unnamed, id {image.trackableId}>"
            : image.referenceImage.name;
    }
}