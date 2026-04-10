using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour {

    [Header("References")]
    private PlayerClaimManager claimManager;
    private PlayerColorManager colorManager;
    private PlayerController playerController;
    private PlayerEffectManager effectManager;
    private PlayerGunManager gunManager;
    private PlayerHealthManager healthManager;
    private GameCore gameCore;
    private GameManager gameManager;
    private CodeManager codeManager;
    private Animator animator;
    private Coroutine menuFadeCoroutine;
    private bool isInitialized;

    [Header("HUD")]
    [SerializeField] private CanvasGroup playerHUD;
    [SerializeField] private float playerHUDFadeDuration;

    [Header("Claimables")]
    [SerializeField] private CanvasGroup claimablesInfoParent;
    [SerializeField] private float claimablesInfoFadeDuration;
    [SerializeField] private ClaimableInfo claimablesInfoPrefab;
    private List<ClaimableInfo> claimablesInfo;

    [Header("Multipliers")]
    [SerializeField] private CanvasGroup multipliersInfoParent;
    [SerializeField] private float multipliersInfoFadeDuration;
    [SerializeField] private MultiplierInfo multipliersInfoPrefab;
    private List<MultiplierInfo> multipliersInfo;

    [Header("Weapon HUD")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Gun Cycle")]
    [SerializeField] private CanvasGroup gunCycleParent;
    [SerializeField] private float gunCycleFadeDuration;
    [SerializeField] private Image gunIconTop;
    [SerializeField] private Image gunIconMiddle;
    [SerializeField] private Image gunIconBottom;
    [SerializeField] private Sprite blankGunSprite;

    [Header("Effects")]
    [SerializeField] private TMP_Text effectText;

    [Header("Health")]
    [SerializeField] private CanvasGroup healthBarParent;
    [SerializeField] private float healthBarFadeDuration;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image sliderFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private float healthLerpDuration;
    [SerializeField] private Gradient healthGradient;
    private Coroutine healthLerpCoroutine;

    [Header("Subtitles")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private GameObject subtitleArrow;
    [SerializeField] private float subtitleTypeDuration;
    private Coroutine subtitleTypeCoroutine;
    private Coroutine subtitleCycleCoroutine;
    private bool subtitleVisible;

    [Header("Pause Menu")]
    [SerializeField] private CanvasGroup pauseMenu;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMainMenuButton;
    private Coroutine pauseMenuCoroutine;

    [Header("Code")]
    [SerializeField] private CanvasGroup codeHUD;
    [SerializeField] private float codeHUDFadeDuration;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button codeHUDCloseButton;
    private VaultController vaultController;
    private bool codeHUDVisible;

    [Header("Loading")]
    [SerializeField] private CanvasGroup loadingScreen;
    [SerializeField] private float loadingScreenFadeDuration;
    [SerializeField] private TMP_Text loadingText;
    // [SerializeField] private float loadingTextDisplayDuration;
    private bool loadingScreenVisible;
    private Coroutine loadingScreenCoroutine;

    [Header("Level Cleared")]
    [SerializeField] private CanvasGroup levelClearedScreen;
    [SerializeField] private RectTransform buttonsLayout;
    [SerializeField] private float levelClearedFadeDuration;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Color disabledColor;

    public void Initialize() {

        gameCore = FindFirstObjectByType<GameCore>();
        gameManager = FindFirstObjectByType<GameManager>();
        animator = GetComponent<Animator>();

        // find the local player specifically; in multiplayer there will be multiple PlayerControllers so we use photonView.IsMine to get only the one this client controls
        foreach (PlayerController playerController in FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) {

            if (playerController.GetComponent<PhotonView>().IsMine) {

                this.playerController = playerController;
                claimManager = playerController.GetComponent<PlayerClaimManager>();
                colorManager = playerController.GetComponent<PlayerColorManager>();
                effectManager = playerController.GetComponent<PlayerEffectManager>();
                gunManager = playerController.GetComponent<PlayerGunManager>();
                healthManager = playerController.GetComponent<PlayerHealthManager>();
                break;

            }
        }

        // player HUD
        playerHUD.alpha = 0f; // reset alpha for fade
        playerHUD.gameObject.SetActive(true);
        StartCoroutine(FadeMenu(playerHUD, 1f, playerHUDFadeDuration)); // fade in player HUD

        DisableClaimablesInfoHUD(); // disabled by default
        DisableGunCycleHUD(); // disabled by default
        DisableHealthBarHUD(); // disabled by default

        // set health slider values
        healthSlider.maxValue = healthManager.GetMaxHealth();
        healthSlider.value = healthSlider.maxValue;

        // claimable info
        claimablesInfo = new List<ClaimableInfo>();

        Dictionary<Color, int> claimables = claimManager.GetClaims();

        foreach (KeyValuePair<Color, int> pair in claimables) {

            claimablesInfo.Add(Instantiate(claimablesInfoPrefab, claimablesInfoParent.transform)); // add to list
            claimablesInfo[^1].UpdateInfo(pair.Key, pair.Value, pair.Key == colorManager.GetCurrentPlayerColor().GetClaimColor()); // update info (select if claimable color is current claim color)

        }

        RefreshLayout(claimablesInfoParent.GetComponent<RectTransform>()); // refresh the layout to prevent UI bugs

        // multiplier info
        multipliersInfo = new List<MultiplierInfo>();

        List<EffectData> multipliers = effectManager.GetEffectMultipliers();

        foreach (EffectData data in multipliers) {

            multipliersInfo.Add(Instantiate(multipliersInfoPrefab, multipliersInfoParent.transform)); // add to list
            multipliersInfo[^1].UpdateInfo(data.GetEffectIcon(), data.GetEffectMultiplier()); // update info (select if claimable color is current claim color)

        }

        RefreshLayout(multipliersInfoParent.GetComponent<RectTransform>()); // refresh the layout to prevent UI bugs

        // subtitles
        subtitleArrow.SetActive(false); // hide arrow by default

        // pause menu
        pauseMenu.gameObject.SetActive(false);

        pauseResumeButton.onClick.AddListener(ClosePauseMenu); // close pause menu
        pauseRestartButton.onClick.AddListener(ReloadLevel); // reload level
        pauseMainMenuButton.onClick.AddListener(LoadMainMenu); // load main menu

        // code
        if (gameManager is LevelManager levelManager && levelManager.LevelHasCode()) { // make sure level has code to avoid null errors  (make sure game manager is level manager)

            codeManager = FindFirstObjectByType<CodeManager>();
            vaultController = FindFirstObjectByType<VaultController>();

            codeHUDCloseButton.onClick.AddListener(CloseCodeHUD);

            codeHUD.gameObject.SetActive(false);
            codeHUD.alpha = 0f;

        }

        codeInput.onValueChanged.AddListener(InputChanged);

        // loading
        loadingScreen.alpha = 1f; // reset alpha for fade
        loadingScreen.gameObject.SetActive(true);

        if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen coroutines
        loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(0f, loadingScreenFadeDuration, LoadingCompleteAction.OnLoadComplete)); // fade out loading screen

        loadingScreenVisible = true;

        // level cleared screen
        levelClearedScreen.gameObject.SetActive(false);
        levelClearedScreen.alpha = 0f;

        replayButton.onClick.AddListener(ReloadLevel);
        mainMenuButton.onClick.AddListener(LoadMainMenu);
        nextLevelButton.onClick.AddListener(LoadNextLevel);

        isInitialized = true;

        // notify managers that UIController is initialized; flushes any updates that were deferred because uiController wasn't assigned yet when they first fired
        healthManager.OnUIControllerInitialized(this);
        gunManager.OnUIControllerInitialized(this);

    }

    private void OnLoadComplete() {

        loadingScreen.gameObject.SetActive(false);
        gameManager.Initialize(); // initialize game manager to enable mechanics & UI
        loadingScreenVisible = false;

    }

    public void UpdateClaimablesHUD() {

        if (gameCore.IsQuitting()) return; // don't update claimables & multipliers HUD if game is quitting

        // claimables HUD
        Dictionary<Color, int> claimables = claimManager.GetClaims();

        foreach (ClaimableInfo info in claimablesInfo)
            info.UpdateInfo(claimables[info.GetColor()], info.GetColor() == colorManager.GetCurrentPlayerColor().GetClaimColor()); // update info (select if claimable color is current claim color)

        RefreshLayout(claimablesInfoParent.GetComponent<RectTransform>()); // refresh the layout to prevent UI bugs

        // multipliers HUD
        List<EffectData> multipliers = effectManager.GetEffectMultipliers();

        for (int i = 0; i < multipliersInfo.Count; i++)
            multipliersInfo[i].UpdateInfo(multipliers[i].GetEffectIcon(), multipliers[i].GetEffectMultiplier()); // update info (select if claimable color is current claim color)

        RefreshLayout(multipliersInfoParent.GetComponent<RectTransform>()); // refresh the layout to prevent UI bugs

    }

    public void UpdateGunHUD(Gun gun, int currGunIndex) {

        ammoText.text = gun.GetCurrentAmmo() + "/" + gun.GetMagazineSize();
        UpdateGunCycle(currGunIndex);

    }

    private void UpdateGunCycle(int currGunIndex) {

        List<Gun> guns = gunManager.GetGuns();

        // guns list is initialized in Awake but populated in Start; guard here in case
        // UpdateGunCycle is called in the same frame before Start has finished adding guns
        if (guns == null || guns.Count == 0) return;

        if (guns.Count == 1) {

            gunIconTop.sprite = blankGunSprite; // top gun is blank
            gunIconMiddle.sprite = guns[0].GetIcon(); // middle gun is equipped gun
            gunIconBottom.sprite = blankGunSprite; // bottom gun is blank
            return;

        }

        if (currGunIndex == 0) { // equipped gun is first gun

            gunIconTop.sprite = guns[^1].GetIcon(); // top gun is last gun
            gunIconMiddle.sprite = guns[currGunIndex].GetIcon(); // middle gun is equipped gun
            gunIconBottom.sprite = guns[currGunIndex + 1].GetIcon(); // bottom gun is second gun

        } else if (currGunIndex == guns.Count - 1) {

            gunIconTop.sprite = guns[currGunIndex - 1].GetIcon(); // top gun is second last gun
            gunIconMiddle.sprite = guns[currGunIndex].GetIcon(); // middle gun is equipped gun
            gunIconBottom.sprite = guns[0].GetIcon(); // bottom gun is first gun

        } else {

            gunIconTop.sprite = guns[currGunIndex - 1].GetIcon(); // top gun is previous gun
            gunIconMiddle.sprite = guns[currGunIndex].GetIcon(); // middle gun is equipped gun
            gunIconBottom.sprite = guns[currGunIndex + 1].GetIcon(); // bottom gun is next gun

        }

        RefreshLayout(gunCycleParent.GetComponent<RectTransform>()); // refresh the layout to prevent UI bugs

    }

    public void UpdateEffectText(PlayerColor playerColor) {

        effectText.text = playerColor.GetEffectType().ToString(); // update effect text
        effectText.color = playerColor.GetSpriteColor(); // update effect text color

    }

    public void UpdateHealth(HealthManager healthManager) { // health manager is passed in to ensure that the health update is for the correct player

        if (healthLerpCoroutine != null)
            StopCoroutine(healthLerpCoroutine);

        healthLerpCoroutine = StartCoroutine(LerpHealth(healthManager.GetCurrentHealth(), healthLerpDuration));

    }

    private IEnumerator LerpHealth(float targetHealth, float duration) {

        float currentTime = 0f;
        float startHealth = healthSlider.value;

        while (currentTime < duration) {

            currentTime += Time.deltaTime;
            healthSlider.value = Mathf.Lerp(startHealth, targetHealth, currentTime / duration);
            healthText.text = Mathf.CeilToInt(healthSlider.value) + ""; // health text is health rounded up
            sliderFill.color = healthGradient.Evaluate(healthSlider.normalizedValue); // normalizedValue returns the value between 0 and 1 (can't use DoTween here because of this line)
            yield return null;

        }

        healthSlider.value = targetHealth;
        healthLerpCoroutine = null;

    }

    public void TogglePause() {

        if (loadingScreenVisible || gameManager.IsLevelCompleted()) return; // can't pause while loading or while level is completed

        if (codeHUDVisible) { // close code HUD if it's open

            CloseCodeHUD();
            return;

        }

        CloseCodeHUD(); // close code HUD if it's open

        if (gameCore.IsPaused())
            ClosePauseMenu();
        else
            OpenPauseMenu();

    }

    private void OpenPauseMenu() {

        if (pauseMenuCoroutine != null) // stop coroutine if it's running
            StopCoroutine(pauseMenuCoroutine);

        if (subtitleVisible)
            subtitleText.gameObject.SetActive(false); // hide subtitle text if it's visible

        gameCore.PauseGame();
        playerController.DisableAllMechanics(); // disable all mechanics

        pauseMenuCoroutine = StartCoroutine(EnablePauseMenu()); // enable pause menu & handle animations

    }

    private void ClosePauseMenu() {

        if (pauseMenuCoroutine != null) // stop coroutine if it's running
            StopCoroutine(pauseMenuCoroutine);

        if (subtitleVisible)
            subtitleText.gameObject.SetActive(true); // show subtitle text if it was visible before

        // give player mechanics back before disabling pause menu to make game feel more responsive
        gameCore.UnpauseGame();
        playerController.EnableAllMechanics(); // enable all mechanics

        pauseMenuCoroutine = StartCoroutine(DisablePauseMenu(animator.GetCurrentAnimatorStateInfo(0).length)); // disable pause menu after animation ends

    }

    private IEnumerator EnablePauseMenu() {

        pauseMenu.gameObject.SetActive(true);

        animator.SetTrigger("openPauseMenu");
        yield return new WaitForEndOfFrame(); // wait for end of frame to make sure animation is playing
        StartCoroutine(FadeMenu(pauseMenu, 1f, animator.GetCurrentAnimatorStateInfo(0).length, unscaledTime: true)); // fade in pause menu with animation length (with unscaled time)

    }

    private IEnumerator DisablePauseMenu(float waitDuration) { // for disabling pause menu after animation

        animator.SetTrigger("closePauseMenu"); // animation should disable pause menu on complete
        yield return new WaitForEndOfFrame(); // wait for end of frame to make sure animation is playing
        StartCoroutine(FadeMenu(pauseMenu, 0f, animator.GetCurrentAnimatorStateInfo(0).length, unscaledTime: true)); // fade out pause menu with animation length (with unscaled time)

        yield return new WaitForSecondsRealtime(waitDuration); // realtime to ignore timescale
        pauseMenu.gameObject.SetActive(false);

        pauseMenuCoroutine = null;

    }

    public void OpenCodeHUD() {

        if (codeHUDVisible) return; // don't open code HUD if it's already open

        playerController.DisableAllMechanics(); // disable all mechanics
        codeHUD.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(codeHUD, 1f, codeHUDFadeDuration)); // fade in code HUD

        codeInput.text = ""; // clear input
        codeInput.ActivateInputField(); // select input field
        codeHUDVisible = true;

    }

    public void CloseCodeHUD() {

        if (!codeHUDVisible) return; // don't close code HUD if it's already closed

        codeHUD.gameObject.SetActive(true);

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(codeHUD, 0f, codeHUDFadeDuration)); // fade out code HUD

        playerController.EnableAllMechanics(); // enable all mechanics
        codeHUDVisible = false;

    }

    private void InputChanged(string input) {

        if (codeManager.CheckCode(input)) {

            vaultController.Open();
            CloseCodeHUD();

        }
    }

    public void OnLevelCleared() {

        // disable subtitles
        if (subtitleCycleCoroutine != null) {

            subtitleArrow.SetActive(false); // hide arrow after subtitles are done cycling
            StopCoroutine(subtitleCycleCoroutine);

        }

        SetSubtitleText("");

        playerController.DisableAllMechanics(); // disable player mechanics

        levelClearedScreen.gameObject.SetActive(true);

        if (!gameCore.HasNextLevel()) { // if there is no next level to load, disable next level button

            nextLevelButton.interactable = false;
            nextLevelButton.GetComponentInChildren<TMP_Text>().color = disabledColor; // change text color to indicate button is disabled

        }

        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine); // stop any existing menu fade coroutines
        menuFadeCoroutine = StartCoroutine(FadeMenu(levelClearedScreen, 1f, levelClearedFadeDuration)); // fade in level cleared screen

        RefreshLayout(levelClearedScreen.GetComponent<RectTransform>()); // refresh the level cleared screen layout
        RefreshLayout(buttonsLayout); // refresh the button layout

    }

    private void LoadMainMenu() {

        ClosePauseMenu(); // make sure game is unpaused before loading level

        levelClearedScreen.gameObject.SetActive(false);
        loadingScreen.gameObject.SetActive(true);

        if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen coroutines
        loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(1f, loadingScreenFadeDuration, LoadingCompleteAction.FinishMainMenuLoad)); // fade in loading screen (no need for unscaled time since the game is unpaused when the pause menu is closed)

        //loadingTextCoroutine = StartCoroutine(UpdateLoadingText()); // REMEMBER TO STOP THIS COROUTINE BEFORE NEW SCENE LOADS
        gameCore.StartLoadMainMenuAsync(); // load first level

    }

    private void FinishMainMenuLoad() {

        //StopCoroutine(loadingTextCoroutine); // IMPORTANT TO PREVENT COROUTINE FROM CYCLING INFINITELY
        gameCore.FinishMainMenuLoad();

    }

    private void LoadNextLevel() {

        ClosePauseMenu(); // make sure game is unpaused before loading level

        if (gameCore.StartLoadLevelAsync(gameManager.GetLevelIndex() + 1)) { // make sure level loads

            levelClearedScreen.gameObject.SetActive(false);
            loadingScreen.gameObject.SetActive(true);

            if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen coroutines
            loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(1f, loadingScreenFadeDuration, LoadingCompleteAction.FinishLevelLoad)); // fade in loading screen (no need for unscaled time since the game is unpaused when the pause menu is closed)

            //loadingTextCoroutine = StartCoroutine(UpdateLoadingText()); // REMEMBER TO STOP THIS COROUTINE BEFORE NEW SCENE LOADS

        }
    }

    private void ReloadLevel() {

        ClosePauseMenu(); // make sure game is unpaused before loading level

        levelClearedScreen.gameObject.SetActive(false);
        loadingScreen.gameObject.SetActive(true);

        if (loadingScreenCoroutine != null) StopCoroutine(loadingScreenCoroutine); // stop any existing loading screen coroutines
        loadingScreenCoroutine = StartCoroutine(FadeLoadingScreen(1f, loadingScreenFadeDuration, LoadingCompleteAction.FinishLevelLoad)); // fade in loading screen (no need for unscaled time since the game is unpaused when the pause menu is closed)

        //loadingTextCoroutine = StartCoroutine(UpdateLoadingText()); // REMEMBER TO STOP THIS COROUTINE BEFORE NEW SCENE LOADS
        gameCore.StartLoadLevelAsync(-1); // pass -1 to reload level

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

    public void SetSubtitleText(string text, bool stopCycle = true) {

        if (text == null || text == "") { // if text is empty, hide subtitle text

            HideSubtitleText();
            return;

        }

        if (stopCycle && subtitleCycleCoroutine != null) { // stop cycle coroutine if it's running and boolean is true

            subtitleArrow.SetActive(false); // hide arrow after subtitles are done cycling
            StopCoroutine(subtitleCycleCoroutine);

        }

        subtitleText.gameObject.SetActive(true);

        if (subtitleTypeCoroutine != null) StopCoroutine(subtitleTypeCoroutine); // stop any existing subtitle type coroutines
        subtitleTypeCoroutine = StartCoroutine(TypeSubtitleText(text, subtitleTypeDuration)); // type subtitle text with animation

        subtitleVisible = true;

    }

    private IEnumerator TypeSubtitleText(string text, float duration) {

        float currentTime = 0f;

        while (currentTime < duration) {

            int charCount = Mathf.FloorToInt((currentTime / duration) * text.Length);
            subtitleText.text = text[..charCount];
            currentTime += Time.deltaTime;
            yield return null;

        }

        subtitleText.text = text; // make sure the full text is set at the end
        subtitleTypeCoroutine = null;

    }

    public void StartCycleSubtitleTexts(string[] subtitleTexts, float duration) {

        if (subtitleCycleCoroutine != null) { // stop cycle coroutine if it's running

            subtitleArrow.SetActive(false); // hide arrow after subtitles are done cycling
            StopCoroutine(subtitleCycleCoroutine);

        }

        subtitleCycleCoroutine = StartCoroutine(CycleSubtitleTexts(subtitleTexts, duration));

    }

    private IEnumerator CycleSubtitleTexts(string[] subtitleTexts, float duration) {

        subtitleArrow.SetActive(true); // show arrow because there is more than one subtitle text

        while (true) {

            foreach (string subtitleText in subtitleTexts) {

                SetSubtitleText(subtitleText, false); // update subtitle text
                yield return new WaitForSeconds(duration); // wait for duration

            }
        }
    }

    public void HideSubtitleText() { // IMPORTANT: use this to disable subtitle text

        subtitleVisible = false;
        subtitleArrow.SetActive(false); // hide arrow
        subtitleText.gameObject.SetActive(false);

    }

    public void SetLoadingText(string text) => loadingText.text = text;

    public void EnableClaimablesInfoHUD() {

        claimablesInfoParent.gameObject.SetActive(true);
        StartCoroutine(FadeElement(claimablesInfoParent, 1f, claimablesInfoFadeDuration)); // fade in claimables info parent

    }

    public void DisableClaimablesInfoHUD() {

        claimablesInfoParent.gameObject.SetActive(false);
        claimablesInfoParent.alpha = 0f; // reset alpha for fade

    }

    public void EnableGunCycleHUD() {

        gunCycleParent.gameObject.SetActive(true);
        StartCoroutine(FadeElement(gunCycleParent, 1f, gunCycleFadeDuration)); // fade in gun cycle parent

    }

    public void DisableGunCycleHUD() {

        gunCycleParent.gameObject.SetActive(false);
        gunCycleParent.alpha = 0f; // reset alpha for fade

    }

    public void EnableHealthBarHUD() {

        healthBarParent.gameObject.SetActive(true);
        StartCoroutine(FadeElement(healthBarParent, 1f, healthBarFadeDuration)); // fade in health bar parent

    }

    public void DisableHealthBarHUD() {

        healthBarParent.gameObject.SetActive(false);
        healthBarParent.alpha = 0f; // reset alpha for fade

    }

    public bool IsInitialized() => isInitialized;

    private IEnumerator FadeMenu(CanvasGroup menu, float targetOpacity, float fadeDuration, bool unscaledTime = false) {

        float currentTime = 0f;
        float startOpacity = menu.alpha;

        while (currentTime < fadeDuration) {

            menu.alpha = Mathf.Lerp(startOpacity, targetOpacity, currentTime / fadeDuration);
            currentTime += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;

        }

        menu.alpha = targetOpacity; // make sure the final opacity is set to the target opacity

        // if the target opacity is 0, disable the canvas group
        if (targetOpacity == 0f)
            menu.gameObject.SetActive(false);

        menuFadeCoroutine = null; // reset the coroutine reference when done

    }

    // for elements, since the fade in requires a coroutine and fade out is instant, there is no need to store the coroutine reference for fade in (and therefore stop it if it's running)
    private IEnumerator FadeElement(CanvasGroup element, float targetOpacity, float fadeDuration) {

        float currentTime = 0f;
        float initialOpacity = element.alpha;

        while (currentTime < fadeDuration) {

            element.alpha = Mathf.Lerp(initialOpacity, targetOpacity, currentTime / fadeDuration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        element.alpha = targetOpacity; // make sure the final opacity is set to the target opacity

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

            case LoadingCompleteAction.OnLoadComplete:
                OnLoadComplete();
                break;

            case LoadingCompleteAction.FinishMainMenuLoad:
                FinishMainMenuLoad();
                break;

            case LoadingCompleteAction.FinishLevelLoad:
                FinishLevelLoad();
                break;

        }

        loadingScreenCoroutine = null;

    }

    // IMPORTANT: this only works when the root is visible
    protected void RefreshLayout(RectTransform root) {

        foreach (LayoutGroup layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());

        LayoutRebuilder.ForceRebuildLayoutImmediate(root); // force a rebuild of the root layout at the end

    }

    private enum LoadingCompleteAction {

        OnLoadComplete,
        FinishMainMenuLoad,
        FinishLevelLoad

    }
}
