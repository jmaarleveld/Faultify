using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.Core.Extensions;
using NLog;

namespace Faultify.MutationCollector
{
    /// <summary>
    ///     Random value generator that can be used for generating random values for IL-instructions.
    /// </summary>
    public static class RandomValueGenerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static Random Rand { get; } = new Random();

        // For making sure that random replacement variables are not out of range.
        private static readonly Dictionary<Type, double> sizes = new Dictionary<Type, double>()
        {
            {typeof(float), float.MaxValue}, 
            {typeof(sbyte), sbyte.MaxValue}, 
            {typeof(short), short.MaxValue}, 
            {typeof(int), int.MaxValue}, 
            {typeof(long), long.MaxValue},
            {typeof(double), double.MaxValue},
            {typeof(byte), byte.MaxValue},
            {typeof(ushort), ushort.MaxValue},
            {typeof(uint), int.MaxValue},
            {typeof(ulong), ulong.MaxValue}

        };
        /// <summary>
        ///     Generates a random value for the given field type.
        /// </summary>
        /// <param name="type">the type of the field for which a random value is to be generated</param>
        /// <param name="reference">a il-reference to the field that contains the originalField value</param>
        /// <returns>The random value.</returns>
        public static object GenerateValueForField(Type type, object reference)
        {
            object newRef = null;

            try
            {
                if (type == typeof(bool))
                {
                    newRef = ChangeBoolean(reference);
                    Logger.Trace($"Changing boolean value from {reference} to {newRef}");
                }
                else if (type == typeof(string))
                {
                    newRef = ChangeString();
                    Logger.Trace($"Changing string value from {reference} to {newRef}");
                }
                else if (type == typeof(char))
                {
                    newRef = ChangeChar(reference);
                    Logger.Trace($"Changing char value from {reference} to {newRef}");
                }
                else if (type.IsNumericType())
                {
                    newRef = ChangeNumber(type, reference);
                    Logger.Trace($"Changing numeric value from {reference} to {newRef}");
                }
                else
                {
                    Logger.Warn($"Type {type} is not supported");
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e, "There was probably an error casting a value, defaulting to null");
            }

            return newRef;
        }

        /// <summary>
        ///     Returns a flipped version of the passed boolean.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private static object ChangeBoolean(object reference)
        {
            var value = Convert.ToBoolean(reference);
            return Convert.ToInt32(!value);
        }

        /// <summary>
        ///     Generates a new random string.
        /// </summary>
        /// <returns>The random string</returns>
        private static object ChangeString()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringChars = new char[32];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[Rand.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        ///     Generates a new random character.
        /// </summary>
        /// <param name="originalRef">Reference to the orginial char</param>
        /// <returns>The random character</returns>
        private static object ChangeChar(object originalRef)
        { 
            var original = Convert.ToChar(originalRef);
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            while (true)
            {
                char generated = chars[Rand.Next(chars.Length)];
                if (original != generated) return generated;
            }
        }

        /// <summary>
        ///     Generates a new value for the given number reference
        /// </summary>
        /// <param name="type">Type of the number</param>
        /// <param name="original">Original number</param>
        /// <returns>The random numeric object</returns>
        private static object ChangeNumber(Type type, object original)
        {
            while (true)
            {
                // Get a random value of a specified type, within the specified type's size range.
                // I am sorry for how hideous this is, but there is no elegant way to do this
                // because C# has no generic type for numerics.
                object generated;
                if (type == typeof(float) || type == typeof(double))
                {
                    generated = Convert.ChangeType(Rand.NextDouble(), type);
                } 
                else
                {
                    // This is a really hacky solution.
                    // Get the maximum possible value of a type from a list
                    // If it's too big, settle for int32 maximum value.
                    var maxSize = sizes.GetValueOrDefault(type) / 2;
                    var bound = maxSize >= int.MaxValue ? int.MaxValue : maxSize; 
                    generated = Convert.ToInt32(Rand.Next((int) bound));

                }
                if (!original.Equals(generated)) return generated;
            }
        }
    }
}
