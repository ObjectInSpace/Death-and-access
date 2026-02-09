namespace Death_and_Access;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using MelonLoader;

internal static class TypeResolver
{
    private static readonly Dictionary<string, Type> Cache = new(StringComparer.Ordinal);
    private static readonly object Sync = new();

    internal static Type Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        lock (Sync)
        {
            if (Cache.TryGetValue(name, out var cached))
                return cached;
        }

        Type type = null;
        try
        {
            type = AccessTools.TypeByName(name);
        }
        catch
        {
            type = null;
        }

        type ??= Type.GetType(name);

        if (type == null)
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(name, false);
                    if (type != null)
                        break;
                }
            }
            catch
            {
                type = null;
            }
        }

        lock (Sync)
        {
            Cache[name] = type;
        }

        return type;
    }
}

internal static class TextSanitizer
{
    internal static string StripRichTextTags(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var builder = new StringBuilder(value.Length);
        var inTag = false;
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (!inTag && ch == '&' && i + 3 < value.Length
                && string.Compare(value, i, "&lt;", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
            {
                var end = value.IndexOf("&gt;", i + 4, StringComparison.OrdinalIgnoreCase);
                if (end >= 0)
                {
                    i = end + 3;
                    i--;
                    continue;
                }
            }

            if (ch == '<')
            {
                inTag = true;
                continue;
            }

            if (ch == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                builder.Append(ch);
        }

        return builder.ToString();
    }

    internal static string RemoveInsensitive(string input, string token)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(token))
            return input ?? string.Empty;

        var result = input;
        var index = result.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            result = result.Remove(index, token.Length);
            index = result.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}

internal static class ReflectionUtils
{
    internal static bool TryGetProperty(Type type, object instance, string name, out object value)
    {
        value = null;
        if (type == null || instance == null || string.IsNullOrWhiteSpace(name))
            return false;

        try
        {
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null)
                return false;

            value = prop.GetValue(instance);
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    internal static bool TryGetBoolProperty(Type type, object instance, string name, out bool value)
    {
        value = false;
        if (!TryGetProperty(type, instance, name, out var obj))
            return false;

        if (obj is bool boolValue)
        {
            value = boolValue;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Provides screenreader functionality and text-to-speech support
/// Uses PowerShell subprocess to access Windows System.Speech via PowerShell's .NET access
/// </summary>
public class ScreenreaderProvider : IDisposable
{
    private readonly Queue<string> _announcementQueue;
    private bool _isSpeaking;
    private bool _enabled;
    private bool _externalScreenreaderActive;
    private int _nextScreenreaderCheckTick;
    private const int ScreenreaderCheckIntervalMs = 2000;
    private bool _uiaAvailableChecked;
    private bool _uiaAvailable;
    private bool _nvdaAvailableChecked;
    private bool _nvdaAvailable;
    private bool _jawsAvailableChecked;
    private bool _jawsAvailable;
    private object _jawsApi;
    private MethodInfo _jawsSayString;
    private bool _narratorDetected;
    private bool _unity2022OrNewerChecked;
    private bool _unity2022OrNewer;
    private string _lastAnnouncedText;
    private int _lastAnnouncedTick;
    private const int MaxQueueLength = 100;
    private int _suppressHoverUntilTick;
    private bool _disposed;

    private static readonly HashSet<string> ExternalScreenreaderProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "nvda",
        "jfw",
        "jfwui",
        "fsd",
        "narrator",
        "narratorui",
        "narratorapp",
        "narratorhost",
        "systemaccess",
        "zoomtext",
        "zoomtextax",
        "fusion",
        "supernova",
        "supernovacore",
        "dolphin",
        "hal",
        "orca",
        "voiceover",
        "voiceoverui",
        "voiceoverhud",
        "voutil"
    };

    private const int SPI_GETSCREENREADER = 0x0046;

    [DllImport("user32.dll", SetLastError = false)]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("UIAutomationCore.dll", CharSet = CharSet.Unicode)]
    private static extern int UiaRaiseNotificationEvent(
        IntPtr provider,
        NotificationKind notificationKind,
        NotificationProcessing notificationProcessing,
        string displayString,
        string activityId);

    [DllImport("UIAutomationCore.dll")]
    private static extern int UiaHostProviderFromHwnd(IntPtr hwnd, out IntPtr provider);

    [DllImport("UIAutomationCore.dll")]
    private static extern bool UiaClientsAreListening();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);


    /// <summary>
    /// Gets or sets whether the screenreader is enabled
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!_enabled)
            {
                _announcementQueue.Clear();
                _isSpeaking = false;
                TryCancelAllSpeech();
            }
        }
    }

    public bool IsBusy => _isSpeaking || _announcementQueue.Count > 0;

    public ScreenreaderProvider()
    {
        _announcementQueue = new Queue<string>();
        _isSpeaking = false;
        _enabled = true;
        
        MelonLogger.Msg("ScreenreaderProvider: Initialized - will use PowerShell for TTS");
    }

    public void Announce(string text)
    {
        AnnounceInternal(text, isPriority: false, allowRepeat: false);
    }
    public void AnnouncePriority(string text)
    {
        AnnounceInternal(text, isPriority: true, allowRepeat: false);
    }

    public void AnnouncePriorityReplay(string text)
    {
        AnnounceInternal(text, isPriority: true, allowRepeat: true);
    }

    public bool ReplayLastAnnouncement()
    {
        if (string.IsNullOrWhiteSpace(_lastAnnouncedText))
            return false;

        AnnounceInternal(_lastAnnouncedText, isPriority: true, allowRepeat: true);
        return true;
    }

    public void SuppressHoverFor(int ms)
    {
        if (ms <= 0)
            return;

        var until = Environment.TickCount + ms;
        if (until > _suppressHoverUntilTick)
            _suppressHoverUntilTick = until;
    }

    public bool ShouldSuppressHover()
    {
        return Environment.TickCount <= _suppressHoverUntilTick;
    }

    private void AnnounceInternal(string text, bool isPriority, bool allowRepeat)
    {
        if (_disposed || !_enabled || string.IsNullOrWhiteSpace(text))
            return;

        text = TextSanitizer.StripRichTextTags(text);
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!allowRepeat && ShouldSuppressRepeat(text))
            return;

        if (isPriority)
        {
            UpdateLastAnnouncement(text);
            SuppressHoverFor(750);
        }

        UpdateExternalScreenreaderStatus();
        if (TrySpeakViaExternalScreenreader(text, isPriority: isPriority, allowUia: _externalScreenreaderActive))
        {
            if (isPriority)
            {
                ClearQueueAndResetSpeaking();
            }
            return;
        }

        if (isPriority && _isSpeaking)
        {
            TryCancelAllSpeech();
        }

        if (isPriority)
        {
            _announcementQueue.Clear();
        }

        EnqueueAnnouncement(text);
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (_disposed)
            return;

        UpdateExternalScreenreaderStatus();
        if (_isSpeaking || _announcementQueue.Count == 0 || !_enabled)
            return;

        _isSpeaking = true;

        try
        {
            while (_announcementQueue.Count > 0 && _enabled)
            {
                var text = _announcementQueue.Dequeue();
                if (!TrySpeakViaExternalScreenreader(text, isPriority: false, allowUia: _externalScreenreaderActive))
                {
                    SpeakViaPowerShell(text);
                }
                UpdateExternalScreenreaderStatus();
                if (_externalScreenreaderActive)
                {
                    ClearQueueAndResetSpeaking();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error("ProcessQueue: Exception during speech: " + ex.Message);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    private void SpeakViaPowerShell(string text)
    {
        try
        {
            // Escape single quotes in the text
            string escapedText = text.Replace("'", "''");
            
            // PowerShell command to use System.Speech - must load assembly first
            string psCommand = "[System.Reflection.Assembly]::LoadWithPartialName('System.Speech'); " +
                              "$speak = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                              "$speak.Speak('" + escapedText + "')";
            
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"" + psCommand + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                // Wait for it to finish (blocking for synchronous speech)
                if (process != null)
                {
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit(10000);  // 10 second timeout
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error("SpeakViaPowerShell: " + ex.Message);
        }
    }

    private void ClearQueueAndResetSpeaking()
    {
        _announcementQueue.Clear();
        _isSpeaking = false;
    }

    private void TryCancelAllSpeech()
    {
        // PowerShell processes can't easily be interrupted, so we just move on
        MelonLogger.Msg("TryCancelAllSpeech: Skipped (PowerShell speech)");
    }

    private void EnqueueAnnouncement(string text)
    {
        while (_announcementQueue.Count >= MaxQueueLength)
        {
            _announcementQueue.Dequeue();
        }

        _announcementQueue.Enqueue(text);
        UpdateLastAnnouncement(text);
    }

    private bool ShouldSuppressRepeat(string text)
    {
        var normalized = NormalizeRepeatText(text);
        var lastNormalized = NormalizeRepeatText(_lastAnnouncedText);
        if (!string.Equals(normalized, lastNormalized, StringComparison.Ordinal))
        {
            UpdateLastAnnouncement(text);
            return false;
        }

        return true;
    }

    private static string NormalizeRepeatText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var stripped = TextSanitizer.StripRichTextTags(text);
        if (string.IsNullOrWhiteSpace(stripped))
            return string.Empty;

        var trimmed = stripped.Trim();
        var buffer = new System.Text.StringBuilder(trimmed.Length);
        var lastWasSpace = false;
        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch))
                continue;

            if (char.IsWhiteSpace(ch))
            {
                if (lastWasSpace)
                    continue;
                buffer.Append(' ');
                lastWasSpace = true;
            }
            else if (char.IsLetterOrDigit(ch))
            {
                buffer.Append(char.ToLowerInvariant(ch));
                lastWasSpace = false;
            }
            else
            {
                // Ignore punctuation/symbol noise for repeat suppression.
            }
        }

        return buffer.ToString().Trim();
    }

    private void UpdateLastAnnouncement(string text)
    {
        UpdateLastAnnouncement(text, Environment.TickCount);
    }

    private void UpdateLastAnnouncement(string text, int tick)
    {
        _lastAnnouncedText = text;
        _lastAnnouncedTick = tick;
    }

    private void UpdateExternalScreenreaderStatus()
    {
        if (_disposed)
            return;

        var now = Environment.TickCount;
        if (now < _nextScreenreaderCheckTick)
            return;

        _nextScreenreaderCheckTick = now + ScreenreaderCheckIntervalMs;

        _narratorDetected = IsNarratorWindowPresent();
        bool active = IsScreenReaderFlagSet(_narratorDetected);
        try
        {
            foreach (var processName in ExternalScreenreaderProcessNames)
            {
                try
                {
                    var matching = Process.GetProcessesByName(processName);
                    if (matching != null && matching.Length > 0)
                    {
                        active = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore access/read failures for individual process names.
                }
            }
        }
        catch
        {
            // Keep current active state if process enumeration fails.
        }

        if (active == _externalScreenreaderActive)
            return;

        _externalScreenreaderActive = active;
        if (_externalScreenreaderActive)
        {
            ClearQueueAndResetSpeaking();
            MelonLogger.Msg("External screenreader detected - TTS disabled");
        }
        else
        {
            MelonLogger.Msg("External screenreader not detected - TTS enabled");
        }
    }

    private static bool IsScreenReaderFlagSet(bool narratorDetected)
    {
        if (!IsWindows())
            return false;

        if (narratorDetected)
            return true;

        try
        {
            int pvParam = 0;
            if (SystemParametersInfo(SPI_GETSCREENREADER, 0, ref pvParam, 0))
            {
                return pvParam != 0;
            }
        }
        catch
        {
            // Ignore failures and fall back to process detection.
        }

        return false;
    }

    private static bool IsNarratorWindowPresent()
    {
        try
        {
            var found = false;
            EnumWindows((hWnd, lParam) =>
            {
                var className = new StringBuilder(256);
                if (GetClassName(hWnd, className, className.Capacity) <= 0)
                    return true;

                var name = className.ToString();
                if (name.IndexOf("Narrator", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = true;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return found;
        }
        catch
        {
            return false;
        }
    }

    private bool TryRaiseUiaNotification(string text, bool isPriority)
    {
        IntPtr provider = IntPtr.Zero;
        try
        {
            if (!IsUiaAvailable())
                return false;

            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                hwnd = Process.GetCurrentProcess().MainWindowHandle;
            }

            if (hwnd == IntPtr.Zero)
                return false;

            if (UiaHostProviderFromHwnd(hwnd, out provider) != 0 || provider == IntPtr.Zero)
                return false;

            var kind = NotificationKind.Other;
            var processing = isPriority ? NotificationProcessing.ImportantMostRecent : NotificationProcessing.MostRecent;
            var result = UiaRaiseNotificationEvent(provider, kind, processing, text, "DeathAndAccess");
            return result == 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (provider != IntPtr.Zero)
            {
                try
                {
                    Marshal.Release(provider);
                }
                catch
                {
                    // Ignore release failures.
                }
            }
        }
    }

    private bool IsUiaAvailable()
    {
        if (!IsWindows())
            return false;

        if (_uiaAvailableChecked)
            return _uiaAvailable;

        _uiaAvailableChecked = true;
        try
        {
            _uiaAvailable = LoadLibrary("UIAutomationCore.dll") != IntPtr.Zero;
        }
        catch
        {
            _uiaAvailable = false;
        }
        return _uiaAvailable;
    }

    private static bool IsWindows()
    {
        try
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        catch
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }

    private bool TrySpeakViaExternalScreenreader(string text, bool isPriority, bool allowUia)
    {
        if (_disposed)
            return false;

        return TrySpeakNvda(text)
               || TrySpeakJaws(text, isPriority)
               || (allowUia && ShouldUseUiaNotifications() && TryRaiseUiaNotification(text, isPriority));
    }

    private bool ShouldUseUiaNotifications()
    {
        // UIA notifications are unstable on newer Unity player builds.
        // Keep them only for Narrator on older builds where they are needed.
        return _narratorDetected && !IsUnity2022OrNewer();
    }

    private bool IsUnity2022OrNewer()
    {
        if (_unity2022OrNewerChecked)
            return _unity2022OrNewer;

        _unity2022OrNewerChecked = true;

        try
        {
            var appType = TypeResolver.Get("UnityEngine.Application");
            var unityVersionProp = appType?.GetProperty("unityVersion", BindingFlags.Public | BindingFlags.Static);
            var unityVersion = unityVersionProp?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(unityVersion))
            {
                _unity2022OrNewer = false;
                return false;
            }

            var yearEnd = unityVersion.IndexOf('.');
            if (yearEnd > 0 && int.TryParse(unityVersion.Substring(0, yearEnd), out var majorYear))
            {
                _unity2022OrNewer = majorYear >= 2022;
                return _unity2022OrNewer;
            }
        }
        catch
        {
            _unity2022OrNewer = false;
        }

        return _unity2022OrNewer;
    }

    private bool TrySpeakNvda(string text)
    {
        if (!IsNvdaAvailable())
            return false;

        try
        {
            if (NvdaController.TestIfRunning() != 0)
                return false;

            NvdaController.Speak(text, interrupt: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsNvdaAvailable()
    {
        if (_nvdaAvailableChecked)
            return _nvdaAvailable;

        _nvdaAvailableChecked = true;

        if (!NvdaController.EnsureClientLoaded())
        {
            _nvdaAvailable = false;
            return false;
        }

        try
        {
            _ = NvdaController.TestIfRunning();
            _nvdaAvailable = true;
            return true;
        }
        catch (DllNotFoundException ex)
        {
            _ = ex;
        }
        catch (EntryPointNotFoundException ex)
        {
            _ = ex;
        }
        catch (BadImageFormatException ex)
        {
            _ = ex;
        }
        catch (Exception ex)
        {
            _ = ex;
        }

        _nvdaAvailable = false;
        return false;
    }

    private bool TrySpeakJaws(string text, bool isPriority)
    {
        if (!IsJawsAvailable())
            return false;

        try
        {
            var parameters = _jawsSayString.GetParameters();
            if (parameters.Length == 2)
            {
                var interrupt = isPriority ? 1 : 0;
                _jawsSayString.Invoke(_jawsApi, new object[] { text, interrupt });
                return true;
            }

            if (parameters.Length == 1)
            {
                _jawsSayString.Invoke(_jawsApi, new object[] { text });
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private bool IsJawsAvailable()
    {
        if (_jawsAvailableChecked)
            return _jawsAvailable;

        _jawsAvailableChecked = true;

        try
        {
            var apiType = Type.GetTypeFromProgID("FreedomSci.JawsApi");
            if (apiType == null)
                return false;

            _jawsApi = Activator.CreateInstance(apiType);
            if (_jawsApi == null)
                return false;

            _jawsSayString = apiType.GetMethod("SayString", new[] { typeof(string), typeof(int) })
                             ?? apiType.GetMethod("SayString", new[] { typeof(string), typeof(bool) })
                             ?? apiType.GetMethod("SayString", new[] { typeof(string) });

            _jawsAvailable = _jawsSayString != null;
            return _jawsAvailable;
        }
        catch
        {
            _jawsAvailable = false;
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _enabled = false;
        _externalScreenreaderActive = false;
        ClearQueueAndResetSpeaking();
        TryCancelAllSpeech();
    }
}

internal static class NvdaController
{
    private static bool _loadAttempted;
    private static bool _loaded;
    private static IntPtr _moduleHandle;
    private static NvdaSpeakTextDelegate _speakText;
    private static NvdaCancelSpeechDelegate _cancelSpeech;
    private static NvdaTestIfRunningDelegate _testIfRunning;

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private delegate int NvdaSpeakTextDelegate(string text);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int NvdaCancelSpeechDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int NvdaTestIfRunningDelegate();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static bool EnsureClientLoaded()
    {
        if (_loadAttempted)
            return _loaded;

        _loadAttempted = true;

        foreach (var path in GetCandidatePaths())
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                if (TryLoadClient(path, out _))
                {
                    _loaded = true;
                    return true;
                }
            }
            catch
            {
                // Ignore and try next path.
            }
        }

        foreach (var dllName in GetDllNames())
        {
            try
            {
                if (TryLoadClient(dllName, out _))
                {
                    _loaded = true;
                    return true;
                }
            }
            catch
            {
                // Ignore and report unavailable.
            }
        }

        _loaded = false;
        return false;
    }

    private static bool TryLoadClient(string pathOrName, out int errorCode)
    {
        errorCode = 0;

        var module = LoadLibrary(pathOrName);
        if (module == IntPtr.Zero)
        {
            errorCode = Marshal.GetLastWin32Error();
            return false;
        }

        if (!TryBindExports(module, out errorCode))
        {
            try
            {
                _ = FreeLibrary(module);
            }
            catch
            {
                // Ignore cleanup failures.
            }

            return false;
        }

        _moduleHandle = module;
        return true;
    }

    private static bool TryBindExports(IntPtr module, out int errorCode)
    {
        errorCode = 0;
        try
        {
            var speakPtr = GetProcAddressWithFallback(module, "nvdaController_speakText", 4);
            if (speakPtr == IntPtr.Zero)
            {
                errorCode = Marshal.GetLastWin32Error();
                return false;
            }

            var cancelPtr = GetProcAddressWithFallback(module, "nvdaController_cancelSpeech", 0);
            if (cancelPtr == IntPtr.Zero)
            {
                errorCode = Marshal.GetLastWin32Error();
                return false;
            }

            var testPtr = GetProcAddressWithFallback(module, "nvdaController_testIfRunning", 0);
            if (testPtr == IntPtr.Zero)
            {
                errorCode = Marshal.GetLastWin32Error();
                return false;
            }

            _speakText = Marshal.GetDelegateForFunctionPointer<NvdaSpeakTextDelegate>(speakPtr);
            _cancelSpeech = Marshal.GetDelegateForFunctionPointer<NvdaCancelSpeechDelegate>(cancelPtr);
            _testIfRunning = Marshal.GetDelegateForFunctionPointer<NvdaTestIfRunningDelegate>(testPtr);
            return _speakText != null && _cancelSpeech != null && _testIfRunning != null;
        }
        catch
        {
            errorCode = Marshal.GetLastWin32Error();
            _speakText = null;
            _cancelSpeech = null;
            _testIfRunning = null;
            return false;
        }
    }

    private static IntPtr GetProcAddressWithFallback(IntPtr module, string baseName, int stdcallStackBytes)
    {
        if (module == IntPtr.Zero || string.IsNullOrWhiteSpace(baseName))
            return IntPtr.Zero;

        // x64 exports are typically undecorated, x86 stdcall exports may be decorated.
        var names = new[]
        {
            baseName,
            "_" + baseName + "@" + stdcallStackBytes,
            baseName + "@" + stdcallStackBytes
        };

        foreach (var name in names)
        {
            var ptr = GetProcAddress(module, name);
            if (ptr != IntPtr.Zero)
                return ptr;
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<string> GetDllNames()
    {
        if (Environment.Is64BitProcess)
        {
            yield return "NVDAControllerClient64.dll";
            yield return "nvdaControllerClient64.dll";
            yield return "nvdaControllerClient.dll";
            yield break;
        }

        yield return "NVDAControllerClient32.dll";
        yield return "nvdaControllerClient32.dll";
        yield return "nvdaControllerClient.dll";
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in GetCandidateDirectories())
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            string fullDir;
            try
            {
                fullDir = Path.GetFullPath(dir);
            }
            catch
            {
                continue;
            }

            if (!seen.Add(fullDir))
                continue;

            foreach (var dllName in GetDllNames())
            {
                yield return Path.Combine(fullDir, dllName);
            }

            foreach (var subDir in GetArchitectureSubdirectories(fullDir))
            {
                foreach (var dllName in GetDllNames())
                {
                    yield return Path.Combine(subDir, dllName);
                }
            }
        }
    }

    private static IEnumerable<string> GetArchitectureSubdirectories(string parentDirectory)
    {
        if (string.IsNullOrWhiteSpace(parentDirectory))
            yield break;

        foreach (var name in GetPreferredArchitectureFolderNames())
        {
            string path;
            try
            {
                path = Path.Combine(parentDirectory, name);
            }
            catch
            {
                continue;
            }

            yield return path;
        }
    }

    private static IEnumerable<string> GetPreferredArchitectureFolderNames()
    {
        if (Environment.Is64BitProcess)
        {
            yield return "x64";
            yield return "x86";
            yield return "arm64";
            yield break;
        }

        yield return "x86";
        yield return "x64";
        yield return "arm64";
    }

    private static IEnumerable<string> GetCandidateDirectories()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(NvdaController).Assembly.Location);
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var currentDir = Environment.CurrentDirectory;

        yield return assemblyDir;
        if (!string.IsNullOrWhiteSpace(assemblyDir))
            yield return Directory.GetParent(assemblyDir)?.FullName;

        yield return baseDir;
        yield return currentDir;

        if (!string.IsNullOrWhiteSpace(baseDir))
        {
            yield return Path.Combine(baseDir, "Mods");
            yield return Path.Combine(baseDir, "UserLibs");
            yield return Path.Combine(baseDir, "Plugins");
            yield return Path.Combine(baseDir, "MelonLoader", "Mods");
            yield return Path.Combine(baseDir, "MelonLoader", "net35");
            yield return Path.Combine(baseDir, "MelonLoader", "Dependencies");
            yield return Path.Combine(baseDir, "MelonLoader", "Dependencies", "SupportModules");
        }
    }

    public static int TestIfRunning()
    {
        if (!EnsureClientLoaded() || _testIfRunning == null)
            return -1;

        return _testIfRunning();
    }

    public static void Speak(string text, bool interrupt)
    {
        if (!EnsureClientLoaded() || _speakText == null || _cancelSpeech == null)
            throw new DllNotFoundException("No loadable NVDA controller client DLL.");

        if (interrupt)
        {
            _cancelSpeech();
        }

        var res = _speakText(text);
        if (res != 0)
        {
            throw new System.ComponentModel.Win32Exception(res);
        }
    }
}

internal enum NotificationKind
{
    ItemAdded = 0,
    ItemRemoved = 1,
    ActionCompleted = 2,
    ActionAborted = 3,
    Other = 4
}

internal enum NotificationProcessing
{
    ImportantAll = 0,
    ImportantMostRecent = 1,
    All = 2,
    MostRecent = 3,
    CurrentThenMostRecent = 4
}
