using System.Collections.Generic;
using System.Text;
using System.Xml;
using SharpDX;

namespace CodeWalker.GameFiles
{
    public class WaypointRecordList : ResourceFileBase
    {
        public ResourceSimpleArray<WaypointRecordEntry> Entries;
        public uint EntriesCount;
        public ulong EntriesPointer;

        public uint Unknown_10h; // 0x00000000
        public uint Unknown_14h; // 0x00000000
        public uint Unknown_24h; // 0x00000000
        public uint Unknown_28h; // 0x00000000
        public uint Unknown_2Ch; // 0x00000000
        public override long BlockLength => 0x30;

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            Unknown_10h = reader.ReadUInt32();
            Unknown_14h = reader.ReadUInt32();
            EntriesPointer = reader.ReadUInt64();
            EntriesCount = reader.ReadUInt32();
            Unknown_24h = reader.ReadUInt32();
            Unknown_28h = reader.ReadUInt32();
            Unknown_2Ch = reader.ReadUInt32();

            Entries = reader.ReadBlockAt<ResourceSimpleArray<WaypointRecordEntry>>(
                EntriesPointer, // offset
                EntriesCount
            );
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            EntriesPointer = (ulong)(Entries?.FilePosition ?? 0);
            EntriesCount = (uint)(Entries?.Count ?? 0);

            // write structure data
            writer.Write(Unknown_10h);
            writer.Write(Unknown_14h);
            writer.Write(EntriesPointer);
            writer.Write(EntriesCount);
            writer.Write(Unknown_24h);
            writer.Write(Unknown_28h);
            writer.Write(Unknown_2Ch);
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            if (Entries?.Data != null)
                foreach (WaypointRecordEntry e in Entries.Data)
                {
                    YwrXml.OpenTag(sb, indent, "Item");
                    e.WriteXml(sb, indent + 1);
                    YwrXml.CloseTag(sb, indent, "Item");
                }
        }

        public void ReadXml(XmlNode node)
        {
            List<WaypointRecordEntry> entries = new List<WaypointRecordEntry>();

            XmlNodeList inodes = node.SelectNodes("Item");
            if (inodes != null)
                foreach (XmlNode inode in inodes)
                {
                    WaypointRecordEntry e = new WaypointRecordEntry();
                    e.ReadXml(inode);
                    entries.Add(e);
                }

            Entries = new ResourceSimpleArray<WaypointRecordEntry>();
            Entries.Data = entries;
        }

        public static void WriteXmlNode(WaypointRecordList l, StringBuilder sb, int indent,
            string name = "WaypointRecordList")
        {
            if (l == null) return;
            if (l.Entries?.Data == null || l.Entries.Data.Count == 0)
            {
                YwrXml.SelfClosingTag(sb, indent, name);
            }
            else
            {
                YwrXml.OpenTag(sb, indent, name);
                l.WriteXml(sb, indent + 1);
                YwrXml.CloseTag(sb, indent, name);
            }
        }

        public static WaypointRecordList ReadXmlNode(XmlNode node)
        {
            if (node == null) return null;
            WaypointRecordList l = new WaypointRecordList();
            l.ReadXml(node);
            return l;
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>(base.GetReferences());
            if (Entries != null) list.Add(Entries);
            return list.ToArray();
        }
    }


    public class WaypointRecordEntry : ResourceSystemBlock
    {
        public Vector3 Position;
        public ushort Unk0;
        public ushort Unk1;
        public ushort Unk2;
        public ushort Unk3;
        public override long BlockLength => 20;

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            Position = reader.ReadVector3();
            Unk0 = reader.ReadUInt16();
            Unk1 = reader.ReadUInt16();
            Unk2 = reader.ReadUInt16();
            Unk3 = reader.ReadUInt16();
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // write structure data
            writer.Write(Position);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(Unk2);
            writer.Write(Unk3);
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YwrXml.SelfClosingTag(sb, indent, "Position " + FloatUtil.GetVector3XmlString(Position));
            YwrXml.ValueTag(sb, indent, "Unk0", Unk0.ToString());
            YwrXml.ValueTag(sb, indent, "Unk1", Unk1.ToString());
            YwrXml.ValueTag(sb, indent, "Unk2", Unk2.ToString());
            YwrXml.ValueTag(sb, indent, "Unk3", Unk3.ToString());
        }

        public void ReadXml(XmlNode node)
        {
            Position = Xml.GetChildVector3Attributes(node, "Position");
            Unk0 = (ushort)Xml.GetChildUIntAttribute(node, "Unk0");
            Unk1 = (ushort)Xml.GetChildUIntAttribute(node, "Unk1");
            Unk2 = (ushort)Xml.GetChildUIntAttribute(node, "Unk2");
            Unk3 = (ushort)Xml.GetChildUIntAttribute(node, "Unk3");
        }
    }
}