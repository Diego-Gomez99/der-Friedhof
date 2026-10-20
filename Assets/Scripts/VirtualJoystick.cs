using UnityEngine;
using UnityEngine.UI;

// On-screen joystick for touch screens. It rests faintly in the bottom-left corner and jumps to
// wherever a finger first touches the screen, so it works for either hand. PlayerController adds
// Value to the keyboard axes. Built at runtime like SlashEffect, so no scene setup is needed.
public class VirtualJoystick : MonoBehaviour
{
    private const float BaseRadius = 110.0f; // Canvas units at the 1920x1080 reference resolution.
    private const float KnobRadius = 50.0f;
    private const float DeadZone = 0.12f; // Fraction of BaseRadius ignored around the centre.
    private const float IdleAlpha = 0.35f; // Opacity while no finger is on the joystick.

    private static readonly Vector2 RestPosition = new Vector2(260.0f, 260.0f); // From the bottom-left corner.

    private static Sprite circleSprite;
    private static Sprite ringSprite;

    // Joystick direction, each axis between -1 and 1. Zero while it is not being touched.
    public static Vector2 Value { get; private set; }

    private RectTransform canvasRect;
    private RectTransform baseRect;
    private RectTransform knobRect;
    private CanvasGroup group;
    private int fingerId = -1;
    private Vector2 touchStart; // Canvas position where the finger went down.

    public static void CreateIfTouchScreen()
    {
        Value = Vector2.zero;
        if (!Input.touchSupported && !Application.isMobilePlatform)
        {
            return;
        }

        GameObject root = new GameObject("Virtual Joystick", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.AddComponent<VirtualJoystick>().Build();
    }

    private void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above the game UI, below SlashEffect.

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // The joystick reads touches itself, so its images must not block taps on the Game Over buttons.
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        canvasRect = (RectTransform)transform;
        baseRect = CreateImage("Base", transform, RingSprite, BaseRadius, new Color(1.0f, 1.0f, 1.0f, 0.5f));
        knobRect = CreateImage("Knob", baseRect, CircleSprite, KnobRadius, new Color(1.0f, 1.0f, 1.0f, 0.6f));
        Release();
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            Release();
            group.alpha = 0.0f;
            return;
        }

        if (fingerId < 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    fingerId = touch.fingerId;
                    touchStart = ToCanvas(touch.position);
                    baseRect.anchoredPosition = touchStart;
                    group.alpha = 1.0f;
                    break;
                }
            }
        }

        if (fingerId >= 0)
        {
            Track();
        }
    }

    private void Track()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != fingerId)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                break;
            }

            Vector2 offset = Vector2.ClampMagnitude(ToCanvas(touch.position) - touchStart, BaseRadius);
            knobRect.anchoredPosition = offset;
            Vector2 value = offset / BaseRadius;
            Value = value.magnitude < DeadZone ? Vector2.zero : value;
            return;
        }

        // The finger was lifted (or is gone).
        Release();
    }

    private void Release()
    {
        fingerId = -1;
        Value = Vector2.zero;
        baseRect.anchoredPosition = RestPosition;
        knobRect.anchoredPosition = Vector2.zero;
        group.alpha = IdleAlpha;
    }

    private void OnDestroy()
    {
        Value = Vector2.zero;
    }

    // Converts a screen position to a position relative to the canvas' bottom-left corner.
    private Vector2 ToCanvas(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local);
        return local - canvasRect.rect.min;
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, float radius, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        bool child = parent is RectTransform && parent.GetComponent<Canvas>() == null;
        rect.anchorMin = rect.anchorMax = child ? new Vector2(0.5f, 0.5f) : Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(radius * 2.0f, radius * 2.0f);

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Sprite CircleSprite => circleSprite != null ? circleSprite : circleSprite = MakeSprite(Circle(128, 0.0f));
    private static Sprite RingSprite => ringSprite != null ? ringSprite : ringSprite = MakeSprite(Circle(256, 0.06f));

    // A soft-edged white disc. With a ringWidth above zero the inside is faint and the rim solid.
    private static Texture2D Circle(int size, float ringWidth)
    {
        Texture2D texture = new Texture2D(size, size) { wrapMode = TextureWrapMode.Clamp };
        float center = (size - 1) * 0.5f;
        float edgeSoftness = 2.0f / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2(x - center, y - center).magnitude / center; // 0 at the centre, 1 at the edge.
                float alpha = Mathf.Clamp01((1.0f - distance) / edgeSoftness);
                if (ringWidth > 0.0f)
                {
                    float rim = Mathf.Clamp01((distance - (1.0f - ringWidth)) / edgeSoftness);
                    alpha *= Mathf.Lerp(0.25f, 1.0f, rim);
                }
                texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Sprite MakeSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
