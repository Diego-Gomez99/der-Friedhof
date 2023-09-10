using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private AudioSource mainMenuAudioSource;
    [SerializeField] private string sceneName;
    [SerializeField] private Animator transitionAnimator;

    private void Start()
    {
        //transitionAnimator = GetComponentInChildren<Animator>();
        PlayMenuMusic();
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void LoadGame()
    {
        SceneManager.LoadScene("Final Scene");
        StartCoroutine (SceneLoad());
    }

    private void PlayMenuMusic()
    {
        mainMenuAudioSource.Play();
    }

    public IEnumerator SceneLoad()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneName);
    }
}
