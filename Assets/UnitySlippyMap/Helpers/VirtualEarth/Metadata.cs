using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace UnitySlippyMap.VirtualEarth
{
    [XmlRoot("Response")]
    public class Metadata
    {
        private string copyright;
        [XmlElement("Copyright")]
        public string Copyright { get { return copyright; } set { copyright = value; } }

        private string brandLogoUri;
        [XmlElement("BrandLogoUri")]
        public string BrandLogoUri { get { return brandLogoUri; } set { brandLogoUri = value; } }

        private int statusCode;
        [XmlElement("StatusCode")]
        public int StatusCode { get { return statusCode; } set { statusCode = value; } }

        private string statusDescription;
        [XmlElement("StatusDescription")]
        public string StatusDescription { get { return statusDescription; } set { statusDescription = value; } }

        private string authenticationResultCode;
        [XmlElement("AuthenticationResultCode")]
        public string AuthenticationResultCode { get { return authenticationResultCode; } set { authenticationResultCode = value; } }

        private string traceId;
        [XmlElement("TraceId")]
        public string TraceId { get { return traceId; } set { traceId = value; } }


        /*
        private ResourceSets resourceSets;
        [XmlElement("ResourceSets")]
        public ResourceSets ResourceSets { get { return resourceSets; } }
         */

        private List<ResourceSet> resourceSets;
        [XmlArray("ResourceSets")]
        [XmlArrayItem("ResourceSet")]
        public List<ResourceSet> ResourceSets { get { return resourceSets; } set { resourceSets = value; } }
    }

    /*
    [XmlRoot("ResourceSets")]
    public class ResourceSets
    {
        private List<ResourceSet> resourceSetList;
        [XmlElement("ResourceSet")]
        public List<ResourceSet> ResourceSetList { get { return resourceSetList; } }
    }
    */
    [XmlRoot("ResourceSet")]
    public class ResourceSet
    {
        private int estimatedTotal;
        [XmlElement("EstimatedTotal")]
        public int EstimatedTotal { get { return estimatedTotal; } set { estimatedTotal = value; } }

        private List<ImageryMetadata> resources;
        [XmlArray("Resources")]
        [XmlArrayItem("ImageryMetadata"/*, Type = typeof(ResourceXmlSerializer<Resource>)*/)] // FIXME: using an abstract type crashes with 'mscorlib : failed to convert parameters at System.Reflection.MonoMethod.Invoke' ; also, specificaly naming the array item as ImageryMetadata kinda defeats the purpose
        public List<ImageryMetadata> Resources { get { return resources; } set { resources = value; } }
    }

    public abstract class Resource
    {
    }

    [XmlRoot("ImageryMetadata")]
    public class ImageryMetadata : Resource
    {
        private string imageUrl;
        [XmlElement("ImageUrl")]
        public string ImageUrl { get { return imageUrl; } set { imageUrl = value; } }

        private List<string> imageUrlSubdomains;
        [XmlArray("ImageUrlSubdomains")]
        [XmlArrayItem("string")]
        public List<string> ImageUrlSubdomains { get { return imageUrlSubdomains; } set { imageUrlSubdomains = value; } }

        private int imageWidth;
        [XmlElement("ImageWidth")]
        public int ImageWidth { get { return imageWidth; } set { imageWidth = value; } }

        private int imageHeight;
        [XmlElement("ImageHeight")]
        public int ImageHeight { get { return imageHeight; } set { imageHeight = value; } }

        private int zoomMin;
        [XmlElement("ZoomMin")]
        public int ZoomMin { get { return zoomMin; } set { zoomMin = value; } }

        private int zoomMax;
        [XmlElement("ZoomMax")]
        public int ZoomMax { get { return zoomMax; } set { zoomMax = value; } }
    }

    /*
    // http://stackoverflow.com/questions/20084/xml-serialization-and-inherited-types
    public class ResourceXmlSerializer<T> : IXmlSerializable
    {
        // Override the Implicit Conversions Since the XmlSerializer
        // Casts to/from the required types implicitly.
        public static implicit operator T(ResourceXmlSerializer<T> o)
        {
            return o.Data;
        }

        public static implicit operator ResourceXmlSerializer<T>(T o)
        {
            return o == null ? null : new ResourceXmlSerializer<T>(o);
        }

        private T _data;
        /// <summary>
        /// [Concrete] Data to be stored/is stored as XML.
        /// </summary>
        public T Data
        {
            get { return _data; }
            set { _data = value; }
        }

        /// <summary>
        /// **DO NOT USE** This is only added to enable XML Serialization.
        /// </summary>
        /// <remarks>DO NOT USE THIS CONSTRUCTOR</remarks>
        public ResourceXmlSerializer()
        {
            // Default Ctor (Required for Xml Serialization - DO NOT USE)
        }

        /// <summary>
        /// Initialises the Serializer to work with the given data.
        /// </summary>
        /// <param name="data">Concrete Object of the T Specified.</param>
        public ResourceXmlSerializer(T data)
        {
            _data = data;
        }

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null; // this is fine as schema is unknown.
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            Type type = Type.GetType("UnitySlippyMap.VirtualEarth." + reader.Name);

            // Check the Type is Found.
            if (type == null)
                throw new InvalidCastException("Unable to Read Xml Data for Abstract Type '" + typeof(T).Name +
                    "' because the type '" + reader.Name + "' specified in the XML was not found.");

            // Check the Type is a Subclass of the T.
            if (!type.IsSubclassOf(typeof(T)))
                throw new InvalidCastException("Unable to Read Xml Data for Abstract Type '" + typeof(T).Name +
                    "' because the Type specified in the XML differs ('" + type.Name + "').");

            // Read the Data, Deserializing based on the (now known) concrete type.
            //reader.ReadStartElement();
            Debug.Log("TEST: " + reader.Name + " " + reader.NamespaceURI);
            this.Data = (T)new
                XmlSerializer(type, reader.NamespaceURI).Deserialize(reader);
            //reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            // Write the Type Name to the XML Element as an Attrib and Serialize
            Type type = _data.GetType();

            // BugFix: Assembly must be FQN since Types can/are external to current.
            writer.WriteAttributeString("type", type.AssemblyQualifiedName);
            new XmlSerializer(type).Serialize(writer, _data);
        }

        #endregion
    }
     */
}
