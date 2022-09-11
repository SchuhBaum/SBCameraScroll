using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WeakTables
{
    /// <summary>
    /// Represents a reference to an instance of <typeparamref name="T"/> that can be garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the object referenced.</typeparam>
    public sealed class WeakRef<T> where T : class
    {
        private GCHandle handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakRef{T}"/> class that references the specified object.
        /// </summary>
        /// <param name="target">The object referenced.</param>
        public WeakRef(T target)
        {
            handle = GCHandle.Alloc(target, GCHandleType.Weak);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakRef{T}"/> class that references nothing.
        /// </summary>
        public WeakRef() { }

        /// <summary>
        /// Tries to retrieve the target object that is referenced by the current <see cref="WeakRef{T}"/> object.
        /// </summary>
        /// <param name="value">The target object, or null if no object is targeted.</param>
        /// <returns>True if the target was retrieved; otherwise, false.</returns>
        public bool TryGetTarget([MaybeNullWhen(false)] out T value)
        {
            if (handle.IsAllocated && handle.Target is T target) {
                value = target;
                return true;
            }
            value = null;
            return false;
        }

        /// <inheritdoc/>
        ~WeakRef()
        {
            if (handle.IsAllocated)
                handle.Free();
        }
    }
}
