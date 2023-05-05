﻿using System;
using System.Diagnostics;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using VersionChecker;

namespace DoorBreach
{
    [BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "1.2.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {

        public static int interactiveLayer;
        private void Awake()
        {
            CheckEftVersion();
            new NewGamePatch().Enable();
            new ApplyHitPatch().Enable();
            new ExplosionPatch().Enable();
        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                EFT.UI.ConsoleScreen.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }
    }
}
