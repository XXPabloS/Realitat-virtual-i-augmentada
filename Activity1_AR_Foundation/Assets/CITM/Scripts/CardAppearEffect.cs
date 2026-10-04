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

    [System.Serializable]
    private class PokemonSound
    {
        public GameObject prefab;
        public AudioClip clip;
    }

    [Header("Pokemon SFX")]
    [SerializeField] private PokemonSound[] pokemonSounds = new PokemonSound[0];
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    private AudioSource sfxSource;

    [Header("Efectos de aparicion")]
    [SerializeField] private bool layeredFx = true; // Destello y anillo generados por codigo
    [SerializeField] private Color revealColor = Color.white; // Color del destello y el anillo
    [SerializeField] private float virtualCardSize = 1f; // Tamano de las cartas virtuales para escalar los efectos

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
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
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

        // img.size es el tamano fisico de la carta en metros
        float cardSize = img.size.x > 0f ? Mathf.Max(img.size.x, img.size.y) : 0.1f;
        SpawnRevealFx(img.transform.position, img.transform.rotation, cardSize);

        Play(model, appear: true);
    }

    private void OnFaceDown(ARTrackedImage img)
    {
        if (img == null || img.transform.childCount == 0) return;
        Transform model = img.transform.GetChild(0);

        if (baseScales.ContainsKey(model))
            Play(model, appear: false);
    }

    public void PlayVirtualEffect(Transform model, bool appear)
    {
        // Aplicar el efecto en las cartas virtuales de modo Debug
        if (appear)
        {
            if (!baseScales.ContainsKey(model))
                baseScales[model] = model.localScale;

            model.gameObject.SetActive(true);

            SpawnRevealFx(model.position, model.rotation, virtualCardSize);
        }

        Play(model, appear);
    }

    private void Play(Transform model, bool appear)
    {
        if (appear) PlayRevealSound(model);

        // Si había otra animación en este modelo, se corta y la nueva sigue desde la forma actual
        if (running.TryGetValue(model, out var current))
            StopCoroutine(current);

        running[model] = StartCoroutine(Animate(model, appear));
    }

    // Busca la entrada de la lista que corresponde a este modelo (por el nombre del prefab)
    private PokemonSound FindEntry(Transform model)
    {
        string modelName = model.name.Replace("(Clone)", "");

        foreach (var sound in pokemonSounds)
        {
            if (sound != null && sound.prefab != null && sound.prefab.name == modelName)
                return sound;
        }

        return null;
    }

    private void PlayRevealSound(Transform model)
    {
        PokemonSound entry = FindEntry(model);
        if (entry != null && entry.clip != null)
            sfxSource.PlayOneShot(entry.clip, sfxVolume);
    }

    // Particulas del prefab + destello y anillo
    private void SpawnRevealFx(Vector3 pos, Quaternion rot, float cardSize)
    {
        if (particlesPrefab != null)
        {
            var ps = Instantiate(particlesPrefab, pos, rot);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        if (layeredFx)
        {
            // Sprites planos sobre la carta, un pelin elevados
            Quaternion flat = rot * Quaternion.Euler(90f, 0f, 0f);
            Vector3 basePos = pos + rot * Vector3.up * (cardSize * 0.02f);

            // Destello corto
            SpriteBurst.Spawn(SpriteBurst.Shape.Glow, basePos, flat,
                              revealColor, cardSize * 0.3f, cardSize * 1.3f, 0.3f);
            // Anillo
            SpriteBurst.Spawn(SpriteBurst.Shape.Ring, basePos, flat,
                              revealColor, cardSize * 0.2f, cardSize * 1.6f, 0.6f);
        }
    }

    private IEnumerator Animate(Transform model, bool appear)
    {
        Vector3[] steps = appear ? appearSteps : disappearSteps;
        float timePerStep = appear ? appearTimePerStep : disappearTimePerStep;
        Vector3 baseScale = baseScales[model];

        // Aparicionm, arranca en el primer paso. Desaparicion, arranca desde la forma actual.
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