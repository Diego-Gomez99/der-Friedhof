using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Full-screen slash played when an enemy hits the player: impact frames,
// one slash drawn across the player, flying shards, hit-stop, camera shake and a loud slice sound.
// Everything is built at runtime, so no prefab or scene setup is needed.
public class SlashEffect : MonoBehaviour
{
    private const float HitStopDuration = 0.18f; // Real seconds the game is frozen on impact.
    private const float ShakeDuration = 0.6f;
    private const float ShakeStrength = 0.5f; // World units.
    private const float UiShakeStrength = 40.0f; // Canvas pixels.
    private const int ShardCount = 36;

    private static readonly Color SlashRed = new Color(0.85f, 0.02f, 0.05f);
    private static readonly Color DarkRed = new Color(0.35f, 0.0f, 0.02f);

    private static Sprite slashSprite;
    private static Sprite glowSprite;
    private static Sprite sparkSprite;
    private static Sprite pixelSprite;
    private static AudioClip sliceClip;

    private RectTransform shakeRoot;
    private Image flash;
    private AudioSource audioSource;
    private float shakeStartTime = -1.0f;
    private Camera shakenCamera;
    private Vector3 shakenCameraPosition;
    private float previousTimeScale = 1.0f;
    private bool timeFrozen;

    public static void Play(AudioClip impactSound, Vector3 target, Vector3 attacker)
    {
        GameObject root = new GameObject("Slash Effect", typeof(Canvas), typeof(CanvasScaler));
        SlashEffect effect = root.AddComponent<SlashEffect>();
        effect.Build();
        effect.StartCoroutine(effect.Run(impactSound, target, attacker));
    }

    private void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        flash = CreateImage("Flash", transform, PixelSprite);
        Stretch(flash.rectTransform);
        flash.color = Color.clear;

