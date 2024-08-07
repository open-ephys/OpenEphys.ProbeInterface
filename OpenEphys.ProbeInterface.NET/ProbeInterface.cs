﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.CodeDom.Compiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace OpenEphys.ProbeInterface;

/// <summary>
/// Abstract class that implements the Probeinterface specification in C# for .NET.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
public abstract class ProbeGroup
{
    private string _specification;
    private string _version;
    private IEnumerable<Probe> _probes;

    /// <summary>
    /// Gets the string defining the specification of the file.
    /// </summary>
    /// <remarks>
    /// For Probeinterface files, this value is expected to be "probeinterface".
    /// </remarks>
    [JsonProperty("specification", Required = Required.Always)]
    public string Specification
    {
        get { return _specification; }
        protected set { _specification = value; }
    }

    /// <summary>
    /// Gets the string defining which version of Probeinterface was used.
    /// </summary>
    [JsonProperty("version", Required = Required.Always)]
    public string Version
    {
        get { return _version; }
        protected set { _version = value; }
    }

    /// <summary>
    /// Gets an IEnumerable of probes that are present.
    /// </summary>
    /// <remarks>
    /// Each probe can contain multiple shanks, and each probe has a unique
    /// contour that defines the physical representation of the probe. Contacts have several representations
    /// for their channel number, specifically <see cref="Probe.ContactIds"/> (a string that is not guaranteed to be unique) and
    /// <see cref="Probe.DeviceChannelIndices"/> (guaranteed to be unique across all probes). <see cref="Probe.DeviceChannelIndices"/>'s can also be set to -1
    /// to indicate that the channel was not connected or recorded from.
    /// </remarks>
    [XmlIgnore]
    [JsonProperty("probes", Required = Required.Always)]
    public IEnumerable<Probe> Probes
    {
        get { return _probes; }
        protected set { _probes = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeGroup"/> class.
    /// </summary>
    /// <param name="specification">String defining the <see cref="Specification"/> parameter.</param>
    /// <param name="version">String defining the <see cref="Version"/> parameter.</param>
    /// <param name="probes">IEnumerable of <see cref="Probe"/> objects.</param>
    public ProbeGroup(string specification, string version, IEnumerable<Probe> probes)
    {
        _specification = specification;
        _version = version;
        _probes = probes;

        Validate();
    }

    /// <summary>
    /// Copy constructor that takes in an existing <see cref="ProbeGroup"/> object and copies the individual fields.
    /// </summary>
    /// <remarks>
    /// After copying the relevant fields, the <see cref="ProbeGroup"/> is validated to ensure that it is compliant
    /// with the Probeinterface specification. See <see cref="Validate"/> for more details on what is checked.
    /// </remarks>
    /// <param name="probeGroup">Existing <see cref="ProbeGroup"/> object.</param>
    protected ProbeGroup(ProbeGroup probeGroup)
    {
        _specification = probeGroup._specification;
        _version = probeGroup._version;
        _probes = probeGroup._probes;

        Validate();
    }

    /// <summary>
    /// Gets the number of contacts across all <see cref="Probe"/> objects.
    /// </summary>
    public int NumberOfContacts
    {
        get
        {
            int numContacts = 0;

            foreach (var probe in _probes)
            {
                numContacts += probe.NumberOfContacts;
            }

            return numContacts;
        }
    }

    /// <summary>
    /// Returns the <see cref="Probe.ContactIds"/>'s of all contacts in all probes.
    /// </summary>
    /// <remarks>
    /// Note that these are not guaranteed to be unique values across probes.
    /// </remarks>
    /// <returns>List of strings containing all contact IDs.</returns>
    public IEnumerable<string> GetContactIds()
    {
        List<string> contactIds = new();

        foreach (var probe in _probes)
        {
            contactIds.AddRange(probe.ContactIds.ToList());
        }

        return contactIds;
    }

    /// <summary>
    /// Returns all <see cref="Contact"/> objects in the <see cref="ProbeGroup"/>.
    /// </summary>
    /// <returns><see cref="List{Contact}"/></returns>
    public List<Contact> GetContacts()
    {
        List<Contact> contacts = new();

        foreach (var p in Probes)
        {
            for (int i = 0; i < p.NumberOfContacts; i++)
            {
                contacts.Add(p.GetContact(i));
            }
        }

        return contacts;
    }

    /// <summary>
    /// Returns all <see cref="Probe.DeviceChannelIndices"/>'s in the <see cref="ProbeGroup"/>.
    /// </summary>
    /// <remarks>
    /// Device channel indices are guaranteed to be unique, unless they are -1. Multiple contacts can be
    /// set to -1 to indicate they are not recorded from.
    /// </remarks>
    /// <returns><see cref="IEnumerable{Int32}"/></returns>
    public IEnumerable<int> GetDeviceChannelIndices()
    {
        List<int> deviceChannelIndices = new();

        foreach (var probe in _probes)
        {
            deviceChannelIndices.AddRange(probe.DeviceChannelIndices.ToList());
        }

        return deviceChannelIndices;
    }

    /// <summary>
    /// Validate that the <see cref="ProbeGroup"/> correctly implements the Probeinterface specification.
    /// </summary>
    /// <remarks>
    /// <para>Check that all necessary fields are populated (<see cref="Specification"/>,
    /// <see cref="Version"/>, <see cref="Probes"/>).</para>
    /// <para>Check that there is at least one <see cref="Probe"/> defined.</para>
    /// <para>Check that all variables in each <see cref="Probe"/> have the same length.</para>
    /// <para>Check if <see cref="Probe.ContactIds"/> are present, and generate default values
    /// based on the index if there are no values defined.</para>
    /// <para>Check if <see cref="Probe.ContactIds"/> are zero-indexed, and convert to
    /// zero-indexed if possible.</para>
    /// <para>Check if <see cref="Probe.ShankIds"/> are defined, and initialize empty strings 
    /// if they are not defined.</para>
    /// <para>Check if <see cref="Probe.DeviceChannelIndices"/> are defined, and initialize default
    /// values (using the <see cref="Probe.ContactIds"/> value as the new <see cref="Probe.DeviceChannelIndices"/>).</para>
    /// <para>Check that all <see cref="Probe.DeviceChannelIndices"/> are unique across all <see cref="Probe"/>'s,
    /// unless the value is -1; multiple contacts can be set to -1.</para>
    /// </remarks>
    public void Validate()
    {
        if (_specification == null || _version == null || _probes == null)
        {
            throw new Exception("Necessary fields are null, unable to validate properly");
        }

        if (_probes.Count() == 0)
        {
            throw new Exception("No probes are listed, probes must be added during construction");
        }

        if (!ValidateVariableLength(out string result))
        {
            throw new Exception(result);
        }

        SetDefaultContactIdsIfMissing();
        ValidateContactIds();
        SetEmptyShankIdsIfMissing();
        SetDefaultDeviceChannelIndicesIfMissing();

        if (!ValidateDeviceChannelIndices())
        {
            throw new Exception("Device channel indices are not unique across all probes.");
        }
    }

    private bool ValidateVariableLength(out string result)
    {
        for (int i = 0; i < _probes.Count(); i++)
        {
            if (_probes.ElementAt(i).NumberOfContacts != _probes.ElementAt(i).ContactPositions.Count() ||
                _probes.ElementAt(i).NumberOfContacts != _probes.ElementAt(i).ContactPlaneAxes.Count() ||
                _probes.ElementAt(i).NumberOfContacts != _probes.ElementAt(i).ContactShapeParams.Count() ||
                _probes.ElementAt(i).NumberOfContacts != _probes.ElementAt(i).ContactShapes.Count())
            {
                result = $"Required contact parameters are not the same length in probe {i}. " +
                         "Check positions / plane axes / shapes / shape parameters for lengths.";
                return false;
            }

            if (_probes.ElementAt(i).ContactIds != null &&
                _probes.ElementAt(i).ContactIds.Count() != _probes.ElementAt(i).NumberOfContacts)
            {
                result = $"Contact IDs does not have the correct number of channels for probe {i}";
                return false;
            }

            if (_probes.ElementAt(i).ShankIds != null &&
                _probes.ElementAt(i).ShankIds.Count() != _probes.ElementAt(i).NumberOfContacts)
            {
                result = $"Shank IDs does not have the correct number of channels for probe {i}";
                return false;
            }

            if (_probes.ElementAt(i).DeviceChannelIndices != null &&
                _probes.ElementAt(i).DeviceChannelIndices.Count() != _probes.ElementAt(i).NumberOfContacts)
            {
                result = $"Device Channel Indices does not have the correct number of channels for probe {i}";
                return false;
            }
        }

        result = "";
        return true;
    }

    private void SetDefaultContactIdsIfMissing()
    {
        int contactNum = 0;

        for (int i = 0; i < _probes.Count(); i++)
        {
            if (_probes.ElementAt(i).ContactIds == null)
            {
                _probes.ElementAt(i).ContactIds = Probe.DefaultContactIds(_probes.ElementAt(i).NumberOfContacts);
            }
            else
                contactNum += _probes.ElementAt(i).NumberOfContacts;
        }
    }

    private void ValidateContactIds()
    {
        CheckIfContactIdsAreZeroIndexed();
    }

    private void CheckIfContactIdsAreZeroIndexed()
    {
        var contactIds = GetContactIds();
        var numericIds = contactIds.Select(c => { return int.Parse(c); })
                                   .ToList();

        var min = numericIds.Min();
        var max = numericIds.Max();

        if (min == 1 && max == NumberOfContacts && numericIds.Count == numericIds.Distinct().Count())
        {
            for (int i = 0; i < _probes.Count(); i++)
            {
                var probe = _probes.ElementAt(i);
                var newContactIds = probe.ContactIds.Select(c => { return (int.Parse(c) - 1).ToString(); });

                for (int j = 0; j < probe.NumberOfContacts; j++)
                {
                    probe.ContactIds.SetValue(newContactIds.ElementAt(j), j);
                }
            }
        }
    }

    private void SetEmptyShankIdsIfMissing()
    {
        for (int i = 0; i < _probes.Count(); i++)
        {
            if (_probes.ElementAt(i).ShankIds == null)
            {
                _probes.ElementAt(i).ShankIds = Probe.DefaultShankIds(_probes.ElementAt(i).NumberOfContacts);
            }
        }
    }

    private void SetDefaultDeviceChannelIndicesIfMissing()
    {
        for (int i = 0; i < _probes.Count(); i++)
        {
            if (_probes.ElementAt(i).DeviceChannelIndices == null)
            {
                _probes.ElementAt(i).DeviceChannelIndices = new int[_probes.ElementAt(i).NumberOfContacts];

                for (int j = 0; j < _probes.ElementAt(i).NumberOfContacts; j++)
                {
                    if (int.TryParse(_probes.ElementAt(i).ContactIds[j], out int result))
                    {
                        _probes.ElementAt(i).DeviceChannelIndices[j] = result;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validate the uniqueness of all <see cref="Probe.DeviceChannelIndices"/>'s across all <see cref="Probe"/>'s.
    /// </summary>
    /// <remarks>
    /// All indices that are greater than or equal to 0 must be unique,
    /// but there can be as many values equal to -1 as there are contacts. A value of -1 indicates that this contact is 
    /// not being recorded.
    /// </remarks>
    /// <returns>True if all values not equal to -1 are unique, False if there are duplicates.</returns>
    public bool ValidateDeviceChannelIndices()
    {
        var activeChannels = GetDeviceChannelIndices()
                             .Where(index => index != -1);

        if (activeChannels.Count() != activeChannels.Distinct().Count())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Update the <see cref="Probe.DeviceChannelIndices"/> at the given probe index.
    /// </summary>
    /// <remarks>
    /// Device channel indices can be updated as contacts are being enabled or disabled. This is done on a 
    /// per-probe basis, where the incoming array of indices must be the same size as the original probe, 
    /// and must follow the standard for uniqueness found in <see cref="Probe.DeviceChannelIndices"/>.
    /// </remarks>
    /// <param name="probeIndex">Zero-based index of the probe to update.</param>
    /// <param name="deviceChannelIndices">Array of <see cref="Probe.DeviceChannelIndices"/>.</param>
    /// <exception cref="ArgumentException"></exception>
    public void UpdateDeviceChannelIndices(int probeIndex, int[] deviceChannelIndices)
    {
        if (_probes.ElementAt(probeIndex).DeviceChannelIndices.Length != deviceChannelIndices.Length)
        {
            throw new ArgumentException($"Incoming device channel indices have {deviceChannelIndices.Length} contacts, " +
                $"but the existing probe {probeIndex} has {_probes.ElementAt(probeIndex).DeviceChannelIndices.Length} contacts");
        }    

        _probes.ElementAt(probeIndex).DeviceChannelIndices = deviceChannelIndices;

        if (!ValidateDeviceChannelIndices())
        {
            throw new ArgumentException("Device channel indices are not valid. Ensure that all values are either -1 or are unique.");
        }
    }
}

/// <summary>
/// Class that implements the Probe Interface specification for a Probe.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
public class Probe
{
    private ProbeNdim _numDimensions;
    private ProbeSiUnits _siUnits;
    private ProbeAnnotations _annotations;
    private ContactAnnotations _contactAnnotations;
    private float[][] _contactPositions;
    private float[][][] _contactPlaneAxes;
    private ContactShape[] _contactShapes;
    private ContactShapeParam[] _contactShapeParams;
    private float[][] _probePlanarContour;
    private int[] _deviceChannelIndices;
    private string[] _contactIds;
    private string[] _shankIds;

    /// <summary>
    /// Gets the <see cref="ProbeNdim"/> to use while plotting the <see cref="Probe"/>.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("ndim", Required = Required.Always)]
    public ProbeNdim NumDimensions
    {
        get { return _numDimensions; }
        protected set { _numDimensions = value; }
    }

    /// <summary>
    /// Gets the <see cref="ProbeSiUnits"/> to use while plotting the <see cref="Probe"/>.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("si_units", Required = Required.Always)]
    public ProbeSiUnits SiUnits
    {
        get { return _siUnits; }
        protected set { _siUnits = value; }
    }

    /// <summary>
    /// Gets the <see cref="ProbeAnnotations"/> for the <see cref="Probe"/>.
    /// </summary>
    /// <remarks>
    /// Used to specify the name of the probe, and the manufacturer.
    /// </remarks>
    [XmlIgnore]
    [JsonProperty("annotations", Required = Required.Always)]
    public ProbeAnnotations Annotations
    {
        get { return _annotations; }
        protected set { _annotations = value; }
    }

    /// <summary>
    /// Gets the <see cref="ProbeInterface.ContactAnnotations"/> for the <see cref="Probe"/>.
    /// </summary>
    /// <remarks>
    /// This field can be used for noting things like where it physically is within a specimen, or if it
    /// is no longer functioning correctly.
    /// </remarks>
    [XmlIgnore]
    [JsonProperty("contact_annotations")]
    public ContactAnnotations ContactAnnotations
    {
        get { return _contactAnnotations; }
        protected set { _contactAnnotations = value; }
    }

    /// <summary>
    /// Gets the <see cref="Contact"/> positions, specifically the center point of every contact.
    /// </summary>
    /// <remarks>
    /// This is a two-dimensional array of floats; the first index is the index of the contact, and
    /// the second index is the X and Y value, respectively.
    /// </remarks>
    [XmlIgnore]
    [JsonProperty("contact_positions", Required = Required.Always)]
    public float[][] ContactPositions
    {
        get { return _contactPositions; }
        protected set { _contactPositions = value; }
    }

    /// <summary>
    /// Gets the plane axes for the contacts.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("contact_plane_axes")]
    public float[][][] ContactPlaneAxes
    {
        get { return _contactPlaneAxes; }
        protected set { _contactPlaneAxes = value; }
    }

    /// <summary>
    /// Gets the <see cref="ContactShape"/> for each contact.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("contact_shapes", Required = Required.Always)]
    public ContactShape[] ContactShapes
    {
        get { return _contactShapes; }
        protected set { _contactShapes = value; }
    }

    /// <summary>
    /// Gets the parameters of the shape for each contact.
    /// </summary>
    /// <remarks>
    /// Depending on which <see cref="ContactShape"/>
    /// is selected, not all parameters are needed; for instance, <see cref="ContactShape.Circle"/> only uses
    /// <see cref="ContactShapeParam.Radius"/>, while <see cref="ContactShape.Square"/> just uses
    /// <see cref="ContactShapeParam.Width"/>.
    /// </remarks>
    [XmlIgnore]
    [JsonProperty("contact_shape_params", Required = Required.Always)]
    public ContactShapeParam[] ContactShapeParams
    {
        get { return _contactShapeParams; }
        protected set { _contactShapeParams = value; }
    }

    /// <summary>
    /// Gets the outline of the probe that represents the physical shape.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("probe_planar_contour")]
    public float[][] ProbePlanarContour
    {
        get { return _probePlanarContour; }
        protected set { _probePlanarContour = value; }
    }

    /// <summary>
    /// Gets the indices of each channel defining their recording channel number. Must be unique, except for contacts
    /// that are set to -1 if they disabled.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("device_channel_indices")]
    public int[] DeviceChannelIndices
    {
        get { return _deviceChannelIndices; }
        internal set { _deviceChannelIndices = value; }
    }

    /// <summary>
    /// Gets the contact IDs for each channel. These do not have to be unique.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("contact_ids")]
    public string[] ContactIds
    {
        get { return _contactIds; }
        internal set { _contactIds = value; }
    }

    /// <summary>
    /// Gets the shank that each contact belongs to.
    /// </summary>
    [XmlIgnore]
    [JsonProperty("shank_ids")]
    public string[] ShankIds
    {
        get { return _shankIds; }
        internal set { _shankIds = value; }
    }

    /// <summary>
    /// Public constructor, defined as the default Json constructor.
    /// </summary>
    /// <param name="ndim"></param>
    /// <param name="si_units"></param>
    /// <param name="annotations"></param>
    /// <param name="contact_annotations"></param>
    /// <param name="contact_positions"></param>
    /// <param name="contact_plane_axes"></param>
    /// <param name="contact_shapes"></param>
    /// <param name="contact_shape_params"></param>
    /// <param name="probe_planar_contour"></param>
    /// <param name="device_channel_indices"></param>
    /// <param name="contact_ids"></param>
    /// <param name="shank_ids"></param>
    [JsonConstructor]
    public Probe(ProbeNdim ndim, ProbeSiUnits si_units, ProbeAnnotations annotations, ContactAnnotations contact_annotations,
        float[][] contact_positions, float[][][] contact_plane_axes, ContactShape[] contact_shapes,
        ContactShapeParam[] contact_shape_params, float[][] probe_planar_contour, int[] device_channel_indices,
        string[] contact_ids, string[] shank_ids)
    {
        _numDimensions = ndim;
        _siUnits = si_units;
        _annotations = annotations;
        _contactAnnotations = contact_annotations;
        _contactPositions = contact_positions;
        _contactPlaneAxes = contact_plane_axes;
        _contactShapes = contact_shapes;
        _contactShapeParams = contact_shape_params;
        _probePlanarContour = probe_planar_contour;
        _deviceChannelIndices = device_channel_indices;
        _contactIds = contact_ids;
        _shankIds = shank_ids;
    }

    /// <summary>
    /// Copy constructor given an existing <see cref="Probe"/> object.
    /// </summary>
    /// <param name="probe"></param>
    protected Probe(Probe probe)
    {
        _numDimensions = probe._numDimensions;
        _siUnits = probe._siUnits;
        _annotations = probe._annotations;
        _contactAnnotations = probe._contactAnnotations;
        _contactPositions = probe._contactPositions;
        _contactPlaneAxes = probe._contactPlaneAxes;
        _contactShapes = probe._contactShapes;
        _contactShapeParams = probe._contactShapeParams;
        _probePlanarContour = probe._probePlanarContour;
        _deviceChannelIndices = probe._deviceChannelIndices;
        _contactIds = probe._contactIds;
        _shankIds = probe._shankIds;
    }

    /// <summary>
    /// Returns default <see cref="ContactShape"/> array that contains the given number of channels and the corresponding shape.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <param name="contactShape">The <see cref="ContactShape"/> to apply to each contact.</param>
    /// <returns><see cref="ContactShape"/> array.</returns>
    public static ContactShape[] DefaultContactShapes(int numberOfContacts, ContactShape contactShape)
    {
        ContactShape[] contactShapes = new ContactShape[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactShapes[i] = contactShape;
        }

        return contactShapes;
    }

    /// <summary>
    /// Returns a default contactPlaneAxes array, with each contact given the same axis; { { 1, 0 }, { 0, 1 } }
    /// </summary>
    /// <remarks>
    /// See Probeinterface documentation for more info.
    /// </remarks>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <returns>Three-dimensional array of <see cref="float"/>s.</returns>
    public static float[][][] DefaultContactPlaneAxes(int numberOfContacts)
    {
        float[][][] contactPlaneAxes = new float[numberOfContacts][][];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactPlaneAxes[i] = new float[2][] { new float[2] { 1.0f, 0.0f }, new float[2] { 0.0f, 1.0f } };
        }

        return contactPlaneAxes;
    }

    /// <summary>
    /// Returns an array of <see cref="ContactShapeParam"/>s for a <see cref="ContactShape.Circle"/>.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <param name="radius">Radius of the contact, in units of <see cref="ProbeSiUnits"/>.</param>
    /// <returns><see cref="ContactShapeParam"/> array.</returns>
    public static ContactShapeParam[] DefaultCircleParams(int numberOfContacts, float radius)
    {
        ContactShapeParam[] contactShapeParams = new ContactShapeParam[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactShapeParams[i] = new ContactShapeParam(radius: radius);
        }

        return contactShapeParams;
    }

    /// <summary>
    /// Returns an array of <see cref="ContactShapeParam"/>s for a <see cref="ContactShape.Square"/>.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <param name="width">Width of the contact, in units of <see cref="ProbeSiUnits"/>.</param>
    /// <returns><see cref="ContactShapeParam"/> array.</returns>
    public static ContactShapeParam[] DefaultSquareParams(int numberOfContacts, float width)
    {
        ContactShapeParam[] contactShapeParams = new ContactShapeParam[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactShapeParams[i] = new ContactShapeParam(width: width);
        }

        return contactShapeParams;
    }

    /// <summary>
    /// Returns an array of <see cref="ContactShapeParam"/>s for a <see cref="ContactShape.Rect"/>.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <param name="width">Width of the contact, in units of <see cref="ProbeSiUnits"/>.</param>
    /// <param name="height">Height of the contact, in units of <see cref="ProbeSiUnits"/>.</param>
    /// <returns><see cref="ContactShapeParam"/> array.</returns>
    public static ContactShapeParam[] DefaultRectParams(int numberOfContacts, float width, float height)
    {
        ContactShapeParam[] contactShapeParams = new ContactShapeParam[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactShapeParams[i] = new ContactShapeParam(height: height);
        }

        return contactShapeParams;
    }

    /// <summary>
    /// Returns a default array of sequential <see cref="Probe.DeviceChannelIndices"/>.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <param name="offset">The first value of the <see cref="Probe.DeviceChannelIndices"/>.</param>
    /// <returns>A serially increasing array of <see cref="Probe.DeviceChannelIndices"/>.</returns>
    public static int[] DefaultDeviceChannelIndices(int numberOfContacts, int offset)
    {
        int[] deviceChannelIndices = new int[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            deviceChannelIndices[i] = i + offset;
        }

        return deviceChannelIndices;
    }

    /// <summary>
    /// Returns a sequential array of <see cref="Probe.ContactIds"/>.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <returns>Array of strings defining the <see cref="Probe.ContactIds"/>.</returns>
    public static string[] DefaultContactIds(int numberOfContacts)
    {
        string[] contactIds = new string[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactIds[i] = i.ToString();
        }

        return contactIds;
    }

    /// <summary>
    /// Returns an array of empty strings as the default shank ID.
    /// </summary>
    /// <param name="numberOfContacts">Number of contacts in a single <see cref="Probe"/>.</param>
    /// <returns>Array of empty strings.</returns>
    public static string[] DefaultShankIds(int numberOfContacts)
    {
        string[] contactIds = new string[numberOfContacts];

        for (int i = 0; i < numberOfContacts; i++)
        {
            contactIds[i] = "";
        }

        return contactIds;
    }

    /// <summary>
    /// Returns a <see cref="Contact"/> object.
    /// </summary>
    /// <param name="index">Relative index of the contact in this <see cref="Probe"/>.</param>
    /// <returns><see cref="Contact"/>.</returns>
    public Contact GetContact(int index)
    {
        return new Contact(ContactPositions[index][0], ContactPositions[index][1], ContactShapes[index], ContactShapeParams[index],
            DeviceChannelIndices[index], ContactIds[index], ShankIds[index], index);
    }

    /// <summary>
    /// Gets the number of contacts within this <see cref="Probe"/>.
    /// </summary>
    public int NumberOfContacts => ContactPositions.Length;
}

/// <summary>
/// Number of dimensions to use while plotting a <see cref="Probe"/>.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
public enum ProbeNdim
{
    /// <summary>
    /// Two-dimensions.
    /// </summary>
    [EnumMemberAttribute(Value = "2")]
    Two = 2,

    /// <summary>
    /// Three-dimensions.
    /// </summary>
    [EnumMemberAttribute(Value = "3")]
    Three = 3,
}

/// <summary>
/// SI units for all values relating to location and position.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
[JsonConverter(typeof(StringEnumConverter))]
public enum ProbeSiUnits
{
    /// <summary>
    /// Millimeters [mm].
    /// </summary>
    [EnumMemberAttribute(Value = "mm")]
    mm = 0,

    /// <summary>
    /// Micrometers [um].
    /// </summary>
    [EnumMemberAttribute(Value = "um")]
    um = 1,
}

/// <summary>
/// Struct that extends the Probeinterface specification by encapsulating all values for a single contact.
/// </summary>
public readonly struct Contact
{
    /// <summary>
    /// Gets the x-position of the contact.
    /// </summary>
    public float PosX { get; }

    /// <summary>
    /// Gets the y-position of the contact.
    /// </summary>
    public float PosY { get; }

    /// <summary>
    /// Gets the <see cref="ContactShape"/> of the contact.
    /// </summary>
    public ContactShape Shape { get; }

    /// <summary>
    /// Gets the <see cref="ContactShapeParam"/>'s of the contact.
    /// </summary>
    public ContactShapeParam ShapeParams { get; }

    /// <summary>
    /// Gets the device ID of the contact.
    /// </summary>
    public int DeviceId { get; }

    /// <summary>
    /// Gets the contact ID of the contact.
    /// </summary>
    public string ContactId { get; }

    /// <summary>
    /// Gets the shank ID of the contact.
    /// </summary>
    public string ShankId { get; }

    /// <summary>
    /// Gets the index of the contact within the <see cref="Probe"/> object.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Contact"/> struct.
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    /// <param name="shape"></param>
    /// <param name="shapeParam"></param>
    /// <param name="device_id"></param>
    /// <param name="contact_id"></param>
    /// <param name="shank_id"></param>
    /// <param name="index"></param>
    public Contact(float posX, float posY, ContactShape shape, ContactShapeParam shapeParam,
        int device_id, string contact_id, string shank_id, int index)
    {
        PosX = posX;
        PosY = posY;
        Shape = shape;
        ShapeParams = shapeParam;
        DeviceId = device_id;
        ContactId = contact_id;
        ShankId = shank_id;
        Index = index;
    }
}

/// <summary>
/// Class holding parameters used to draw the contact.
/// </summary>
/// <remarks>
/// Fields are nullable, since not all fields are required depending on the shape selected.
/// </remarks>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
public class ContactShapeParam
{
    private float? _radius;
    private float? _width;
    private float? _height;

    /// <summary>
    /// Gets the radius of the contact.
    /// </summary>
    /// <remarks>
    /// This is only used to draw <see cref="ContactShape.Circle"/> contacts. Field can be null.
    /// </remarks>
    public float? Radius
    {
        get { return _radius; }
        protected set { _radius = value; }
    }

    /// <summary>
    /// Gets the width of the contact.
    /// </summary>
    /// <remarks>
    /// This is used to draw <see cref="ContactShape.Square"/> or <see cref="ContactShape.Rect"/> contacts.
    /// Field can be null.
    /// </remarks>
    public float? Width
    {
        get { return _width; }
        protected set { _width = value; }
    }

    /// <summary>
    /// Gets the height of the contact.
    /// </summary>
    /// <remarks>
    /// This is only used to draw <see cref="ContactShape.Rect"/> contacts. Field can be null.
    /// </remarks>
    public float? Height
    {
        get { return _height; }
        protected set { _height = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactShapeParam"/> class.
    /// </summary>
    /// <param name="radius">Radius. Can be null.</param>
    /// <param name="width">Width. Can be null.</param>
    /// <param name="height">Height. Can be null.</param>
    [JsonConstructor]
    public ContactShapeParam(float? radius = null, float? width = null, float? height = null)
    {
        _radius = radius;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Copy constructor given an existing <see cref="ContactShapeParam"/> object.
    /// </summary>
    /// <param name="shape"></param>
    protected ContactShapeParam(ContactShapeParam shape)
    {
        _radius = shape._radius;
        _width = shape._width;
        _height = shape._height;
    }
}

/// <summary>
/// Shape of the contact.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
[JsonConverter(typeof(StringEnumConverter))]
public enum ContactShape
{
    /// <summary>
    /// Circle.
    /// </summary>
    [EnumMemberAttribute(Value = "circle")]
    Circle = 0,

    /// <summary>
    /// Rectangle.
    /// </summary>
    [EnumMemberAttribute(Value = "rect")]
    Rect = 1,

    /// <summary>
    /// Square.
    /// </summary>
    [EnumMemberAttribute(Value = "square")]
    Square = 2,
}

/// <summary>
/// Class holding the <see cref="Probe"/> annotations.
/// </summary>
[GeneratedCodeAttribute("Bonsai.Sgen", "0.3.0.0 (Newtonsoft.Json v13.0.0.0)")]
public class ProbeAnnotations
{
    private string _name;
    private string _manufacturer;

    /// <summary>
    /// Gets the name of the probe as defined by the manufacturer, or a descriptive name such as the neurological target.
    /// </summary>
    [JsonProperty("name")]
    public string Name
    {
        get { return _name; }
        protected set { _name = value; }
    }

    /// <summary>
    /// Gets the name of the manufacturer who created the probe.
    /// </summary>
    [JsonProperty("manufacturer")]
    public string Manufacturer
    {
        get { return _manufacturer; }
        protected set { _manufacturer = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeAnnotations"/> class.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="manufacturer"></param>
    [JsonConstructor]
    public ProbeAnnotations(string name, string manufacturer)
    {
        _name = name;
        _manufacturer = manufacturer;
    }

    /// <summary>
    /// Copy constructor that copies data from an existing <see cref="ProbeAnnotations"/> object.
    /// </summary>
    /// <param name="probeAnnotations"></param>
    protected ProbeAnnotations(ProbeAnnotations probeAnnotations)
    {
        _name = probeAnnotations._name;
        _manufacturer = probeAnnotations._manufacturer;
    }
}

/// <summary>
/// Class holding all of the annotations for each contact.
/// </summary>
public class ContactAnnotations
{
    private string[] _contactAnnotations;

    /// <summary>
    /// Gets the array of strings holding annotations for each contact. Not all indices must have annotations.
    /// </summary>
    public string[] Annotations
    {
        get { return _contactAnnotations; }
        protected set { _contactAnnotations = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactAnnotations"/> class.
    /// </summary>
    /// <param name="contactAnnotations"></param>
    [JsonConstructor]
    public ContactAnnotations(string[] contactAnnotations)
    {
        _contactAnnotations = contactAnnotations;
    }
}
