using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MoonCleaners;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoonSweepers.SurveySitesAndFlags;

[BepInPlugin("moon-sweepers.survey-sites-and-flags", "Survey Sites and Flags", "0.1.4")]
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
            // The base map lazily initializes landmark data only after the
            // first landmark has been discovered.  Ask the world to build its
            // complete catalog before reading the arrays so a fresh save gets
            // the same marker set as an experienced save.
            EnsureLandmarks(world);
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
                var placedFlags = 0;
                var placedSites = 0;
                var raised = 0;
                var landmarkCount = Math.Max(world.LandmarkCount, world.landmarks?.Length ?? 0);
                for (var i = 0; i < landmarkCount; i++)
                {
                    var landmark = world.Landmark(i);
                    var direction = world.LandmarkDirection(i);
                    if (direction == Vector3.zero && world.landmarkDirections != null && i < world.landmarkDirections.Length)
                        direction = world.landmarkDirections[i];
                    if (direction == Vector3.zero || projected == null) continue;
                    var args = new object[] { world, direction, null };
                    if (!(projected.Invoke(__instance, args) is bool visible) || !visible || args[2] is not Vector2 point) continue;
                    var isSite = landmark != null && landmark.IsSite;
                    var template = isSite ? siteTemplate : flagTemplate;
                    var marker = CreateMarker(__instance, template, isSite);
                    if (marker == null) continue;
                    marker.transform.SetParent(__instance.markerLayer, false);
                    marker.name = $"LSF_{(isSite ? "Site" : "Flag")}_{i:000}";
                    marker.SetActive(true);
                    var rect = marker.GetComponent<RectTransform>();
                    // ScreenOf returns screen-space coordinates. Scale them by
                    // the actual globe view rect, not a hard-coded .62 factor,
                    // so the markers track the rendered orange globe.
                    var viewRect = __instance.view != null ? __instance.view.rectTransform : null;
                    var scaleX = viewRect != null && Screen.width > 0 ? viewRect.rect.width / Screen.width : .62f;
                    var scaleY = viewRect != null && Screen.height > 0 ? viewRect.rect.height / Screen.height : .62f;
                    var localPoint = (point - new Vector2(Screen.width, Screen.height) * .5f) * new Vector2(scaleX, scaleY);
                    rect.anchoredPosition = localPoint;
                    var image = marker.GetComponent<Image>();
                    if (image != null)
                    {
                        var isRaised = world.IsLandmarkRaised(i);
                        if (isRaised) raised++;
                        // Green is used by the game for a fully swept map, so
                        // discovered landmarks disappear into the map fill.
                        // Keep undiscovered markers muted, but make completed
                        // flags/sites a high-contrast red.
                        image.color = isRaised ? new Color(1f, .08f, .08f, 1f) : new Color(.42f, .46f, .5f, .8f);
                    }
                    if (isSite) placedSites++; else placedFlags++;
                    placed++;
                }
                Logger.LogInfo($"Landing guide placed {placed} markers ({placedFlags} flags, {placedSites} sites; {raised} already raised) on the station map.");
            }
            catch (Exception ex) { Logger.LogError("Map marker diagnostic failed: " + ex); }
        }

        private static GameObject CreateMarker(MoonMapScreen screen, Image template, bool isSite)
        {
            if (template != null)
                return UnityEngine.Object.Instantiate(template.gameObject);

            // On a fresh save the game's discovered-marker pools are empty, so
            // there is no native template to clone. Build a real UI marker from
            // the map's own shape sprite instead of silently dropping it.
            var marker = new GameObject("LSF_Marker");
            marker.AddComponent<RectTransform>();
            marker.AddComponent<Image>();
            var image = marker.GetComponent<Image>();
            var shapeMethod = typeof(MoonMapScreen).GetMethod("Shape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var shapeProperty = typeof(MoonMapScreen).GetProperty(isSite ? "SiteShape" : "FlagShape", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var sizeProperty = typeof(MoonMapScreen).GetProperty(isSite ? "SiteSize" : "FlagSize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var shapeId = (int)(shapeProperty?.GetValue(null) ?? 0);
            image.sprite = shapeMethod?.Invoke(screen, new object[] { shapeId }) as Sprite ?? screen.solid;
            image.raycastTarget = false;
            var size = (float)(sizeProperty?.GetValue(null) ?? 16f);
            marker.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            return marker;
        }

    private static void EnsureLandmarks(MoonWorld world)
    {
        if (world == null) return;
        try
        {
            if (world.landmarks == null || world.landmarks.Length == 0)
                world.InitializeLandmarks();
        }
        catch (Exception ex) { Logger.LogWarning("Could not initialize the world's landmark catalog: " + ex.Message); }
    }
    }

}