        // Everything but the flash shakes, so the flash always covers the whole screen.
        shakeRoot = new GameObject("Shake Root", typeof(RectTransform)).GetComponent<RectTransform>();
        shakeRoot.SetParent(transform, false);
        Stretch(shakeRoot);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f;
        audioSource.volume = 1.0f;
        audioSource.ignoreListenerPause = true;
    }

    private IEnumerator Run(AudioClip impactSound, Vector3 target, Vector3 attacker)
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;
        timeFrozen = true;
        shakeStartTime = Time.unscaledTime;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

        if (impactSound != null)
        {
            audioSource.PlayOneShot(impactSound, 1.0f);
        }

        // The slash crosses the player perpendicular to the direction the hit comes from.
        Vector2 center = Vector2.zero;
        float angle = -28.0f;
        Camera camera = ActiveCamera();
        if (camera != null)
        {
            Vector3 targetScreen = camera.WorldToScreenPoint(target);
            Vector3 attackerScreen = camera.WorldToScreenPoint(attacker);
            float canvasScale = Mathf.Min(Screen.width / 1920.0f, Screen.height / 1080.0f);
            center = ((Vector2)targetScreen - new Vector2(Screen.width, Screen.height) * 0.5f) / canvasScale;
            Vector2 hit = targetScreen - attackerScreen;
            if (hit.sqrMagnitude > 1.0f)
            {
                angle = Mathf.Atan2(hit.y, hit.x) * Mathf.Rad2Deg + 90.0f;
            }
        }

        StartCoroutine(FlashSequence());
        StartCoroutine(Slash(angle, center, 1.3f, 1.0f));
        SpawnShards(angle, center);

        yield return new WaitForSecondsRealtime(HitStopDuration);
        UnfreezeTime();

        yield return new WaitForSecondsRealtime(1.5f);
        Destroy(gameObject);
    }

    // Of the enabled cameras, the one drawn last is the one the player sees.
    private static Camera ActiveCamera()
    {
        Camera best = null;
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera.cameraType == CameraType.Game && (best == null || camera.depth > best.depth))
            {
                best = camera;
            }
        }
        return best;
    }

    // White frame, black "impact" frame, then a red tint that fades out.
    private IEnumerator FlashSequence()
    {
        flash.color = Color.white;
        yield return new WaitForSecondsRealtime(0.05f);
        flash.color = new Color(0.0f, 0.0f, 0.0f, 0.92f);
        yield return new WaitForSecondsRealtime(0.07f);
        flash.color = Color.white;
        yield return new WaitForSecondsRealtime(0.03f);

        const float fade = 1.1f;
        for (float t = 0.0f; t < fade; t += Time.unscaledDeltaTime)
        {
            float k = t / fade;
            flash.color = new Color(SlashRed.r, 0.0f, 0.0f, Mathf.Lerp(0.55f, 0.0f, k * k));
            yield return null;
        }
        flash.color = Color.clear;
    }

    // One slash drawn across the screen: the stroke grows from one side, holds, then thins out.
    private IEnumerator Slash(float angle, Vector2 offset, float thickness, float pitch)
    {
        GameObject slashObject = new GameObject("Slash", typeof(RectTransform));
        RectTransform slash = slashObject.GetComponent<RectTransform>();
        slash.SetParent(shakeRoot, false);
        slash.anchoredPosition = offset;
        slash.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);

        // A wide dark-red glow behind the bright stroke.
        Image glow = CreateImage("Glow", slash, GlowSprite);
        glow.rectTransform.sizeDelta = new Vector2(2900, 260 * thickness);
        glow.color = new Color(DarkRed.r, DarkRed.g, DarkRed.b, 0.85f);
        MakeFilled(glow);

        Image stroke = CreateImage("Stroke", slash, SlashSprite);
        stroke.rectTransform.sizeDelta = new Vector2(2800, 140 * thickness);
        MakeFilled(stroke);

        PlaySlice(pitch);

        const float draw = 0.07f;
        for (float t = 0.0f; t < draw; t += Time.unscaledDeltaTime)
        {
            float k = 1.0f - Mathf.Pow(1.0f - t / draw, 3.0f);
            stroke.fillAmount = k;
            glow.fillAmount = k;
            yield return null;
        }
        stroke.fillAmount = 1.0f;
        glow.fillAmount = 1.0f;

        yield return new WaitForSecondsRealtime(0.12f);

        const float vanish = 0.45f;
        for (float t = 0.0f; t < vanish; t += Time.unscaledDeltaTime)
        {
            float k = t / vanish;
            slash.localScale = new Vector3(1.0f + 0.08f * k, Mathf.Lerp(1.0f, 0.0f, k * k), 1.0f);
            glow.color = new Color(DarkRed.r, DarkRed.g, DarkRed.b, 0.85f * (1.0f - k));
            yield return null;
        }
        Destroy(slashObject);
    }

    private void SpawnShards(float angle, Vector2 center)
    {
        for (int i = 0; i < ShardCount; i++)
        {
            StartCoroutine(Shard(angle, center));
        }
    }

    private IEnumerator Shard(float angle, Vector2 center)
    {
        Image shard = CreateImage("Shard", shakeRoot, SparkSprite);
        float size = Random.Range(10.0f, 30.0f);
        shard.color = Random.value < 0.4f ? Color.white : SlashRed;

        // Start along the slash and fly outwards.
        float along = Random.Range(-700.0f, 700.0f);
        Vector2 slashDirection = Quaternion.Euler(0, 0, angle) * Vector2.right;
        Vector2 position = center + slashDirection * along;
        Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(600.0f, 2200.0f);
        float life = Random.Range(0.5f, 1.0f);

        for (float t = 0.0f; t < life; t += Time.unscaledDeltaTime)
        {
            float dt = Time.unscaledDeltaTime;
            velocity += Vector2.down * 2600.0f * dt;
            velocity *= 1.0f - 2.0f * dt;
            position += velocity * dt;
            // Each spark points where it flies and stretches with its speed.
            shard.rectTransform.anchoredPosition = position;
            shard.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            shard.rectTransform.sizeDelta = new Vector2(size + velocity.magnitude * 0.04f, size * 0.35f);
            Color c = shard.color;
            c.a = 1.0f - t / life;
            shard.color = c;
            yield return null;
        }
        Destroy(shard.gameObject);
    }

    private void PlaySlice(float pitch)
    {
        // Each slice gets its own source so the pitches do not interfere.
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.0f;
        source.volume = 1.0f;
        source.pitch = pitch;
        source.clip = SliceClip;
        source.Play();
    }

    private void Update()
    {
        if (shakeStartTime < 0.0f)
        {
            return;
        }
        float k = 1.0f - (Time.unscaledTime - shakeStartTime) / ShakeDuration;
        shakeRoot.anchoredPosition = k > 0.0f ? Random.insideUnitCircle * UiShakeStrength * k * k : Vector2.zero;
    }

    // The cameras are moved by several scripts in LateUpdate, so the shake is applied only
    // while each camera renders and undone straight after.
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType != CameraType.Game)
        {
            return;
        }
        float k = 1.0f - (Time.unscaledTime - shakeStartTime) / ShakeDuration;
        if (k <= 0.0f)
        {
            return;
        }
        shakenCamera = camera;
        shakenCameraPosition = camera.transform.position;
        camera.transform.position += (Vector3)Random.insideUnitCircle * ShakeStrength * k * k;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == shakenCamera)
        {
            camera.transform.position = shakenCameraPosition;
            shakenCamera = null;
        }
    }

    private void UnfreezeTime()
    {
        if (timeFrozen)
        {
            Time.timeScale = previousTimeScale;
            timeFrozen = false;
        }
    }

    private void OnDestroy()
    {
        UnfreezeTime();
        if (shakenCamera != null)
        {
            shakenCamera.transform.position = shakenCameraPosition;
        }
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static void MakeFilled(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0.0f;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite PixelSprite
    {
        get
        {
            if (pixelSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                pixelSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            return pixelSprite;
        }
    }

    // A crescent with a white core that blends into red edges, drawn at a high resolution with
    // bilinear filtering so it stays smooth when stretched across the screen.
    private static Sprite SlashSprite
    {
        get
        {
            if (slashSprite == null)
            {
                slashSprite = CreateCrescentSprite((r, aa) =>
                {
                    Color c = r < 0.5f
                        ? Color.Lerp(Color.white, new Color(1.0f, 0.55f, 0.55f), Mathf.SmoothStep(0.15f, 0.5f, r))
                        : Color.Lerp(new Color(1.0f, 0.55f, 0.55f), SlashRed, Mathf.SmoothStep(0.5f, 0.85f, r));
                    // Anti-aliased outer edge.
                    c.a = Mathf.Clamp01((1.0f - r) / aa);
                    return c;
                });
            }
            return slashSprite;
        }
    }

    // The same crescent as a soft white halo that fades out towards its edges, tinted for the glow.
    private static Sprite GlowSprite
    {
        get
        {
            if (glowSprite == null)
            {
                glowSprite = CreateCrescentSprite((r, aa) => new Color(1.0f, 1.0f, 1.0f, Mathf.Pow(Mathf.Clamp01(1.0f - r), 1.5f)));
            }
            return glowSprite;
        }
    }

    // Builds a crescent texture: thick in the middle, pointed at both ends, a little heavier towards
    // the end of the stroke. colorAt gets the distance from the centre line (0) to the edge (1) and
    // the width of one pixel in those units.
    private static Sprite CreateCrescentSprite(System.Func<float, float, Color> colorAt)
    {
        const int width = 512;
        const int height = 64;
        Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width;
            float halfThickness = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f)), 0.6f) * (height / 2.0f - 1.0f);
            for (int y = 0; y < height; y++)
            {
                float d = Mathf.Abs(y + 0.5f - height / 2.0f);
                Color c = Color.clear;
                if (halfThickness > 0.01f && d < halfThickness + 1.0f)
                {
                    c = colorAt(Mathf.Min(d / halfThickness, 1.0f), 1.0f / halfThickness);
                    if (d > halfThickness)
                    {
                        c.a = 0.0f;
                    }
                }
                pixels[y * width + x] = c;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // A soft white streak, brighter in the middle, used for the sparks that fly off the slash.
    private static Sprite SparkSprite
    {
        get
        {
            if (sparkSprite == null)
            {
                const int width = 64;
                const int height = 16;
                Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float dx = (x + 0.5f) / width * 2.0f - 1.0f;
                        float dy = (y + 0.5f) / height * 2.0f - 1.0f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        pixels[y * width + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Pow(Mathf.Clamp01(1.0f - r), 0.7f));
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                sparkSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }
            return sparkSprite;
        }
    }

    // A synthesized slice: a bright noise swish, a metallic ring and a low thump, soft-clipped to be loud.
    private static AudioClip SliceClip
    {
        get
        {
            if (sliceClip == null)
            {
                const int sampleRate = 44100;
                const float duration = 0.6f;
                int count = (int)(sampleRate * duration);
                float[] samples = new float[count];
                System.Random random = new System.Random(1234);
                float previousNoise = 0.0f;
                float highPassed = 0.0f;
                float[] ringFrequencies = { 1870.0f, 2630.0f, 3920.0f, 5210.0f };
                float thumpPhase = 0.0f;

                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / sampleRate;

                    float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    highPassed = 0.85f * (highPassed + noise - previousNoise);
                    previousNoise = noise;
                    float swishEnvelope = Mathf.Clamp01(t / 0.008f) * Mathf.Exp(-t * 11.0f);
                    float swish = highPassed * swishEnvelope;

                    float ring = 0.0f;
                    foreach (float frequency in ringFrequencies)
                    {
                        ring += Mathf.Sin(2.0f * Mathf.PI * frequency * t);
                    }
                    ring *= 0.22f * Mathf.Exp(-t * 7.0f) * Mathf.Clamp01(t / 0.004f);

                    float thumpFrequency = Mathf.Lerp(170.0f, 40.0f, Mathf.Clamp01(t / 0.25f));
                    thumpPhase += 2.0f * Mathf.PI * thumpFrequency / sampleRate;
                    float thump = Mathf.Sin(thumpPhase) * Mathf.Exp(-t * 13.0f);

                    float mix = 1.1f * swish + ring + 1.2f * thump;
                    samples[i] = (float)System.Math.Tanh(mix * 1.6f) * 0.95f;
                }

                sliceClip = AudioClip.Create("Slice", count, 1, sampleRate, false);
                sliceClip.SetData(samples, 0);
            }
            return sliceClip;
        }
    }
}
