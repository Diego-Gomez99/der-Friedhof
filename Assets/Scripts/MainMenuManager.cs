using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        MenuButtonEffect.LockAll(usedButtonColor);
        Application.Quit();
    }
    public void LoadGame()
    {
        if (buttonUsed)
        {
            return;
        }
        buttonUsed = true;

        MenuButtonEffect.LockAll(usedButtonColor);
        SceneFader.LoadScene(sceneName, mainMenuAudioSource, minimumLoadTime, fadeDuration);
    }

    private void PlayMenuMusic()
    {
        mainMenuAudioSource.Play();
    }
}
