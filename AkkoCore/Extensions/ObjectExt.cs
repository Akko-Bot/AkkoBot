using System.Collections.Generic;

namespace AkkoCore.Extensions
{
    public static class ObjectExt
    {
        /// <summary>
        /// Determines whether this object is equal to any element in <paramref name="objects"/>.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="objects">Collection of objects to compare to.</param>
        /// <returns><see langword="true"/> if this object is equal to any element in <paramref name="objects"/>, <see langword="false"/> otherwise.</returns>
        public static bool EqualsAny(this object thisObj, params object[] objects)
        {
            if (thisObj is null && objects is null)
                return true;

            foreach (var obj in objects)
            {
                if (thisObj?.Equals(obj) is true)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this object is equal to any element in <paramref name="objects"/>.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="objects">Collection of objects to compare to.</param>
        /// <returns><see langword="true"/> if this object is equal to any element in <paramref name="objects"/>, <see langword="false"/> otherwise.</returns>
        public static bool EqualsAny(this object thisObj, IEnumerable<object> objects)
        {
            if (thisObj is null && objects is null)
                return true;

            foreach (var obj in objects)
            {
                if (thisObj?.Equals(obj) is true)
                    return true;
            }

            return false;
        }
    }
}