using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WateringCanWeapon : GenericWeapon {
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private FluidStream stream;
    [SerializeField] private GenericReagentContainer container;
    private static readonly int Fire = Animator.StringToHash("Fire");

    protected override void OnFireRPC(int viewID) {
        base.OnFireRPC(viewID);
        weaponAnimator.SetBool(Fire, true);
        stream.OnFire(container);
    }

    protected override void OnEndFireRPC(int viewID) {
        base.OnEndFireRPC(viewID);
        weaponAnimator.SetBool(Fire, false);
        stream.OnEndFire();
    }
}
