using Photon.Pun;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour {

    [Header("References")]
    [SerializeField] private GunData gunData;
    private GameCore gameCore;
    private LevelAudioManager audioManager;
    private Animator animator;
    private UIController uiController;

    [Header("Shooting")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab; // must be inside the Resources/ folder
    private EntityType entityType;
    private LayerMask shootableMask;
    private int gunIndex;
    private int currAmmo;
    private bool isReloading;
    private bool shotReady;

    [Header("Tracer")]
    [SerializeField] private LineRenderer bulletTracer;
    [SerializeField] private float bulletTracerDisplayDuration;
    private Coroutine tracerCoroutine; // stored so rapid fire doesn't stack coroutines and cause flashing

    [Header("Impact")]
    [SerializeField] private GameObject impactEffect;
    private new Collider2D collider;

    /*
    IMPORTANT: RELOAD ANIMATION MUST BE 1 SECOND LONG FOR SCALING TO WORK
    */

    // start function
    public void Initialize(EntityType entityType, LayerMask shootableMask, Collider2D collider, int gunIndex) {

        gameCore = FindFirstObjectByType<GameCore>();
        audioManager = FindFirstObjectByType<LevelAudioManager>();
        animator = GetComponent<Animator>();

        // only get UI reference for player guns (phantoms don't need UI)
        if (entityType == EntityType.Player)
            uiController = FindFirstObjectByType<UIController>();

        this.entityType = entityType;
        this.shootableMask = shootableMask;
        this.collider = collider;
        this.gunIndex = gunIndex;

        currAmmo = gunData.GetMagazineSize();
        shotReady = true;

    }

    // returns the tracer end point if this is a raycast gun so PlayerGunManager can RPC it to other clients, or null if no tracer should be shown
    // onTracerFired: called when a raycast shot fires so PlayerGunManager can RPC the tracer to other clients
    // onReloadStarted: called when auto-reload triggers on empty so PlayerGunManager can RPC the animation to other clients
    public IEnumerator Shoot(float multiplier = 1f, System.Action<Vector2, Vector2> onTracerFired = null, System.Action onReloadStarted = null) {

        if (isReloading || !shotReady) yield break; // don't check if ammo is greater than 0 because reload is handled after this

        // reload if out of ammo
        if (currAmmo == 0) {

            StartReload();
            onReloadStarted?.Invoke(); // notify PlayerGunManager so it can RPC the animation to other clients
            yield break;

        }

        shotReady = false;

        if (gunData.GetShootSound() != null)
            audioManager.PlaySound(gunData.GetShootSound()); // play shoot sound

        if (gunData.UsesRaycastShooting()) {

            RaycastHit2D shootableHit = Physics2D.Raycast(muzzle.position, muzzle.right, gunData.GetMaxRange(), shootableMask); // for checking if a shootable is hit
            RaycastHit2D obstacleHit = Physics2D.Raycast(muzzle.position, muzzle.right, gunData.GetMaxRange(), gameCore.GetEnvironmentMask()); // for checking if an obstacle is in the way

            Vector2 tracerStart = muzzle.position;
            Vector2 tracerEnd;

            if (obstacleHit && (Vector2.Distance(muzzle.position, obstacleHit.point) <= Vector2.Distance(muzzle.position, shootableHit.point) || !shootableHit)) { // obstacle in the way or shot didn't hit shootable, but hit obstacle

                // impact effect
                Instantiate(impactEffect, obstacleHit.point, Quaternion.identity);
                tracerEnd = obstacleHit.point;

            } else if (shootableHit) {

                // route damage through RequestTakeDamage so it goes via the correct RPC target
                // (player damage -> photonView.Owner, phantom damage -> MasterClient)
                if (shootableHit.transform.TryGetComponent(out PlayerHealthManager playerHealth))
                    playerHealth.RequestTakeDamage(gunData.GetDamage() * multiplier);
                else if (shootableHit.transform.TryGetComponent(out PhantomHealthManager phantomHealth))
                    phantomHealth.RequestTakeDamage(gunData.GetDamage() * multiplier);

                // impact effect (always show since we can't know death outcome across the network)
                Instantiate(impactEffect, shootableHit.point, Quaternion.identity);
                tracerEnd = shootableHit.point;

            } else { // miss

                tracerEnd = (Vector2) muzzle.position + (Vector2) muzzle.right * gunData.GetMaxRange();
                // tracerEnd = (Vector2) muzzle.position + (Vector2) muzzle.right * 100f; // illusion for infinite length tracer when missed

            }

            ShowTracer(tracerStart, tracerEnd); // show tracer locally
            onTracerFired?.Invoke(tracerStart, tracerEnd); // notify PlayerGunManager of the endpoints so it can RPC them to other clients

        } else {

            // use PhotonNetwork.Instantiate so the bullet is visible on all clients
            Bullet bullet = PhotonNetwork.Instantiate(bulletPrefab.name, muzzle.position, muzzle.rotation).GetComponent<Bullet>();
            bullet.Initialize(gunData.GetDamage() * multiplier, muzzle.position, gunData.GetMaxRange(), collider); // initialize bullet

        }

        currAmmo--;

        yield return new WaitForSeconds(1 / gunData.GetFireRate()); // use fire rate to prevent shooting
        shotReady = true;

    }

    // shows the tracer on this client (called both locally after shooting and on remote clients via RPC)
    public void ShowTracer(Vector2 start, Vector2 end) {

        bulletTracer.SetPosition(0, start);
        bulletTracer.SetPosition(1, end);

        // stop any existing tracer coroutine before starting a new one to prevent rapid-fire flashing
        if (tracerCoroutine != null)
            StopCoroutine(tracerCoroutine);

        tracerCoroutine = StartCoroutine(DisplayTracer());

    }

    private IEnumerator DisplayTracer() {

        bulletTracer.enabled = true;
        yield return new WaitForSeconds(bulletTracerDisplayDuration);
        bulletTracer.enabled = false;
        tracerCoroutine = null;

    }

    private bool CanReload() => currAmmo < gunData.GetMagazineSize() && !isReloading;

    public void StartReload() => StartCoroutine(Reload());

    private IEnumerator Reload() {

        if (!CanReload()) yield break;

        isReloading = true;

        // play the visual part of the reload on this client
        yield return StartCoroutine(PlayReloadAnimation());

        currAmmo = GetMagazineSize(); // reload gun

        if (entityType == EntityType.Player) // only update UI for player guns
            uiController.UpdateGunHUD(this, gunIndex); // update ui

        isReloading = false;
        shotReady = true; // reset fire rate cooldown

    }

    // plays only the visual part of the reload (sound + animation); no ammo or UI changes
    // called on remote clients via RPC so the animation shows without affecting their local state
    public IEnumerator PlayReloadAnimation() {

        if (gunData.GetReloadSound() != null)
            audioManager.PlaySound(gunData.GetReloadSound()); // play reload sound

        // uiController.SetAmmoReloadingText(); // for notifying player of reload

        animator.SetTrigger("reload"); // trigger animation

        yield return new WaitForEndOfFrame(); // wait for end of frame so animation starts playing
        animator.speed = 1f / gunData.GetReloadTime(); // scale animation speed by reload time
        yield return new WaitForSeconds(gunData.GetReloadTime()); // wait for reload duration

    }

    public void InstantReload() {

        if (!CanReload()) return;

        currAmmo = GetMagazineSize(); // reload gun

        if (entityType == EntityType.Player) // player is reloading
            uiController.UpdateGunHUD(this, gunIndex); // update ui

    }

    public Sprite GetIcon() => gunData.GetIcon();

    public bool IsAutomatic() => gunData.IsAutomatic();

    public int GetCurrentAmmo() => currAmmo;

    public int GetMagazineSize() => gunData.GetMagazineSize();

    public bool IsReloading() => isReloading;

}
