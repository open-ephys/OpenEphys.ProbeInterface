"""
Regenerate JSON fixture files from the Python probeinterface library.

Usage:
    pip install probeinterface
    python generate_fixtures.py

Outputs files to OpenEphys.ProbeInterface.NET.Tests/Fixtures/.
The generated files are the authoritative reference for round-trip testing.
"""

import json
from pathlib import Path

try:
    from probeinterface import Probe, ProbeGroup
    from probeinterface.io import write_probeinterface
except ImportError:
    raise SystemExit("probeinterface is not installed. Run: pip install probeinterface")

OUT = Path(__file__).parent / "OpenEphys.ProbeInterface.NET.Tests" / "Fixtures"
OUT.mkdir(parents=True, exist_ok=True)


def write(name: str, group: ProbeGroup) -> None:
    path = OUT / name
    write_probeinterface(path, group)
    # Pretty-print for readability
    with open(path) as f:
        data = json.load(f)
    with open(path, "w") as f:
        json.dump(data, f, indent=2)
    print(f"Written: {path}")


# --- minimal_probe.json ---
p = Probe(ndim=2, si_units="um")
p.set_contacts(
    positions=[[0, 0], [0, 20], [16, 10]],
    shapes="circle",
    shape_params={"radius": 5},
)
p.set_device_channel_indices([0, 1, 2])
p.annotate(model_name="TestProbe", manufacturer="TestManufacturer")
g = ProbeGroup()
g.add_probe(p)
write("minimal_probe.json", g)

# --- probe_with_contact_annotations.json ---
p = Probe(ndim=2, si_units="um")
p.set_contacts(
    positions=[[0, 0], [0, 20], [16, 10]],
    shapes="circle",
    shape_params={"radius": 5},
)
p.set_device_channel_indices([0, 1, 2])
p.annotate(model_name="TestProbe", manufacturer="TestManufacturer")
p.set_contact_annotations(brain_area=["CA1", "CA1", "DG"], channel_names=["ch0", "ch1", "ch2"],
                           impedance=[125.3, 98.7, 110.1])
g = ProbeGroup()
g.add_probe(p)
write("probe_with_contact_annotations.json", g)

# --- probe_with_contact_sides.json ---
p = Probe(ndim=2, si_units="um")
p.set_contacts(
    positions=[[0, 0], [0, 20], [16, 10]],
    shapes="circle",
    shape_params={"radius": 5},
    contact_ids=["0", "1", "2"],
)
p.set_device_channel_indices([0, 1, 2])
p.annotate(model_name="TestProbe", manufacturer="TestManufacturer")
p.contact_sides = ["front", "front", "back"]
g = ProbeGroup()
g.add_probe(p)
write("probe_with_contact_sides.json", g)

# --- probe_with_nonnumeric_ids.json ---
p = Probe(ndim=2, si_units="um")
p.set_contacts(
    positions=[[0, 0], [0, 20], [16, 10]],
    shapes="circle",
    shape_params={"radius": 5},
    contact_ids=["e0", "e1", "e2"],
)
p.set_device_channel_indices([0, 1, 2])
p.annotate(model_name="TestProbe", manufacturer="TestManufacturer")
g = ProbeGroup()
g.add_probe(p)
write("probe_with_nonnumeric_ids.json", g)

# --- multi_probe_group.json ---
p1 = Probe(ndim=2, si_units="um")
p1.set_contacts(
    positions=[[0, 0], [0, 20]],
    shapes="circle",
    shape_params={"radius": 5},
)
p1.set_device_channel_indices([0, 1])
p1.annotate(model_name="TestProbeA", manufacturer="TestManufacturer")

p2 = Probe(ndim=2, si_units="um")
p2.set_contacts(
    positions=[[0, 0], [0, 20], [16, 10]],
    shapes="rect",
    shape_params={"width": 10, "height": 5},
)
p2.set_device_channel_indices([2, 3, 4])
p2.annotate(model_name="TestProbeB", manufacturer="TestManufacturer")

g = ProbeGroup()
g.add_probe(p1)
g.add_probe(p2)
write("multi_probe_group.json", g)

# --- probe_3d.json ---
p = Probe(ndim=3, si_units="um")
p.set_contacts(
    positions=[[0, 0, 0], [0, 20, 5], [16, 10, 10]],
    shapes="circle",
    shape_params={"radius": 5},
)
p.set_device_channel_indices([0, 1, 2])
p.annotate(model_name="TestProbe3D", manufacturer="TestManufacturer")
g = ProbeGroup()
g.add_probe(p)
write("probe_3d.json", g)

print("\nAll fixtures written. Verify them against the JSON schema before committing.")
