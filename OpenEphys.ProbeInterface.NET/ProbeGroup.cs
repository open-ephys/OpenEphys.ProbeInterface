using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Implements the probeinterface specification in C# for .NET.
    /// </summary>
    public class ProbeGroup
    {
        private static readonly Regex VersionPattern = new(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

        /// <summary>
        /// The probeinterface specification version implemented by this library.
        /// </summary>
        public static readonly Version SupportedSpecVersion = new(0, 3, 2);

        /// <summary>
        /// Gets the specification identifier. Must be "probeinterface".
        /// </summary>
        [JsonProperty("specification", Required = Required.Always)]
        public string Specification { get; }

        /// <summary>
        /// Gets the probeinterface version string (major.minor.patch).
        /// </summary>
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; }

        /// <summary>
        /// Gets the probes in this group.
        /// </summary>
        [XmlIgnore]
        [JsonProperty("probes", Required = Required.Always)]
        public IEnumerable<Probe> Probes { get; }

        /// <summary>
        /// Gets the number of probes in this group.
        /// </summary>
        [JsonIgnore]
        public int NumberOfProbes => Probes.Count();

        /// <summary>
        /// Gets the total number of contacts across all probes.
        /// </summary>
        [JsonIgnore]
        public int NumberOfContacts => Probes.Sum(p => p.NumberOfContacts);

        /// <summary>
        /// Initializes a <see cref="ProbeGroup"/> and immediately validates it. Used by Newtonsoft.Json
        /// during deserialization.
        /// </summary>
        /// <param name="specification">Must be "probeinterface".</param>
        /// <param name="version">Semver string (major.minor.patch).</param>
        /// <param name="probes">One or more probes.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="specification"/> is not "probeinterface", <paramref name="version"/>
        /// is malformed or incompatible with this library, <paramref name="probes"/> is null or empty, or
        /// channel indices are not unique across probes.
        /// </exception>
        [JsonConstructor]
        protected ProbeGroup(string specification, string version, IEnumerable<Probe> probes)
        {
            Specification = specification;
            Version = version;
            Probes = probes;
            Validate();
        }

        /// <summary>
        /// Deep copy constructor. Produces a fully independent instance: each probe, its channel map, and its
        /// contact annotations are cloned.
        /// </summary>
        /// <param name="probeGroup">The source group to copy from.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when channel indices are not unique across probes (same conditions as the primary
        /// constructor).
        /// </exception>
        protected ProbeGroup(ProbeGroup probeGroup)
        {
            Specification = probeGroup.Specification;
            Version = probeGroup.Version;
            Probes = probeGroup.Probes.Select(p => new Probe(p)).ToArray();
            Validate();
        }

        /// <summary>
        /// Validates the group against the probeinterface specification. Throws <see
        /// cref="InvalidOperationException"/> if the specification string, version format, probe count, or
        /// channel index uniqueness are invalid. Per-contact array length consistency is validated by <see
        /// cref="Probe"/>'s constructor.
        /// </summary>
        private void Validate()
        {
            if (Specification != "probeinterface")
                throw new InvalidOperationException(
                    $"Specification must be \"probeinterface\" but was \"{Specification}\".");

            if (string.IsNullOrEmpty(Version) || !VersionPattern.IsMatch(Version))
                throw new InvalidOperationException(
                    $"Version \"{Version}\" does not match the required pattern major.minor.patch (e.g. \"0.3.2\").");

            var v = new Version(Version);
            if (v.Major != SupportedSpecVersion.Major || v.Minor != SupportedSpecVersion.Minor)
                throw new InvalidOperationException(
                    $"Version \"{Version}\" is not compatible with this library, which implements " +
                    $"probeinterface {SupportedSpecVersion.Major}.{SupportedSpecVersion.Minor}.x.");

            if (Probes == null || !Probes.Any())
                throw new InvalidOperationException("At least one probe must be defined.");

            if (!ValidateDeviceChannelIndices())
                throw new InvalidOperationException("Device channel indices are not unique across all probes.");
        }

        /// <summary>
        /// Returns true if all assigned channel indices are unique across all probes. Probes with no channel
        /// mapping assigned are excluded from the check. Called by <see cref="Validate"/> on construction and
        /// by <see cref="ChannelWiring"/> after mutations.
        /// </summary>
        internal bool ValidateDeviceChannelIndices()
        {
            var active = Probes
                .Where(p => p.ChannelMap != null)
                .SelectMany(p => p.ChannelMap!.Keys)
                .ToList();
            return active.Count == active.Distinct().Count();
        }

        /// <summary>
        /// Gets the channel mapping across all probes as a read-only dictionary mapping hardware channel to
        /// (probe index, contact index within that probe), or null if no channels have been assigned
        /// anywhere. Use <see cref="Probes"/> and <see cref="Probe.Contacts"/> to look up the <see
        /// cref="Contact"/> for a given entry.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<int, (int ProbeIndex, int ContactIndex)>? ChannelMap
        {
            get
            {
                var result = new Dictionary<int, (int ProbeIndex, int ContactIndex)>();
                int probeIndex = 0;
                foreach (var probe in Probes)
                {
                    var perProbe = probe.ChannelMap;
                    if (perProbe != null)
                    {
                        foreach (var kvp in perProbe)
                            result[kvp.Key] = (probeIndex, kvp.Value);
                    }
                    probeIndex++;
                }
                return result.Count > 0 ? result : null;
            }
        }

        /// <summary>
        /// Gets the hardware channel assigned to the contact at <paramref name="contactIndex"/> on the probe
        /// at <paramref name="probeIndex"/>.
        /// </summary>
        /// <param name="probeIndex">Zero-based index of the probe within this group.</param>
        /// <param name="contactIndex">Zero-based index of the contact within that probe.</param>
        /// <param name="channel">
        /// When this method returns true, contains the hardware channel assigned to the contact. When this
        /// method returns false, contains -1.
        /// </param>
        /// <returns>True if the contact is assigned to a channel; otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="probeIndex"/> is outside the range of <see cref="Probes"/>.
        /// </exception>
        public bool TryGetMappedChannel(int probeIndex, int contactIndex, out int channel) =>
            Probes.ElementAt(probeIndex).TryGetMappedChannel(contactIndex, out channel);
    }
}
