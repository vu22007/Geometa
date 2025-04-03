using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource audioSourceLobby;
    private AudioSource audioSourceGame;

    [SerializeField] private AudioClip audioClipLobby;
    [SerializeField] private AudioClip audioClipGame;

    private string previousScene;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSourceLobby = gameObject.AddComponent<AudioSource>();
            audioSourceLobby.resource = audioClipLobby;
            audioSourceLobby.volume = 0.8f;
            audioSourceGame = gameObject.AddComponent<AudioSource>();
            audioSourceGame.resource = audioClipGame;
            audioSourceGame.volume = 0.8f;
            audioSourceLobby.loop = true;
            audioSourceGame.loop = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        previousScene = SceneManager.GetActiveScene().name;
        audioSourceLobby.Play();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "MapTest" || scene.name == "GenerateMap")
        {
            Debug.Log("Player game");
            audioSourceLobby.Stop();
            audioSourceGame.Play();
        }

        if((previousScene == "MapTest" || previousScene == "GenerateMap") 
                & scene.name != "MapTest" & scene.name != "GenerateMap")
        {
            Debug.Log("Play lobby");
            audioSourceGame.Stop();
            audioSourceLobby.Play();
        }

        previousScene = scene.name;
    }
}
