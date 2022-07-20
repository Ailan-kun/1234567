using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Naelstrof.Mozzarella;
using Photon.Pun;
using SkinnedMeshDecals;
using UnityEngine;
using Vilar.AnimationStation;

public class MilkingTable : GenericUsable, IAnimationStationSet {
    [SerializeField]
    private Sprite milkingSprite;
    [SerializeField]
    private List<AnimationStation> stations;
    private ReadOnlyCollection<AnimationStation> readOnlyStations;
    [SerializeField]
    private Material milkSplatMaterial;
    [SerializeField]
    private FluidStream stream;

    private GenericReagentContainer container;

    private WaitForSeconds waitSpurt;
    void Awake() {
        waitSpurt = new WaitForSeconds(1f);
        readOnlyStations = stations.AsReadOnly();
        container = gameObject.AddComponent<GenericReagentContainer>();
        container.type = GenericReagentContainer.ContainerType.Mouth;
        photonView.ObservedComponents.Add(container);
    }
    public override Sprite GetSprite(Kobold k) {
        return milkingSprite;
    }
    public override bool CanUse(Kobold k) {
        if (k.GetEnergy() <= 0) {
            return false;
        }
        foreach (var station in stations) {
            if (station.info.user == null) {
                return true;
            }
        }
        return false;
    }

    public override void LocalUse(Kobold k) {
        Debug.Log("Huh?");
        for (int i = 0; i < stations.Count; i++) {
            if (stations[i].info.user == null) {
                k.photonView.RPC(nameof(CharacterControllerAnimator.BeginAnimationRPC), RpcTarget.All,
                    photonView.ViewID, i);
                break;
            }
        }
        base.LocalUse(k);
    }
    public override void Use() {
        StopAllCoroutines();
        StartCoroutine(WaitThenMilk());
    }
    private IEnumerator WaitThenMilk() {
        Debug.Log("Milk Stage 1");
        yield return new WaitForSeconds(8f);
        // Validate that we have two characters with energy that have been animating for 5 seconds
        for (int i = 0; i < stations.Count; i++) {
            if (stations[i].info.user == null || stations[i].info.user.GetEnergy() <= 0) {
                yield break;
            }
        }
        // Consume their energy!
        for (int i = 0; i < stations.Count; i++) {
            if (!stations[i].info.user.TryConsumeEnergy(1)) {
                yield break;
            }
        }
        Debug.Log("Milk Stage 2");
        // Now do some milk stuff.
        int pulses = 12;
        ReagentContents milkVolume = new ReagentContents();
        float totalVolume = stations[0].info.user.baseBoobSize;
        milkVolume.AddMix(ReagentDatabase.GetReagent("Milk").GetReagent(totalVolume));
        for (int i = 0; i < pulses; i++) {
            foreach (Transform t in stations[0].info.user.GetNipples()) {
                if (MozzarellaPool.instance.TryInstantiate(out Mozzarella mozzarella)) {
                    mozzarella.SetFollowTransform(t);
                    mozzarella.SetVolumeMultiplier(milkVolume.volume * 0.25f);
                    mozzarella.SetLocalForward(Vector3.up);
                    Color color = milkVolume.GetColor();
                    mozzarella.hitCallback += (hit, startPos, dir, length, volume) => {
                        milkSplatMaterial.color = color;
                        PaintDecal.RenderDecalForCollider(hit.collider, milkSplatMaterial,
                            hit.point - hit.normal * 0.1f, Quaternion.LookRotation(hit.normal, Vector3.up)*Quaternion.AngleAxis(UnityEngine.Random.Range(-180f,180f), Vector3.forward),
                            Vector2.one * (volume * 4f), length);
                    };
                }
            }
            container.AddMix(milkVolume.Spill(totalVolume / pulses), GenericReagentContainer.InjectType.Inject);
            stream.OnFire(container);
            yield return waitSpurt;
        }
        Debug.Log("Milk Stage 3");
        yield return waitSpurt;
        for (int i = 0; i < stations.Count; i++) {
            if (stations[i].info.user != null && stations[i].info.user.GetEnergy() <= 0) {
                stations[i].info.user.photonView
                    .RPC(nameof(CharacterControllerAnimator.StopAnimationRPC), RpcTarget.All);
            }
        }
    }

    public ReadOnlyCollection<AnimationStation> GetAnimationStations() {
        return readOnlyStations;
    }
}
