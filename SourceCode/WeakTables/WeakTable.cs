using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WeakTables.Collections;
using static System.Reflection.BindingFlags;

namespace WeakTables
{
    /// <summary>
    /// Attaches data of type <typeparamref name="TData"/> to instances of <typeparamref name="T"/> while avoiding memory leaks.
    /// </summary>
    /// <typeparam name="T">The type to attach data to.</typeparam>
    /// <typeparam name="TData">The data to attach to <typeparamref name="T"/> instances.</typeparam>
    public sealed class WeakTable<T, TData> where T : notnull where TData : class
    {
        /// <summary>
        /// A delegate used to get new <typeparamref name="TData"/> instances.
        /// </summary>
        /// <param name="instance">The instance that the data will be attached to.</param>
        /// <returns>A new instance of <typeparamref name="TData"/>.</returns>
        public delegate TData FactoryCallback(T instance);

        private static bool verified = false;

        private static void VerifyTypes()
        {
            if (verified) {
                return;
            }

            if (!typeof(TData).IsSealed) {
                throw new ArgumentException($"The type \"{GenericToString(typeof(TData))}\" is not sealed.");
            }

            const BindingFlags fieldFlags = Public | NonPublic | Instance | DeclaredOnly;

            static string GenericToString(Type t)
            {
                // Thanks https://stackoverflow.com/a/2448918
                if (!t.IsGenericType) {
                    return t.FullName;
                }

                string genericTypeName = t.GetGenericTypeDefinition().FullName;
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
                string genericArgs = string.Join(", ", t.GetGenericArguments().Select(GenericToString).ToArray());
                return $"{genericTypeName}<{genericArgs}>";
            }

            static IEnumerable<FieldInfo> GetRecursiveFields()
            {
                var cache = new HashSet<Type>();

                foreach (var field in typeof(TData).GetFields(fieldFlags)) {
                    if (IsRecursive(cache, field.FieldType, true)) {
                        yield return field;
                    }
                }
            }

            static bool IsRecursive(HashSet<Type> cache, Type t, bool checkAssignment)
            {
                if (t is null || t.IsPrimitive || t.IsPointer || t.IsEnum || !cache.Add(t)) {
                    return false;
                }

                if (checkAssignment && (!t.IsSealed || t.IsAssignableFrom(typeof(T)))) {
                    return true;
                }

                foreach (var f in t.GetFields(fieldFlags)) {
                    if (IsRecursive(cache, f.FieldType, true)) {
                        return true;
                    }
                }

                return IsRecursive(cache, t.BaseType, false) || IsRecursive(cache, t.GetElementType(), true);
            }

            var fields = GetRecursiveFields().ToList();

            if (fields.Count > 0) {
                string fieldString;

                if (fields.Count > 1) {
                    fieldString = $"In {GenericToString(typeof(TData))}, the fields {{{string.Join(",", fields.Select(f => f.Name).ToArray())}}}";
                } else {
                    fieldString = $"The field {GenericToString(typeof(TData))}::{fields[0].Name}";
                }

                string s = fields.Count > 1 ? "s" : "";

                throw new LeakyFieldException($"{fieldString} can hold a reference to instances of \"{GenericToString(typeof(T))}\", causing memory leaks. Consider using WeakRef<T> for the field type{s} or representing the data another way.", fields);
            }

            verified = true;
        }

        private readonly ConditionalWeakTable<T, TData> data = new ConditionalWeakTable<T, TData>();
        private readonly FactoryCallback factory;

        /// <summary>
        /// Initializes a new <see cref="WeakTable{T, TData}"/> instance.
        /// </summary>
        /// <param name="factory">A function that returns a new <typeparamref name="TData"/> instance given its associated <typeparamref name="T"/> instance.</param>
        /// <exception cref="LeakyFieldException"/>
        public WeakTable(FactoryCallback factory)
        {
            VerifyTypes();

            this.factory = factory;
        }

        /// <summary>
        /// Initializes a new <see cref="WeakTable{T, TData}"/> instance.
        /// </summary>
        /// <param name="factory">A function that returns a new <typeparamref name="TData"/> instance given its associated <typeparamref name="T"/> instance.</param>
        /// <param name="verify">If <see langword="false"/>, the instance will be initialized without checking for possible memory leaks.</param>
        /// <exception cref="LeakyFieldException"/>
        public WeakTable(FactoryCallback factory, bool verify)
        {
            if (verify) {
                VerifyTypes();
            }

            this.factory = factory;
        }

        /// <summary>
        /// Gets the data associated with <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance whose data to fetch.</param>
        public TData this[T instance] => data.GetValue(instance, Factory);

        private TData Factory(T t) => factory(t);
    }
}
