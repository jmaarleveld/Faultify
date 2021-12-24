using System;
using System.Collections.Generic;

namespace Faultify.MutationCollector
{
    /// <summary>
    ///     Collection of Types.
    /// </summary>
    public class TypeChecker
    {
        private static readonly ICollection<Type> NumericTypes = new HashSet<Type>
        {
            typeof(float), typeof(double),
            typeof(sbyte), typeof(byte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
        };

        /// <summary>
        ///     Specifies wether or not the given type is valid for an array target
        /// </summary>
        /// <param name="t">Type to be checked</param>
        /// <returns>True if a valid array type, false otherwise</returns>
        public static bool IsArrayType(Type t)
        {
            ISet<Type> arrayTypes = new HashSet<Type>();
            arrayTypes.UnionWith(NumericTypes);
            arrayTypes.Add(typeof(bool));
            arrayTypes.Add(typeof(char));

            return arrayTypes.Contains(t);
        }

        /// <summary>
        ///     Specifies whether or not the given type is of a numeric type
        /// </summary>
        /// <param name="t">Type to be checked</param>
        /// <returns>True if a valid array type, false otherwise</returns>
        public static bool IsNumericType(Type t)
        {
            return NumericTypes.Contains(t);
        }


        /// <summary>
        ///     Specifies wether or not the given type is valid for a variable target
        /// </summary>
        /// <param name="t">Type to be checked</param>
        /// <returns>True if a valid variable type, false otherwise</returns>
        public static bool IsVariableType(Type t)
        {
            ISet<Type> arrayTypes = new HashSet<Type>();
            arrayTypes.UnionWith(NumericTypes);
            arrayTypes.Add(typeof(bool));

            return arrayTypes.Contains(t);
        }


        /// <summary>
        ///     Specifies wether or not the given type is valid for a constant target
        /// </summary>
        /// <param name="t">Type to be checked</param>
        /// <returns>True if a valid constant type, false otherwise</returns>
        public static bool IsConstantType(Type t)
        {
            ISet<Type> arrayTypes = new HashSet<Type>();
            arrayTypes.UnionWith(NumericTypes);
            arrayTypes.Add(typeof(bool));
            arrayTypes.Add(typeof(string));

            return arrayTypes.Contains(t);
        }
    }
}
