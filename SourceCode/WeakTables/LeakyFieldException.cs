using System;
using System.Collections.Generic;
using System.Reflection;

namespace WeakTables
{
    /// <summary>
    /// The exception that is thrown when a weak table's value can reference its key, causing a memory leak.
    /// </summary>
    [Serializable]
    public sealed class LeakyFieldException : Exception
    {
        /// <inheritdoc/>
        public LeakyFieldException(string message, IEnumerable<FieldInfo> fields) : base(message)
        {
            Fields = fields;
        }

        /// <summary>
        /// The fields that can hold a leaky reference.
        /// </summary>
        public IEnumerable<FieldInfo> Fields { get; }
    }
}
