using System.Collections.Generic;

namespace OpenEphys.ProbeInterface.NET
{
    /// <summary>
    /// Shared backing store for per-contact annotations within a single <see cref="Probe"/>.
    /// One instance is created per probe and passed to every <see cref="Contact"/> so that
    /// mutations made through any contact are immediately visible at the probe level (and
    /// vice-versa) without requiring a back-reference from contact to probe.
    /// </summary>
    internal sealed class ContactAnnotationStore
    {
        internal Dictionary<string, object[]>? Data;
    }
}
