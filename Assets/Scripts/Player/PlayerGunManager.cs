using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PlayerController))]
public class PlayerGunManager : MonoBehaviourPun {

    [Header("References")]
    private PlayerColorManager colorManager;
    private PlayerController playerController;
    private PlayerEffectManager effectManager;
    private new Collider2D collider;
    private UIController uiController;

    [Header("Aiming")]
    [SerializeField] private bool useMouseAiming;
    [SerializeField] private float reloadAimMultiplier;

    [Header("Guns")]
    [SerializeField] private List<Gun> starterGuns; // DON'T USE GUNS FROM THIS, THEY AREN'T INSTANTIATED
    [SerializeField] private Transform gunSlot;
    [SerializeField] private LayerMask shootableMask; // just to avoid player and bullet collisions
    private List<Gun> guns; // contains the actual instantiated guns
    private int currGunIndex;

    [Header("Keybinds")]
    [SerializeField] private KeyCode reloadKey;

    [Header("Deferred Updates")]
    private bool pendingVisualUpdate; // set true when UpdateGunVisual fires before guns or uiController are ready

    private void Start() {

        colorManager = GetComponent<PlayerColorManager>();
        playerController = GetComponent<PlayerController>();
        effectManager = GetComponent<PlayerEffectManager>();
        collider = GetComponent<Collider2D>();

        guns = new List<Gun>();

        // guns are instantiated locally on every client (gun visuals aren't networked objects)
        foreach (Gun gun in starterGuns)
            AddGun(gun);

        currGunIndex = 0;
        UpdateGunVisual(); // update visuals; also flushes pendingVisualUpdate if it was set by an early RPC

    }

    // called by UIController.Initialize() after it has fully set itself up; flushes any deferred updates
    public void OnUIControllerInitialized(UIController ui) {

        uiController = ui;

        if (pendingVisualUpdate) {

            pendingVisualUpdate = false;
            UpdateGunVisual(); // flush the visual update that was missed before ui was ready

        }
    }

