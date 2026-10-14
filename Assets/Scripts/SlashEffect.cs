using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Full-screen pixel-art slash played when an enemy hits the player: impact frames,
// three slashes drawn across the screen, flying shards, hit-stop, camera shake and a loud slice sound.
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

    public static void Play(AudioClip impactSound)
    {
        GameObject root = new GameObject("Slash Effect", typeof(Canvas), typeof(CanvasScaler));
        SlashEffect effect = root.AddComponent<SlashEffect>();
        effect.Build();
        effect.StartCoroutine(effect.Run(impactSound));
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

    private IEnumerator Run(AudioClip impactSound)
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

        StartCoroutine(FlashSequence());
        StartCoroutine(Slash(-28.0f, new Vector2(0, 40), 1.0f, 1.0f));
        SpawnShards();

        yield return new WaitForSecondsRealtime(0.09f);
        StartCoroutine(Slash(32.0f, new Vector2(-60, -20), 0.9f, 1.18f));

        yield return new WaitForSecondsRealtime(HitStopDuration - 0.09f);
        UnfreezeTime();

        yield return new WaitForSecondsRealtime(0.04f);
        StartCoroutine(Slash(-78.0f, new Vector2(80, 0), 0.75f, 0.85f));

        yield return new WaitForSecondsRealtime(1.4f);
        Destroy(gameObject);
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
        Image glow = CreateImage("Glow", slash, SlashSprite);
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

    private void SpawnShards()
    {
        for (int i = 0; i < ShardCount; i++)
        {
            StartCoroutine(Shard());
        }
    }

    private IEnumerator Shard()
    {
        Image shard = CreateImage("Shard", shakeRoot, PixelSprite);
        float size = Random.Range(2, 6) * 6.0f;
        shard.rectTransform.sizeDelta = new Vector2(size, size);
        shard.color = Random.value < 0.4f ? Color.white : SlashRed;

        // Start along the first slash and fly outwards.
        float along = Random.Range(-700.0f, 700.0f);
        Vector2 slashDirection = Quaternion.Euler(0, 0, -28.0f) * Vector2.right;
        Vector2 position = slashDirection * along + new Vector2(0, 40);
        Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(600.0f, 2200.0f);
        float life = Random.Range(0.5f, 1.0f);

        for (float t = 0.0f; t < life; t += Time.unscaledDeltaTime)
        {
            float dt = Time.unscaledDeltaTime;
            velocity += Vector2.down * 2600.0f * dt;
            velocity *= 1.0f - 2.0f * dt;
            position += velocity * dt;
            // Snap to a 6 px grid so the shards move like pixel art.
            shard.rectTransform.anchoredPosition = new Vector2(Mathf.Round(position.x / 6) * 6, Mathf.Round(position.y / 6) * 6);
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

    // A low-resolution crescent with a white core and red edges, drawn with point filtering
    // so it reads as pixel art when stretched across the screen.
    private static Sprite SlashSprite
    {
        get
        {
            if (slashSprite == null)
            {
                const int width = 96;
                const int height = 9;
                Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    // Thick in the middle, pointed at both ends, a little heavier towards the end of the stroke.
                    float halfThickness = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f)), 0.6f) * (height / 2.0f);
                    for (int y = 0; y < height; y++)
                    {
                        float d = Mathf.Abs(y - (height - 1) / 2.0f);
                        Color c = Color.clear;
                        if (d <= halfThickness * 0.35f)
                        {
                            c = Color.white;
                        }
                        else if (d <= halfThickness * 0.7f)
                        {
                            c = new Color(1.0f, 0.55f, 0.55f);
                        }
                        else if (d <= halfThickness)
                        {
                            c = SlashRed;
                        }
                        texture.SetPixel(x, y, c);
                    }
                }
                texture.Apply();
                slashSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }
            return slashSprite;
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
