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

        if (m_TrackedImageManager == null)
        {
            Debug.LogError("TrackedImages: no ARTrackedImageManager found in the scene.");
            enabled = false;
        }
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
            string imageName = GetImageName(newImage);
            Debug.Log($"Image added: {imageName}");
            GameObject prefabToSpawn = null;

            switch (imageName)
            {
                case "Image1":
                    prefabToSpawn = prefabsToSpawn[0];
                    break;
                case "Image2":
                    prefabToSpawn = prefabsToSpawn[1];
                    break;
                case "Image3":
                    prefabToSpawn = prefabsToSpawn[2];
                    break;
                case "Image4":
                    prefabToSpawn = prefabsToSpawn[3];
                    break;
                case "Image5":
                    prefabToSpawn = prefabsToSpawn[4];
                    break;
                case "Image6":
                    prefabToSpawn = prefabsToSpawn[5];
                    break;
                case "Image7":
                    prefabToSpawn = prefabsToSpawn[6];
                    break;
                case "Image8":
                    prefabToSpawn = prefabsToSpawn[7];
                    break;
                case "Image9":
                    prefabToSpawn = prefabsToSpawn[8];
                    break;
                case "Image10":
                    prefabToSpawn = prefabsToSpawn[9];
                    break;
                case "Image11":
                    prefabToSpawn = prefabsToSpawn[10];
                    break;
                case "Image12":
                    prefabToSpawn = prefabsToSpawn[11];
                    break;
                case "Image13":
                    prefabToSpawn = prefabsToSpawn[12];
                    break;
                case "Image14":
                    prefabToSpawn = prefabsToSpawn[13];
                    break;
                case "Image15":
                    prefabToSpawn = prefabsToSpawn[14];
                    break;
                case "Image16":
                    prefabToSpawn = prefabsToSpawn[15];
                    break;
                case "Image17":
                    prefabToSpawn = prefabsToSpawn[16];
                    break;
                case "Image18":
                    prefabToSpawn = prefabsToSpawn[17];
                    break;
            }

            if (prefabToSpawn == null) continue;
            var content = Instantiate(prefabToSpawn, newImage.transform); // child: follows the image
            content.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            // Hide content when the image is not actively tracked (Limited / None)
            //bool visible = updatedImage.trackingState == TrackingState.Tracking;
            //foreach (Transform child in updatedImage.transform)
            //    child.gameObject.SetActive(visible);
        }

        foreach (var pair in eventArgs.removed)
        {
            Debug.Log($"Image removed: {GetImageName(pair.Value)}");
            // Children are destroyed along with the ARTrackedImage GameObject
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
