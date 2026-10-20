using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Grows and brightens a text button while it is hovered or selected, and clicks it on press instead of on release.
public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private Color hoverColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float speed = 12f;

    private TMP_Text label;
    private Color normalColor;
    private bool highlighted;
    private bool locked;
    private Color lockedColor;

    private void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        normalColor = label.color;
    }

    private void Update()
    {
        float targetScale = highlighted && !locked ? hoverScale : 1f;
        Color targetColor = locked ? lockedColor : highlighted ? hoverColor : normalColor;
        float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, t);
        label.color = Color.Lerp(label.color, targetColor, t);
    }

    // Stops reacting to hover and selection and settles at normal size on the given color.
    public void Lock(Color color)
    {
        locked = true;
        lockedColor = color;
    }

    public void Lock()
    {
        Lock(normalColor);
    }

    // Locks every button with this effect and darkens the one that was used.
    public static void LockAll(Color usedColor)
    {
        // Hovering or navigating to a button selects it, so the selected one is the one just used.
        GameObject used = EventSystem.current.currentSelectedGameObject;
        foreach (MenuButtonEffect effect in FindObjectsByType<MenuButtonEffect>())
        {
            if (effect.gameObject == used)
            {
                effect.Lock(usedColor);
            }
            else
            {
                effect.Lock();
            }
        }
    }

    // Hovering selects the button, so mouse and keyboard share a single highlight.
    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // The button's own click still fires on release, so its handlers must ignore repeated calls.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            GetComponent<Button>().onClick.Invoke();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        highlighted = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        highlighted = false;
    }
}
