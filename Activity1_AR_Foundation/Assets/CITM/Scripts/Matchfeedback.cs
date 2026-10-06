using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Feedback visual y de sonido cuando se acierta o se falla una pareja

public class MatchFeedback : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("Timing")]
    [SerializeField] private float feedbackDelay = 0.5f; 

    [Header("Acierto")]
    [SerializeField] private Color matchColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private ParticleSystem matchParticles;
    [SerializeField] private AudioClip matchClip;
    [SerializeField] private int hopCount = 2; // Cuantos botes da el pokemon
    [SerializeField] private float hopDuration = 0.5f;
    [SerializeField, Range(0.05f, 1f)] private float hopHeight = 0.3f; // Proporcion de la altura del pokemon
    [SerializeField] private bool vibrateOnMatch = true;

    [Header("Aura")]
    [SerializeField] private bool showAura = true; // Aro que se queda en las cartas ya emparejadas
    [SerializeField] private GameObject auraPrefab; // Si no se asigna, el aro se genera por codigo (que es igual al de acierto )

    [Header("Fallo")]
    [SerializeField] private Color mismatchColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] private ParticleSystem mismatchParticles;
    [SerializeField] private AudioClip mismatchClip;
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField, Range(0.01f, 0.5f)] private float shakeAmount = 0.12f; // Proporcion de la altura del pokemon

    [Header("Efectos sobre la carta")]
    [SerializeField] private string cardVisualName = "CardVisual"; // Hijo de la carta que se usa para colocar y dimensionar los efectos
    [SerializeField, Range(0.2f, 1.5f)] private float effectSizeRatio = 0.7f; // Proporcion del ancho de la carta, igual para todos los pokemon

    [Header("General")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource sfxSource;

    // Guarda el modelo junto a su aura para saber cuando hay que ocultarla
    private class MatchedAura
    {
        public Transform model;
        public GameObject obj;
    }

    private readonly List<MatchedAura> auras = new List<MatchedAura>();

    // Corrutina de movimiento de cada modelo y su posicion original
    private readonly Dictionary<Transform, Coroutine> motions = new Dictionary<Transform, Coroutine>();
    private readonly Dictionary<Transform, Vector3> restPositions = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.OnMatch += HandleMatch;
        gameManager.OnMismatch += HandleMismatch;
        gameManager.OnGameReset += ResetAll;
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.OnMatch -= HandleMatch;
        gameManager.OnMismatch -= HandleMismatch;
        gameManager.OnGameReset -= ResetAll;
    }

    private void Update()
    {
        // El aura solo se ve mientras el pokemon esta visible, y si la carta ya no existe se borra
        for (int i = auras.Count - 1; i >= 0; i--)
        {
            MatchedAura a = auras[i];

            if (a.model == null || a.obj == null)
            {
                if (a.obj != null) Destroy(a.obj);
                auras.RemoveAt(i);
                continue;
            }

            bool visible = a.model.gameObject.activeInHierarchy;
            if (a.obj.activeSelf != visible) a.obj.SetActive(visible);
        }
    }

    private void HandleMatch(Transform a, Transform b)
    {
        StartCoroutine(FeedbackRoutine(a, b, true));
    }

    private void HandleMismatch(Transform a, Transform b)
    {
        StartCoroutine(FeedbackRoutine(a, b, false));
    }

    private IEnumerator FeedbackRoutine(Transform a, Transform b, bool isMatch)
    {
        // Esperar a que termine la animacion de aparicion de la segunda carta
        yield return new WaitForSeconds(feedbackDelay);

        PlaySfx(isMatch ? matchClip : mismatchClip);
        if (isMatch && vibrateOnMatch) Handheld.Vibrate();

        ApplyFeedback(a, isMatch);
        ApplyFeedback(b, isMatch);
    }

    // Aplica los efectos a un pokemon: particulas, anillos y salto (acierto) o shake (fallo)
    private void ApplyFeedback(Transform model, bool isMatch)
    {
        // Si ya no esta visible (carta girada o partida reiniciada)
        if (model == null || !model.gameObject.activeInHierarchy) return;

        Color color = isMatch ? matchColor : mismatchColor;

        Bounds bounds = Measure(model);
        GetPlacement(model, bounds, out Vector3 basePoint, out Quaternion flatRot, out float size);

        SpawnParticles(isMatch ? matchParticles : mismatchParticles, basePoint, flatRot);
        SpawnRings(basePoint, flatRot, color, size);

        if (isMatch) AddAura(model, basePoint, flatRot, size);

        // El salto y el shake dependen de la altura del pokemon
        float localHeight = bounds.size.y / ParentScale(model);
        Move(model, localHeight * (isMatch ? hopHeight : shakeAmount), isMatch);
    }

    private void AddAura(Transform model, Vector3 basePoint, Quaternion flatRot, float size)
    {
        if (!showAura || model.parent == null) return;

        // Si esa carta ya tiene aura no se le pone otra
        foreach (MatchedAura a in auras)
            if (a.model == model) return;

        GameObject obj;

        if (auraPrefab != null)
        {
            obj = Instantiate(auraPrefab, model.parent);
            obj.transform.position = basePoint;
            obj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Sin prefab se genera un aro que pulsa
            obj = SpriteBurst.SpawnLoop(SpriteBurst.Shape.Ring, model.parent, basePoint, flatRot, matchColor, size * 1.3f).gameObject;
        }

        MatchedAura aura = new MatchedAura();
        aura.model = model;
        aura.obj = obj;
        auras.Add(aura);
    }

    // Mueve el modelo del pokemon, salto si hop es true, y si no un shake de lado a lado
    private void Move(Transform model, float amount, bool hop)
    {
        // Si ya se estaba moviendo, se corta y se devuelve a su sitio antes de empezar el nuevo
        if (motions.TryGetValue(model, out Coroutine running))
        {
            StopCoroutine(running);
            model.localPosition = restPositions[model];
        }

        restPositions[model] = model.localPosition;
        motions[model] = StartCoroutine(MotionRoutine(model, amount, hop));
    }

    private IEnumerator MotionRoutine(Transform model, float amount, bool hop)
    {
        Vector3 rest = restPositions[model];
        float duration = hop ? hopDuration : shakeDuration;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (model == null) break;

            float k = t / duration; // Progreso del movimiento, de 0 a 1
            Vector3 offset;

            if (hop)
            {
                offset = Vector3.up * (Mathf.Abs(Mathf.Sin(k * Mathf.PI * hopCount)) * amount);
            }
            else
            {
                offset = Vector3.right * (Mathf.Sin(k * Mathf.PI * 8f) * (1f - k) * amount);
            }

            model.localPosition = rest + offset;
            yield return null;
        }

        // Al terminar vuelve a su posicion original
        if (model != null) model.localPosition = rest;
        motions.Remove(model);
        restPositions.Remove(model);
    }

    private void SpawnParticles(ParticleSystem prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;

        ParticleSystem ps = Instantiate(prefab, pos, rot);
        ps.Play();
        Destroy(ps.gameObject, 3f);
    }

    // Un destello rapido y un anillo que se expande en la base del pokemon
    private void SpawnRings(Vector3 basePoint, Quaternion flatRot, Color color, float size)
    {
        SpriteBurst.Spawn(SpriteBurst.Shape.Glow, basePoint, flatRot,
                          Color.Lerp(color, Color.white, 0.5f), size * 0.4f, size * 1.3f, 0.3f);
        SpriteBurst.Spawn(SpriteBurst.Shape.Ring, basePoint, flatRot,
                          color, size * 0.3f, size * 1.8f, 0.6f);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Caja que envuelve al pokemon sin contar las particulas, para saber su tamano real
    private Bounds Measure(Transform model)
    {
        bool found = false;
        Bounds b = new Bounds(model.position, Vector3.one * 0.05f);

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;

            if (!found)
            {
                b = r.bounds;
                found = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }

        return b;
    }

    // Saca el punto de la base del pokemon, la rotacion para que los sprites queden planos
    private bool MeasureCard(Transform card, out Bounds b)
    {
        b = new Bounds();

        Transform visual = card.Find(cardVisualName);
        if (visual == null) return false;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;

        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return true;
    }

    private void GetPlacement(Transform model, Bounds bounds, out Vector3 basePoint, out Quaternion flatRot, out float size)
    {
        Transform reference = model.parent != null ? model.parent : model;

        if (MeasureCard(reference, out Bounds card))
        {
            size = Mathf.Min(card.size.x, card.size.z) * effectSizeRatio;
            // Centro de la carta y altura de su parte de arriba
            basePoint = new Vector3(card.center.x, card.max.y, card.center.z);
        }
        else
        {
            // Si no se encuentra la carta se usa el modelo 
            size = Mathf.Max(bounds.size.x, bounds.size.z);
            basePoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        // Subido un poquito para que no clipee con la carta
        basePoint += reference.up * (size * 0.05f);
        flatRot = reference.rotation * Quaternion.Euler(90f, 0f, 0f);
    }

    // Escala de la carta
    private float ParentScale(Transform model)
    {
        return model.parent != null ? Mathf.Max(model.parent.lossyScale.y, 0.00001f) : 1f;
    }

    private void ResetAll()
    {
        StopAllCoroutines();

        foreach (var kv in restPositions)
            if (kv.Key != null) kv.Key.localPosition = kv.Value;

        restPositions.Clear();
        motions.Clear();

        foreach (MatchedAura a in auras)
            if (a.obj != null) Destroy(a.obj);

        auras.Clear();
    }
}