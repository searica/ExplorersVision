using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Extensions;
using Configs;
using Logging;
using System;
using ExplorersVision.Patches;


namespace ExplorersVision;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class ExplorersVision : BaseUnityPlugin
{
    public const string PluginName = "ExplorersVision";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.1";

    internal static ExplorersVision Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    // Global settings
    internal const string GlobalSection = "Global";
    internal const string MultiplierSection = "Multipliers";
    internal const string MiscSection = "Miscellaneous";

    public ConfigEntry<float> LandExploreRadius { get; private set; }
    public ConfigEntry<float> SeaExploreRadius { get; private set; }
    public ConfigEntry<float> AltitudeRadiusBonus { get; private set; }
    public ConfigEntry<float> ForestRadiusPenalty { get; private set; }
    public ConfigEntry<float> DaylightRadiusScale { get; private set; }
    public ConfigEntry<float> WeatherRadiusScale { get; private set; }
    public ConfigEntry<bool> DisplayCurrentRadiusValue { get; private set; }
    public ConfigEntry<bool> DisplayVariables { get; private set; }

    public void Awake()
    {
        Instance = this;
        ConfigFile = Config;
        Log.Init(Logger);

        Config.DisableSaveOnConfigSet();
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

        // Create a file watcher for the config file
        ConfigFileWatcher = new(Config);
    }

    internal void SetUpConfigEntries()
    {
        LandExploreRadius = Config.BindConfigInOrder(
            GlobalSection,
            nameof(LandExploreRadius),
            150f,
            "The radius around the player to uncover while travelling on land near sea level. " +
            "Higher values may cause performance issues. Game default is 100.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(50f, 1000f)
        );
  
        SeaExploreRadius = Config.BindConfigInOrder(
            GlobalSection, 
            nameof(SeaExploreRadius), 
            300.0f, 
            "The radius around the player to uncover while travelling on a boat. " +
            "Higher values may cause performance issues. Game default is 100.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(50f, 1000f)
        );
      
        AltitudeRadiusBonus = Config.BindConfigInOrder(
            MultiplierSection, 
            nameof(AltitudeRadiusBonus), 
            0.5f, 
            "Bonus multiplier to apply to land exploration radius based on altitude. " +
            "For every 100 units above sea level (smooth scale), add this value multiplied " +
            "by LandExploreRadius to the total. " +
            "For example, with a radius of 200 and a multiplier of 0.5, " +
            "radius is 200 at sea level, 250 at 50 altitude, 300 at 100 altitude, " +
            "400 at 200 altitude, etc. For reference, a typical mountain peak is around 170 altitude. " +
            "Set to 0 to disable.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(0f, 2f)
        );
       
        ForestRadiusPenalty = Config.BindConfigInOrder(
            MultiplierSection, 
            nameof(ForestRadiusPenalty), 
            0.3f, 
            "Penalty to apply to land exploration radius when in a forest " +
            "(black forest, forested parts of meadows and plains). " +
            "This value is multiplied by the base land exploration radius " +
            "and subtraced from the total. Set to 0 to disable.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(0f, 1f)
        );

        DaylightRadiusScale = Config.BindConfigInOrder(
            MultiplierSection, 
            nameof(DaylightRadiusScale), 
            0.2f, 
            "Influences how much daylight (directional and ambient light) affects exploration radius. " +
            "This value is multiplied by the base land or sea exploration radius and added to the total. " +
            "Set to 0 to disable.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(0f, 1f)
        );

        WeatherRadiusScale = Config.BindConfigInOrder(
            MultiplierSection, 
            nameof(WeatherRadiusScale), 
            0.5f, 
            "Influences how much the current weather affects exploration radius. " +
            "This value is multiplied by the base land or sea exploration radius and added to the total. " +
            "Set to 0 to disable.",
            synced: true,
            acceptableValues: new AcceptableValueRange<float>(0f, 1f)
        );

        DisplayCurrentRadiusValue = Config.BindConfigInOrder(
            MiscSection, 
            nameof(DisplayCurrentRadiusValue), 
            false, 
            "Enabling this will display the currently computed exploration radius in the " +
            "bottom left of the in-game Hud. Useful if you are trying to tweak config values " +
            "and want to see the result.",
            synced: false
        );
        DisplayCurrentRadiusValue.SettingChanged += UpdateDisplayRadiusText;

        DisplayVariables = Config.BindConfigInOrder(
            MiscSection,
            nameof(DisplayVariables), 
            false, 
            "Enabling this will display on the Hud the values of various variables that go " +
            "into calculating the exploration radius. Mostly useful for debugging " +
            "and tweaking the config.",
            synced: false
        );
        DisplayVariables.SettingChanged += UdpateDisplayVariablesText;
    }

    private void UpdateDisplayRadiusText(object sender, EventArgs e)
    {
        if (HudPatches.RadiusHudText is null)
        {
            return;
        }

        HudPatches.RadiusHudText.gameObject.SetActive(DisplayVariables.Value);
        if (!DisplayVariables.Value)
        {
            HudPatches.RadiusHudText.text = string.Empty;
        }
    }

    private void UdpateDisplayVariablesText(object sender, EventArgs e)
    {
        if (HudPatches.VariablesHudText is null)
        {
            return;
        }

        HudPatches.VariablesHudText.gameObject.SetActive(DisplayVariables.Value);
        if (!DisplayVariables.Value)
        {
            HudPatches.VariablesHudText.text = string.Empty;
        }
    }

    public void OnDestroy()
    {
        Config.Save();
    }

}
