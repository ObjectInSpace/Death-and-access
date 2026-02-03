namespace Death_and_Access;

using System;
using System.Reflection;

internal static class VirtualMouseState
{
    private static Type _inputType;
    private static Type _keyCodeType;
    private static MethodInfo _getKeyMethod;
    private static MethodInfo _getKeyDownMethod;
    private static MethodInfo _getKeyUpMethod;
    private static object _returnKey;
    private static object _keypadEnterKey;
    private static object _spaceKey;
    private static bool _spaceToggleHeld;
    private static bool _held;
    private static bool _down;
    private static bool _up;
    private static int _lastUpdateTick;

    internal static bool GetHeld()
    {
        RefreshIfNeeded();
        return _held;
    }

    internal static bool GetDown()
    {
        RefreshIfNeeded();
        return _down;
    }

    internal static bool GetUp()
    {
        RefreshIfNeeded();
        return _up;
    }

    internal static void Reset()
    {
        _held = false;
        _down = false;
        _up = false;
        _spaceToggleHeld = false;
        _lastUpdateTick = 0;
    }

    private static void RefreshIfNeeded()
    {
        var now = Environment.TickCount;
        if (now == _lastUpdateTick)
            return;

        _lastUpdateTick = now;
        UpdateFromInput();
    }

    private static void UpdateFromInput()
    {
        if (!EnsureInputTypes())
        {
            _held = false;
            _down = false;
            _up = false;
            return;
        }

        var enterHeld = GetKey(_returnKey) || GetKey(_keypadEnterKey);
        var enterDown = GetKeyDown(_returnKey) || GetKeyDown(_keypadEnterKey);
        var enterUp = GetKeyUp(_returnKey) || GetKeyUp(_keypadEnterKey);
        var spaceDown = GetKeyDown(_spaceKey);

        _down = false;
        _up = false;

        if (enterHeld || enterDown || enterUp)
        {
            _held = enterHeld;
            _down = enterDown;
            _up = enterUp;
            return;
        }

        if (spaceDown)
        {
            _spaceToggleHeld = !_spaceToggleHeld;
            _down = _spaceToggleHeld;
            _up = !_spaceToggleHeld;
        }

        _held = _spaceToggleHeld;
    }

    private static bool EnsureInputTypes()
    {
        _inputType ??= TypeResolver.Get("UnityEngine.Input");
        _keyCodeType ??= TypeResolver.Get("UnityEngine.KeyCode");
        if (_inputType == null || _keyCodeType == null)
            return false;

        _getKeyMethod ??= _inputType.GetMethod("GetKey", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
        _getKeyDownMethod ??= _inputType.GetMethod("GetKeyDown", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
        _getKeyUpMethod ??= _inputType.GetMethod("GetKeyUp", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
        _returnKey ??= Enum.Parse(_keyCodeType, "Return");
        _keypadEnterKey ??= Enum.Parse(_keyCodeType, "KeypadEnter");
        _spaceKey ??= Enum.Parse(_keyCodeType, "Space");

        return _getKeyMethod != null && _getKeyDownMethod != null && _getKeyUpMethod != null;
    }

    private static bool GetKey(object keyCode)
    {
        try
        {
            if (keyCode == null || _getKeyMethod == null)
                return false;

            var result = _getKeyMethod.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }

    private static bool GetKeyDown(object keyCode)
    {
        try
        {
            if (keyCode == null || _getKeyDownMethod == null)
                return false;

            var result = _getKeyDownMethod.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }

    private static bool GetKeyUp(object keyCode)
    {
        try
        {
            if (keyCode == null || _getKeyUpMethod == null)
                return false;

            var result = _getKeyUpMethod.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }
}
