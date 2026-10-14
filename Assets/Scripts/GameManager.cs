using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] public bool isGameOver = false;
    [SerializeField] private AudioSource soundTrackAudioSource;
    [SerializeField] private Color usedButtonColor = new Color(0.62f, 0f, 0f, 1f);
    [SerializeField] private float fadeDuration = 1f;
    private Animator fadedAnimator;
    private bool sceneLoading;


    private void Awake()
    {
        instance = this;
        gameOverPanel = GameObject.Find("Game Over Panel");
        gameOverPanel.SetActive(false);
       
    }

    // Start is called before the first frame update
    private void Start()
    {
        fadedAnimator = GetComponentInChildren<Animator>();
        StartGame();
    }

    private void StartGame()
    {
        isGameOver = false;
        PlaySoundTrack();
    }

    public void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
    }

    // Menu buttons click on press and again on release, so only the first call loads.
    public void LoadScene(string sceneName)
    {
        if (sceneLoading)
        {
            return;
        }
        sceneLoading = true;
        MenuButtonEffect.LockAll(usedButtonColor);
        SceneFader.LoadScene(sceneName, soundTrackAudioSource, 0f, fadeDuration);
    }

    public void LoadScene(int sceneNumber)
    {
        if (sceneLoading)
        {
            return;
        }
        sceneLoading = true;
        MenuButtonEffect.LockAll(usedButtonColor);
        SceneFader.LoadScene(sceneNumber, soundTrackAudioSource, 0f, fadeDuration);
    }

    private void PlaySoundTrack()
    {
        if (soundTrackAudioSource != null)
        {
            soundTrackAudioSource.Play();
        }
    }

    //public IEnumerator FadedSceneLoad()
    //{
    //    fadedAnimator.SetTrigger("StartTransition");
    //}

}
