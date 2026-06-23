using Newtonsoft.Json;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Class holding parameters used to draw the contact.
    /// </summary>
    /// <remarks>
    /// Fields are nullable, since not all fields are required depending on the shape selected.
    /// </remarks>
    public class ContactShapeParam
    {
        /// <summary>
        /// Gets the radius of the contact.
        /// </summary>
        /// <remarks>
        /// This is only used to draw <see cref="ContactShape.Circle"/> contacts. Field can be null.
        /// </remarks>
        [JsonProperty("radius", NullValueHandling = NullValueHandling.Ignore)]
        public double? Radius { get; }

        /// <summary>
        /// Gets the width of the contact.
        /// </summary>
        /// <remarks>
        /// This is used to draw <see cref="ContactShape.Square"/> or <see cref="ContactShape.Rect"/> contacts.
        /// Field can be null.
        /// </remarks>
        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public double? Width { get; }

        /// <summary>
        /// Gets the height of the contact.
        /// </summary>
        /// <remarks>
        /// This is only used to draw <see cref="ContactShape.Rect"/> contacts. Field can be null.
        /// </remarks>
        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public double? Height { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactShapeParam"/> class.
        /// Used by Newtonsoft.Json during deserialization.
        /// </summary>
        [JsonConstructor]
        internal ContactShapeParam(double? radius = null, double? width = null, double? height = null)
        {
            Radius = radius;
            Width = width;
            Height = height;
        }
    }
}
