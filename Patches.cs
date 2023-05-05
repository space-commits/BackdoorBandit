using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace DoorBreach
{
    //re-initializes each new game
    public class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //stolen from drakiaxyz - thanks
            DoorBreachPlugin.interactiveLayer = LayerMask.NameToLayer("Interactive");
            DoorBreachComponent.Enable();
        }
    }

    public class ExplosionPatch : ModulePatch
    {
        private static Hitpoints hitpoints;
        private static Door door;

        private static Collider[] colliders = new Collider[512];

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1595).GetMethod("Explosion", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        public static void PatchPrefix(Vector3 explosionPosition, IExplosiveItem grenadeItem, Func<DamageInfo> getDamageInfo)
        {
            Array.Clear(colliders, 0, colliders.Length);
            DamageInfo di = getDamageInfo();
            float grenadeDistance = Mathf.Max(grenadeItem.MinExplosionDistance, 3f);
            int overlappedObjects = Physics.OverlapSphereNonAlloc(explosionPosition, grenadeDistance, colliders, LayerMaskClass.GrenadeAffectedMask);
            for (int i = 0; i < overlappedObjects; i++)
            {
                Collider collider = colliders[i];
                bool isDoor = collider.GetComponentInParent<Door>() != null;
                bool hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;
                if (isDoor && hasHitPoints) 
                {
                    door = collider.GetComponentInParent<Door>();
                    if (Physics.Linecast(explosionPosition, door.transform.position, LayerMaskClass.GrenadeObstaclesColliderMask))
                    {

                        bool isLauncher = grenadeItem.MinExplosionDistance == 1f;
                        float damage = isLauncher ? grenadeItem.MaxExplosionDistance * 100f : grenadeItem.MinExplosionDistance * 100f;
                   
                        Logger.LogWarning("removing hit points");

                        hitpoints = collider.GetComponentInParent<Hitpoints>() as Hitpoints;
                        hitpoints.hitpoints -= damage;
                        Logger.LogWarning("damage = " + damage);
                        Logger.LogWarning("hitpoints = " + hitpoints.hitpoints);
      
                        if (hitpoints.hitpoints <= 0)
                        {
                            Player player = di.Player;
                            door.interactWithoutAnimation = true;
                            di.Player.CurrentState.ExecuteDoorInteraction(door, new GClass2599(EInteractionType.Breach), null, player);
                            door.interactWithoutAnimation = false;
                        }
                    }
                }
            }

        }
    }

    public class ApplyHitPatch : ModulePatch
    {
        private static HashSet<string> bruteForceMeleeWeapons = new HashSet<string>
        {
            "63a0b208f444d32d6f03ea1e", //FierceBlowSledgeHammer
            "6087e570b998180e9f76dc24", //SuperforsDeadBlowHammer
            "5bffe7930db834001b734a39", //CrashAxe
            "57cd379a24597778e7682ecf", //Kiba Tomahawk
            "5c012ffc0db834001d23f03f" //Camper Axe
        };

        private static HashSet<string> technicalMeleeWeapons = new HashSet<string>
        {
            "5c07df7f0db834001b73588a", //FreemanCrowbar
            "57cd379a24597778e7682ecf" //Kiba Tomahawk
        };

        private static Hitpoints hitpoints;
        private static Door door;

        private static bool isValidHit(DamageInfo damageInfo, bool isMelee)
        {
            Collider col = damageInfo.HitCollider;

            if (col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>() != null)
            {
                float distance = isMelee ? 0.5f : 0.1f;
                Vector3 localHitPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                DoorHandle doorHandle = col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>();
                Vector3 doorHandleLocalPos = doorHandle.transform.localPosition;
                float distanceToHandle = Vector3.Distance(localHitPoint, doorHandleLocalPos);
                return distanceToHandle < distance;
            }
            return false;
        }

        private static bool isValid(Player player, DamageInfo damageInfo, ref float damage)
        {
            if (damageInfo.DamageType == EDamageType.GrenadeFragment)
            {
                return false;
            }

            var material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            var weaponId = damageInfo.Weapon.TemplateId;
 
            if (damageInfo.Weapon.Template._parent == "543be6564bdc2df4348b4568")
            {
                return false;
            }

            if (material != MaterialType.MetalThin && material != MaterialType.MetalThick)
            {
                bool isMelee = damageInfo.DamageType == EDamageType.Melee;
                bool validHit = isValidHit(damageInfo, isMelee);

                if (isMelee)
                {
                    if ((technicalMeleeWeapons.Contains(weaponId) && validHit))
                    {
                        damage *= 5;
                        return true;
                    }
                    if (bruteForceMeleeWeapons.Contains(weaponId))
                    {
                        if (validHit)
                        {
                            damage *= 3.5f;
                        }
                        else
                        {
                            damage *= 2;
                        }

                        return true;
                    }
                    return false;
                }

                if (damageInfo.SourceId != null)
                {
                    var bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;
                    bool validHandleWeapon = bulletTemplate.Caliber == "Caliber20g" || bulletTemplate.Caliber == "Caliber12g" || bulletTemplate.Caliber == "Caliber23x75" || bulletTemplate.Caliber == "Caliber40x46" || bulletTemplate.Caliber == "Caliber127x55" || bulletTemplate.Caliber == "Caliber86x70";

                    if (validHit && validHandleWeapon)
                    {
                        return true;
                    }

                }
                Logger.LogWarning("end");
            }
            return false;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BallisticCollider).GetMethod("ApplyHit", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static void Prefix(DamageInfo damageInfo, GStruct307 shotID)
        {

            if (damageInfo.DamageType != EDamageType.Explosion && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window) 
            {
                Collider collider = damageInfo.HitCollider;
                if (collider != null)
                {
                    bool isDoor = collider.GetComponentInParent<Door>() != null;
                    bool hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;
                    if (isDoor && hasHitPoints)
                    {
                        float damage = damageInfo.Damage;
                        if (isValid(damageInfo.Player, damageInfo, ref damage))
                        {
                            door = collider.GetComponentInParent<Door>();
                            hitpoints = collider.GetComponentInParent<Hitpoints>() as Hitpoints;
                            hitpoints.hitpoints -= damage;
                            if (hitpoints.hitpoints <= 0)
                            {
                                damageInfo.Player.CurrentState.ExecuteDoorInteraction(door, new GClass2599(EInteractionType.Breach), null, damageInfo.Player);
                            }
                        }
                    }
                }
            }     
        }
    }
}
