using System;

namespace Gameplay {
    public enum FogOfWarSetting {
        None = 0,
        TwoRange = 2,
        ThreeRange = 3
    }

    public static class FogOfWarSettingExtensions {
        public static int ToFogOfWarIndex(this FogOfWarSetting fogOfWarSetting) {
            return fogOfWarSetting switch {
                FogOfWarSetting.None => 0,
                FogOfWarSetting.TwoRange => 1,
                FogOfWarSetting.ThreeRange => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(fogOfWarSetting), fogOfWarSetting, null)
            };
        }

        public static FogOfWarSetting ToFogOfWarSetting(this int index) {
            return index switch {
                0 => FogOfWarSetting.None,
                1 => FogOfWarSetting.TwoRange,
                2 => FogOfWarSetting.ThreeRange,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }
    }
}