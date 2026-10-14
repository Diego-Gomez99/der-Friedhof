using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Full-screen fades: loads a scene behind a fade to black and back, or fades to black and quits.
public static class SceneFader
{
    public static void LoadScene(string sceneName, AudioSource music, float minimumLoadTime, float fadeDuration)
    {
        Fade(SceneManager.LoadSceneAsync(sceneName), music, minimumLoadTime, fadeDuration);
    }

    public static void LoadScene(int sceneIndex, AudioSource music, float minimumLoadTime, float fadeDuration)
    {
        Fade(SceneManager.LoadSceneAsync(sceneIndex), music, minimumLoadTime, fadeDuration);
    }

    // Fades to black, then stops Play mode in the editor or closes the built game.
    public static void Quit(AudioSource music, float fadeDuration)
    {
        Image fadeImage = CreateFadeOverlay();
        fadeImage.StartCoroutine(FadeAndQuit(fadeImage, music, fadeDuration));
    }

    private static void Fade(AsyncOperation load, AudioSource music, float minimumLoadTime, float fadeDuration)
    {
        load.allowSceneActivation = false;
        Image fadeImage = CreateFadeOverlay();
        // The overlay outlives the current scene, so it runs the coroutine.
        fadeImage.StartCoroutine(SceneLoad(fadeImage, load, music, minimumLoadTime, fadeDuration));
    }

    private static Image CreateFadeOverlay()
    {
        GameObject canvasObject = new GameObject("Scene Fade", typeof(Canvas), typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(canvasObject);

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

        // The image also blocks clicks on the old scene while it is on screen.
        Image image = imageObject.GetComponent<Image>();
        image.color = Color.clear;
        return image;
    }

    private static IEnumerator SceneLoad(Image fadeImage, AsyncOperation load, AudioSource music,
        float minimumLoadTime, float fadeDuration)
    {
        float startTime = Time.unscaledTime;

        // Progress stops at 0.9 until the scene is allowed to activate.
        while (load.progress < 0.9f || Time.unscaledTime - startTime < minimumLoadTime)
        {
            yield return null;
        }

        yield return FadeToBlack(fadeImage, music, fadeDuration);

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
        Object.Destroy(fadeImage.transform.parent.gameObject);
    }

    private static IEnumerator FadeAndQuit(Image fadeImage, AudioSource music, float fadeDuration)
    {
        yield return FadeToBlack(fadeImage, music, fadeDuration);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fades the overlay to black and the music to silence, then stops the music.
    private static IEnumerator FadeToBlack(Image fadeImage, AudioSource music, float fadeDuration)
    {
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
    }

    // Clamped so a long frame cannot skip the fade.
    private static float FadeStep(float duration)
    {
        return Mathf.Min(Time.unscaledDeltaTime, 0.05f) / duration;
    }
}
