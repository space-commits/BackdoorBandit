using System;
using System.Diagnostics;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;

namespace DoorBreach
{
    [BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "1.2.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {

        public static int interactiveLayer;
        private void Awake()
        {
            new NewGamePatch().Enable();
            new ApplyHitPatch().Enable();
            new ExplosionPatch().Enable();
        }
    }
}
