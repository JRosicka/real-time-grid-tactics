using System.Linq;
using UnityEngine;

namespace Util {
    /// <summary>
    /// Utility methods for vectors
    /// </summary>
    public static class VectorUtil {
        public static string ConvertToString(this Vector2Int vector) {
            return $"({vector.x},{vector.y})";
        }
        
        public static Vector2Int ToVector2Int(this string vectorString) {
            // Remove the parentheses
            string[] numberStrings = vectorString.Trim('(', ')').Split(',').ToArray();
            int x = int.Parse(numberStrings[0]);
            int y = int.Parse(numberStrings[1]);
            return new Vector2Int(x, y);
        }
    }
}