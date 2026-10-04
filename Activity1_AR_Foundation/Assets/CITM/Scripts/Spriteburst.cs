using UnityEngine;

// Anillos y destellos generados por codigo
// Los de un solo uso se expanden y desaparecen solos, los de loop se quedan pulsando
public class SpriteBurst : MonoBehaviour
{
    public enum Shape { Ring, Glow }

    private static Sprite ringSprite;
    private static Sprite glowSprite;

    private SpriteRenderer sr;
    private Color color;
    private float startSize, endSize, duration;
    private float timer = 0f;
    private bool loop = false;
    private float loopSize;

    public static void Spawn(Shape shape, Vector3 position, Quaternion rotation, Color color,
                             float startSize, float endSize, float duration)
    {
        SpriteBurst burst = CreateBurst(shape, position, rotation, null, color);
        burst.startSize = startSize;
        burst.endSize = endSize;
        burst.duration = Mathf.Max(0.01f, duration);
        burst.UpdateVisual(0f);
    }

    public static SpriteBurst SpawnLoop(Shape shape, Transform parent, Vector3 position, Quaternion rotation,
                                        Color color, float worldSize)
    {
        SpriteBurst burst = CreateBurst(shape, position, rotation, parent, color);
        burst.loop = true;

        // Al ser hijo hay que compensar la escala del padre para que mida lo que toca en el mundo
        float parentScale = parent != null ? Mathf.Max(parent.lossyScale.x, 0.00001f) : 1f;
        burst.loopSize = worldSize / parentScale;
        burst.UpdateVisual(0f);
        return burst;
    }

    private static SpriteBurst CreateBurst(Shape shape, Vector3 position, Quaternion rotation, Transform parent, Color color)
    {
        GameObject go = new GameObject("SpriteBurst_" + shape);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = rotation;

        SpriteBurst burst = go.AddComponent<SpriteBurst>();
        burst.color = color;
        burst.sr = go.AddComponent<SpriteRenderer>();
        burst.sr.sprite = GetSprite(shape);
        burst.sr.sortingOrder = 10;
        return burst;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (loop)
        {
            UpdateVisual(0f);
            return;
        }

        // k es el progreso del efecto, de 0 a 1. Al llegar a 1 se destruye
        float k = Mathf.Clamp01(timer / duration);
        UpdateVisual(k);

        if (k >= 1f) Destroy(gameObject);
    }

    private void UpdateVisual(float k)
    {
        Color c = color;

        if (loop)
        {
            // Pulso suave con un seno: cambia un poco el tamano y la transparencia
            float s = 0.5f + 0.5f * Mathf.Sin(timer * 3f);
            transform.localScale = Vector3.one * (loopSize * (1f + 0.08f * s));
            c.a = color.a * (0.35f + 0.3f * s);
        }
        else
        {
            // Crece rapido al principio y va frenando, a la vez que se hace transparente
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, ease);
            c.a = color.a * (1f - k);
        }

        sr.color = c;
    }

    private static Sprite GetSprite(Shape shape)
    {
        if (shape == Shape.Ring)
        {
            if (ringSprite == null) ringSprite = MakeSprite(true);
            return ringSprite;
        }

        if (glowSprite == null) glowSprite = MakeSprite(false);
        return glowSprite;
    }

    private static Sprite MakeSprite(bool ring)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distancia al centro, 0 en el centro y 1 en el borde
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;

                float alpha;
                if (ring) alpha = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f); // El aro esta al 85% del radio
                else alpha = Mathf.Clamp01(1f - d);

                // degradado suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}