using System;

/// <summary>
/// Util methods for actions
/// </summary>
public static class ActionUtil {
    public static void SafeInvoke(this Action action) {
        action?.Invoke();
    }

    public static void SafeInvoke<T>(this Action<T> action, T param) {
        action?.Invoke(param);
    }

    public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 param1, T2 param2) {
        action?.Invoke(param1, param2);
    }
    
    public static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3) {
        action?.Invoke(param1, param2, param3);
    }
}