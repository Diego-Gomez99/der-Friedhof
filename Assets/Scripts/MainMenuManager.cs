using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private AudioSource mainMenuAudioSource;
    [SerializeField] private string sceneName;
    [SerializeField] private Color usedButtonColor = new Color(0.62f, 0f, 0f, 1f);
    [SerializeField] private float minimumLoadTime = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private bool buttonUsed;

    private void Start()
    {
        PlayMenuMusic();
    }
    public void QuitGame()
    {
        if (buttonUsed)
        {
            return;
        }
        buttonUsed = true;

        LockButtons();
        Application.Quit();
    }
    public void LoadGame()
    {
        if (buttonUsed)
        {
            return;
        }
        buttonUsed = true;

        LockButtons();
        Image fadeImage = CreateFadeOverlay();
        // The overlay outlives this scene, so it runs the coroutine instead of this object.
        fadeImage.StartCoroutine(SceneLoad(fadeImage, mainMenuAudioSource, sceneName, minimumLoadTime, fadeDuration));
    }

    private void PlayMenuMusic()
    {
        mainMenuAudioSource.Play();
    }

    // Stops the hover effect on every button and darkens the one that was used.
    private void LockButtons()
    {
        // Hovering or navigating to a button selects it, so the selected one is the one just used.
        GameObject used = EventSystem.current.currentSelectedGameObject;
        foreach (Button button in FindObjectsOfType<Button>())
        {
            MenuButtonEffect effect = button.GetComponent<MenuButtonEffect>();
            if (effect == null)
            {
                continue;
            }
            if (button.gameObject == used)
            {
                effect.Lock(usedButtonColor);
            }
            else
            {
                effect.Lock();
            }
        }
    }

    private static Image CreateFadeOverlay()
    {
        GameObject canvasObject = new GameObject("Scene Fade", typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject imageObject = new GameObject("Image", typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // The image also blocks clicks on the menu while it is on screen.
        Image image = imageObject.GetComponent<Image>();
        image.color = Color.clear;
        return image;
    }

    private static IEnumerator SceneLoad(Image fadeImage, AudioSource music, string sceneName,
        float minimumLoadTime, float fadeDuration)
    {
        float startTime = Time.unscaledTime;
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        // Progress stops at 0.9 until the scene is allowed to activate.
        while (load.progress < 0.9f || Time.unscaledTime - startTime < minimumLoadTime)
        {
            yield return null;
        }

        float startVolume = music != null ? music.volume : 0f;
        for (float t = 0f; t < 1f; t += FadeStep(fadeDuration))
        {
            fadeImage.color = new Color(0f, 0f, 0f, t);
            if (music != null)
            {
                music.volume = Mathf.Lerp(startVolume, 0f, t);
            }
            yield return null;
        }
        fadeImage.color = Color.black;
        if (music != null)
        {
            music.Stop();
        }

        load.allowSceneActivation = true;
        while (!load.isDone)
        {
            yield return null;
        }
        // Skip the frame that carries the loading hitch.
        yield return null;

        for (float t = 0f; t < 1f; t += FadeStep(fadeDuration))
        {
            fadeImage.color = new Color(0f, 0f, 0f, 1f - t);
            yield return null;
        }
        Destroy(fadeImage.transform.parent.gameObject);
    }

    // Clamped so a long frame cannot skip the fade.
    private static float FadeStep(float duration)
    {
        return Mathf.Min(Time.unscaledDeltaTime, 0.05f) / duration;
    }
}
