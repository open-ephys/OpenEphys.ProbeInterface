using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Encapsulates all per-contact data for a single electrode contact on a <see cref="Probe"/>.
    /// Instances are created by <see cref="Probe"/> during construction and cannot be created
    /// externally. Contact-level annotations can be read and written via <see cref="GetAnnotation{T}"/>,
    /// <see cref="SetAnnotation{T}"/>, and <see cref="RemoveAnnotation"/>. Channel mapping is
    /// managed at the <see cref="ProbeGroup"/> level via <see cref="Probe.ChannelMap"/>.
    /// </summary>
    public sealed class Contact
    {
        private readonly ContactAnnotationStore store;
        private readonly int index;
        private readonly int totalContacts;

        /// <summary>Gets the x-position of the contact centre.</summary>
        public double PosX { get; }

        /// <summary>Gets the y-position of the contact centre.</summary>
        public double PosY { get; }

        /// <summary>Gets the z-position of the contact centre, or null for 2D probes.</summary>
        public double? PosZ { get; }

        /// <summary>Gets the shape of the contact.</summary>
        public ContactShape Shape { get; }

        /// <summary>Gets the shape parameters for the contact.</summary>
        public ContactShapeParam ShapeParams { get; }

        /// <summary>Gets the contact ID label, or null if the source JSON omitted contact_ids. Not guaranteed to be unique across probes.</summary>
        public string? ContactId { get; }

        /// <summary>Gets the shank ID this contact belongs to, or null if the source JSON omitted shank_ids.</summary>
        public string? ShankId { get; }

        /// <summary>Gets the contact plane axes as a 2×ndim matrix, or null if not specified.</summary>
        public double[][]? PlaneAxes { get; }

        /// <summary>Gets the probe side this contact is on (e.g. "front", "back"), or null if not specified.</summary>
        public string? Side { get; }

        /// <summary>
        /// Initializes a new <see cref="Contact"/>. Called by <see cref="Probe"/> during construction.
        /// <paramref name="store"/> is shared across all contacts on the same probe; mutations
        /// through any contact are immediately visible probe-wide.
        /// </summary>
        internal Contact(
            double posX, double posY, double? posZ,
            ContactShape shape, ContactShapeParam shapeParams,
            string? contactId, string? shankId,
            double[][]? planeAxes, string? side,
            int index, int totalContacts,
            ContactAnnotationStore store)
        {
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            Shape = shape;
            ShapeParams = shapeParams;
            ContactId = contactId;
            ShankId = shankId;
            PlaneAxes = planeAxes;
            Side = side;
            this.index = index;
            this.totalContacts = totalContacts;
            this.store = store;
        }

        /// <summary>
        /// Returns the annotation value for this contact for the given key, converted to
        /// <typeparamref name="T"/>. Returns the default value of <typeparamref name="T"/> if the
        /// key is absent or the stored value is null.
        /// </summary>
        public T? GetAnnotation<T>(string key)
        {
            if (store.Data == null || !store.Data.TryGetValue(key, out var arr))
                return default;
            var value = arr[index];
            if (value == null) return default;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Sets the annotation value for this contact for the given key. If the key does not yet
        /// exist in the probe's annotation store, a new array (length = total contacts on the
        /// probe) is created with all other slots initialized to null.
        /// </summary>
        public void SetAnnotation<T>(string key, T value)
        {
            store.Data ??= new Dictionary<string, object[]>();
            if (!store.Data.TryGetValue(key, out var arr))
            {
                arr = new object[totalContacts];
                store.Data[key] = arr;
            }
            arr[index] = value!;
        }

        /// <summary>
        /// Clears the annotation value for this contact for the given key (sets the slot to null).
        /// Returns true if the key was found; false if it was absent. The key itself remains in
        /// the store until all contacts' values for it are null.
        /// </summary>
        public bool RemoveAnnotation(string key)
        {
            if (store.Data == null || !store.Data.TryGetValue(key, out var arr)) return false;
            arr[index] = null!;
            if (arr.All(v => v == null))
                store.Data.Remove(key);
            return true;
        }
    }
}
