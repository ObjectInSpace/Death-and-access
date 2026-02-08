namespace Death_and_Access;

using System;
/// <summary>
/// Main accessibility manager for the Death and Taxes mod
/// </summary>
public class AccessibilityManager : IDisposable
{
    private readonly ScreenreaderProvider _screenreader;
    private bool _isInitialized;

    public ScreenreaderProvider Screenreader => _screenreader;
    public AccessibilityManager()
    {
        _screenreader = new ScreenreaderProvider();
        _isInitialized = false;
    }

    /// <summary>
    /// Initializes the accessibility system
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;
        _screenreader.Enabled = true;
        _isInitialized = true;
    }

    public void Dispose()
    {
        _screenreader.Dispose();
    }
}
