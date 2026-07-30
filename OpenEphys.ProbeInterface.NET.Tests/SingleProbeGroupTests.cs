using System;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    public class SingleProbeGroupTests
    {
        // Minimal concrete subclass for testing.
        private class TestSingleProbeGroup : SingleProbeGroup
        {
            [JsonConstructor]
            public TestSingleProbeGroup(string specification, string version, Probe[] probes)
                : base(specification, version, probes) { }

            public TestSingleProbeGroup(ProbeGroup probeGroup)
                : base(probeGroup) { }
        }

        private static TestSingleProbeGroup Deserialize(string json) =>
            JsonConvert.DeserializeObject<TestSingleProbeGroup>(json)
                ?? throw new JsonException("Deserialization returned null.");

        private static string MakeJson(string? deviceChannelIndices = null, string? contactIds = null) =>
            $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
                  "ndim": 2, "si_units": "um",
                  "annotations": { "model_name": "T", "manufacturer": "T" },
                  "contact_positions": [[0.0, 0.0], [0.0, 20.0]],
                  "contact_shapes": ["circle", "circle"],
                  "contact_shape_params": [{"radius": 5.0}, {"radius": 5.0}]
                  {{(contactIds != null ? $", \"contact_ids\": {contactIds}" : "")}}
                  {{(deviceChannelIndices != null ? $", \"device_channel_indices\": {deviceChannelIndices}" : "")}}
                }
              ]
            }
            """;

        private static string MakeTwoProbeJson() =>
            $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
                  "ndim": 2, "si_units": "um",
                  "annotations": { "model_name": "T", "manufacturer": "T" },
                  "contact_positions": [[0.0, 0.0]],
                  "contact_shapes": ["circle"],
                  "contact_shape_params": [{"radius": 5.0}]
                },
                {
                  "ndim": 2, "si_units": "um",
                  "annotations": { "model_name": "T", "manufacturer": "T" },
                  "contact_positions": [[0.0, 0.0]],
                  "contact_shapes": ["circle"],
                  "contact_shape_params": [{"radius": 5.0}]
                }
              ]
            }
            """;

        [Fact]
        public void MultipleProbes_Throws()
        {
            Assert.Throws<ArgumentException>(() => Deserialize(MakeTwoProbeJson()));
        }

        [Fact]
        public void Probe_ReturnsSingleProbe()
        {
            var group = Deserialize(MakeJson());
            Assert.Same(group.Probes.First(), group.Probe);
        }

        [Fact]
        public void ChannelMap_NoChannelsAssigned_IsNull()
        {
            var group = Deserialize(MakeJson());
            Assert.Null(group.ChannelMap);
        }

        [Fact]
        public void ChannelMap_ReturnsChannelToContactIndex()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            var map = group.ChannelMap;
            Assert.NotNull(map);
            Assert.Equal(0, map![3]);
            Assert.Equal(1, map[7]);
        }

        [Fact]
        public void TryGetChannel_MappedContact_ReturnsTrue()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            Assert.True(group.TryGetMappedChannel(0, out int ch));
            Assert.Equal(3, ch);
        }

        [Fact]
        public void TryGetChannel_UnmappedContact_ReturnsFalse()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[-1, 7]"));
            Assert.False(group.TryGetMappedChannel(0, out int ch));
            Assert.Equal(-1, ch);
        }

        [Fact]
        public void TryGetChannel_NoMap_ReturnsFalse()
        {
            var group = Deserialize(MakeJson());
            Assert.False(group.TryGetMappedChannel(0, out int ch));
            Assert.Equal(-1, ch);
        }

        [Fact]
        public void ChannelMap_AfterWiring_Reflects()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannel(group, 0, 1, 99);
            Assert.Equal(1, group.ChannelMap![99]);
        }

        [Fact]
        public void CopyConstructor_PreservesProbeAndMap()
        {
            var source = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            var copy = new TestSingleProbeGroup(source);
            Assert.Equal(source.ChannelMap!.Keys.ToList(), copy.ChannelMap!.Keys.ToList());
        }
    }
}
