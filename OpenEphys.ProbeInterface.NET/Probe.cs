using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Represents a single probe in a <see cref="ProbeGroup"/>.
    /// The primary public API is <see cref="Contacts"/>, which exposes all per-contact data as a
    /// strongly-typed collection. Channel mapping is stored in <see cref="ChannelMap"/> and managed
    /// exclusively via <see cref="ChannelWiring"/>. JSON serialization/deserialization preserves the
    /// probeinterface parallel-array format transparently.
    /// </summary>
    public class Probe
    {
        /// <summary>Gets the number of spatial dimensions (2 or 3).</summary>
        [XmlIgnore]
        [JsonProperty("ndim", Required = Required.Always)]
        public ProbeNdim NumDimensions { get; }

        /// <summary>Gets the SI unit used for contact positions.</summary>
        [XmlIgnore]
        [JsonProperty("si_units", Required = Required.Always)]
        public ProbeSiUnits SiUnits { get; }

        /// <summary>Gets the probe-level annotations (model name, manufacturer).</summary>
        [XmlIgnore]
        [JsonProperty("annotations", Required = Required.Always)]
        public ProbeAnnotations Annotations { get; }

        /// <summary>Gets the planar contour describing the physical outline of the probe, or null.</summary>
        [XmlIgnore]
        [JsonProperty("probe_planar_contour", NullValueHandling = NullValueHandling.Ignore)]
        public double[][]? ProbePlanarContour { get; }

        /// <summary>
        /// Gets the contacts on this probe. Each <see cref="Contact"/> carries all per-contact data:
        /// position, shape, shank, plane axes, side, and annotations.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<Contact> Contacts { get; }

        /// <summary>Gets the number of contacts on this probe.</summary>
        [JsonIgnore]
        public int NumberOfContacts => Contacts.Count;

        /// <summary>Gets the contact annotation keys defined for this probe.</summary>
        [JsonIgnore]
        public IEnumerable<string> ContactAnnotationKeys =>
            annotationStore.Data?.Keys ?? Enumerable.Empty<string>();

        private Dictionary<int, int>? channelMap;

        /// <summary>
        /// Gets the channel mapping for this probe as a read-only dictionary mapping contact index
        /// to hardware channel, or null if no mapping has been assigned. Contacts absent from the
        /// dictionary are not connected. Managed exclusively via <see cref="ProbeGroup"/>.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<int, int>? ChannelMap => channelMap;

        /// <summary>Replaces the channel map. Called by <see cref="ProbeGroup"/> to keep mapping consistent across all probes.</summary>
        internal void SetChannelMap(Dictionary<int, int>? map) => channelMap = map;

        private readonly ContactAnnotationStore annotationStore = new ContactAnnotationStore();

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
                if (channelMap == null) return null;
                var arr = new int[NumberOfContacts];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = channelMap.TryGetValue(i, out var ch) ? ch : -1;
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
        /// builds the <see cref="Contacts"/> collection. Throws <see cref="ArgumentException"/> if any
        /// parallel arrays have inconsistent lengths.
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
            int n = contact_positions.Length;

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

            NumDimensions = ndim;
            SiUnits = si_units;
            Annotations = annotations;
            ProbePlanarContour = probe_planar_contour;
            annotationStore.Data = contact_annotations;

            // Convert the parallel array to a dictionary, skipping -1 (not connected) entries.
            if (device_channel_indices != null)
            {
                var map = new Dictionary<int, int>();
                for (int i = 0; i < device_channel_indices.Length; i++)
                {
                    if (device_channel_indices[i] != -1)
                        map[i] = device_channel_indices[i];
                }
                channelMap = map.Count > 0 ? map : null;
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
            int n, double[][] positions, double[][][]? planeAxes,
            ContactShape[] shapes, ContactShapeParam[] shapeParams,
            string[]? contactIds, string[]? shankIds,
            string[]? contactSides, ContactAnnotationStore store)
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
        /// Returns a dictionary mapping each assigned hardware channel to a tuple of
        /// (contact index within this probe, <see cref="Contact"/>), or null if no channels
        /// have been assigned on this probe.
        /// </summary>
        public IReadOnlyDictionary<int, (int ContactIndex, Contact Contact)>? GetChannelMap()
        {
            if (channelMap == null) return null;
            var result = new Dictionary<int, (int ContactIndex, Contact Contact)>();
            foreach (var kvp in channelMap)
                result[kvp.Value] = (kvp.Key, Contacts[kvp.Key]);
            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// Returns all per-contact values for the given annotation key as an array of
        /// <typeparamref name="T"/>, or null if the key is absent from the probe entirely.
        /// Contacts that have no value for the key yield the default of <typeparamref name="T"/>.
        /// </summary>
        public T[]? GetContactAnnotation<T>(string key)
        {
            if (annotationStore.Data == null || !annotationStore.Data.TryGetValue(key, out var values) || values == null)
                return null;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return Array.ConvertAll(values, v => v == null ? default! : (T)Convert.ChangeType(v, targetType));
        }

        /// <summary>
        /// Adds or replaces a per-contact annotation for the given key. <paramref name="values"/>
        /// must contain one element per contact. Null elements are permitted for contacts without
        /// a value. Per-contact <see cref="Contact.GetAnnotation{T}"/> reflects the update
        /// immediately.
        /// </summary>
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
        /// Returns true if the key was found and removed.
        /// </summary>
        public bool RemoveContactAnnotation(string key) =>
            annotationStore.Data != null && annotationStore.Data.Remove(key);
    }
}
