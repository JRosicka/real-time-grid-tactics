using System;

namespace Util {
    /// <summary>
    /// Provides helper methods to work with enums. Includes more generalized implementations of existing helper methods.
    /// </summary>
    public static class EnumUtil {
        /// <summary>
        /// Gets all the values of the indicated enum type and stores them in a new, sorted array. 
        /// If indicated, the array is resorted in signed order. 
        /// Enum.GetValues documentation: https://msdn.microsoft.com/en-us/library/system.enum.getvalues(v=vs.110).aspx
        /// </summary>
        /// <returns>The sorted array of enum values.</returns>
        /// <param name="signedOrder">If set to <c>true</c> sort the array in signed order.
        /// If set to <c>false</c>, sort the array by the binary values of their enumeration constants (in unsigned order).</param>
        /// <typeparam name="T">The enumeration type being used.</typeparam>
        public static T[] GetValues<T>(bool signedOrder = true) where T : struct {
            return (T[])GetValues(typeof(T), signedOrder);
        }

        /// <summary>
        /// Gets all the values of the indicated enum type and stores them in a new, sorted array. 
        /// If indicated, the array is resorted in signed order. 
        /// Enum.GetValues documentation: https://msdn.microsoft.com/en-us/library/system.enum.getvalues(v=vs.110).aspx
        /// </summary>
        /// <returns>The sorted array of enum values.</returns>
        /// <param name="t">The enum type whose elements are wanted.</param>
        /// <param name="signedOrder">If set to <c>true</c> sort the array in signed order.
        /// If set to <c>false</c>, sort the array by the binary values of their enumeration constants (in unsigned order).</param>
        public static Array GetValues(Type t, bool signedOrder = true) {
            Array data = Enum.GetValues(t);
            if (signedOrder) {
                Array.Sort(data);
            }

            return data;
        }
    }
}