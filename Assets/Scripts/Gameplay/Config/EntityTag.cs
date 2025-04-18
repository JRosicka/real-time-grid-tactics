using System;

namespace Gameplay.Config {
    public enum EntityTag {
        Structure = 1,
        Cavalry = 2, 
        Infantry = 3,
        Flying = 4,
        HomeBase = 5,
        Worker = 6,
        Resource = 7,
        ResourceCollector = 8
    }

    public static class EntityTagExtensions {
        public static string UnitDescriptorPlural(this EntityTag tag) {
            return tag switch {
                EntityTag.Structure => "structures",
                EntityTag.Cavalry => "cavalry units",
                EntityTag.Infantry => "infantry units",
                EntityTag.Flying => "flying units",
                EntityTag.HomeBase => "the keep",
                EntityTag.Worker => "workers",
                EntityTag.Resource => "resources",
                EntityTag.ResourceCollector => "resource collection structures",
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };
        }
    }
}