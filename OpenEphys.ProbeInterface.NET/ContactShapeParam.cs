using System;
using Newtonsoft.Json;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Class holding parameters used to draw the contact.
    /// </summary>
    /// <remarks>
    /// Fields are nullable, since not all fields are required depending on the shape selected. Per the
    /// probeinterface schema, at least one of <see cref="Radius"/> or <see cref="Width"/> must be specified,
    /// and any of the three fields that are specified must be non-negative.
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
        /// Initializes a new instance of the <see cref="ContactShapeParam"/> class. Used by Newtonsoft.Json
        /// during deserialization.
        /// </summary>
        /// <param name="radius">The radius of the contact. Must be non-negative if specified.</param>
        /// <param name="width">The width of the contact. Must be non-negative if specified.</param>
        /// <param name="height">The height of the contact. Must be non-negative if specified.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="radius"/>, <paramref name="width"/>, or <paramref name="height"/> is
        /// negative, or when neither <paramref name="radius"/> nor <paramref name="width"/> is specified.
        /// </exception>
        [JsonConstructor]
        internal ContactShapeParam(double? radius = null, double? width = null, double? height = null)
        {
            if (radius.HasValue && radius.Value < 0)
                throw new ArgumentException($"radius must be >= 0, but was {radius.Value}.");
            if (width.HasValue && width.Value < 0)
                throw new ArgumentException($"width must be >= 0, but was {width.Value}.");
            if (height.HasValue && height.Value < 0)
                throw new ArgumentException($"height must be >= 0, but was {height.Value}.");
            if (!radius.HasValue && !width.HasValue)
                throw new ArgumentException("Either radius or width must be specified.");

            Radius = radius;
            Width = width;
            Height = height;
        }
    }
}
