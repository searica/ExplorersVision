using System;
using HarmonyLib;
using UnityEngine;

namespace ExplorersVision.Patches;

[HarmonyPatch]
internal static class MapRadiusPatches
{

    /// <summary>
    ///     Adjust radius when exploring.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="p"></param>
    /// <param name="radius"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore))]
    private static void AdjustExploreRadius(Minimap __instance, Vector3 p, ref float radius)
    {
        if(!Player.m_localPlayer) {  return; }
        radius = GetExploreRadius(Player.m_localPlayer);
    }

    private static float GetExploreRadius(Player player)
    {
        float result;

        if (player.InInterior())
        {
            // In a dungeon. Dungeons are way up high and we dont want to reveal a huge section of the map when entering one.
            // We actually want to reduce the radius since it doesnt make sense to be able to explore the map while in a dungeon
            result = Mathf.Max(ExplorersVision.Instance.LandExploreRadius.Value * 0.2f, 10.0f);
            HudPatches.SetDisplayRadiusText(result);
            return result;
        }

        float baseRadius;
        float multiplier = 1.0f;
        Ship localShip = Ship.GetLocalShip();
        if (localShip && localShip.IsPlayerInBoat(player))
        {
            baseRadius = ExplorersVision.Instance.SeaExploreRadius.Value;
        }
        else
        {
            baseRadius = ExplorersVision.Instance.LandExploreRadius.Value;
        }

        // Take the higher of directional or ambient light, subtract 1 to turn this into a value we can add to our multiplier
        float light = Mathf.Max(GetColorMagnitude(EnvMan.instance.m_dirLight.color * EnvMan.instance.m_dirLight.intensity), GetColorMagnitude(RenderSettings.ambientLight));
        float lightModifier = (light - 1.0f) * ExplorersVision.Instance.DaylightRadiusScale.Value;
        multiplier += lightModifier;

        // Account for weather
        float particles = 0.0f;
        foreach (GameObject particleSystem in EnvMan.instance.GetCurrentEnvironment().m_psystems)
        {
            // Certain particle systems heavily obstruct view
            if (particleSystem.name.Equals("Mist", StringComparison.InvariantCultureIgnoreCase))
            {
                particles += 0.5f;
            }
            if (particleSystem.name.Equals("SnowStorm", StringComparison.InvariantCultureIgnoreCase))
            {
                // Snow storm lowers visibility during the day more than at night
                particles += 0.7f * light;
            }
        }

        // Fog density range seems to be 0.001 to 0.15 based on environment data. Multiply by 10 to get a more meaningful range.
        float fog = Mathf.Clamp(RenderSettings.fogDensity * 10.0f + particles, 0.0f, 1.5f);
        float fogModifier = -fog * ExplorersVision.Instance.WeatherRadiusScale.Value;
        multiplier += fogModifier;

        // Sea level = 30, tallest mountains (not including the rare super mountains) seem to be around 220. Stop adding altitude bonus after 400
        float altitude = Mathf.Clamp(player.transform.position.y - ZoneSystem.instance.m_waterLevel, 0.0f, 400.0f);
        float adjustedAltitude = altitude / 100.0f * Mathf.Max(0.05f, 1.0f - particles);
        float altitudeModifier = adjustedAltitude * ExplorersVision.Instance.AltitudeRadiusBonus.Value;
        multiplier += altitudeModifier;

        // Make adjustments based on biome
        float environmentModifier = GetEnvironementModifier(player, adjustedAltitude);
        multiplier += environmentModifier;

        // Clamp results
        multiplier = Mathf.Clamp(multiplier, 0.2f, 5f);
        result = Mathf.Clamp(baseRadius * multiplier, 20.0f, 2000.0f);

        HudPatches.SetDisplayRadiusText(result);
        HudPatches.SetDisplayVariablesText(result, baseRadius, multiplier, lightModifier, fogModifier, altitudeModifier, environmentModifier);
        return result;
    }

    private static float GetColorMagnitude(Color color)
    {
        // Intentionally ignoring alpha here
        return Mathf.Sqrt(color.r * color.r + color.g * color.g + color.b * color.b);
    }

    private static float GetEnvironementModifier(Player player, float altitude)
    {
        // Account for altitude bonus, as being a forest negates bonus from altitude.
        float baseForestPenalty = ExplorersVision.Instance.ForestRadiusPenalty.Value;
        float forestPenalty = baseForestPenalty * (1 + altitude * ExplorersVision.Instance.AltitudeRadiusBonus.Value);

        // Forest thresholds based on logic found in MiniMap.GetMaskColor
        switch (player.GetCurrentBiome())
        {
            case Heightmap.Biome.BlackForest:
                // Small extra penalty to account for high daylight values in black forest
                return -forestPenalty - 0.25f * ExplorersVision.Instance.DaylightRadiusScale.Value;
            case Heightmap.Biome.Meadows:
                return WorldGenerator.InForest(player.transform.position) ? -forestPenalty : 0.0f;
            case Heightmap.Biome.Plains:
                // Small extra bonus to account for low daylight values in plains
                float lightRadiusBonus = ExplorersVision.Instance.DaylightRadiusScale.Value;
                return (WorldGenerator.GetForestFactor(player.transform.position) < 0.8f ? -forestPenalty : 0.0f) + 0.1f * lightRadiusBonus;
            default:
                return 0.0f;
        }
    }
    
}
