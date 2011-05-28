using System;
using System.Collections.Generic;
using System.Text;

namespace VisualGit
{
    public static class EnumTools
    {
        /// <summary>
        /// Gets the first item from an IEnumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static T GetFirst<T>(IEnumerable<T> item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            
            foreach (T i in item)
            {
                return i;
            }

            return default(T);
        }

        /// <summary>
        /// Gets the first item from an IEnumerable or null if the item has 0 or more that 1 item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static T GetSingle<T>(IEnumerable<T> item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            T first = default(T);
            bool next = false;
            foreach (T i in item)
            {
                if (next)
                    return default(T);

                first = i;
                next = true;
            }

            return first;
        }

        public static bool IsNullOrEmpty<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            foreach (T t in enumerable)
                return false;
            return true;
        }

        public static bool IsEmpty<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null) 
                throw new ArgumentNullException("enumerable");

            foreach(T t in enumerable)
                return false;
            return true;
        }
    }
}