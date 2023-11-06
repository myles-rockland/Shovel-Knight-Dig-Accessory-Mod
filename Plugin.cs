using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using I2.Loc;
using BepInEx.Logging;
using System.Linq;

namespace AccessoryBagPlugin
{
    //Plugin to load patch
    [BepInPlugin("rockm3000.skdig.accessory", "Accessory Mod", "1.0.1.0")]
    [BepInProcess("skDig64.exe")]
    [BepInIncompatibility("rockm3000.skdig.firedig")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin rockm3000.skdig.accessory is loaded!");

            //Applying the mechanic patch
            var harmony = new Harmony("rockm3000.skdig.accessory");
            var original = typeof(Player).GetMethod(nameof(Player.ActionPressedDown));
            var postfix = typeof(AccessoryPatch).GetMethod(nameof(AccessoryPatch.SpawnBag));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            original = typeof(Player).GetMethod(nameof(Player.OnJumpInput));
            postfix = typeof(FoodPatch).GetMethod(nameof(FoodPatch.SpawnFood));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            original = typeof(TitleScreen).GetMethod(nameof(TitleScreen.PressStart));
            postfix = typeof(AccessoryNotifPatch).GetMethod(nameof(AccessoryNotifPatch.SpawnAccessoryNotif));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            Logger.LogInfo("Accessory patch should be applied");
        }
    }

    //Accessory mechanic patch
    [HarmonyPatch(typeof(Player), nameof(Player.ActionPressedDown))]
    class AccessoryPatch
    {
        private static ManualLogSource buffMechPatchLog = BepInEx.Logging.Logger.CreateLogSource("BuffMechPatchLog");
        [HarmonyPostfix]
        public static void SpawnBag(Player __instance)
        {
            if (__instance.Input.m_HoldingUp)
            {
                //__instance.m_Anim.PlayAnimation("first land");
                //__instance.m_Anim.SetCurrentFrame(20);
                //Spawn the bag
                UpgradeItem buff = Inventory.Player1Inventory.GetRandomBuffFromUnlocked(true);
                StageController.Instance.UseBuffEvent(buff, true);
                StageController.Instance.m_Player.AddTempItem(buff);
                Vector3 startPos = __instance.transform.position - StageController.Instance.m_Camera.transform.position;
                Vector3 targetPos = new Vector3(-CameraController.Instance.WIDTH + 20f, CameraController.Instance.HEIGHT - 40f, 0f);
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(AssetCatalogue.Instance.GetObject("trail effect"));
                gameObject.GetComponent<TrailEffectSequence>().Launch(gameObject.GetComponent<TrailEffectSequence>().m_DefaultTrail_prefab, startPos, targetPos, new Vector3(1f, 1f, 0f), 10f, 11f, delegate
                {
                    if (!StageController.Instance.TryPickupBuffFirstTime(buff))
                    {
                        UICanvas.UI.m_NotificationSystem.SendNotification(LocalizationManager.GetTranslation("Items/" + buff.m_ID + "_NAME", true, 0, true, false, null, null, true), buff.m_ItemSmallSprite, buff, NotificationSystem.SIZE.MINI, 16f, "");
                    }
                });
                buffMechPatchLog.LogInfo("Player should have been buffed with accessory.");
            }
        }
    }

    //Special food mechanic patch
    [HarmonyPatch(typeof(Player), nameof(Player.OnJumpInput))]
    class FoodPatch
    {
        private static ManualLogSource foodMechPatchLog = BepInEx.Logging.Logger.CreateLogSource("FoodMechPatchLog");
        private static int i = 0;
        [HarmonyPostfix]
        public static void SpawnFood(Player __instance)
        {
            if (__instance.Input.m_HoldingUp)
            {
                foodMechPatchLog.LogInfo($"{UICanvas.UI.m_HealthBar.SpecialFoods[i].m_ItemName}");
                UICanvas.UI.m_HealthBar.StartAddingSequence(__instance.transform.position - StageController.Instance.m_Camera.transform.position, -1, -1, UICanvas.UI.m_HealthBar.SpecialFoods[i], null);
                if (UICanvas.UI.m_HealthBar.SpecialFoods[i].m_FoodValue == 0)
                {
                    StageController.Instance.UseBuffEvent(UICanvas.UI.m_HealthBar.SpecialFoods[i], true);
                }
                else
                {
                    StageController.Instance.m_Player.ChangeHealth(UICanvas.UI.m_HealthBar.SpecialFoods[i].m_FoodValue, false, true, true, false, false);
                    DigResults.Instance.OnFoodHealthEaten(UICanvas.UI.m_HealthBar.SpecialFoods[i].m_FoodValue);
                }
                UICanvas.UI.m_NotificationSystem.SendNotification(LocalizationManager.GetTranslation("Items/" + UICanvas.UI.m_HealthBar.SpecialFoods[i].m_ID + "_NAME", true, 0, true, false, null, null, true), UICanvas.UI.m_HealthBar.SpecialFoods[i].m_ItemSmallSprite, UICanvas.UI.m_HealthBar.SpecialFoods[i], NotificationSystem.SIZE.MINI, 16f, "");
                StageController.Instance.m_Player.AddTempItem(UICanvas.UI.m_HealthBar.SpecialFoods[i]);
                foodMechPatchLog.LogInfo("Special food should have been applied.");
                i = i == 7 ? 0 : i + 1;

            }
        }
    }

    //Accessory active mod notif patch
    [HarmonyPatch(typeof(TitleScreen), nameof(TitleScreen.PressStart))]
    class AccessoryNotifPatch
    {
        private static bool pressedStart = false;
        [HarmonyPostfix]
        public static void SpawnAccessoryNotif()
        {
            if (!pressedStart)
            {
                UICanvas.UI.DisplayStageNameWithDelay("Accessory Mod Active", 2);
                pressedStart = true;
            }
        }
    }
}
