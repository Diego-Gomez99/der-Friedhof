using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Loads a scene behind a full-screen fade: fades to black, switches scenes, then fades back in.
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
        Object.Destroy(fadeImage.transform.parent.gameObject);
    }

    // Clamped so a long frame cannot skip the fade.
    private static float FadeStep(float duration)
    {
        return Mathf.Min(Time.unscaledDeltaTime, 0.05f) / duration;
    }
}
