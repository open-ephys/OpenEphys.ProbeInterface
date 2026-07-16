using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Static helper methods for wiring hardware channels to contacts in a <see cref="ProbeGroup"/>.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="ProbeGroup"/> because not all wiring operations are valid for
    /// every hardware type. For example, Neuropixels 2.0 always maps all 384 channels to some set
    /// of electrodes, so unwiring operations do not apply. Calling these methods directly makes the caller's
    /// intent explicit and keeps the <see cref="ProbeGroup"/> type hierarchy free of operations that
    /// would need to be suppressed in certain subclasses.
    /// </remarks>
    public static class ChannelWiring
    {
        /// <summary>
        /// Incrementally assigns hardware channels to contacts on the specified probe.
        /// </summary>
        /// <remarks>
        /// The update is incremental: contacts not in <paramref name="assignments"/> keep their
        /// current channel. If the probe has no existing mapping, unspecified contacts start
        /// unconnected.
        /// <para>
        /// If a channel in <paramref name="assignments"/> is already held by a different contact
        /// on the same probe, then that contact loses its mapping.
        /// </para>
        /// </remarks>
        /// <param name="group">The probe group to update.</param>
        /// <param name="probeIndex">Zero-based index of the probe to update.</param>
        /// <param name="assignments">Contact index → channel index. Values must be &gt;= 0 and unique within the call.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when a key is out of range, any value is negative, values within the call are
        /// not unique, or the result would duplicate a channel already assigned on another probe.
        /// </exception>
        public static void WireChannels(ProbeGroup group, int probeIndex, IDictionary<int, int> assignments)
        {
            var probe = group.Probes.ElementAt(probeIndex);
            int n = probe.NumberOfContacts;

            foreach (var kvp in assignments)
            {
                if (kvp.Key < 0 || kvp.Key >= n)
                    throw new ArgumentException(
                        $"Contact index {kvp.Key} is out of range [0, {n}).", nameof(assignments));
                if (kvp.Value < 0)
                    throw new ArgumentException(
                        $"Channel value {kvp.Value} for contact {kvp.Key} must be >= 0.", nameof(assignments));
            }

            var incomingChannels = assignments.Values.ToList();
            if (incomingChannels.Count != incomingChannels.Distinct().Count())
                throw new ArgumentException(
                    "Channel values within a single assignment call must be unique.", nameof(assignments));

            var previousMap = probe.ChannelMap?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var newMap = probe.ChannelMap != null
                ? probe.ChannelMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                : new Dictionary<int, int>();

            foreach (var kvp in assignments)
            {
                // Displace any other contact that currently holds this channel.
                var displaced = newMap
                    .Where(e => e.Value == kvp.Value && e.Key != kvp.Key)
                    .Select(e => e.Key)
                    .ToArray();
                foreach (var k in displaced)
                    newMap.Remove(k);

                newMap[kvp.Key] = kvp.Value;
            }

            probe.SetChannelMap(newMap.Count > 0 ? newMap : null);

            if (!group.ValidateDeviceChannelIndices())
            {
                probe.SetChannelMap(previousMap);
                throw new ArgumentException(
                    "Channel indices must be unique across all probes in the group.", nameof(assignments));
            }
        }

        /// <summary>
        /// Assigns a single hardware channel to a contact on the specified probe.
        /// Displaces any other contact on the same probe that currently holds <paramref name="channel"/>.
        /// </summary>
        /// <param name="group">The probe group to update.</param>
        /// <param name="probeIndex">Zero-based index of the probe to update.</param>
        /// <param name="contactIndex">Zero-based index of the contact within the probe.</param>
        /// <param name="channel">Hardware channel to assign. Must be &gt;= 0.</param>
        public static void WireChannel(ProbeGroup group, int probeIndex, int contactIndex, int channel) =>
            WireChannels(group, probeIndex, new Dictionary<int, int> { { contactIndex, channel } });

        /// <summary>Removes all channel mappings across every probe in the group.</summary>
        /// <param name="group">The probe group to clear.</param>
        public static void UnwireChannels(ProbeGroup group)
        {
            foreach (var probe in group.Probes)
                probe.SetChannelMap(null);
        }

        /// <summary>Removes all channel mappings on the specified probe.</summary>
        /// <param name="group">The probe group to update.</param>
        /// <param name="probeIndex">Zero-based index of the probe to clear.</param>
        public static void UnwireChannels(ProbeGroup group, int probeIndex) =>
            group.Probes.ElementAt(probeIndex).SetChannelMap(null);

        /// <summary>
        /// Removes the channel mapping for a set of contacts on the specified probe.
        /// Contacts with no existing mapping are silently skipped.
        /// </summary>
        /// <param name="group">The probe group to update.</param>
        /// <param name="probeIndex">Zero-based index of the probe to update.</param>
        /// <param name="contactIndices">Contact indices whose mappings should be removed.</param>
        public static void UnwireChannels(ProbeGroup group, int probeIndex, IEnumerable<int> contactIndices)
        {
            var probe = group.Probes.ElementAt(probeIndex);
            if (probe.ChannelMap == null) return;

            var map = probe.ChannelMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var ci in contactIndices)
                map.Remove(ci);
            probe.SetChannelMap(map.Count > 0 ? map : null);
        }

        /// <summary>
        /// Removes the channel mapping for a single contact on the specified probe.
        /// Has no effect if the contact has no mapping.
        /// </summary>
        /// <param name="group">The probe group to update.</param>
        /// <param name="probeIndex">Zero-based index of the probe to update.</param>
        /// <param name="contactIndex">Zero-based index of the contact to unwire.</param>
        public static void UnwireChannel(ProbeGroup group, int probeIndex, int contactIndex)
        {
            var probe = group.Probes.ElementAt(probeIndex);
            if (probe.ChannelMap == null) return;

            var map = probe.ChannelMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            map.Remove(contactIndex);
            probe.SetChannelMap(map.Count > 0 ? map : null);
        }
    }
}
