using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Probe-level annotations. <see cref="ModelName"/> and <see cref="Manufacturer"/> are required by the
    /// spec; any additional key-value pairs are stored in <see cref="additionalProperties"/> and accessible
    /// via <see cref="GetAnnotation{T}"/>, <see cref="SetAnnotation{T}"/>, and <see
    /// cref="RemoveAnnotation"/>.
    /// </summary>
    public class ProbeAnnotations
    {
        /// <summary>
        /// Gets the model name of the probe as defined by the manufacturer.
        /// </summary>
        [JsonProperty("model_name", Required = Required.Always)]
        public string ModelName { get; }

        /// <summary>
        /// Gets the name of the manufacturer who created the probe.
        /// </summary>
        [JsonProperty("manufacturer", Required = Required.Always)]
        public string Manufacturer { get; }

        [JsonExtensionData]
        private Dictionary<string, JToken>? additionalProperties;

        /// <summary>
        /// Gets the keys of all additional annotations present on this probe.
        /// </summary>
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
        /// Deep copy constructor.
        /// </summary>
        internal ProbeAnnotations(ProbeAnnotations source)
        {
            ModelName = source.ModelName;
            Manufacturer = source.Manufacturer;
            if (source.additionalProperties != null)
                additionalProperties = new Dictionary<string, JToken>(source.additionalProperties);
        }

        /// <summary>
        /// Returns an additional annotation value for the given key converted to
        /// <typeparamref name="T"/>, or the default value of <typeparamref name="T"/> if absent.
        /// </summary>
        /// <typeparam name="T">The type to convert the stored value to.</typeparam>
        /// <param name="key">The annotation key to look up.</param>
        /// <returns>
        /// The annotation value converted to <typeparamref name="T"/>, or the default value of
        /// <typeparamref name="T"/> if the key is absent.
        /// </returns>
        public T? GetAnnotation<T>(string key)
        {
            if (additionalProperties == null || !additionalProperties.TryGetValue(key, out var token))
                return default;
            return token.ToObject<T>();
        }

        /// <summary>
        /// Adds or replaces an additional annotation for the given key.
        /// </summary>
        /// <typeparam name="T">The type of the annotation value.</typeparam>
        /// <param name="key">The annotation key to set.</param>
        /// <param name="value">The value to store.</param>
        public void SetAnnotation<T>(string key, T value)
        {
            additionalProperties ??= new Dictionary<string, JToken>();
            additionalProperties[key] = JToken.FromObject(value!);
        }

        /// <summary>
        /// Removes the additional annotation with the given key.
        /// </summary>
        /// <param name="key">The annotation key to remove.</param>
        /// <returns>True if the key was found and removed; false if it was absent.</returns>
        public bool RemoveAnnotation(string key) =>
            additionalProperties != null && additionalProperties.Remove(key);
    }
}
