using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Base class for probe groups that always contain exactly one probe. Enforces the single-probe invariant
    /// and provides <see cref="Probe"/>, <see cref="ChannelMap"/>, and <see cref="TryGetMappedChannel"/>
    /// without requiring a probe-index argument.
    /// </summary>
    public abstract class SingleProbeGroup : ProbeGroup
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SingleProbeGroup"/> from deserialized
        /// probeinterface data. Throws <see cref="ArgumentException"/> if <paramref name="probes"/>
        /// does not contain exactly one probe.
        /// </summary>
        protected SingleProbeGroup(string specification, string version, IEnumerable<Probe> probes)
            : base(specification, version, probes)
        {
            if (Probes.Count() != 1)
                throw new ArgumentException(
                    $"A {GetType().Name} must contain exactly one probe, but {Probes.Count()} were provided.");
        }

        /// <summary>
        /// Copy constructor. Throws <see cref="ArgumentException"/> if <paramref name="probeGroup"/>
        /// does not contain exactly one probe.
        /// </summary>
        protected SingleProbeGroup(ProbeGroup probeGroup)
            : base(probeGroup)
        {
            if (Probes.Count() != 1)
                throw new ArgumentException(
                    $"A {GetType().Name} must contain exactly one probe, but {Probes.Count()} were provided.");
        }

        /// <summary>
        /// Gets the single probe in this group.
        /// </summary>
        [JsonIgnore]
        public Probe Probe => Probes.First();

        /// <summary>
        /// Gets the channel mapping for the probe as a read-only dictionary mapping hardware channel
        /// to contact index, or null if no channels are assigned.
        /// </summary>
        [JsonIgnore]
        public new IReadOnlyDictionary<int, int>? ChannelMap => Probe.ChannelMap;

        /// <summary>
        /// Gets the hardware channel assigned to the contact at <paramref name="contactIndex"/> within the
        /// the single probe in this group.
        /// </summary>
        /// <param name="contactIndex">Zero-based index of the contact within the probe.</param>
        /// <param name="channel">
        /// When this method returns true, contains the assigned hardware channel. When this method returns
        /// false, contains -1.
        /// </param>
        /// <returns>True if the contact is assigned to a channel; otherwise false.</returns>
        public bool TryGetMappedChannel(int contactIndex, out int channel) =>
            Probe.TryGetMappedChannel(contactIndex, out channel);
    }
}
