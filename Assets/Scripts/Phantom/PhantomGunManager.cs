using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhantomController))]
public class PhantomGunManager : GunManager {

    [Header("References")]
    private Gun gun;

    public void SetGun(Gun gun) => photonView.RPC(nameof(RPC_SetGun), RpcTarget.All, gun.name);

    [PunRPC]
    private void RPC_SetGun(string gunPrefabName) {

        Gun prefab = Resources.Load<Gun>("Guns/" + gunPrefabName);

        gun = Instantiate(prefab, gunSlot);
        gun.Initialize(EntityType.Phantom, shootableMask, GetComponent<Collider2D>(), 0);

    }

    public void Shoot() {

        // gun shooting & reloading
        StartCoroutine(gun.Shoot(onTracerFired: (start, end) => SendTracerToOthers(start, end)));
        gun.InstantReload(); // phantom guns don't have ammo, so just instantly reload after shooting

    }

    // sends tracer endpoints to all other clients so they can display it on the correct gun
    private void SendTracerToOthers(Vector2 start, Vector2 end) => photonView.RPC(nameof(RPC_ShowTracer), RpcTarget.Others, start, end);

    // RPC: received by all other clients to display the tracer on the correct gun
    [PunRPC]
    private void RPC_ShowTracer(Vector2 start, Vector2 end) => gun.ShowTracer(start, end);

    public override void RequestReload() => photonView.RPC(nameof(RPC_SyncPhantomReload), RpcTarget.All);

    [PunRPC]
    private void RPC_SyncPhantomReload() {

        // MasterClient owns the phantom's ammo state so only it runs the full reload
        // all other clients just play the animation so it shows on their screen
        if (PhotonNetwork.IsMasterClient)
            gun.StartReload();
        else
            StartCoroutine(gun.PlayReloadAnimation());

    }
}
