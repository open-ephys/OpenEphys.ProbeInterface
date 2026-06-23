using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    /// <summary>
    /// Validates JSON against the bundled probeinterface JSON schema (probe.json.schema).
    /// Covers both raw fixture files and the output of our serialization pipeline.
    /// </summary>
    public class SchemaValidationTests
    {
        private static readonly JsonSchema Schema =
            JsonSchema.FromJsonAsync(FixtureHelper.LoadFixture("probe.json.schema"))
                      .GetAwaiter().GetResult();

        private static ICollection<ValidationError> Validate(string json) => Schema.Validate(json);

        public static IEnumerable<object[]> AllFixtures =>
            new[]
            {
                new object[] { "minimal_probe.json" },
                new object[] { "probe_with_contact_annotations.json" },
                new object[] { "probe_with_contact_sides.json" },
                new object[] { "probe_with_nonnumeric_ids.json" },
                new object[] { "multi_probe_group.json" },
                new object[] { "probe_3d.json" },
            };

        [Theory]
        [MemberData(nameof(AllFixtures))]
        public void FixtureJson_PassesSchemaValidation(string fileName)
        {
            var errors = Validate(FixtureHelper.LoadFixture(fileName));
            Assert.Empty(errors);
        }

        [Theory]
        [MemberData(nameof(AllFixtures))]
        public void SerializedProbeGroup_PassesSchemaValidation(string fileName)
        {
            var json = FixtureHelper.LoadFixture(fileName);
            var group = JsonConvert.DeserializeObject<ProbeGroup>(json)!;
            var errors = Validate(JsonConvert.SerializeObject(group));
            Assert.Empty(errors);
        }

        [Fact]
        public void ProbeAnnotations_WithExtraKeys_PassesSchemaValidation()
        {
            string json = $$"""
                {
                  "specification": "probeinterface",
                  "version": "{{ProbeGroup.SupportedSpecVersion}}",
                  "probes": [{
                    "ndim": 2,
                    "si_units": "um",
                    "annotations": {
                      "model_name": "Neuropixels 1.0",
                      "manufacturer": "IMEC",
                      "custom_note": "implanted 2025-01-01",
                      "depth_um": 3840
                    },
                    "contact_positions": [[0.0, 0.0]],
                    "contact_shapes": ["circle"],
                    "contact_shape_params": [{"radius": 5.0}],
                    "contact_ids": ["0"],
                    "shank_ids": [""]
                  }]
                }
                """;
            var errors = Validate(json);
            Assert.Empty(errors);
        }

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
        public void ProbeLibraryFile_SerializedOutputPassesSchema(string resourceName)
        {
            var json = FixtureHelper.LoadProbeLibraryFile(resourceName);
            var group = JsonConvert.DeserializeObject<ProbeGroup>(json)!;
            var errors = Validate(JsonConvert.SerializeObject(group));
            Assert.Empty(errors);
        }

        [Fact]
        public void ProbeAnnotations_WithExtraKeys_SerializedOutputPassesSchema()
        {
            string json = $$"""
                {
                  "specification": "probeinterface",
                  "version": "{{ProbeGroup.SupportedSpecVersion}}",
                  "probes": [{
                    "ndim": 2,
                    "si_units": "um",
                    "annotations": {
                      "model_name": "Neuropixels 1.0",
                      "manufacturer": "IMEC",
                      "custom_note": "implanted 2025-01-01",
                      "depth_um": 3840
                    },
                    "contact_positions": [[0.0, 0.0]],
                    "contact_shapes": ["circle"],
                    "contact_shape_params": [{"radius": 5.0}],
                    "contact_ids": ["0"],
                    "shank_ids": [""]
                  }]
                }
                """;
            var group = JsonConvert.DeserializeObject<ProbeGroup>(json)!;
            var errors = Validate(JsonConvert.SerializeObject(group));
            Assert.Empty(errors);
        }
    }
}
