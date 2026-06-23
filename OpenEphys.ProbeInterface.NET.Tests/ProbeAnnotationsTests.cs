using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    public class ProbeAnnotationsTests
    {
        private static string Json => $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
                  "ndim": 2,
                  "si_units": "um",
                  "annotations": { "model_name": "Neuropixels 1.0", "manufacturer": "IMEC" },
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

        [Fact]
        public void ModelName_DeserializesFromModelNameKey()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            Assert.Equal("Neuropixels 1.0", group.Probes.First().Annotations.ModelName);
            Assert.Equal("IMEC", group.Probes.First().Annotations.Manufacturer);
        }

        [Fact]
        public void ModelName_SerializesToModelNameKey()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(Json)!;
            var serialized = JsonConvert.SerializeObject(group);
            var roundTrip = JsonConvert.DeserializeObject<ProbeGroup>(serialized)!;
            Assert.Equal("Neuropixels 1.0", roundTrip.Probes.First().Annotations.ModelName);
        }

        private static string JsonWithExtraAnnotations => $$"""
            {
              "specification": "probeinterface",
              "version": "{{ProbeGroup.SupportedSpecVersion}}",
              "probes": [
                {
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
                }
              ]
            }
            """;

        [Fact]
        public void ExtraAnnotationKeys_AreDeserializedIntoAdditionalProperties()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            var ann = group.Probes.First().Annotations;
            Assert.Contains("custom_note", ann.AnnotationKeys);
            Assert.Contains("depth_um", ann.AnnotationKeys);
        }

        [Fact]
        public void ExtraAnnotationKeys_TypedAccessor_String()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            Assert.Equal("implanted 2025-01-01",
                group.Probes.First().Annotations.GetAnnotation<string>("custom_note"));
        }

        [Fact]
        public void ExtraAnnotationKeys_TypedAccessor_Numeric()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            Assert.Equal(3840, group.Probes.First().Annotations.GetAnnotation<int>("depth_um"));
        }

        [Fact]
        public void ExtraAnnotationKeys_RoundTrip()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            var serialized = JsonConvert.SerializeObject(group);
            var roundTrip = JsonConvert.DeserializeObject<ProbeGroup>(serialized)!;
            Assert.Equal("implanted 2025-01-01",
                roundTrip.Probes.First().Annotations.GetAnnotation<string>("custom_note"));
            Assert.Equal(3840, roundTrip.Probes.First().Annotations.GetAnnotation<int>("depth_um"));
        }

        [Fact]
        public void ExtraAnnotationKeys_AbsentKey_ReturnsDefault()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            Assert.Null(group.Probes.First().Annotations.GetAnnotation<string>("nonexistent"));
        }

        [Fact]
        public void KnownKeys_NotDuplicatedInAdditionalProperties()
        {
            var group = JsonConvert.DeserializeObject<ProbeGroup>(JsonWithExtraAnnotations)!;
            var ann = group.Probes.First().Annotations;
            // model_name and manufacturer are handled by named properties, not extension data
            Assert.DoesNotContain("model_name", ann.AnnotationKeys);
            Assert.DoesNotContain("manufacturer", ann.AnnotationKeys);
        }
    }
}
