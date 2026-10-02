using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// Animación de aparición y desaparición del modelo de la carta

public class CardAppearEffect : MonoBehaviour
{
    [SerializeField] private CardFlipDetector detector;
    [SerializeField] private ParticleSystem particlesPrefab;
    [SerializeField] private float appearTimePerStep = 0.18f;
    [SerializeField] private float disappearTimePerStep = 0.15f;

    // Forma del modelo en cada paso, como multiplicador de su tamaño normal
    private static readonly Vector3[] appearSteps =
    {
        new Vector3(0.4f, 0.001f, 0.4f),
        new Vector3(0.85f, 1.25f, 0.85f),
        new Vector3(1.1f, 0.9f, 1.1f),
        Vector3.one
    };

    private static readonly Vector3[] disappearSteps =
    {
        new Vector3(1.1f, 0.9f, 1.1f),
        new Vector3(0.85f, 1.25f, 0.85f),
        new Vector3(0.4f, 0.001f, 0.4f)
    };

    private readonly Dictionary<Transform, Vector3> baseScales = new();
    private readonly Dictionary<Transform, Coroutine> running = new();

    private void Awake()
    {
        if (detector == null) detector = FindAnyObjectByType<CardFlipDetector>(); // por siaca
    }

    private void OnEnable()
    {
        detector.OnCardFaceUp += OnFaceUp;
        detector.OnCardFaceDown += OnFaceDown;
    }

    private void OnDisable()
    {
        detector.OnCardFaceUp -= OnFaceUp;
        detector.OnCardFaceDown -= OnFaceDown;
    }

    private void OnFaceUp(ARTrackedImage img)
    {
        if (img.transform.childCount == 0) return;
        Transform model = img.transform.GetChild(0);

        // El tamaño real se guarda solo la primera vez
        if (!baseScales.ContainsKey(model))
            baseScales[model] = model.localScale;

        model.gameObject.SetActive(true);

        if (particlesPrefab != null)
        {
            var ps = Instantiate(particlesPrefab, img.transform.position, img.transform.rotation);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        Play(model, appear: true);
    }

    private void OnFaceDown(ARTrackedImage img)
    {
        if (img == null || img.transform.childCount == 0) return;
        Transform model = img.transform.GetChild(0);

        if (baseScales.ContainsKey(model))
            Play(model, appear: false);
    }

    private void Play(Transform model, bool appear)
    {
        // Si había otra animación en este modelo, se corta y la nueva sigue desde la forma actual
        if (running.TryGetValue(model, out var current))
            StopCoroutine(current);

        running[model] = StartCoroutine(Animate(model, appear));
    }

    private IEnumerator Animate(Transform model, bool appear)
    {
        Vector3[] steps = appear ? appearSteps : disappearSteps;
        float timePerStep = appear ? appearTimePerStep : disappearTimePerStep;
        Vector3 baseScale = baseScales[model];

        // Aparicion: arranca en el primer paso. Desaparición: arranca desde la forma actual.
        Vector3 from = steps[0];
        if (!appear)
        {
            Vector3 cur = model.localScale;
            from = new Vector3(cur.x / baseScale.x, cur.y / baseScale.y, cur.z / baseScale.z);
        }
        else
        {
            model.localScale = Vector3.Scale(baseScale, from);
        }

        for (int i = appear ? 1 : 0; i < steps.Length; i++)
        {
            Vector3 to = steps[i];

            for (float t = 0f; t < timePerStep; t += Time.deltaTime)
            {
                if (model == null)
                {
                    running.Remove(model);
                    yield break;
                }

                float k = Mathf.SmoothStep(0f, 1f, t / timePerStep);
                model.localScale = Vector3.Scale(baseScale, Vector3.Lerp(from, to, k));
                yield return null;
            }

            from = to;
        }

        if (model != null)
        {
            model.localScale = Vector3.Scale(baseScale, steps[steps.Length - 1]);
            if (!appear) model.gameObject.SetActive(false);
        }

        running.Remove(model);
    }
}