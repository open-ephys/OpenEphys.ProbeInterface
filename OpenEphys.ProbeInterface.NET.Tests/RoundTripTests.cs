using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    /// <summary>
    /// Verifies that each JSON fixture can be deserialized into a <see cref="ProbeGroup"/> and
    /// re-serialized to JSON that is structurally equivalent to the original.
    /// </summary>
    public class RoundTripTests
    {
        private static ProbeGroup Deserialize(string json) =>
            JsonConvert.DeserializeObject<ProbeGroup>(json)
                ?? throw new JsonException("Deserialization returned null.");

        private static bool JsonEquivalent(string a, string b) =>
            TokensEqual(JToken.Parse(a), JToken.Parse(b));

        private static bool TokensEqual(JToken a, JToken b)
        {
            if (a.Type == b.Type)
            {
                if (a is JArray arrA && b is JArray arrB)
                {
                    if (arrA.Count != arrB.Count) return false;
                    for (int i = 0; i < arrA.Count; i++)
                        if (!TokensEqual(arrA[i], arrB[i])) return false;
                    return true;
                }
                if (a is JObject objA && b is JObject objB)
                {
                    if (objA.Count != objB.Count) return false;
                    foreach (var prop in objA.Properties())
                    {
                        var bProp = objB.Property(prop.Name);
                        if (bProp == null || !TokensEqual(prop.Value, bProp.Value)) return false;
                    }
                    return true;
                }
                return JToken.DeepEquals(a, b);
            }
            // Integer vs float: compare by numeric value.
            if ((a.Type == JTokenType.Integer || a.Type == JTokenType.Float) &&
                (b.Type == JTokenType.Integer || b.Type == JTokenType.Float))
                return a.Value<double>() == b.Value<double>();
            return false;
        }

        [Fact]
        public void MinimalProbe_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("minimal_probe.json");
            var group = Deserialize(json);
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        [Fact]
        public void ProbeWithContactAnnotations_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("probe_with_contact_annotations.json");
            var group = Deserialize(json);
            Assert.NotNull(group.Probes.First().GetContactAnnotation<string>("brain_area"));
            Assert.NotNull(group.Probes.First().GetContactAnnotation<double>("impedance"));
            Assert.NotNull(group.Probes.First().GetContactAnnotation<string>("channel_names"));
            Assert.Equal(new[] { "CA1", "CA1", "DG" }, group.Probes.First().GetContactAnnotation<string>("brain_area"));
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        [Fact]
        public void ProbeWithContactSides_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("probe_with_contact_sides.json");
            var group = Deserialize(json);
            Assert.Equal(new[] { "front", "front", "back" }, group.Probes.First().Contacts.Select(c => c.Side).ToArray());
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        [Fact]
        public void ProbeWithNonNumericIds_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("probe_with_nonnumeric_ids.json");
            var group = Deserialize(json);
            Assert.Equal("e0", group.Probes.First().Contacts[0].ContactId);
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        [Fact]
        public void MultiProbeGroup_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("multi_probe_group.json");
            var group = Deserialize(json);
            Assert.Equal(2, group.Probes.Count());
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        [Fact]
        public void Probe3D_RoundTrips()
        {
            var json = FixtureHelper.LoadFixture("probe_3d.json");
            var group = Deserialize(json);
            var contact = group.Probes.First().Contacts[1];
            Assert.Equal(20.0, contact.PosY);
            Assert.Equal(5.0, contact.PosZ);
            var roundTripped = JsonConvert.SerializeObject(group);
            Assert.True(JsonEquivalent(json, roundTripped));
        }

        // Real-world probe library

        public static IEnumerable<object[]> ProbeLibraryFiles =>
            FixtureHelper.GetProbeLibraryResourceNames()
                .Where(n => {
                    var json = FixtureHelper.LoadProbeLibraryFile(n);
                    var sv = ProbeGroup.SupportedSpecVersion;
                    return json.Contains($"\"{sv.Major}.{sv.Minor}.");
                })
                .Select(n => new object[] { n });

        [Theory]
        [MemberData(nameof(ProbeLibraryFiles))]
        public void ProbeLibraryFile_RoundTrips(string resourceName)
        {
            var json = FixtureHelper.LoadProbeLibraryFile(resourceName);
            var group = Deserialize(json);
            Assert.True(group.NumberOfContacts > 0);
            Assert.True(JsonEquivalent(json, JsonConvert.SerializeObject(group)));
        }
    }
}
