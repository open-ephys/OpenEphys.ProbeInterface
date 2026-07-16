using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenEphys.ProbeInterface.NET.Tests
{
    internal static class FixtureHelper
    {
        private static readonly Assembly Assembly = typeof(FixtureHelper).Assembly;

        public static string LoadFixture(string fileName)
        {
            var resourceName = $"OpenEphys.ProbeInterface.NET.Tests.Fixtures.{fileName}";
            using var stream = Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static IEnumerable<string> GetProbeLibraryResourceNames() =>
            Assembly.GetManifestResourceNames()
                    .Where(n => n.Contains(".probeinterface_library.") && n.EndsWith(".json"));

        public static string LoadProbeLibraryFile(string resourceName)
        {
            using var stream = Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
