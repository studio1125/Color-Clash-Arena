using UnityEngine;

public class MenuManager : MonoBehaviour {

    [Header("Loading")]
    private static bool firstLoadCompleted;

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [RuntimeInitializeOnLoadMethod]
    private static void OnFirstLoad() => firstLoadCompleted = true; // only gets called on first load of game

    public bool IsFirstLoadCompleted() { return firstLoadCompleted; }

    public AudioClip GetBackgroundMusic() { return backgroundMusic; }

}
