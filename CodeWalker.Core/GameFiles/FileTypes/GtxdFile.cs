using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;

namespace CodeWalker.GameFiles
{
    [TypeConverter(typeof(ExpandableObjectConverter))] public class GtxdFile : GameFile, PackedFile
    {

        public RbfFile Rbf { get; set; }


        public Dictionary<string, string> TxdRelationships { get; set; }




        public GtxdFile() : base(null, GameFileType.Gtxd)
        {
        }
        public GtxdFile(RpfFileEntry entry) : base(entry, GameFileType.Gtxd)
        {
        }



        public void Load(byte[] data, RpfFileEntry entry)
        {
            RpfFileEntry = entry;
            Name = entry.Name;
            FilePath = Name;


            if (entry.NameLower.EndsWith(".ymt"))
            {
                MemoryStream ms = new MemoryStream(data);
                if (RbfFile.IsRBF(ms))
                {
                    Rbf = new RbfFile();
                    RbfStructure rbfstruct = Rbf.Load(ms);

                    if (rbfstruct.Name == "CMapParentTxds")
                    {
                        LoadTxdRelationships(rbfstruct);
                    }

                    Loaded = true;
                    return;
                }
                else
                {
                    //not an RBF file...
                }
            }
            else if (entry.NameLower.EndsWith(".meta"))
            {
                //required for update\x64\dlcpacks\mpheist\dlc.rpf\common\data\gtxd.meta and update\x64\dlcpacks\mpluxe\dlc.rpf\common\data\gtxd.meta
                string xml = TextUtil.GetUTF8Text(data);

                LoadTxdRelationships(xml);
                Loaded = true;
            }


        }


        private void LoadTxdRelationships(RbfStructure rbfstruct)
        {

            TxdRelationships = new Dictionary<string, string>();
            //StringBuilder sblist = new StringBuilder();
            foreach (IRbfType child in rbfstruct.Children)
            {
                RbfStructure childstruct = child as RbfStructure;
                if ((childstruct != null) && (childstruct.Name == "txdRelationships"))
                {
                    foreach (IRbfType txdrel in childstruct.Children)
                    {
                        RbfStructure txdrelstruct = txdrel as RbfStructure;
                        if ((txdrelstruct != null) && (txdrelstruct.Name == "item"))
                        {
                            string parentstr = string.Empty;
                            string childstr = string.Empty;
                            foreach (IRbfType item in txdrelstruct.Children)
                            {
                                RbfStructure itemstruct = item as RbfStructure;
                                if ((itemstruct != null))
                                {
                                    RbfBytes strbytes = itemstruct.Children[0] as RbfBytes;
                                    string thisstr = string.Empty;
                                    if (strbytes != null)
                                    {
                                        thisstr = Encoding.ASCII.GetString(strbytes.Value).Replace("\0", "");
                                    }
                                    switch (item.Name)
                                    {
                                        case "parent":
                                            parentstr = thisstr;
                                            break;
                                        case "child":
                                            childstr = thisstr;
                                            break;
                                    }
                                }

                            }
                            if ((!string.IsNullOrEmpty(parentstr)) && (!string.IsNullOrEmpty(childstr)))
                            {
                                if (!TxdRelationships.ContainsKey(childstr))
                                {
                                    TxdRelationships.Add(childstr, parentstr);
                                }
                                else
                                {
                                }
                                //sblist.AppendLine(childstr + ": " + parentstr);
                            }
                        }
                    }
                }
            }
            //string alltxdmap = sblist.ToString();
            //if (!string.IsNullOrEmpty(alltxdmap))
            //{
            //}

        }



        private void LoadTxdRelationships(string xml)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(xml); //maybe better load xml.ToLower() and use "cmapparenttxds/txdrelationships/item" as xpath?
            XmlNodeList items = xmldoc.SelectNodes("CMapParentTxds/txdRelationships/Item | CMapParentTxds/txdRelationships/item");

            TxdRelationships = new Dictionary<string, string>();
            for (int i = 0; i < items.Count; i++)
            {
                string parentstr = Xml.GetChildInnerText(items[i], "parent");
                string childstr = Xml.GetChildInnerText(items[i], "child");

                if ((!string.IsNullOrEmpty(parentstr)) && (!string.IsNullOrEmpty(childstr)))
                {
                    if (!TxdRelationships.ContainsKey(childstr))
                    {
                        TxdRelationships.Add(childstr, parentstr);
                    }
                }
            }
        }

    }
}
