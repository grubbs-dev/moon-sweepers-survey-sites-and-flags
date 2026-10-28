using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MoonCleaners;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoonSweepers.SurveySitesAndFlags;

[BepInPlugin("moon-sweepers.survey-sites-and-flags", "Survey Sites and Flags", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    public static BepInEx.Logging.ManualLogSource Logger;
    public override void Load()
    {
        Logger = Log;
        new Harmony("moon-sweepers.survey-sites-and-flags").PatchAll();
        Log.LogInfo("Survey Sites and Flags loaded. Native Moon Map landmark layer enabled.");
    }

    [HarmonyPatch(typeof(MoonMapScreen), "FlagsOnMap")]
    private static class AlwaysShowLandmarks
    {
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(MoonMapScreen), "ShowFlags")]
    private static class AllLandmarkMarkers
    {
        private static void Postfix(MoonMapScreen __instance, MoonWorld world)
        {
            try
            {
                if (world?.landmarks == null || __instance.markerLayer == null) return;
                foreach (var old in UnityEngine.Object.FindObjectsOfType<RectTransform>())
                    if (old.name.StartsWith("LSF_", StringComparison.Ordinal)) UnityEngine.Object.Destroy(old.gameObject);

                var flagTemplate = __instance.flagMarks != null && __instance.flagMarks.Count > 0 ? __instance.flagMarks.get_Item(0) : null;
                var siteTemplate = __instance.siteMarks != null && __instance.siteMarks.Count > 0 ? __instance.siteMarks.get_Item(0) : flagTemplate;
                // The native map draws discovered markers from these pools. Hide
                // those originals so the complete LSF marker set cannot duplicate
                // a raised flag or surveyed site.
                if (__instance.flagMarks != null)
                    for (var n = 0; n < __instance.flagMarks.Count; n++) __instance.flagMarks.get_Item(n)?.gameObject.SetActive(false);
                if (__instance.siteMarks != null)
                    for (var n = 0; n < __instance.siteMarks.Count; n++) __instance.siteMarks.get_Item(n)?.gameObject.SetActive(false);
                var projected = typeof(MoonMapScreen).GetMethod("ScreenOf", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var placed = 0;
                for (var i = 0; i < world.landmarks.Length; i++)
                {
                    var landmark = world.landmarks[i];
                    var direction = world.LandmarkDirection(i);
                    if (direction == Vector3.zero || projected == null) continue;
                    var args = new object[] { world, direction, null };
                    if (!(projected.Invoke(__instance, args) is bool visible) || !visible || args[2] is not Vector2 point) continue;
                    var isSite = landmark != null && landmark.IsSite;
                    var template = isSite ? siteTemplate : flagTemplate;
                    if (template == null) continue;
                    var marker = UnityEngine.Object.Instantiate(template.gameObject, __instance.markerLayer);
                    marker.name = $"LSF_{(isSite ? "Site" : "Flag")}_{i:000}";
                    marker.SetActive(true);
                    var rect = marker.GetComponent<RectTransform>();
                    // ScreenOf uses the map camera's full projection radius. The
                    // rendered orange globe is intentionally smaller than that
                    // projection, so fit the points to the visible globe.
                    var localPoint = (point - new Vector2(Screen.width, Screen.height) * .5f) * .62f;
                    rect.anchoredPosition = localPoint;
                    var image = marker.GetComponent<Image>();
                    if (image != null)
                    {
                        var raised = world.IsLandmarkRaised(i);
                        image.color = raised ? new Color(.25f, 1f, .35f, 1f) : new Color(.42f, .46f, .5f, .8f);
                    }
                    placed++;
                }
                Logger.LogInfo($"Landing guide placed {placed} native-style landmark markers on the station map.");
            }
            catch (Exception ex) { Logger.LogError("Map marker diagnostic failed: " + ex); }
        }
    }

}
