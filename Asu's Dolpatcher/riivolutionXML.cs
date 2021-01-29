using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Asu_s_Dolpatcher
{
    public class riivolutionXML
    {
        [XmlAttribute(AttributeName = "version")]
        public int version { get; set; }

        [XmlAttribute(AttributeName = "shiftfiles")]
        public bool shiftfiles { get; set; }

        [XmlAttribute(AttributeName = "root")]
        public string root { get; set; }

        [XmlAttribute(AttributeName = "log")]
        public bool log { get; set; }


        [XmlElement(ElementName = "id")]
        public riivolutionID id { get; set; }

        [XmlElement(ElementName = "options")]
        public riivolutionOptions options { get; set; }

        [XmlElement(ElementName = "patch")]
        public riivolutionPatch[] patch { get; set; }

        public static riivolutionXML load(string xmlPath)
        {
            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = "wiidisc";
            xRoot.IsNullable = true;

            XmlSerializer serializer = new XmlSerializer(typeof(riivolutionXML), xRoot);
            MemoryStream stream = new MemoryStream(File.ReadAllBytes(xmlPath));
            return (riivolutionXML)serializer.Deserialize(stream);
        }
        public int findPatchIndexByName(string name)
        {
            for (int i = 0; i < patch.Length; i++)
            {
                if (patch[i].id == name)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public class riivolutionID
    {
        [XmlAttribute(AttributeName = "game")]
        public string game { get; set; }


        [XmlElement(ElementName = "region")]
        public riivolutionIDRegion[] region { get; set; }
    }

    public class riivolutionIDRegion
    {
        [XmlAttribute(AttributeName = "type")]
        public string type { get; set; }
    }

    public class riivolutionOptions
    {
        [XmlElement(ElementName = "section")]
        public riivolutionOptionsSection[] section { get; set; }
    }

    public class riivolutionOptionsSection
    {
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }

        [XmlElement(ElementName = "option")]
        public riivolutionOptionsSectionOption[] option { get; set; }
    }

    public class riivolutionOptionsSectionOption
    {
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "default")]
        public int defaultValue { get; set; }

        [XmlElement(ElementName = "choice")]
        public riivolutionOptionsSectionOptionChoice[] choice { get; set; }

    }

    public class riivolutionOptionsSectionOptionChoice
    {
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }

        [XmlElement(ElementName = "patch")]
        public riivolutionOptionsSectionOptionChoicePatch[] patch { get; set; }
    }

    public class riivolutionOptionsSectionOptionChoicePatch
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }
    }

    public class riivolutionPatch
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [XmlElement(ElementName = "savegame")]
        public riivolutionPatchSavegame[] savegame { get; set; }

        [XmlElement(ElementName = "folder")]
        public riivolutionPatchFolder[] folder { get; set; }

        [XmlElement(ElementName = "memory")]
        public riivolutionPatchMemory[] memory { get; set; }
    }

    public class riivolutionPatchSavegame
    {
        [XmlAttribute(AttributeName = "external")]
        public string external { get; set; }

        [XmlAttribute(AttributeName = "clone")]
        public bool clone { get; set; }
    }

    public class riivolutionPatchFolder
    {
        [XmlAttribute(AttributeName = "external")]
        public string external { get; set; }

        [XmlAttribute(AttributeName = "disc")]
        public string disc { get; set; }

        [XmlAttribute(AttributeName = "create")]
        public bool create { get; set; }
    }

    public class riivolutionPatchMemory
    {
        [XmlAttribute(AttributeName = "offset")]
        public string offset { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string value { get; set; }

        [XmlAttribute(AttributeName = "original")]
        public string original { get; set; }

        [XmlAttribute(AttributeName = "target")]
        public string target { get; set; }

        [XmlAttribute(AttributeName = "valuefile")]
        public string valuefile { get; set; }
    }

}
