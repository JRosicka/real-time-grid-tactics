using System;
using System.Collections.Generic;

namespace Util {
    public static class ListUtil {
        /// <summary>
        /// Add an element to a list. Assumes that the list is already sorted.
        /// </summary>
        public static void AddSorted<T>(this List<T> list, T item) where T : IComparable<T> {
            if (list.Count == 0) {
                // Nothing in list yet, so just add
                list.Add(item);
                return;
            }

            if (list[^1].CompareTo(item) <= 0) {
                // Last element in list is less than the item, so add item to end
                list.Add(item);
                return;
            }

            if (list[0].CompareTo(item) >= 0) {
                // First element in list is greater than the item, so add item to beginning
                list.Insert(0, item);
                return;
            }

            int index = list.BinarySearch(item);
            if (index < 0) {
                // If the item is not in the list, then BinarySearch returns "a negative number that is the bitwise
                // complement of the index of the next element that is larger than the item or, if there is no larger
                // element, the bitwise complement of Count." So, getting the bitwise complement of the index gives us 
                // where we want to put the new item.
                index = ~index;
            }
            list.Insert(index, item);
        }
    }
}