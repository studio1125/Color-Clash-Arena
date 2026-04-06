using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

    [Header("References")]
    private GameCore gameCore;
    private MenuManager menuManager;
    private Coroutine menuFadeCoroutine;

    [Header("Menu")]
    [SerializeField] private CanvasGroup menuScreen;
    [SerializeField] private float menuScreenFadeDuration;
    [SerializeField] private RectTransform menuButtonsLayout;
    [SerializeField] private Button playButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button instructionsButton;
    [SerializeField] private Button quitButton;

    [Header("Instructions")]
    [SerializeField] private CanvasGroup instructionsScreen;
    [SerializeField] private float instructionsScreenFadeDuration;
    [SerializeField] private Button instructionsCloseButton;

    [Header("Lobby")]
    [SerializeField] private CanvasGroup lobbyScreen;
    [SerializeField] private float lobbyScreenFadeDuration;
    [SerializeField] private Button lobbyScreenCloseButton;

    [Header("Connecting")]
    [SerializeField] private CanvasGroup connectingScreen;
    [SerializeField] private float connectingScreenFadeDuration;

    [Header("Loading")]
    [SerializeField] private CanvasGroup loadingScreen;
    [SerializeField] private float loadingScreenFadeDuration;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float loadingTextDisplayDuration;
    //private Coroutine loadingTextCoroutine;
    private Coroutine loadingScreenCoroutine;

    private void Start() {

        gameCore = FindFirstObjectByType<GameCore>();
        menuManager = FindFirstObjectByType<MenuManager>();

        // menu
        playButton.onClick.AddListener(Play);
        tutorialButton.onClick.AddListener(PlayTutorial);
        instructionsButton.onClick.AddListener(ShowInstructions);
        quitButton.onClick.AddListener(Quit);

        // instructions
        instructionsCloseButton.onClick.AddListener(HideInstructions);

        instructionsScreen.gameObject.SetActive(false);
        instructionsScreen.alpha = 0f;

        // lobby
        lobbyScreenCloseButton.onClick.AddListener(HideLobbyScreen);

        lobbyScreen.gameObject.SetActive(false);
        lobbyScreen.alpha = 0f;

        // connecting
        if (!PhotonNetwork.IsConnectedAndReady) {

            menuScreen.gameObject.SetActive(false); // make sure menu is disabled while connecting screen is active

            connectingScreen.alpha = 1f; // reset alpha for fade
            connectingScreen.gameObject.SetActive(true);

            StartCoroutine(HandleConnection()); // fade out connecting screen when connected

        }

        // loading
        if (!menuManager.IsFirstLoadCompleted()) { // don't fade in loading screen on first load (connection occurs on first load, so the connecting screen is shown instead)

            loadingScreen.alpha = 1f; // reset alpha for fade
            loadingScreen.gameObject.SetActive(true);

            if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen fade coroutines
            loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(0f, loadingScreenFadeDuration, LoadingCompleteAction.DisableLoadingScreen, true)); // fade out loading screen on first load

        }
    }

    private void Play() {

        menuScreen.gameObject.SetActive(false);
        //loadingScreen.gameObject.SetActive(true);

        //if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen fade coroutines
        //loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(1f, loadingScreenFadeDuration, LoadingCompleteAction.FinishLevelLoad)); // fade in loading screen and finish level load when done

        ShowLobbyScreen();

        //loadingTextCoroutine = StartCoroutine(UpdateLoadingText()); // REMEMBER TO STOP THIS COROUTINE BEFORE NEW SCENE LOADS
        gameCore.UnpauseGame(); // make sure game is unpaused before loading level
        //gameCore.StartLoadLevelAsync(1); // load first level (level index 0 is tutorial)

    }

    private void PlayTutorial() {

        menuScreen.gameObject.SetActive(false);
        loadingScreen.gameObject.SetActive(true);

        if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen fade coroutines
        loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(1f, loadingScreenFadeDuration, LoadingCompleteAction.FinishLevelLoad)); // fade in loading screen and finish level load when done

        //loadingTextCoroutine = StartCoroutine(UpdateLoadingText()); // REMEMBER TO STOP THIS COROUTINE BEFORE NEW SCENE LOADS
        gameCore.UnpauseGame(); // make sure game is unpaused before loading level
        gameCore.StartLoadLevelAsync(0); // load tutorial (level index 0 is tutorial)

    }

    private void FinishLevelLoad() {

        //StopCoroutine(loadingTextCoroutine); // IMPORTANT TO PREVENT COROUTINE FROM CYCLING INFINITELY
        gameCore.FinishLevelLoad();

    }

    /*
    for adding ... after loading in a cycle (doesn't look good with current font)
    private IEnumerator UpdateLoadingText() {

        while (true) {

            for (int i = 0; i < 4; i++) {

                switch (i) {

                    case 0:

                        loadingText.text = "Loading";
                        break;

                    case 1:

                        loadingText.text = "Loading.";
                        break;

                    case 2:

                        loadingText.text = "Loading..";
                        break;

                    case 3:

                        loadingText.text = "Loading...";
                        break;

                }

                yield return new WaitForSeconds(loadingTextDisplayDuration);

            }

            yield return null;

        }
    }
    */

    private void ShowInstructions() {

        menuScreen.gameObject.SetActive(false);
        menuScreen.alpha = 0f; // reset alpha for fade

        instructionsScreen.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(instructionsScreen, 1f, instructionsScreenFadeDuration)); // fade in instructions

        RefreshLayout(instructionsScreen.GetComponent<RectTransform>()); // refresh instructions screen layout

    }

    private void HideInstructions() {

        instructionsScreen.gameObject.SetActive(false);
        instructionsScreen.alpha = 0f;

        menuScreen.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(menuScreen, 1f, menuScreenFadeDuration)); // fade in menu

    }

    private void ShowLobbyScreen() {

        menuScreen.gameObject.SetActive(false);
        menuScreen.alpha = 0f; // reset alpha for fade

        lobbyScreen.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(lobbyScreen, 1f, lobbyScreenFadeDuration)); // fade in lobby screen

        RefreshLayout(lobbyScreen.GetComponent<RectTransform>()); // refresh lobby screen layout

    }

    private void HideLobbyScreen() {

        lobbyScreen.gameObject.SetActive(false);
        lobbyScreen.alpha = 0f;

        menuScreen.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(menuScreen, 1f, menuScreenFadeDuration)); // fade in menu

    }

    private IEnumerator FadeMenu(CanvasGroup menu, float targetOpacity, float fadeDuration) {

        float currentTime = 0f;
        float startOpacity = menu.alpha;

        while (currentTime < fadeDuration) {

            currentTime += Time.deltaTime;
            menu.alpha = Mathf.Lerp(startOpacity, targetOpacity, currentTime / fadeDuration);
            yield return null;

        }

        menu.alpha = targetOpacity; // make sure the final opacity is set to the target opacity

        // if the target opacity is 0, disable the canvas group
        if (targetOpacity == 0f)
            menu.gameObject.SetActive(false);

        menuFadeCoroutine = null; // reset the coroutine reference when done

    }

    private IEnumerator FadeLoadingScreen(float targetOpacity, float fadeDuration, LoadingCompleteAction action, bool unscaledTime = false) {

        float currentTime = 0f;
        float initialOpacity = loadingScreen.alpha;

        while (currentTime < fadeDuration) {

            loadingScreen.alpha = Mathf.Lerp(initialOpacity, targetOpacity, currentTime / fadeDuration);
            currentTime += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;

        }

        loadingScreen.alpha = targetOpacity; // make sure the final opacity is set to the target opacity

        if (targetOpacity == 0f)
            loadingScreen.gameObject.SetActive(false);

        switch (action) {

            case LoadingCompleteAction.FinishLevelLoad:
                FinishLevelLoad();
                break;

            case LoadingCompleteAction.DisableLoadingScreen:
                loadingScreen.gameObject.SetActive(false);
                break;

        }

        loadingScreenCoroutine = null;

    }

    private IEnumerator HandleConnection() {

        // wait until connected to lobby before fading out connecting screen and showing menu
        while (!PhotonNetwork.InLobby)
            yield return null;

        float currentTime = 0f;
        float initialOpacity = loadingScreen.alpha;

        while (currentTime < connectingScreenFadeDuration) {

            connectingScreen.alpha = Mathf.Lerp(initialOpacity, 0f, currentTime / connectingScreenFadeDuration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        connectingScreen.alpha = 0f; // make sure the final opacity is set to 0
        connectingScreen.gameObject.SetActive(false);

        menuScreen.alpha = 0f; // reset alpha for fade
        menuScreen.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(menuScreen, 1f, menuScreenFadeDuration)); // fade in menu

        RefreshLayout(menuScreen.GetComponent<RectTransform>()); // refresh menu screen layout
        RefreshLayout(menuButtonsLayout); // refresh menu buttons layout

    }

    private void Quit() => Application.Quit();

    public void SetLoadingText(string text) => loadingText.text = text;

    // IMPORTANT: this only works when the root is visible
    protected void RefreshLayout(RectTransform root) {

        foreach (LayoutGroup layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());

        LayoutRebuilder.ForceRebuildLayoutImmediate(root); // force a rebuild of the root layout at the end

    }

    private enum LoadingCompleteAction {

        FinishLevelLoad,
        DisableLoadingScreen

    }
}
