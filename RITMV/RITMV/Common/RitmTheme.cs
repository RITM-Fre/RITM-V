using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;

namespace RITMV.Common;

/// <summary>
/// Central palette for the RITM V neon theme.
///
/// The manager writes a set of <c>DynamicResource</c> brushes into
/// <see cref="Application.Resources"/>. Every view only references those keys, so the whole
/// shell (window chrome, toolbars, grids, footer, dialogs) follows the active theme without
/// hard-coded colours scattered around the XAML.
/// </summary>
public static class RitmTheme
{
    // ---- Brand colours ---------------------------------------------------
    public const string AccentHex = "#8B5CF6";   // neon violet
    public const string Accent2Hex = "#22D3EE";  // neon cyan
    public const string AccentLightHex = "#7C3AED";
    public const string Accent2LightHex = "#0891B2";

    // ---- Resource keys ---------------------------------------------------
    public const string KWindow = "RitmWindowBrush";
    public const string KSurface = "RitmSurfaceBrush";
    public const string KSurfaceAlt = "RitmSurfaceAltBrush";
    public const string KBorder = "RitmBorderBrush";
    public const string KText = "RitmTextBrush";
    public const string KTextDim = "RitmTextDimBrush";
    public const string KAccent = "RitmAccentBrush";
    public const string KAccent2 = "RitmAccent2Brush";
    public const string KAccentSoft = "RitmAccentSoftBrush";
    public const string KToolbar = "RitmToolbarBrush";
    public const string KAccentGradient = "RitmAccentGradientBrush";
    public const string KSelectedRow = "RitmSelectedRowBrush";
    public const string KHover = "RitmHoverBrush";
    public const string KFooter = "RitmFooterBrush";
    public const string KGlow = "RitmGlowBrush";
    public const string KTextOnAccent = "RitmTextOnAccentBrush";

    /// <summary>True when the last applied palette was the dark one.</summary>
    public static bool IsDark { get; private set; } = true;

    /// <summary>
    /// Resolves the effective dark/light state from the persisted theme name
    /// (<c>FollowSystem</c> falls back to the Windows "app mode" setting).
    /// </summary>
    public static bool ResolveIsDark(string? theme)
    {
        if (string.Equals(theme, nameof(ETheme.Dark), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (string.Equals(theme, nameof(ETheme.Light), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return IsSystemDark();
    }

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v)
            {
                return v == 0;
            }
        }
        catch
        {
            // registry unavailable -> assume dark, it is the RITM V default
        }
        return true;
    }

    /// <summary>Applies the RITM V palette for the given base theme.</summary>
    public static void Apply(bool isDark)
    {
        var res = Application.Current?.Resources;
        if (res == null)
        {
            return;
        }

        IsDark = isDark;

        if (isDark)
        {
            ApplyPalette(res,
                window: "#0B0714", surface: "#150F26", surfaceAlt: "#1D1533",
                border: "#33265A", text: "#EFEAFE", textDim: "#A79AD6",
                accent: AccentHex, accent2: Accent2Hex, accentSoft: "#261A47",
                selected: "#33235F", hover: "#241A44", footer: "#100A1E",
                toolbarFrom: "#1F1440", toolbarTo: "#100A1E", glow: "#6636D8");
        }
        else
        {
            ApplyPalette(res,
                window: "#F7F5FF", surface: "#FFFFFF", surfaceAlt: "#F1ECFF",
                border: "#DCD3F7", text: "#1A1130", textDim: "#6A5C90",
                accent: AccentLightHex, accent2: Accent2LightHex, accentSoft: "#EDE7FF",
                selected: "#E3D9FF", hover: "#F0EAFF", footer: "#F1EBFF",
                toolbarFrom: "#FFFFFF", toolbarTo: "#F1EAFF", glow: "#C4B5FD");
        }

        SyncMaterialDesignPalette(isDark);
    }

    /// <summary>
    /// Keeps the MaterialDesign palette in sync so built-in controls
    /// (text boxes, chips, ripples, snackbar...) also turn violet.
    /// </summary>
    private static void SyncMaterialDesignPalette(bool isDark)
    {
        try
        {
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

            var accent = (Color)ColorConverter.ConvertFromString(isDark ? AccentHex : AccentLightHex);
            var accent2 = (Color)ColorConverter.ConvertFromString(isDark ? Accent2Hex : Accent2LightHex);

            theme.PrimaryLight = new ColorPair(accent.Lighten());
            theme.PrimaryMid = new ColorPair(accent);
            theme.PrimaryDark = new ColorPair(accent.Darken());

            theme.SecondaryLight = new ColorPair(accent2.Lighten());
            theme.SecondaryMid = new ColorPair(accent2);
            theme.SecondaryDark = new ColorPair(accent2.Darken());

            helper.SetTheme(theme);
        }
        catch
        {
            // palette helper is best-effort only
        }
    }

    private static void ApplyPalette(
        ResourceDictionary res,
        string window, string surface, string surfaceAlt,
        string border, string text, string textDim,
        string accent, string accent2, string accentSoft,
        string selected, string hover, string footer,
        string toolbarFrom, string toolbarTo, string glow)
    {
        Set(res, KWindow, window);
        Set(res, KSurface, surface);
        Set(res, KSurfaceAlt, surfaceAlt);
        Set(res, KBorder, border);
        Set(res, KText, text);
        Set(res, KTextDim, textDim);
        Set(res, KAccent, accent);
        Set(res, KAccent2, accent2);
        Set(res, KAccentSoft, accentSoft);
        Set(res, KSelectedRow, selected);
        Set(res, KHover, hover);
        Set(res, KFooter, footer);
        Set(res, KGlow, glow);
        Set(res, KTextOnAccent, "#FFFFFF");

        res[KToolbar] = Gradient(toolbarFrom, toolbarTo, vertical: true);
        res[KAccentGradient] = Gradient(accent, accent2, vertical: false);
    }

    private static void Set(ResourceDictionary res, string key, string hex)
    {
        var brush = new SolidColorBrush(Parse(hex));
        brush.Freeze();
        res[key] = brush;
    }

    private static LinearGradientBrush Gradient(string from, string to, bool vertical)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = vertical ? new Point(0, 1) : new Point(1, 0),
        };
        brush.GradientStops.Add(new GradientStop(Parse(from), 0));
        brush.GradientStops.Add(new GradientStop(Parse(to), 1));
        brush.Freeze();
        return brush;
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
