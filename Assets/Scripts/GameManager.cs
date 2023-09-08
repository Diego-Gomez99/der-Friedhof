using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private AudioSource soundTrackAudioSource;

    private void Awake()
    {
        instance = this;
        gameOverPanel = GameObject.Find("Game Over Panel");
        gameOverPanel.SetActive(false);
    }

    // Start is called before the first frame update
    private void Start()
    {
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

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneNumber)
    {
        SceneManager.LoadScene(sceneNumber);
    }

    private void Update()
    {
        if (isGameOver)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void PlaySoundTrack()
    {
        soundTrackAudioSource.Play();
    }
}
