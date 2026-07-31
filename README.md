# OpenEphys.ProbeInterface.NET

A C# .NET library for reading and writing neural probe configurations in the
[probeinterface](https://probeinterface.readthedocs.io) JSON format.
Probeinterface is an open standard describing probe geometry, contact positions,
and channel mappings, and is widely used by Open Ephys, SpikeInterface, and
related tools.

## What it does

- **Deserializes** probeinterface JSON files into strongly-typed .NET objects
- **Serializes** probe configurations back to compliant JSON
- **Validates** files against the specification on load (correct version format,
  required fields, unique channel indices, array length consistency)
- **Supports** all probe properties: contact positions (2D and 3D), shapes
  (`circle`, `rect`, `square`), plane axes, planar contour, shank IDs, contact
  sides, and free-form per-contact annotations with typed access

## Quick start

```csharp
using Newtonsoft.Json;
using OpenEphys.ProbeInterface.NET;

// Load a probeinterface JSON file
var json = File.ReadAllText("my_probe.json");
var probeGroup = JsonConvert.DeserializeObject<ProbeGroup>(json);

// Iterate contacts on each probe
foreach (var probe in probeGroup.Probes)
{
    foreach (var contact in probe.Contacts)
        Console.WriteLine($"Contact {contact.ContactId}: ({contact.PosX}, {contact.PosY})");
}

// Access per-contact annotations at the contact level
var probe0 = probeGroup.Probes.First();
var contact0 = probe0.Contacts[0];
double? impedance = contact0.GetAnnotation<double>("impedance");
string? region = contact0.GetAnnotation<string>("brain_area");

// Set or remove an annotation on a single contact
contact0.SetAnnotation("brain_area", "CA1");
contact0.RemoveAnnotation("brain_area");

// Bulk access across all contacts on a probe (returns one element per contact)
double[]? impedances = probe0.GetContactAnnotation<double>("impedance");
probe0.SetContactAnnotation("brain_area", new[] { "CA1", "CA3", "DG" });
probe0.RemoveContactAnnotation("brain_area");

// Access probe-level annotations (model, manufacturer, any custom fields)
Console.WriteLine(probe0.Annotations.ModelName);
probe0.Annotations.SetAnnotation("implant_date", "2025-01-01");
string? date = probe0.Annotations.GetAnnotation<string>("implant_date");

// Wire hardware channels to contacts (channel number → contact index)
// Validates uniqueness within and across all probes in the group
ChannelWiring.WireChannels(probeGroup, probeIndex: 0, new Dictionary<int, int>
{
    { 0, 3 }, { 1, 1 }, { 2, 2 }, { 3, 0 }
});

// Query the resulting channel map (channel number → probe index, contact index)
var map = probeGroup.ChannelMap;
if (map != null)
{
    foreach (var (channel, entry) in map)
        Console.WriteLine($"Channel {channel} → probe {entry.ProbeIndex}, contact {entry.ContactIndex}");
}

// Wire a single contact, or clear the mapping when done
ChannelWiring.WireChannel(probeGroup, probeIndex: 0, contactIndex: 4, channel: 7);
ChannelWiring.UnwireChannel(probeGroup, probeIndex: 0, contactIndex: 4);
ChannelWiring.UnwireChannels(probeGroup, probeIndex: 0); // clear one probe
ChannelWiring.UnwireChannels(probeGroup); // clear all probes

// Serialize back to JSON
File.WriteAllText("output.json", JsonConvert.SerializeObject(probeGroup, Formatting.Indented));
```