    private void Update() {

        if (!photonView.IsMine) return; // only allow local player to shoot and cycle guns

        if (playerController.IsMechanicEnabled(MechanicType.Guns)) { // don't return if false to allow for more code to be added to this method later

            if (useMouseAiming && !guns[currGunIndex].IsReloading())
                HandleGunRotation();

            // shooting (pass a callback so Gun can hand back the tracer endpoints for network sync & a callback for reload start to sync that as well)
            if (guns[currGunIndex].IsAutomatic() ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) {

                StartCoroutine(guns[currGunIndex].Shoot(colorManager.GetCurrentPlayerColor().GetEffectType() == EffectType.Damage ? effectManager.GetEffectMultiplier(EffectType.Damage) : 1f, onTracerFired: (start, end) => SendTracerToOthers(currGunIndex, start, end), onReloadStarted: () => SendReloadToOthers(currGunIndex))); // RPC tracer and reload animation to other clients
                uiController.UpdateGunHUD(guns[currGunIndex], currGunIndex);

            }

            // gun cycling via number keys
            if (Input.GetKeyDown(KeyCode.Alpha1))
                CycleToGun(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                CycleToGun(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                CycleToGun(2);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                CycleToGun(3);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                CycleToGun(4);
            if (Input.GetKeyDown(KeyCode.Alpha6))
                CycleToGun(5);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                CycleToGun(6);
            if (Input.GetKeyDown(KeyCode.Alpha8))
                CycleToGun(7);
            if (Input.GetKeyDown(KeyCode.Alpha9))
                CycleToGun(8);

            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput > 0f)
                CyclePreviousGun();
            else if (scrollInput < 0f)
                CycleNextGun();

            // gun reloading
            if (Input.GetKeyDown(reloadKey) && guns[currGunIndex].CanReload()) { // only sync if reload will actually happen

                guns[currGunIndex].StartReload();
                SendReloadToOthers(currGunIndex); // sync animation to other clients

            }
        }
    }

    private void HandleGunRotation() {

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // get mouse position in world space
        Vector2 aimDirection = (mousePos - gunSlot.position).normalized; // get direction from gun to mouse; normalize for consistent rotation regardless of distance

        float gunAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg; // convert to angle in degrees
        Quaternion targetRotation;

        // if player is facing left, flip the gun vertically by rotating 180 degrees and subtracting the angle from 180 to mirror it; if facing right, just rotate by the angle
        if (!playerController.IsFacingRight())
            targetRotation = Quaternion.Euler(0f, 0f, 180f - gunAngle);
        else
            targetRotation = Quaternion.Euler(0f, 0f, gunAngle);

        float currentSpeed = guns[currGunIndex].GetWeightAimFactor();

        // apply reload multiplier if the current gun is reloading
        if (guns[currGunIndex].IsReloading())
            currentSpeed *= reloadAimMultiplier;

        gunSlot.localRotation = Quaternion.Slerp(gunSlot.localRotation, targetRotation, currentSpeed * Time.deltaTime); // rotate towards target rotation at a speed of aimFollowSpeed degrees per second (multiplied by 100 to make the inspector variables more readable)

    }

    private void AddGun(Gun gun) {

        Gun newGun = Instantiate(gun, gunSlot); // add gun under gunSlot (local; gun visuals aren't networked)
        newGun.Initialize(EntityType.Player, shootableMask, collider, guns.Count); // index of new gun will be guns.Count
        guns.Add(newGun);

    }

    // sends tracer endpoints to all other clients so they can display it on the correct gun
    private void SendTracerToOthers(int gunIndex, Vector2 start, Vector2 end) => photonView.RPC(nameof(RPC_ShowTracer), RpcTarget.Others, gunIndex, start, end);

    // RPC: received by all other clients to display the tracer on the correct gun
    [PunRPC]
    private void RPC_ShowTracer(int gunIndex, Vector2 start, Vector2 end) {

        if (gunIndex < 0 || gunIndex >= guns.Count) return; // sanity check; should never happen unless there's a desync in gun indices or a very early RPC before guns are initialized
        guns[gunIndex].ShowTracer(start, end); // show tracer on the correct gun; RPC ensures it shows on the same gun for all clients

    }

    private void CyclePreviousGun() {

        if (guns[currGunIndex].IsReloading()) return; // deny swap if gun is reloading

        currGunIndex--;

        // cycle the guns in loop
        if (currGunIndex < 0)
            currGunIndex = guns.Count - 1;

        photonView.RPC(nameof(RPC_SyncGunIndex), RpcTarget.Others, currGunIndex); // sync gun index to all clients so they see the right gun equipped

        UpdateGunVisual(); // update visuals

    }

    private void CycleToGun(int gunIndex) {

        if (guns[currGunIndex].IsReloading()) return; // deny swap if gun is reloading

        if (gunIndex < 0 || gunIndex >= guns.Count)
            return;

        currGunIndex = gunIndex;

        photonView.RPC(nameof(RPC_SyncGunIndex), RpcTarget.Others, currGunIndex); // sync gun index to all clients so they see the right gun equipped

        UpdateGunVisual(); // update visuals

    }

    private void CycleNextGun() {

        if (guns[currGunIndex].IsReloading()) return; // deny swap if gun is reloading

        currGunIndex++;

        // cycle the guns in loop
        if (currGunIndex >= guns.Count)
            currGunIndex = 0;

        photonView.RPC(nameof(RPC_SyncGunIndex), RpcTarget.Others, currGunIndex); // sync gun index to all clients so they see the right gun equipped

        UpdateGunVisual(); // update visuals

    }

    // RPC: syncs the equipped gun index on remote clients (so they see the right gun sprite)
    [PunRPC]
    private void RPC_SyncGunIndex(int gunIndex) {

        if (gunIndex < 0 || gunIndex >= guns.Count) return;

        currGunIndex = gunIndex;
        UpdateGunVisual(); // update visual on remote client

    }

    // sends a reload notification to all other clients so the animation plays on their screen
    private void SendReloadToOthers(int gunIndex) => photonView.RPC(nameof(RPC_SyncReload), RpcTarget.Others, gunIndex);

    // RPC: received by remote clients to play the reload animation without changing their ammo state
    [PunRPC]
    private void RPC_SyncReload(int gunIndex) {

        if (gunIndex < 0 || gunIndex >= guns.Count) return;
        StartCoroutine(guns[gunIndex].PlayReloadAnimation()); // visual only; ammo state is owned by the local player

    }

    private void UpdateGunVisual() {

        // guns list is initialized in Awake but populated in Start; if called before then (e.g. an early RPC), defer until Start finishes and calls UpdateGunVisual itself
        if (guns.Count == 0) {

            pendingVisualUpdate = true;
            return;

        }

        // make all gun slot children invisible before cycling gun
        for (int i = 0; i < gunSlot.childCount; i++)
            gunSlot.GetChild(i).gameObject.SetActive(false);

        guns[currGunIndex].gameObject.SetActive(true); // make current gun visible

        // if uiController is still not initialized, defer until OnUIControllerInitialized
        if (photonView.IsMine && !uiController.IsInitialized()) {

            pendingVisualUpdate = true;
            return;

        }

        pendingVisualUpdate = false; // clear flag; we're executing successfully

        if (photonView.IsMine)
            uiController.UpdateGunHUD(guns[currGunIndex], currGunIndex); // update ui

    }

    public List<Gun> GetGuns() => guns;

    public bool UseMouseAiming() => useMouseAiming;

}
