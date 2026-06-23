using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    public class ContactAnnotationsTests
    {
        private readonly string Json = $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
                  "ndim": 2,
                  "si_units": "um",
                  "annotations": { "model_name": "TestProbe", "manufacturer": "TestMfg" },
                  "contact_annotations": {
                    "brain_area": ["CA1", "CA1", "DG"],
                    "custom_label": ["a", "b", "c"],
                    "impedance": [125.3, 98.7, 110.1]
                  },
                  "contact_positions": [[0.0, 0.0], [0.0, 20.0], [16.0, 10.0]],
                  "contact_shapes": ["circle", "circle", "circle"],
                  "contact_shape_params": [{"radius": 5.0}, {"radius": 5.0}, {"radius": 5.0}],
                  "contact_ids": ["0", "1", "2"],
                  "shank_ids": ["", "", ""],
                  "device_channel_indices": [0, 1, 2]
                }
              ]
            }
            """;

        [Fact]
        public void StringAnnotation_TypedAccessor()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var areas = group.Probes.First().GetContactAnnotation<string>("brain_area");
            Assert.Equal(new[] { "CA1", "CA1", "DG" }, areas);
        }

        [Fact]
        public void NumericAnnotation_TypedAccessor_Double()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var impedances = group.Probes.First().GetContactAnnotation<double>("impedance");
            Assert.Equal(new[] { 125.3, 98.7, 110.1 }, impedances);
        }

        [Fact]
        public void NumericAnnotation_TypedAccessor_NullableDouble()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var impedances = group.Probes.First().GetContactAnnotation<double?>("impedance");
            Assert.Equal(new double?[] { 125.3, 98.7, 110.1 }, impedances);
        }

        [Fact]
        public void NumericAnnotation_TypedAccessor_Float()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var impedances = group.Probes.First().GetContactAnnotation<float>("impedance");
            Assert.Equal(3, impedances!.Length);
            Assert.Equal(125.3, (double)impedances[0], precision: 1);
        }

        [Fact]
        public void NumericAnnotation_RoundTrip_PreservesNumbers()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var serialized = JsonConvert.SerializeObject(group);
            var parsed = JToken.Parse(serialized);
            var impedanceToken = parsed["probes"]![0]!["contact_annotations"]!["impedance"]!;
            // Values must serialize as JSON numbers, not strings
            Assert.Equal(JTokenType.Float, impedanceToken[0]!.Type);
            Assert.Equal(125.3, impedanceToken[0]!.Value<double>(), 3);
        }

        [Fact]
        public void ArbitraryKeys_RoundTrip()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var serialized = JsonConvert.SerializeObject(group);
            var roundTrip = JsonConvert.DeserializeObject<ProbeGroup>(serialized)!;
            var labels = roundTrip.Probes.First().GetContactAnnotation<string>("custom_label");
            Assert.Equal(new[] { "a", "b", "c" }, labels);
        }

        [Fact]
        public void MissingKey_ReturnsNull()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            Assert.Null(group.Probes.First().GetContactAnnotation<string>("nonexistent"));
        }

        [Fact]
        public void NoContactAnnotations_DoesNotThrow()
        {
            string jsonNoAnnotations = $$"""
                {
                  "specification": "probeinterface",
                  "version": "{{ProbeGroup.SupportedSpecVersion}}",
                  "probes": [
                    {
                      "ndim": 2,
                      "si_units": "um",
                      "annotations": { "model_name": "TestProbe", "manufacturer": "TestMfg" },
                      "contact_positions": [[0.0, 0.0]],
                      "contact_shapes": ["circle"],
                      "contact_shape_params": [{"radius": 5.0}],
                      "contact_ids": ["0"],
                      "shank_ids": [""],
                      "device_channel_indices": [0]
                    }
                  ]
                }
                """;
            var group = JsonConvert.DeserializeObject<ProbeGroup>(jsonNoAnnotations)!;
            Assert.Empty(group.Probes.First().ContactAnnotationKeys);
            Assert.Null(group.Probes.First().GetContactAnnotation<string>("brain_area"));
        }

        [Fact]
        public void SetContactAnnotation_String_CanBeReadBack()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            probe.SetContactAnnotation("region", new[] { "CA3", "CA3", "CA1" });
            Assert.Equal(new[] { "CA3", "CA3", "CA1" }, probe.GetContactAnnotation<string>("region"));
        }

        [Fact]
        public void SetContactAnnotation_Numeric_RoundTrips()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            probe.SetContactAnnotation("gain", new double[] { 1.0, 2.5, 3.0 });
            var serialized = JsonConvert.SerializeObject(group);
            var reloaded = JsonConvert.DeserializeObject<ProbeGroup>(serialized)!;
            var gain = reloaded.Probes.First().GetContactAnnotation<double>("gain");
            Assert.Equal(new[] { 1.0, 2.5, 3.0 }, gain);
        }

        [Fact]
        public void SetContactAnnotation_InitializesStoreWhenEmpty()
        {
            string bare = $$"""
                {
                  "specification": "probeinterface",
                  "version": "{{ProbeGroup.SupportedSpecVersion}}",
                  "probes": [{
                    "ndim": 2, "si_units": "um",
                    "annotations": { "model_name": "P", "manufacturer": "M" },
                    "contact_positions": [[0.0,0.0],[0.0,20.0]],
                    "contact_shapes": ["circle","circle"],
                    "contact_shape_params": [{"radius":5.0},{"radius":5.0}],
                    "contact_ids": ["0","1"], "shank_ids": ["",""],
                    "device_channel_indices": [0,1]
                  }]
                }
                """;
            var probe = JsonConvert.DeserializeObject<ProbeGroup>(bare)!.Probes.First();
            Assert.Empty(probe.ContactAnnotationKeys);
            probe.SetContactAnnotation("label", new[] { "a", "b" });
            Assert.Contains("label", probe.ContactAnnotationKeys);
            Assert.Equal(new[] { "a", "b" }, probe.GetContactAnnotation<string>("label"));
        }

        [Fact]
        public void SetContactAnnotation_WrongLength_Throws()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First(); // 3 contacts
            Assert.Throws<System.ArgumentException>(() =>
                probe.SetContactAnnotation("bad", new[] { "only_two", "values" }));
        }

        [Fact]
        public void RemoveContactAnnotation_RemovesKey()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            Assert.True(probe.RemoveContactAnnotation("brain_area"));
            Assert.Null(probe.GetContactAnnotation<string>("brain_area"));
        }

        [Fact]
        public void RemoveContactAnnotation_MissingKey_ReturnsFalse()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            Assert.False(group.Probes.First().RemoveContactAnnotation("nonexistent"));
        }

        [Fact]
        public void PerContact_SetAnnotation_CanBeReadBack()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            var contact = probe.Contacts[1];
            contact.SetAnnotation("gain", 3.5);
            Assert.Equal(3.5, contact.GetAnnotation<double>("gain"));
            // Visible at probe level too
            var all = probe.GetContactAnnotation<double>("gain");
            Assert.NotNull(all);
            Assert.Equal(3.5, all![1]);
        }

        [Fact]
        public void PerContact_RemoveAnnotation_ClearsSlot()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            var contact = probe.Contacts[0];
            Assert.True(contact.RemoveAnnotation("brain_area"));
            Assert.Null(contact.GetAnnotation<string>("brain_area"));
            // Key still present (other contacts still have values); probe-level array has null at [0]
            var all = probe.GetContactAnnotation<string>("brain_area");
            Assert.NotNull(all);
            Assert.Null(all![0]);
            Assert.Equal("CA1", all[1]);
        }

        [Fact]
        public void PerContact_RemoveAnnotation_AllNull_RemovesKeyFromStore()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            // Remove "brain_area" from every contact — key should disappear from the store
            foreach (var contact in probe.Contacts)
                contact.RemoveAnnotation("brain_area");
            Assert.DoesNotContain("brain_area", probe.ContactAnnotationKeys);
            Assert.Null(probe.GetContactAnnotation<string>("brain_area"));
        }

        [Fact]
        public void PerContact_SetAnnotation_NewKey_OtherContactsGetDefault()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            probe.Contacts[0].SetAnnotation("new_key", "only_first");
            var all = probe.GetContactAnnotation<string>("new_key");
            Assert.NotNull(all);
            Assert.Equal("only_first", all![0]);
            Assert.Null(all[1]);
            Assert.Null(all[2]);
        }

        [Fact]
        public void PerContact_SetAnnotation_PartialAnnotation_RoundTrips()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var probe = group.Probes.First();
            probe.Contacts[0].SetAnnotation("partial", "present");
            var serialized = JsonConvert.SerializeObject(group);
            var parsed = JToken.Parse(serialized);
            var arr = parsed["probes"]![0]!["contact_annotations"]!["partial"]!;
            Assert.Equal("present", arr[0]!.Value<string>());
            Assert.Equal(JTokenType.Null, arr[1]!.Type);
            Assert.Equal(JTokenType.Null, arr[2]!.Type);
        }
    }
}
