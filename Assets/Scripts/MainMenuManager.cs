using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private AudioSource mainMenuAudioSource;

    private void Start()
    {
        PlayMenuMusic();
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void LoadGame()
    {
        print("Final Scene");
        SceneManager.LoadScene("Final Scene");
    }

    private void PlayMenuMusic()
    {
        mainMenuAudioSource.Play();
    }
}
