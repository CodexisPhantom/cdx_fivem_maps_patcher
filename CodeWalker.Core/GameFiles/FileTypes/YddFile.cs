using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CodeWalker.GameFiles
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class YddFile : GameFile, PackedFile
    {
        public YddFile() : base(null, GameFileType.Ydd)
        {
        }

        public YddFile(RpfFileEntry entry) : base(entry, GameFileType.Ydd)
        {
        }

        public DrawableDictionary DrawableDict { get; set; }

        public Dictionary<uint, Drawable> Dict { get; set; }
        public Drawable[] Drawables { get; set; }

        public void Load(byte[] data, RpfFileEntry entry)
        {
            Name = entry.Name;
            RpfFileEntry = entry;


            RpfResourceFileEntry resentry = entry as RpfResourceFileEntry;
            if (resentry == null) throw new Exception("File entry wasn't a resource! (is it binary data?)");

            ResourceDataReader rd = new ResourceDataReader(resentry, data);

            if (rd.IsGen9)
                switch (resentry.Version)
                {
                    case 159:
                        break;
                    case 165:
                        rd.IsGen9 = false;
                        break;
                }


            DrawableDict = rd.ReadBlock<DrawableDictionary>();

            //MemoryUsage = 0; //uses decompressed filesize now...
            //if (DrawableDict != null)
            //{
            //    MemoryUsage += DrawableDict.MemoryUsage;
            //}

            if (DrawableDict != null &&
                DrawableDict.Drawables != null &&
                DrawableDict.Drawables.data_items != null &&
                DrawableDict.Hashes != null)
            {
                Dict = new Dictionary<uint, Drawable>();
                Drawable[] drawables = DrawableDict.Drawables.data_items;
                uint[] hashes = DrawableDict.Hashes;
                for (int i = 0; i < drawables.Length && i < hashes.Length; i++)
                {
                    Drawable drawable = drawables[i];
                    uint hash = hashes[i];
                    Dict[hash] = drawable;
                    drawable.Owner = this;
                }


                for (int i = 0; i < drawables.Length && i < hashes.Length; i++)
                {
                    Drawable drawable = drawables[i];
                    uint hash = hashes[i];
                    if (drawable.Name == null || JenkHash.GenHash(drawable.Name) != hash)
                        drawable.Name = YddXml.HashString((MetaHash)hash);
                }

                Drawables = Dict.Values.ToArray();
            }

            Loaded = true;
        }

        public void Load(byte[] data)
        {
            //direct load from a raw, compressed ydd file

            RpfFile.LoadResourceFile(this, data, (uint)GetVersion(RpfManager.IsGen9));

            Loaded = true;
        }

        public byte[] Save()
        {
            Drawable[] drawables = DrawableDict?.Drawables?.data_items;
            bool gen9 = RpfManager.IsGen9;
            if (gen9 && drawables != null)
                foreach (Drawable drawable in drawables)
                    drawable?.EnsureGen9();

            byte[] data = ResourceBuilder.Build(DrawableDict, GetVersion(gen9), true, gen9);

            return data;
        }

        public int GetVersion(bool gen9)
        {
            return gen9 ? 159 : 165;
        }
    }


    public class YddXml : MetaXmlBase
    {
        public static string GetXml(YddFile ydd, string outputFolder = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(XmlHeader);

            if (ydd?.DrawableDict != null) DrawableDictionary.WriteXmlNode(ydd.DrawableDict, sb, 0, outputFolder);

            return sb.ToString();
        }
    }

    public class XmlYdd
    {
        public static YddFile GetYdd(string xml, string inputFolder = "")
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return GetYdd(doc, inputFolder);
        }

        public static YddFile GetYdd(XmlDocument doc, string inputFolder = "")
        {
            YddFile r = new YddFile();

            string ddsfolder = inputFolder;

            XmlElement node = doc.DocumentElement;
            if (node != null) r.DrawableDict = DrawableDictionary.ReadXmlNode(node, ddsfolder);

            r.Name = Path.GetFileName(inputFolder);

            return r;
        }
    }
}