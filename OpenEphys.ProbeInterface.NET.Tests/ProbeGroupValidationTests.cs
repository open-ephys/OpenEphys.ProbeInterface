using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    public class ProbeGroupValidationTests
    {
        private static ProbeGroup Deserialize(string json) =>
            JsonConvert.DeserializeObject<ProbeGroup>(json)
                ?? throw new JsonException("Deserialization returned null.");

        private static string MakeTwoProbeJson(
            string? probe0ChannelIndices = null, string? probe0ContactIds = null,
            string? probe1ChannelIndices = null, string? probe1ContactIds = null) =>
            $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
                  "ndim": 2, "si_units": "um",
                  "annotations": { "model_name": "P", "manufacturer": "M" },
                  "contact_positions": [[0.0, 0.0]],
                  "contact_shapes": ["circle"],
                  "contact_shape_params": [{"radius": 5.0}]
                  {{(probe0ContactIds != null ? $", \"contact_ids\": {probe0ContactIds}" : "")}}
                  {{(probe0ChannelIndices != null ? $", \"device_channel_indices\": {probe0ChannelIndices}" : "")}}
                },
                {
                  "ndim": 2, "si_units": "um",
                  "annotations": { "model_name": "P", "manufacturer": "M" },
                  "contact_positions": [[0.0, 0.0]],
                  "contact_shapes": ["circle"],
                  "contact_shape_params": [{"radius": 5.0}]
                  {{(probe1ContactIds != null ? $", \"contact_ids\": {probe1ContactIds}" : "")}}
                  {{(probe1ChannelIndices != null ? $", \"device_channel_indices\": {probe1ChannelIndices}" : "")}}
                }
              ]
            }
            """;

        private static string MakeJson(
            string specification = "probeinterface",
            string? version = null,
            string modelName = "TestProbe",
            string manufacturer = "TestMfg",
            string? contactIds = null,
            string? deviceChannelIndices = null) =>
            $$"""
            {
              "specification": "{{specification}}",
              "version": "{{version ?? ProbeGroup.SupportedSpecVersion.ToString()}}",
              "probes": [
                {
                  "ndim": 2,
                  "si_units": "um",
                  "annotations": { "model_name": "{{modelName}}", "manufacturer": "{{manufacturer}}" },
                  "contact_positions": [[0.0, 0.0], [0.0, 20.0]],
                  "contact_shapes": ["circle", "circle"],
                  "contact_shape_params": [{"radius": 5.0}, {"radius": 5.0}]
                  {{(contactIds != null ? $", \"contact_ids\": {contactIds}" : "")}}
                  {{(deviceChannelIndices != null ? $", \"device_channel_indices\": {deviceChannelIndices}" : "")}}
                }
              ]
            }
            """;

        [Fact]
        public void WrongSpecification_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Deserialize(MakeJson(specification: "other")));
            Assert.Contains("probeinterface", ex.Message);
        }

        [Theory]
        [InlineData("0.2")]
        [InlineData("1")]
        [InlineData("abc")]
        [InlineData("")]
        public void MalformedVersion_Throws(string version)
        {
            Assert.Throws<InvalidOperationException>(() =>
                Deserialize(MakeJson(version: version)));
        }

        public static TheoryData<string> CompatibleVersions => new TheoryData<string>
        {
            $"{ProbeGroup.SupportedSpecVersion.Major}.{ProbeGroup.SupportedSpecVersion.Minor}.0",
            ProbeGroup.SupportedSpecVersion.ToString(),
            $"{ProbeGroup.SupportedSpecVersion.Major}.{ProbeGroup.SupportedSpecVersion.Minor}.99",
        };

        public static TheoryData<string> IncompatibleVersions => new TheoryData<string>
        {
            $"{ProbeGroup.SupportedSpecVersion.Major}.{ProbeGroup.SupportedSpecVersion.Minor - 1}.0",
            $"{ProbeGroup.SupportedSpecVersion.Major}.{ProbeGroup.SupportedSpecVersion.Minor + 1}.0",
            $"{ProbeGroup.SupportedSpecVersion.Major + 1}.0.0",
        };

        [Theory]
        [MemberData(nameof(CompatibleVersions))]
        public void CompatibleMinorVersion_DoesNotThrow(string version)
        {
            var ex = Record.Exception(() => Deserialize(MakeJson(version: version)));
            Assert.Null(ex);
        }

        [Theory]
        [MemberData(nameof(IncompatibleVersions))]
        public void IncompatibleMinorVersion_Throws(string version)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Deserialize(MakeJson(version: version)));
            Assert.Contains($"0.{ProbeGroup.SupportedSpecVersion.Minor}.x", ex.Message);
        }

        [Fact]
        public void MissingContactPlaneAxes_DoesNotThrow()
        {
            var ex = Record.Exception(() => Deserialize(MakeJson()));
            Assert.Null(ex);
        }

        [Fact]
        public void NonNumericContactIds_ArePreserved()
        {
            var group = Deserialize(MakeJson(contactIds: "[\"e0\", \"e1\"]", deviceChannelIndices: "[0, 1]"));
            Assert.Equal("e0", group.Probes.First().Contacts[0].ContactId);
            Assert.Equal("e1", group.Probes.First().Contacts[1].ContactId);
        }

        [Fact]
        public void MissingDeviceChannelIndices_ChannelMapIsNull()
        {
            var group = Deserialize(MakeJson());
            Assert.Null(group.Probes.First().ChannelMap);
        }

        [Fact]
        public void DeviceChannelIndices_PopulateChannelMap()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            var map = group.Probes.First().ChannelMap;
            Assert.NotNull(map);
            Assert.Equal(3, map![0]);
            Assert.Equal(7, map[1]);
        }

        [Fact]
        public void AllMinusOne_ChannelMapIsNull()
        {
            // All -1 entries → no connected contacts → ChannelMap is null (not set)
            var group = Deserialize(MakeJson(deviceChannelIndices: "[-1, -1]"));
            Assert.Null(group.Probes.First().ChannelMap);
        }

        [Fact]
        public void DuplicateDeviceChannelIndices_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Deserialize(MakeJson(deviceChannelIndices: "[5, 5]")));
        }

        [Fact]
        public void WireChannels_FirstCall_AssignsSpecifiedContacts()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 0, 3 } });
            var map = group.Probes.First().ChannelMap;
            Assert.NotNull(map);
            Assert.Equal(3, map![0]);
            Assert.False(map.ContainsKey(1)); // contact 1 was not assigned
        }

        [Fact]
        public void WireChannels_SecondCall_IsIncremental()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 0, 3 } });
            ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 1, 7 } });
            var map = group.Probes.First().ChannelMap;
            Assert.Equal(3, map![0]); // still assigned from first call
            Assert.Equal(7, map[1]);  // added by second call
        }

        [Fact]
        public void WireChannels_ChannelConflict_DisplacesExistingContact()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 0, 5 } });
            // Assign channel 5 to contact 1 — should displace contact 0
            ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 1, 5 } });
            var map = group.Probes.First().ChannelMap;
            Assert.False(map!.ContainsKey(0)); // displaced
            Assert.Equal(5, map[1]);
        }

        [Fact]
        public void WireChannels_OutOfRangeContactIndex_Throws()
        {
            var group = Deserialize(MakeJson());
            Assert.Throws<ArgumentException>(() =>
                ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 5, 0 } }));
        }

        [Fact]
        public void WireChannels_NegativeChannelValue_Throws()
        {
            var group = Deserialize(MakeJson());
            Assert.Throws<ArgumentException>(() =>
                ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 0, -1 } }));
        }

        [Fact]
        public void WireChannels_DuplicateChannelWithinCall_Throws()
        {
            var group = Deserialize(MakeJson());
            Assert.Throws<ArgumentException>(() =>
                ChannelWiring.WireChannels(group,0, new Dictionary<int, int> { { 0, 5 }, { 1, 5 } }));
        }

        [Fact]
        public void WireChannels_CrossProbeConflict_ThrowsAndRollsBack()
        {
            var group = Deserialize(MakeTwoProbeJson(probe0ChannelIndices: "[10]"));
            // Probe 0 already has channel 10; try to assign 10 to probe 1
            Assert.Throws<ArgumentException>(() =>
                ChannelWiring.WireChannels(group,1, new Dictionary<int, int> { { 0, 10 } }));
            // Probe 1 map must be rolled back to null
            Assert.Null(group.Probes.ElementAt(1).ChannelMap);
        }

        [Fact]
        public void WireChannel_AssignsSingleContact()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannel(group,0, 1, 42);
            Assert.Equal(42, group.Probes.First().ChannelMap![1]);
        }

        [Fact]
        public void UnwireChannel_RemovesEntry()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            ChannelWiring.UnwireChannel(group,0, 0);
            var map = group.Probes.First().ChannelMap;
            Assert.NotNull(map);
            Assert.False(map!.ContainsKey(0));
            Assert.Equal(7, map[1]);
        }

        [Fact]
        public void UnwireChannel_LastEntry_SetsMapToNull()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, -1]"));
            ChannelWiring.UnwireChannel(group,0, 0); // only contact 0 had an entry
            Assert.Null(group.Probes.First().ChannelMap);
        }

        [Fact]
        public void UnwireChannel_MissingMap_DoesNotThrow()
        {
            var group = Deserialize(MakeJson());
            var ex = Record.Exception(() => ChannelWiring.UnwireChannel(group,0, 0));
            Assert.Null(ex);
        }

        [Fact]
        public void UnwireChannels_RemovesMultipleContacts()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            ChannelWiring.UnwireChannels(group,0, new[] { 0, 1 });
            Assert.Null(group.Probes.First().ChannelMap);
        }

        [Fact]
        public void UnwireChannels_Probe_ClearsAllOnThatProbe()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[3, 7]"));
            ChannelWiring.UnwireChannels(group,0);
            Assert.Null(group.Probes.First().ChannelMap);
        }

        [Fact]
        public void UnwireChannels_Probe_DoesNotThrowWhenAlreadyEmpty()
        {
            var group = Deserialize(MakeJson()); // no channel indices
            var ex = Record.Exception(() => ChannelWiring.UnwireChannels(group,0));
            Assert.Null(ex);
        }

        [Fact]
        public void UnwireChannels_All_ClearsEveryProbe()
        {
            var group = Deserialize(MakeTwoProbeJson(probe0ChannelIndices: "[10]", probe1ChannelIndices: "[20]"));
            ChannelWiring.UnwireChannels(group);
            Assert.Null(group.Probes.ElementAt(0).ChannelMap);
            Assert.Null(group.Probes.ElementAt(1).ChannelMap);
        }

        [Fact]
        public void GetChannelMap_NoChannelsAssigned_ReturnsNull()
        {
            var group = Deserialize(MakeJson());
            Assert.Null(group.GetChannelMap());
        }

        [Fact]
        public void GetChannelMap_ReturnsChannelToContactMapping()
        {
            var group = Deserialize(MakeJson(contactIds: "[\"e0\", \"e1\"]", deviceChannelIndices: "[3, 7]"));
            var map = group.GetChannelMap();
            Assert.NotNull(map);
            Assert.Equal("e0", map![3].Contact.ContactId);
            Assert.Equal("e1", map[7].Contact.ContactId);
        }

        [Fact]
        public void GetChannelMap_ContactPropertiesAreAccessible()
        {
            var group = Deserialize(MakeJson(deviceChannelIndices: "[0, 1]"));
            var map = group.GetChannelMap()!;
            Assert.Equal(0.0, map[0].Contact.PosX);
            Assert.Equal(0.0, map[0].Contact.PosY);
            Assert.Equal(0.0, map[1].Contact.PosX);
            Assert.Equal(20.0, map[1].Contact.PosY);
        }

        [Fact]
        public void GetChannelMap_ContactIndex_IsCorrect()
        {
            // contact_positions has 2 contacts; device_channel_indices assigns channel 99 to contact 1
            var group = Deserialize(MakeJson(deviceChannelIndices: "[-1, 99]"));
            var map = group.GetChannelMap()!;
            Assert.Equal(1, map[99].ContactIndex);
        }

        [Fact]
        public void GetChannelMap_MultiProbe_CombinesBothProbes()
        {
            var group = Deserialize(MakeTwoProbeJson(
                probe0ContactIds: "[\"a0\"]", probe0ChannelIndices: "[10]",
                probe1ContactIds: "[\"b0\"]", probe1ChannelIndices: "[20]"));
            var map = group.GetChannelMap()!;
            Assert.Equal(2, map.Count);
            Assert.Equal("a0", map[10].Contact.ContactId);
            Assert.Equal(0, map[10].ProbeIndex);
            Assert.Equal("b0", map[20].Contact.ContactId);
            Assert.Equal(1, map[20].ProbeIndex);
        }

        [Fact]
        public void GetChannelMap_AfterWireChannel_ReflectsUpdate()
        {
            var group = Deserialize(MakeJson());
            ChannelWiring.WireChannel(group,0, 0, 42);
            var map = group.GetChannelMap()!;
            Assert.Single(map);
            Assert.True(map.ContainsKey(42));
            Assert.Equal(0, map[42].ProbeIndex);
            Assert.Equal(0, map[42].ContactIndex);
        }
    }
}
