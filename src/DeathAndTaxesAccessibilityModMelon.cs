// MelonLoader Mod Assembly Attributes
using MelonLoader;

[assembly: MelonInfo(typeof(Death_and_Access.DeathAndTaxesAccessibilityModMelon), "Death and Taxes Accessibility Mod", "1.0.0", "Accessibility Mod Author")]
[assembly: MelonGame("Placeholder Gameworks", "Death and Taxes")]

namespace Death_and_Access;

using System;

/// <summary>
/// MelonLoader entry point for Death and Taxes Accessibility Mod
/// This class is found and loaded by MelonLoader
/// </summary>
public class DeathAndTaxesAccessibilityModMelon : MelonMod
{
    private static AccessibilityManager _accessibilityManager;
    private static bool _announcedInit = false;
    private static SpecificTextAnnouncer _specificTextAnnouncer;
    private static UiNavigationHandler _uiNavigationHandler;
    private static bool IsReady => _announcedInit && _accessibilityManager != null;

    public override void OnInitializeMelon()
    {
        try
        {
            MelonLogger.Msg("Initializing Death and Taxes Accessibility Mod...");
            
            _accessibilityManager = new AccessibilityManager();
            _accessibilityManager.Initialize();
            
            _specificTextAnnouncer = new SpecificTextAnnouncer();
            _specificTextAnnouncer.Initialize(_accessibilityManager.Screenreader);
            _uiNavigationHandler = new UiNavigationHandler();
            _uiNavigationHandler.Initialize(_accessibilityManager.Screenreader);
            
            MelonLogger.Msg("Death and Taxes Accessibility Mod initialized successfully!");
            _announcedInit = true;
        }
        catch (Exception ex)
        {
            MelonLogger.Error("Failed to initialize: " + ex.Message);
            MelonLogger.Error(ex.StackTrace);
        }
    }

    public override void OnUpdate()
    {
        if (!IsReady) return;
        _specificTextAnnouncer?.Update();
        _uiNavigationHandler?.Update();
    }

    public override void OnDeinitializeMelon()
    {
        try
        {
            _specificTextAnnouncer?.Cleanup();
            _specificTextAnnouncer = null;
            _uiNavigationHandler = null;
            
            if (_accessibilityManager != null)
            {
                _accessibilityManager.Dispose();
            }
            _accessibilityManager = null;
            _announcedInit = false;
            
            MelonLogger.Msg("Death and Taxes Accessibility Mod unloaded.");
        }
        catch (Exception ex)
        {
            MelonLogger.Error("Error during unload: " + ex.Message);
        }
    }

}
