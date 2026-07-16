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

        /// <summary>The probeinterface specification version implemented by this library.</summary>
        public static readonly Version SupportedSpecVersion = new Version(0, 3, 2);

        /// <summary>Gets the specification identifier. Must be "probeinterface".</summary>
        [JsonProperty("specification", Required = Required.Always)]
        public string Specification { get; }

        /// <summary>Gets the probeinterface version string (major.minor.patch).</summary>
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; }

        /// <summary>
        /// Gets the probes in this group. Use <see cref="Probe.Contacts"/> on each probe for
        /// per-contact data and <see cref="Probe.ChannelMap"/> for the channel mapping.
        /// </summary>
        [XmlIgnore]
        [JsonProperty("probes", Required = Required.Always)]
        public IEnumerable<Probe> Probes { get; }

        /// <summary>Gets the total number of contacts across all probes.</summary>
        [JsonIgnore]
        public int NumberOfContacts => Probes.Sum(p => p.NumberOfContacts);

        /// <summary>
        /// Initializes a <see cref="ProbeGroup"/> and immediately validates it.
        /// Used by Newtonsoft.Json during deserialization.
        /// </summary>
        /// <param name="specification">Must be "probeinterface".</param>
        /// <param name="version">Semver string (major.minor.patch).</param>
        /// <param name="probes">One or more probes.</param>
        [JsonConstructor]
        protected ProbeGroup(string specification, string version, IEnumerable<Probe> probes)
        {
            Specification = specification;
            Version = version;
            Probes = probes;
            Validate();
        }

        /// <summary>Protected copy constructor for subclasses.</summary>
        protected ProbeGroup(ProbeGroup probeGroup)
        {
            Specification = probeGroup.Specification;
            Version = probeGroup.Version;
            Probes = probeGroup.Probes;
            Validate();
        }

        /// <summary>
        /// Validates the group against the probeinterface specification. Throws
        /// <see cref="InvalidOperationException"/> if the specification string, version format, probe
        /// count, or channel index uniqueness are invalid.
        /// Per-contact array length consistency is validated by <see cref="Probe"/>'s constructor.
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
        /// Returns true if all assigned channel indices are unique across all probes.
        /// Probes with no channel mapping assigned are excluded from the check.
        /// Called by <see cref="Validate"/> on construction and by <see cref="ChannelWiring"/> after mutations.
        /// </summary>
        internal bool ValidateDeviceChannelIndices()
        {
            var active = Probes
                .Where(p => p.ChannelMap != null)
                .SelectMany(p => p.ChannelMap!.Values)
                .ToList();
            return active.Count == active.Distinct().Count();
        }

        /// <summary>
        /// Returns a dictionary mapping each assigned hardware channel to a tuple of
        /// (probe index, contact index within that probe, <see cref="Contact"/>), across all probes
        /// in the group, or null if no channels have been assigned anywhere.
        /// </summary>
        public IReadOnlyDictionary<int, (int ProbeIndex, int ContactIndex, Contact Contact)>? GetChannelMap()
        {
            var result = new Dictionary<int, (int ProbeIndex, int ContactIndex, Contact Contact)>();
            int probeIndex = 0;
            foreach (var probe in Probes)
            {
                var perProbe = probe.GetChannelMap();
                if (perProbe != null)
                {
                    foreach (var kvp in perProbe)
                        result[kvp.Key] = (probeIndex, kvp.Value.ContactIndex, kvp.Value.Contact);
                }
                probeIndex++;
            }
            return result.Count > 0 ? result : null;
        }
    }
}
