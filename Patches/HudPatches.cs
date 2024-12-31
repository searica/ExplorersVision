using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;


namespace ExplorersVision.Patches;

[HarmonyPatch]
internal static class HudPatches
{
    const string Format = "+0.000;-0.000;0.000";

    private static GameObject RadiusGameObject;
    public static Text RadiusHudText { get; private set; }

    private static GameObject VariablesGameObject;
    public static Text VariablesHudText { get; private set; }
    

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]       
    private static void CreateDisplayObjects(Hud __instance)
    {
        if (!RadiusGameObject)
        {
            RadiusGameObject = new($"{nameof(ExplorersVision)}_{nameof(RadiusHudText)}");
            RadiusGameObject.AddComponent<CanvasRenderer>();
            RadiusGameObject.transform.localPosition = Vector3.zero;

            RectTransform transform = RadiusGameObject.AddComponent<RectTransform>();
            transform.SetParent(__instance.m_rootObject.transform);
            transform.pivot = transform.anchorMin = transform.anchorMax = new Vector2(0.0f, 0.0f);
            transform.offsetMin = new Vector2(10.0f, 5.0f);
            transform.offsetMax = new Vector2(210.0f, 165.0f);

            RadiusHudText = RadiusGameObject.AddComponent<Text>();
            RadiusHudText.raycastTarget = false;
            RadiusHudText.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Helvetica", "Arial" }, 12);
            RadiusHudText.fontStyle = FontStyle.Bold;
            RadiusHudText.color = Color.white;
            RadiusHudText.fontSize = 12;
            RadiusHudText.alignment = TextAnchor.LowerLeft;

            Outline textOutline = RadiusGameObject.AddComponent<Outline>();
            textOutline.effectColor = Color.black;

            RadiusGameObject.SetActive(ExplorersVision.Instance.DisplayCurrentRadiusValue.Value);
        }

        if (!VariablesGameObject)
        {
            VariablesGameObject = new($"{nameof(ExplorersVision)}_{nameof(VariablesHudText)}");
            VariablesGameObject.AddComponent<CanvasRenderer>();
            VariablesGameObject.transform.localPosition = Vector3.zero;

            RectTransform transform = VariablesGameObject.AddComponent<RectTransform>();
            transform.SetParent(__instance.m_rootObject.transform);
            transform.pivot = transform.anchorMin = transform.anchorMax = new Vector2(0.0f, 0.0f);
            transform.offsetMin = new Vector2(240.0f, 5.0f);
            transform.offsetMax = new Vector2(440.0f, 165.0f);

            VariablesHudText = VariablesGameObject.AddComponent<Text>();
            VariablesHudText.raycastTarget = false;
            VariablesHudText.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Helvetica", "Arial" }, 12);
            VariablesHudText.fontStyle = FontStyle.Bold;
            VariablesHudText.color = Color.white;
            VariablesHudText.fontSize = 12;
            VariablesHudText.alignment = TextAnchor.LowerLeft;

            Outline textOutline = VariablesGameObject.AddComponent<Outline>();
            textOutline.effectColor = Color.black;

            VariablesGameObject.SetActive(ExplorersVision.Instance.DisplayVariables.Value);
        }
    }

    /// <summary>
    ///     Update display text if enabled.
    /// </summary>
    /// <param name="radius"></param>
    public static void SetDisplayRadiusText(float radius)
    {
        if (ExplorersVision.Instance.DisplayCurrentRadiusValue.Value)
        {
            RadiusHudText.text = $"Dynamic Radius: {radius:0.0}";
        }
    }

    /// <summary>
    ///     Update display text if enabled.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="baseRadius"></param>
    /// <param name="multiplier"></param>
    /// <param name="light"></param>
    /// <param name="weather"></param>
    /// <param name="altitude"></param>
    /// <param name="environment"></param>
    public static void SetDisplayVariablesText(
        float radius, 
        float baseRadius, 
        float multiplier, 
        float light, 
        float weather, 
        float altitude, 
        float environment
    )
    {
        if (!ExplorersVision.Instance.DisplayVariables.Value)
        {
            return;
        }

        VariablesHudText.text = "Dynamic Variables\n" +
                              $"Radius: {radius:0.0}\n" +
                              $"Base: {baseRadius:0.#}\n" +
                              $"Multiplier: {multiplier:0.000}\n\n" +
                              $"Light: {light.ToString(Format)}\n" +
                              $"Weather: {weather.ToString(Format)}\n" +
                              $"Altitude: {altitude.ToString(Format)}\n" +
                              $"Environment: {environment.ToString(Format)}\n";
    }
}
