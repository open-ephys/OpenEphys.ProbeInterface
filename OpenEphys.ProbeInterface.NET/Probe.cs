using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Represents a single probe in a <see cref="ProbeGroup"/>. The primary public API is
    /// <see cref="Contacts"/>, which exposes all per-contact data as a strongly-typed collection.
    /// Channel mapping is managed exclusively via <see cref="ChannelWiring"/> and exposed through
    /// <see cref="ProbeGroup.ChannelMap"/> or <see cref="SingleProbeGroup.ChannelMap"/>.
    /// JSON serialization/deserialization preserves the probeinterface parallel-array format transparently.
    /// </summary>
    public class Probe
    {
        /// <summary>
        /// Gets the number of spatial dimensions (2 or 3).
        /// </summary>
        [XmlIgnore]
        [JsonProperty("ndim", Required = Required.Always)]
        public ProbeNdim NumDimensions { get; }

        /// <summary>
        /// Gets the SI unit used for contact positions.
        /// </summary>
        [XmlIgnore]
        [JsonProperty("si_units", Required = Required.Always)]
        public ProbeSiUnits SiUnits { get; }

        /// <summary>
        /// Gets the probe-level annotations (model name, manufacturer).
        /// </summary>
        [XmlIgnore]
        [JsonProperty("annotations", Required = Required.Always)]
        public ProbeAnnotations Annotations { get; }

        /// <summary>
        /// Gets the planar contour describing the physical outline of the probe, or null.
        /// </summary>
        [XmlIgnore]
        [JsonProperty("probe_planar_contour", NullValueHandling = NullValueHandling.Ignore)]
        public double[][]? ProbePlanarContour { get; }

        /// <summary>
        /// Gets the contacts on this probe. Each <see cref="Contact"/> carries all per-contact data:
        /// position, shape, shank, plane axes, side, and annotations.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<Contact> Contacts { get; }

        /// <summary>
        /// Gets the number of contacts on this probe.
        /// </summary>
        [JsonIgnore]
        public int NumberOfContacts => Contacts.Count;

        /// <summary>
        /// Gets the annotation keys that have at least one value defined across the probe's contacts.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> ContactAnnotationKeys =>
            annotationStore.Data?.Keys ?? Enumerable.Empty<string>();

        private readonly ContactAnnotationStore annotationStore = new();

        // Newtonsoft.Json serializes non-public [JsonProperty] members; deserialization
        // goes through [JsonConstructor] so these getters are never called during reads.

        [JsonProperty("contact_positions", Required = Required.Always)]
        private double[][] ContactPositionsJson => Contacts.Select(c =>
            c.PosZ.HasValue
                ? new double[] { c.PosX, c.PosY, c.PosZ.Value }
                : new double[] { c.PosX, c.PosY }).ToArray();

        [JsonProperty("contact_plane_axes", NullValueHandling = NullValueHandling.Ignore)]
        private double[][][]? ContactPlaneAxesJson =>
            Contacts.All(c => c.PlaneAxes == null) ? null
            : Contacts.Select(c => c.PlaneAxes ?? new double[][] { new double[] { 1, 0 }, new double[] { 0, 1 } }).ToArray();

        [JsonProperty("contact_shapes", Required = Required.Always)]
        private ContactShape[] ContactShapesJson => Contacts.Select(c => c.Shape).ToArray();

        [JsonProperty("contact_shape_params", Required = Required.Always)]
        private ContactShapeParam[] ContactShapeParamsJson => Contacts.Select(c => c.ShapeParams).ToArray();

        [JsonProperty("device_channel_indices", NullValueHandling = NullValueHandling.Ignore)]
        private int[]? DeviceChannelIndicesJson
        {
            get
            {
                if (ChannelMap == null) return null;
                var arr = Enumerable.Repeat(-1, NumberOfContacts).ToArray();
                foreach (var kvp in ChannelMap)
                    arr[kvp.Value] = kvp.Key;
                return arr;
            }
        }

        [JsonProperty("contact_ids", NullValueHandling = NullValueHandling.Ignore)]
        private string?[]? ContactIdsJson => Contacts.All(c => c.ContactId == null) ? null
            : Contacts.Select(c => c.ContactId).ToArray();

        [JsonProperty("shank_ids", NullValueHandling = NullValueHandling.Ignore)]
        private string?[]? ShankIdsJson => Contacts.All(c => c.ShankId == null) ? null
            : Contacts.Select(c => c.ShankId).ToArray();

        [JsonProperty("contact_sides", NullValueHandling = NullValueHandling.Ignore)]
        private string[]? ContactSidesJson =>
            Contacts.All(c => c.Side == null) ? null
            : Contacts.Select(c => c.Side ?? "").ToArray();

        [JsonProperty("contact_annotations", NullValueHandling = NullValueHandling.Ignore)]
        private Dictionary<string, object[]>? ContactAnnotationsJson => annotationStore.Data;

        /// <summary>
        /// JSON constructor. Deserializes a probe from the probeinterface parallel-array format and
        /// builds the <see cref="Contacts"/> collection. Throws <see cref="ArgumentException"/> if the
        /// probeinterface json schema is not respected.
        /// </summary>
        [JsonConstructor]
        internal Probe(
            ProbeNdim ndim, ProbeSiUnits si_units, ProbeAnnotations annotations,
            Dictionary<string, object[]>? contact_annotations,
            double[][] contact_positions, double[][][]? contact_plane_axes,
            ContactShape[] contact_shapes, ContactShapeParam[] contact_shape_params,
            double[][]? probe_planar_contour, int[]? device_channel_indices,
            string[]? contact_ids, string[]? shank_ids, string[]? contact_sides)
        {
            if (ndim != ProbeNdim.Two && ndim != ProbeNdim.Three)
                throw new ArgumentException($"ndim must be 2 or 3, but was {(int)ndim}.");

            int n = contact_positions.Length;

            // Same number of contacts for every input array
            if (contact_shapes.Length != n || contact_shape_params.Length != n)
                throw new ArgumentException(
                    $"contact_positions ({n}), contact_shapes ({contact_shapes.Length}), and " +
                    $"contact_shape_params ({contact_shape_params.Length}) must all have the same length.");

            if (contact_plane_axes != null && contact_plane_axes.Length != n)
                throw new ArgumentException(
                    $"contact_plane_axes length ({contact_plane_axes.Length}) must match contact count ({n}).");
            if (contact_ids != null && contact_ids.Length != n)
                throw new ArgumentException(
                    $"contact_ids length ({contact_ids.Length}) must match contact count ({n}).");
            if (shank_ids != null && shank_ids.Length != n)
                throw new ArgumentException(
                    $"shank_ids length ({shank_ids.Length}) must match contact count ({n}).");
            if (device_channel_indices != null && device_channel_indices.Length != n)
                throw new ArgumentException(
                    $"device_channel_indices length ({device_channel_indices.Length}) must match contact count ({n}).");
            if (contact_sides != null && contact_sides.Length != n)
                throw new ArgumentException(
                    $"contact_sides length ({contact_sides.Length}) must match contact count ({n}).");
            if (contact_annotations != null)
            {
                foreach (var kvp in contact_annotations)
                {
                    if (kvp.Value.Length != n)
                        throw new ArgumentException(
                            $"contact_annotations[\"{kvp.Key}\"] length ({kvp.Value.Length}) must match contact count ({n}).");
                }
            }

            // Every position must have exactly ndim coordinates.
            if (contact_positions.Any(x => x.Length != (int)ndim))
                throw new ArgumentException(
                    $"Every contact_positions entry must have exactly {(int)ndim} elements to match ndim.");

            // Per the schema, each contact_plane_axes entry is exactly 2 axis vectors, each with length
            // matching ndim.
            if (contact_plane_axes != null)
            {
                foreach (var axes in contact_plane_axes)
                {
                    if (axes == null)
                        throw new ArgumentException("contact_plane_axes entries cannot be null.");
                    if (axes.Length != 2)
                        throw new ArgumentException(
                            $"Every contact_plane_axes entry must contain exactly 2 axis vectors, but found {axes.Length}.");
                    if (axes.Any(row => row.Length != (int)ndim))
                        throw new ArgumentException(
                            $"Every contact_plane_axes axis vector must have exactly {(int)ndim} elements to match ndim.");
                }
            }

            // Planar contour points must also match ndim.
            if (probe_planar_contour != null && probe_planar_contour.Any(row => row.Length != (int)ndim))
                throw new ArgumentException(
                    $"Every probe_planar_contour entry must have exactly {(int)ndim} elements to match ndim.");

            // Each contact's shape parameters must be consistent with its shape: circles need a radius,
            // rects need both width and height, squares need a width.
            for (int i = 0; i < n; i++)
            {
                var shape = contact_shapes[i];
                var shapeParams = contact_shape_params[i];
                switch (shape)
                {
                    case ContactShape.Circle:
                        if (!shapeParams.Radius.HasValue)
                            throw new ArgumentException(
                                $"contact_shape_params[{i}] must specify radius for a circle contact.");
                        break;
                    case ContactShape.Rect:
                        if (!shapeParams.Width.HasValue || !shapeParams.Height.HasValue)
                            throw new ArgumentException(
                                $"contact_shape_params[{i}] must specify both width and height for a rect contact.");
                        break;
                    case ContactShape.Square:
                        if (!shapeParams.Width.HasValue)
                            throw new ArgumentException(
                                $"contact_shape_params[{i}] must specify width for a square contact.");
                        break;
                }
            }

            NumDimensions = ndim;
            SiUnits = si_units;
            Annotations = annotations;
            ProbePlanarContour = probe_planar_contour;
            annotationStore.Data = contact_annotations;

            // Convert the parallel array to a channel to contact dictionary, skipping -1 (not connected) entries.
            // Duplicate channels are detected here because the dict would silently overwrite them otherwise.
            if (device_channel_indices != null)
            {
                var map = new Dictionary<int, int>();
                int nonNegative = 0;
                for (int i = 0; i < device_channel_indices.Length; i++)
                {
                    if (device_channel_indices[i] != -1)
                    {
                        map[device_channel_indices[i]] = i;
                        nonNegative++;
                    }
                }
                if (map.Count < nonNegative)
                    throw new InvalidOperationException("device_channel_indices contains duplicate channel values.");
                ChannelMap = map.Count > 0 ? map : null;
            }

            Contacts = BuildContacts(n, contact_positions, contact_plane_axes, contact_shapes,
                contact_shape_params, contact_ids, shank_ids, contact_sides, annotationStore);
        }

        /// <summary>
        /// Constructs the <see cref="Contacts"/> array from the probeinterface parallel arrays.
        /// All contacts receive a reference to the same <paramref name="store"/> so mutations made
        /// through any contact are immediately visible probe-wide.
        /// </summary>
        private static Contact[] BuildContacts(
            int n, double[][] positions, double[][]?[]? planeAxes,
            ContactShape[] shapes, ContactShapeParam[] shapeParams,
            string?[]? contactIds, string?[]? shankIds,
            string?[]? contactSides, ContactAnnotationStore store)
        {
            var contacts = new Contact[n];
            for (int i = 0; i < n; i++)
            {
                double? posZ = positions[i].Length >= 3 ? positions[i][2] : (double?)null;
                contacts[i] = new Contact(
                    posX: positions[i][0],
                    posY: positions[i][1],
                    posZ: posZ,
                    shape: shapes[i],
                    shapeParams: shapeParams[i],
                    contactId: contactIds?[i],
                    shankId: shankIds?[i],
                    planeAxes: planeAxes?[i],
                    side: contactSides?[i],
                    index: i,
                    totalContacts: n,
                    store: store);
            }
            return contacts;
        }

        /// <summary>
        /// Deep copy constructor. Produces a fully independent probe with its own channel map, contact
        /// annotation store, and contact objects.
        /// </summary>
        internal Probe(Probe source)
        {
            NumDimensions = source.NumDimensions;
            SiUnits = source.SiUnits;
            Annotations = new ProbeAnnotations(source.Annotations);
            ProbePlanarContour = source.ProbePlanarContour?.Select(row => (double[])row.Clone()).ToArray();

            ChannelMap = source.ChannelMap?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var newStore = new ContactAnnotationStore();
            if (source.annotationStore.Data != null)
                newStore.Data = source.annotationStore.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object[])kvp.Value.Clone());
            annotationStore = newStore;

            int n = source.NumberOfContacts;
            var positions = source.Contacts.Select(c =>
                c.PosZ.HasValue
                    ? new double[] { c.PosX, c.PosY, c.PosZ.Value }
                    : new double[] { c.PosX, c.PosY }).ToArray();
            var planeAxes = source.Contacts.All(c => c.PlaneAxes == null) ? null
                : source.Contacts.Select(c => c.PlaneAxes?.Select(row => (double[])row.Clone()).ToArray()).ToArray();
            var shapes = source.Contacts.Select(c => c.Shape).ToArray();
            var shapeParams = source.Contacts.Select(c => c.ShapeParams).ToArray();
            var contactIds = source.Contacts.All(c => c.ContactId == null) ? null
                : source.Contacts.Select(c => c.ContactId).ToArray();
            var shankIds = source.Contacts.All(c => c.ShankId == null) ? null
                : source.Contacts.Select(c => c.ShankId).ToArray();
            var contactSides = source.Contacts.All(c => c.Side == null) ? null
                : source.Contacts.Select(c => c.Side).ToArray();

            Contacts = BuildContacts(n, positions, planeAxes, shapes, shapeParams,
                contactIds, shankIds, contactSides, newStore);
        }

        /// <summary>
        /// Gets the channel mapping for this probe as a dictionary mapping hardware channel to contact index,
        /// or null if no channels are assigned. Internal: public access is through the containing <see
        /// cref="ProbeGroup"/> or <see cref="SingleProbeGroup"/>. Managed exclusively via <see
        /// cref="ChannelWiring"/>.
        /// </summary>
        [JsonIgnore]
        internal IReadOnlyDictionary<int, int>? ChannelMap { get; set; }

        /// <summary>
        /// Gets the hardware channel assigned to the contact at <paramref name="contactIndex"/>.
        /// </summary>
        /// <param name="contactIndex">Zero-based index of the contact within this probe.</param>
        /// <param name="channel">
        /// When this method returns true, contains the hardware channel assigned to the contact. When this
        /// method returns false, contains -1.
        /// </param>
        /// <returns>True if the contact is assigned to a channel; otherwise false.</returns>
        internal bool TryGetMappedChannel(int contactIndex, out int channel)
        {
            if (ChannelMap != null)
            {
                foreach (var kvp in ChannelMap)
                {
                    if (kvp.Value == contactIndex)
                    {
                        channel = kvp.Key;
                        return true;
                    }
                }
            }
            channel = -1;
            return false;
        }

        /// <summary>
        /// Returns all per-contact values for the given annotation key as an array of
        /// <typeparamref name="T"/>, or null if the key is absent from the probe entirely. Contacts that have
        /// no value for the key yield the default of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert each stored value to.</typeparam>
        /// <param name="key">The annotation key to retrieve.</param>
        /// <returns>
        /// An array of length <see cref="NumberOfContacts"/> containing each contact's value, or null if the
        /// key is absent from this probe.
        /// </returns>
        public T[]? GetContactAnnotation<T>(string key)
        {
            if (annotationStore.Data == null || !annotationStore.Data.TryGetValue(key, out var values) || values == null)
                return null;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return Array.ConvertAll(values, v => v == null ? default! : (T)Convert.ChangeType(v, targetType));
        }

        /// <summary>
        /// Adds or replaces a per-contact annotation for the given key. <paramref name="values"/> must
        /// contain one element per contact. Null elements are permitted for contacts without a value.
        /// Per-contact <see cref="Contact.GetAnnotation{T}"/> reflects the update immediately.
        /// </summary>
        /// <typeparam name="T">The type of the annotation values.</typeparam>
        /// <param name="key">The annotation key to set.</param>
        /// <param name="values">An array of length <see cref="NumberOfContacts"/> with one value per
        /// contact.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="values"/>.Length does not equal <see cref="NumberOfContacts"/>.
        /// </exception>
        public void SetContactAnnotation<T>(string key, T[] values)
        {
            if (values.Length != NumberOfContacts)
                throw new ArgumentException(
                    $"Annotation array length ({values.Length}) must match the number of contacts ({NumberOfContacts}).",
                    nameof(values));

            annotationStore.Data ??= new Dictionary<string, object[]>();
            annotationStore.Data[key] = Array.ConvertAll(values, v => (object)v!);
        }

        /// <summary>
        /// Removes the per-contact annotation with the given key entirely.
        /// </summary>
        /// <param name="key">The annotation key to remove.</param>
        /// <returns>True if the key was found and removed; false if it was absent.</returns>
        public bool RemoveContactAnnotation(string key) =>
            annotationStore.Data != null && annotationStore.Data.Remove(key);
    }
}
