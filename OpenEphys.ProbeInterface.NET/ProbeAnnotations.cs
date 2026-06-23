using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Probe-level annotations. <see cref="ModelName"/> and <see cref="Manufacturer"/> are required
    /// by the spec; any additional key-value pairs are stored in <see cref="additionalProperties"/>
    /// and accessible via <see cref="GetAnnotation{T}"/>, <see cref="SetAnnotation{T}"/>, and
    /// <see cref="RemoveAnnotation"/>.
    /// </summary>
    public class ProbeAnnotations
    {
        /// <summary>Gets the model name of the probe as defined by the manufacturer.</summary>
        [JsonProperty("model_name")]
        public string ModelName { get; }

        /// <summary>Gets the name of the manufacturer who created the probe.</summary>
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; }

        [JsonExtensionData]
        private Dictionary<string, JToken>? additionalProperties;

        /// <summary>Gets the keys of all additional annotations present on this probe.</summary>
        [JsonIgnore]
        public IEnumerable<string> AnnotationKeys =>
            additionalProperties?.Keys ?? Enumerable.Empty<string>();

        /// <summary>
        /// Used by Newtonsoft.Json during deserialization.
        /// </summary>
        [JsonConstructor]
        internal ProbeAnnotations(string model_name, string manufacturer)
        {
            ModelName = model_name;
            Manufacturer = manufacturer;
        }

        /// <summary>
        /// Returns an additional annotation value for the given key converted to
        /// <typeparamref name="T"/>, or the default value of <typeparamref name="T"/> if absent.
        /// </summary>
        public T? GetAnnotation<T>(string key)
        {
            if (additionalProperties == null || !additionalProperties.TryGetValue(key, out var token))
                return default;
            return token.ToObject<T>();
        }

        /// <summary>
        /// Adds or replaces an additional annotation for the given key.
        /// </summary>
        public void SetAnnotation<T>(string key, T value)
        {
            additionalProperties ??= new Dictionary<string, JToken>();
            additionalProperties[key] = JToken.FromObject(value!);
        }

        /// <summary>
        /// Removes the additional annotation with the given key.
        /// Returns true if the key was found and removed.
        /// </summary>
        public bool RemoveAnnotation(string key) =>
            additionalProperties != null && additionalProperties.Remove(key);
    }
}
