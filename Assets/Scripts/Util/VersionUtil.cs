using System;
using UnityEngine;

namespace Util {
    /// <summary>
    /// Utility methods around the app version
    /// </summary>
    public static class VersionUtil {
        public static int GetVersionNumber() {
            string[] versionParts = Application.version.Split('.');
            int majorVersion = Convert.ToInt32(versionParts[0]);
            int minorVersion = Convert.ToInt32(versionParts[1]);
            int superMinorVersion = Convert.ToInt32(versionParts[2]);
            return majorVersion * 10000 + minorVersion * 100 + superMinorVersion;
        }
    }
}