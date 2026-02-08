namespace Death_and_Access;

using System.Reflection;
using HarmonyLib;

[HarmonyPatch]
internal static class InputMousePositionPatch
{
    private static MethodBase TargetMethod()
    {
        var inputType = TypeResolver.Get("UnityEngine.Input");
        return inputType?.GetProperty("mousePosition", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod();
    }

    private static bool Prefix(ref object __result)
    {
        var nav = UiNavigationHandler.Instance;
        if (nav == null || !nav.IsVirtualCursorActive || nav.ShouldBypassVirtualMousePositionPatch)
            return true;

        if (!nav.TryGetVirtualCursorPosition(out var x, out var y))
            return true;

        var vector3Type = TypeResolver.Get("UnityEngine.Vector3");
        if (vector3Type == null)
            return true;

        try
        {
            var ctor = vector3Type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            if (ctor == null)
                return true;

            __result = ctor.Invoke(new object[] { x, y, 0f });
            return false;
        }
        catch
        {
            return true;
        }
    }
}

[HarmonyPatch]
internal static class InputGetMouseButtonPatch
{
    private static MethodBase TargetMethod()
    {
        var inputType = TypeResolver.Get("UnityEngine.Input");
        return inputType?.GetMethod("GetMouseButton", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
    }

    private static bool Prefix(int button, ref bool __result)
    {
        var nav = UiNavigationHandler.Instance;
        if (button != 0 || nav == null || !nav.IsVirtualCursorActive)
            return true;

        if (!VirtualMouseState.GetHeld())
            return true;

        __result = true;
        return false;
    }
}

[HarmonyPatch]
internal static class InputGetMouseButtonDownPatch
{
    private static MethodBase TargetMethod()
    {
        var inputType = TypeResolver.Get("UnityEngine.Input");
        return inputType?.GetMethod("GetMouseButtonDown", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
    }

    private static bool Prefix(int button, ref bool __result)
    {
        var nav = UiNavigationHandler.Instance;
        if (button != 0 || nav == null || !nav.IsVirtualCursorActive)
            return true;

        if (!VirtualMouseState.GetDown())
            return true;

        __result = true;
        return false;
    }
}

[HarmonyPatch]
internal static class InputGetMouseButtonUpPatch
{
    private static MethodBase TargetMethod()
    {
        var inputType = TypeResolver.Get("UnityEngine.Input");
        return inputType?.GetMethod("GetMouseButtonUp", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
    }

    private static bool Prefix(int button, ref bool __result)
    {
        var nav = UiNavigationHandler.Instance;
        if (button != 0 || nav == null || !nav.IsVirtualCursorActive)
            return true;

        if (!VirtualMouseState.GetUp())
            return true;

        __result = true;
        return false;
    }
}
