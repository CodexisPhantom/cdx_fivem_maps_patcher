using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using SharpDX;

namespace CodeWalker.GameFiles
{
    public class XmlMeta
    {


        public static byte[] GetData(XmlDocument doc, MetaFormat mformat, string fpathin)
        {
            switch (mformat)
            {
                case MetaFormat.RSC:
                    return GetRSCData(doc);
                case MetaFormat.PSO:
                    return GetPSOData(doc);
                case MetaFormat.RBF:
                    return GetRBFData(doc);
                case MetaFormat.AudioRel:
                    return GetRelData(doc);
                case MetaFormat.Ynd:
                    return GetYndData(doc);
                case MetaFormat.Ynv:
                    return GetYnvData(doc);
                case MetaFormat.Ycd:
                    return GetYcdData(doc);
                case MetaFormat.Ybn:
                    return GetYbnData(doc);
                case MetaFormat.Ytd:
                    return GetYtdData(doc, fpathin);
                case MetaFormat.Ydr:
                    return GetYdrData(doc, fpathin);
                case MetaFormat.Ydd:
                    return GetYddData(doc, fpathin);
                case MetaFormat.Yft:
                    return GetYftData(doc, fpathin);
                case MetaFormat.Ypt:
                    return GetYptData(doc, fpathin);
                case MetaFormat.Yld:
                    return GetYldData(doc, fpathin);
                case MetaFormat.Yed:
                    return GetYedData(doc, fpathin);
                case MetaFormat.Ywr:
                    return GetYwrData(doc, fpathin);
                case MetaFormat.Yvr:
                    return GetYvrData(doc, fpathin);
                case MetaFormat.Awc:
                    return GetAwcData(doc, fpathin);
                case MetaFormat.Fxc:
                    return GetFxcData(doc, fpathin);
                case MetaFormat.CacheFile:
                    return GetCacheFileData(doc);
                case MetaFormat.Heightmap:
                    return GetHeightmapData(doc);
                case MetaFormat.Ypdb:
                    return GetYpdbData(doc);
                case MetaFormat.Yfd:
                    return GetYfdData(doc);
                case MetaFormat.Mrf:
                    return GetMrfData(doc);
            }
            return null;
        }
        public static byte[] GetRSCData(XmlDocument doc)
        {
            Meta meta = GetMeta(doc);
            if ((meta.DataBlocks?.Data == null) || (meta.DataBlocks.Count == 0)) return null;
            return ResourceBuilder.Build(meta, 2); //meta is RSC V:2
        }
        public static byte[] GetPSOData(XmlDocument doc)
        {
            PsoFile pso = XmlPso.GetPso(doc);
            if ((pso.DataSection == null) || (pso.DataMapSection == null) || (pso.SchemaSection == null)) return null;
            return pso.Save();
        }
        public static byte[] GetRBFData(XmlDocument doc)
        {
            RbfFile rbf = XmlRbf.GetRbf(doc);
            if (rbf.current == null) return null;
            return rbf.Save();
        }
        public static byte[] GetRelData(XmlDocument doc)
        {
            RelFile rel = XmlRel.GetRel(doc);
            if ((rel.RelDatasSorted == null) || (rel.RelDatas == null)) return null;
            return rel.Save();
        }
        public static byte[] GetYndData(XmlDocument doc)
        {
            YndFile ynd = XmlYnd.GetYnd(doc);
            if (ynd.NodeDictionary == null) return null;
            return ynd.Save();
        }
        public static byte[] GetYnvData(XmlDocument doc)
        {
            YnvFile ynv = XmlYnv.GetYnv(doc);
            if (ynv.Nav == null) return null;
            return ynv.Save();
        }
        public static byte[] GetYcdData(XmlDocument doc)
        {
            YcdFile ycd = XmlYcd.GetYcd(doc);
            if (ycd.ClipDictionary == null) return null;
            return ycd.Save();
        }
        public static byte[] GetYbnData(XmlDocument doc)
        {
            YbnFile ybn = XmlYbn.GetYbn(doc);
            if (ybn.Bounds == null) return null;
            return ybn.Save();
        }
        public static byte[] GetYtdData(XmlDocument doc, string fpathin)
        {
            YtdFile ytd = XmlYtd.GetYtd(doc, fpathin);
            if (ytd.TextureDict == null) return null;
            return ytd.Save();
        }
        public static byte[] GetYdrData(XmlDocument doc, string fpathin)
        {
            YdrFile ydr = XmlYdr.GetYdr(doc, fpathin);
            if (ydr.Drawable == null) return null;
            return ydr.Save();
        }
        public static byte[] GetYddData(XmlDocument doc, string fpathin)
        {
            YddFile ydd = XmlYdd.GetYdd(doc, fpathin);
            if (ydd.DrawableDict == null) return null;
            return ydd.Save();
        }
        public static byte[] GetYftData(XmlDocument doc, string fpathin)
        {
            YftFile yft = XmlYft.GetYft(doc, fpathin);
            if (yft.Fragment == null) return null;
            return yft.Save();
        }
        public static byte[] GetYptData(XmlDocument doc, string fpathin)
        {
            YptFile ypt = XmlYpt.GetYpt(doc, fpathin);
            if (ypt.PtfxList == null) return null;
            return ypt.Save();
        }
        public static byte[] GetYldData(XmlDocument doc, string fpathin)
        {
            YldFile yld = XmlYld.GetYld(doc, fpathin);
            if (yld.ClothDictionary == null) return null;
            return yld.Save();
        }
        public static byte[] GetYedData(XmlDocument doc, string fpathin)
        {
            YedFile yed = XmlYed.GetYed(doc, fpathin);
            if (yed.ExpressionDictionary == null) return null;
            return yed.Save();
        }
        public static byte[] GetYwrData(XmlDocument doc, string fpathin)
        {
            YwrFile ywr = XmlYwr.GetYwr(doc, fpathin);
            if (ywr.Waypoints == null) return null;
            return ywr.Save();
        }
        public static byte[] GetYvrData(XmlDocument doc, string fpathin)
        {
            YvrFile yvr = XmlYvr.GetYvr(doc, fpathin);
            if (yvr.Records == null) return null;
            return yvr.Save();
        }
        public static byte[] GetAwcData(XmlDocument doc, string fpathin)
        {
            AwcFile awc = XmlAwc.GetAwc(doc, fpathin);
            if (awc.Streams == null) return null;
            return awc.Save();
        }
        public static byte[] GetFxcData(XmlDocument doc, string fpathin)
        {
            FxcFile fxc = XmlFxc.GetFxc(doc, fpathin);
            if (fxc.Shaders == null) return null;
            return fxc.Save();
        }
        public static byte[] GetCacheFileData(XmlDocument doc)
        {
            CacheDatFile cdf = XmlCacheDat.GetCacheDat(doc);
            if (cdf == null) return null;
            return cdf.Save();
        }
        public static byte[] GetHeightmapData(XmlDocument doc)
        {
            HeightmapFile hmf = XmlHmap.GetHeightmap(doc);
            if (hmf.MaxHeights == null) return null;
            return hmf.Save();
        }
        public static byte[] GetYpdbData(XmlDocument doc)
        {
            YpdbFile ypdb = XmlYpdb.GetYpdb(doc);
            if (ypdb.WeightSet == null) return null;
            return ypdb.Save();
        }
        public static byte[] GetYfdData(XmlDocument doc)
        {
            YfdFile yfd = XmlYfd.GetYfd(doc);
            if (yfd.FrameFilterDictionary == null) return null;
            return yfd.Save();
        }
        public static byte[] GetMrfData(XmlDocument doc)
        {
            MrfFile mrf = XmlMrf.GetMrf(doc);
            if (mrf == null) return null;
            return mrf.Save();
        }


        public static string GetXMLFormatName(MetaFormat mformat)
        {
            switch (mformat)
            {
                case MetaFormat.RSC: return "Meta XML";
                case MetaFormat.PSO: return "PSO XML";
                case MetaFormat.RBF: return "RBF XML";
                case MetaFormat.AudioRel: return "REL XML";
                case MetaFormat.Ynd: return "YND XML";
                case MetaFormat.Ynv: return "YNV XML";
                case MetaFormat.Ycd: return "YCD XML";
                case MetaFormat.Ybn: return "YBN XML";
                case MetaFormat.Ytd: return "YTD XML";
                case MetaFormat.Ydr: return "YDR XML";
                case MetaFormat.Ydd: return "YDD XML";
                case MetaFormat.Yft: return "YFT XML";
                case MetaFormat.Ypt: return "YPT XML";
                case MetaFormat.Yld: return "YLD XML";
                case MetaFormat.Yed: return "YED XML";
                case MetaFormat.Ywr: return "YWR XML";
                case MetaFormat.Yvr: return "YVR XML";
                case MetaFormat.Awc: return "AWC XML";
                case MetaFormat.Fxc: return "FXC XML";
                case MetaFormat.CacheFile: return "CacheFile XML";
                case MetaFormat.Heightmap: return "Heightmap XML";
                case MetaFormat.Ypdb: return "YPDB XML";
                case MetaFormat.Mrf: return "MRF XML";
                case MetaFormat.Yfd: return "YFD XML";
                default: return "XML";
            }
        }
        public static MetaFormat GetXMLFormat(string fnamel, out int trimlength)
        {
            MetaFormat mformat = MetaFormat.RSC;
            trimlength = 4;

            if (!fnamel.EndsWith(".xml"))
            {
                mformat = MetaFormat.XML;//not really correct, but have to return something...
            }
            if (fnamel.EndsWith(".pso.xml"))
            {
                mformat = MetaFormat.PSO;
                trimlength = 8;
            }
            if (fnamel.EndsWith(".rbf.xml"))
            {
                mformat = MetaFormat.RBF;
                trimlength = 8;
            }
            if (fnamel.EndsWith(".rel.xml"))
            {
                mformat = MetaFormat.AudioRel;
            }
            if (fnamel.EndsWith(".ynd.xml"))
            {
                mformat = MetaFormat.Ynd;
            }
            if (fnamel.EndsWith(".ynv.xml"))
            {
                mformat = MetaFormat.Ynv;
            }
            if (fnamel.EndsWith(".ycd.xml"))
            {
                mformat = MetaFormat.Ycd;
            }
            if (fnamel.EndsWith(".ybn.xml"))
            {
                mformat = MetaFormat.Ybn;
            }
            if (fnamel.EndsWith(".ytd.xml"))
            {
                mformat = MetaFormat.Ytd;
            }
            if (fnamel.EndsWith(".ydr.xml"))
            {
                mformat = MetaFormat.Ydr;
            }
            if (fnamel.EndsWith(".ydd.xml"))
            {
                mformat = MetaFormat.Ydd;
            }
            if (fnamel.EndsWith(".yft.xml"))
            {
                mformat = MetaFormat.Yft;
            }
            if (fnamel.EndsWith(".ypt.xml"))
            {
                mformat = MetaFormat.Ypt;
            }
            if (fnamel.EndsWith(".yld.xml"))
            {
                mformat = MetaFormat.Yld;
            }
            if (fnamel.EndsWith(".yed.xml"))
            {
                mformat = MetaFormat.Yed;
            }
            if (fnamel.EndsWith(".ywr.xml"))
            {
                mformat = MetaFormat.Ywr;
            }
            if (fnamel.EndsWith(".yvr.xml"))
            {
                mformat = MetaFormat.Yvr;
            }
            if (fnamel.EndsWith(".awc.xml"))
            {
                mformat = MetaFormat.Awc;
            }
            if (fnamel.EndsWith(".fxc.xml"))
            {
                mformat = MetaFormat.Fxc;
            }
            if (fnamel.EndsWith("cache_y.dat.xml"))
            {
                mformat = MetaFormat.CacheFile;
            }
            if (fnamel.EndsWith(".dat.xml") && fnamel.StartsWith("heightmap"))
            {
                mformat = MetaFormat.Heightmap;
            }
            if (fnamel.EndsWith(".ypdb.xml"))
            {
                mformat = MetaFormat.Ypdb;
            }
            if (fnamel.EndsWith(".yfd.xml"))
            {
                mformat = MetaFormat.Yfd;
            }
            if (fnamel.EndsWith(".mrf.xml"))
            {
                mformat = MetaFormat.Mrf;
            }
            return mformat;
        }



        public static Meta GetMeta(XmlDocument doc)
        {
            MetaBuilder mb = new MetaBuilder();

            Traverse(doc.DocumentElement, mb, 0, true);

            XmlNode metaName = doc.DocumentElement.Attributes.GetNamedItem("name");

            if (metaName != null)
                return mb.GetMeta(metaName.Value);
            else
                return mb.GetMeta();
        }

        private static byte[] Traverse(XmlNode node, MetaBuilder mb, MetaName type = 0, bool isRoot = false)
        {
            if(type == 0)
            {
                type = (MetaName)(uint)GetHash(node.Name);
            }

            MetaStructureInfo infos = MetaTypes.GetStructureInfo(type);

            if (infos != null)
            {
                byte[] data = new byte[infos.StructureSize];
                ArrayResults arrayResults = new ArrayResults();

                arrayResults.Structures = new Dictionary<int, Array_Structure>();
                arrayResults.StructurePointers = new Dictionary<int, Array_StructurePointer>();
                arrayResults.UInts = new Dictionary<int, Array_uint>();
                arrayResults.UShorts = new Dictionary<int, Array_ushort>();
                arrayResults.UBytes = new Dictionary<int, Array_byte>();
                arrayResults.Floats = new Dictionary<int, Array_float>();
                arrayResults.Float_XYZs = new Dictionary<int, Array_Vector3>();
                arrayResults.Hashes = new Dictionary<int, Array_uint>();

                Array.Clear(data, 0, infos.StructureSize);

                MetaStructureEntryInfo_s arrEntry = new MetaStructureEntryInfo_s();

                if (isRoot)
                {
                    mb.EnsureBlock(type);
                }

                for (int i = 0; i < infos.Entries.Length; i++)
                {
                    MetaStructureEntryInfo_s entry = infos.Entries[i];

                    XmlNode cnode = GetEntryNode(node.ChildNodes, entry);

                    if (entry.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO)
                    {
                        arrEntry = entry;
                        continue;
                    }

                    if (cnode == null)
                    {
                        continue;
                    }

                    switch (entry.DataType)
                    {
                        case MetaStructureEntryDataType.Array:
                            {
                                TraverseArray(cnode, mb, arrEntry, entry.DataOffset, arrayResults);
                                break;
                            }

                        case MetaStructureEntryDataType.ArrayOfBytes:
                            {
                                GetParsedArrayOfBytes(cnode, data, entry, arrEntry);
                                break;
                            }
                            
                        case MetaStructureEntryDataType.ArrayOfChars:
                            {
                                int offset = entry.DataOffset;
                                string split = cnode.InnerText;// Split(cnode.InnerText, 1);

                                for (int j = 0; j < split.Length; j++)
                                {
                                    byte val = (byte)split[j];// Convert.ToByte(split[j], 16);
                                    data[offset] = val;
                                    offset += sizeof(byte);
                                }

                                break;
                            }

                        case MetaStructureEntryDataType.Boolean:
                            {
                                byte val = (cnode.Attributes["value"].Value == "false") ? (byte)0 : (byte)1;
                                data[entry.DataOffset] = val;
                                break;
                            }

                        case MetaStructureEntryDataType.ByteEnum:
                            {
                                byte val = Convert.ToByte(cnode.Attributes["value"].Value);
                                data[entry.DataOffset] = val;
                                break;
                            }


                        case MetaStructureEntryDataType.CharPointer:
                            {
                                if (!string.IsNullOrEmpty(cnode.InnerText))
                                {
                                    CharPointer ptr = mb.AddStringPtr(cnode.InnerText);
                                    byte[] val = MetaTypes.ConvertToBytes(ptr);

                                    Buffer.BlockCopy(val, 0, data, entry.DataOffset, val.Length);
                                }

                                break;
                            }

                        case MetaStructureEntryDataType.DataBlockPointer:
                            {
                                NumberStyles ns = NumberStyles.HexNumber;
                                CultureInfo ic = CultureInfo.InvariantCulture;
                                char[] sa = new[] { ' ', '\n' };
                                StringSplitOptions so = StringSplitOptions.RemoveEmptyEntries;
                                string[] split = cnode.InnerText.Trim().Split(sa, so); //split = Split(node.InnerText, 2); to read as unsplitted HEX
                                List<byte> bytes = new List<byte>();
                                for (int j = 0; j < split.Length; j++)
                                {
                                    byte val;// = Convert.ToByte(split[j], 10);
                                    if (byte.TryParse(split[j].Trim(), ns, ic, out val))
                                    {
                                        bytes.Add(val);
                                    }
                                }
                                DataBlockPointer ptr = mb.AddDataBlockPtr(bytes.ToArray(), (MetaName)MetaTypeName.BYTE);
                                byte[] byt = MetaTypes.ConvertToBytes(ptr);
                                Buffer.BlockCopy(byt, 0, data, entry.DataOffset, byt.Length);
                                break;
                            }

                        case MetaStructureEntryDataType.Float:
                            {
                                float val = FloatUtil.Parse(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.Float_XYZ:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                float z = FloatUtil.Parse(cnode.Attributes["z"].Value);

                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                Write(z, data, entry.DataOffset + sizeof(float) * 2);

                                break;
                            }


                        case MetaStructureEntryDataType.Float_XYZW:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                float z = FloatUtil.Parse(cnode.Attributes["z"].Value);
                                float w = FloatUtil.Parse(cnode.Attributes["w"].Value);

                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                Write(z, data, entry.DataOffset + sizeof(float) * 2);
                                Write(w, data, entry.DataOffset + sizeof(float) * 3);

                                break;
                            }

                        case MetaStructureEntryDataType.Hash:
                            {
                                MetaHash hash = GetHash(cnode.InnerText);
                                Write(hash, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.IntEnum:
                        case MetaStructureEntryDataType.IntFlags1:
                        case MetaStructureEntryDataType.IntFlags2:
                            {
                                if (entry.ReferenceKey != 0)
                                {
                                    MetaEnumInfo _infos = MetaTypes.GetEnumInfo(entry.ReferenceKey);
                                    mb.AddEnumInfo(_infos.EnumNameHash);
                                }

                                int val = GetEnumInt(entry.ReferenceKey, cnode.InnerText, entry.DataType);
                                Write(val, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.ShortFlags:
                            {
                                if (entry.ReferenceKey != 0)
                                {
                                    MetaEnumInfo _infos = MetaTypes.GetEnumInfo(entry.ReferenceKey);
                                    mb.AddEnumInfo(_infos.EnumNameHash);
                                }

                                int val = GetEnumInt(entry.ReferenceKey, cnode.InnerText, entry.DataType);
                                Write((short)val, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.SignedByte:
                            {
                                sbyte val = Convert.ToSByte(cnode.Attributes["value"].Value);
                                data[entry.DataOffset] = (byte)val;
                                break;
                            }

                        case MetaStructureEntryDataType.SignedInt:
                            {
                                int val = Convert.ToInt32(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.SignedShort:
                            {
                                short val = Convert.ToInt16(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }

                        case MetaStructureEntryDataType.Structure:
                            {
                                byte[] struc = Traverse(cnode, mb, entry.ReferenceKey);

                                if(struc != null)
                                {
                                    Buffer.BlockCopy(struc, 0, data, entry.DataOffset, struc.Length);
                                }

                                break;
                            }

                        case MetaStructureEntryDataType.StructurePointer:
                            {
                                // TODO
                                break;
                            }

                        case MetaStructureEntryDataType.UnsignedByte:
                            {
                                byte val = Convert.ToByte(cnode.Attributes["value"].Value);
                                data[entry.DataOffset] = val;
                                break;
                            }

                        case MetaStructureEntryDataType.UnsignedInt:
                            {
                                switch (entry.EntryNameHash)
                                {
                                    case MetaName.color:
                                        {
                                            uint val = Convert.ToUInt32(cnode.Attributes["value"].Value, 16);
                                            Write(val, data, entry.DataOffset);
                                            break;
                                        }

                                    default:
                                        {
                                            uint val = Convert.ToUInt32(cnode.Attributes["value"].Value);
                                            Write(val, data, entry.DataOffset);
                                            break;
                                        }
                                }

                                break;
                            }

                        case MetaStructureEntryDataType.UnsignedShort:
                            {
                                ushort val = Convert.ToUInt16(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }

                        default: break;

                    }
                }

                arrayResults.WriteArrays(data);

                mb.AddStructureInfo(infos.StructureNameHash);

                if (isRoot)
                {
                    mb.AddItem(type, data);
                }

                return data;
            }

            return null;
        }

        private static void GetParsedArrayOfBytes(XmlNode node, byte[] data, MetaStructureEntryInfo_s entry, MetaStructureEntryInfo_s arrEntry)
        {
            int offset = entry.DataOffset;

            NumberStyles ns = NumberStyles.Any;
            CultureInfo ic = CultureInfo.InvariantCulture;
            char[] sa = new[] { ' ' };
            StringSplitOptions so = StringSplitOptions.RemoveEmptyEntries;
            string[] split = node.InnerText.Trim().Split(sa, so); //split = Split(node.InnerText, 2); to read as unsplitted HEX

            switch (arrEntry.DataType)
            {
                default: //expecting hex string.
                    split = Split(node.InnerText, 2);
                    for (int j = 0; j < split.Length; j++)
                    {
                        byte val = Convert.ToByte(split[j], 16);
                        data[offset] = val;
                        offset += sizeof(byte);
                    }
                    break;
                case MetaStructureEntryDataType.SignedByte: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        sbyte val;// = Convert.ToSByte(split[j], 10);
                        if (sbyte.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            data[offset] = (byte)val;
                            offset += sizeof(sbyte);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedByte: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        byte val;// = Convert.ToByte(split[j], 10);
                        if (byte.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            data[offset] = val;
                            offset += sizeof(byte);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.SignedShort: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        short val;// = Convert.ToInt16(split[j], 10);
                        if (short.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            Write(val, data, offset);
                            offset += sizeof(short);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedShort: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        ushort val;// = Convert.ToUInt16(split[j], 10);
                        if (ushort.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            Write(val, data, offset);
                            offset += sizeof(ushort);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.SignedInt: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        int val;// = Convert.ToInt32(split[j], 10);
                        if (int.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            Write(val, data, offset);
                            offset += sizeof(int);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedInt: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        uint val;// = Convert.ToUInt32(split[j], 10);
                        if (uint.TryParse(split[j].Trim(), ns, ic, out val))
                        {
                            Write(val, data, offset);
                            offset += sizeof(uint);
                        }
                    }
                    break;
                case MetaStructureEntryDataType.Float: //expecting space-separated array.
                    for (int j = 0; j < split.Length; j++)
                    {
                        float val;// = FloatUtil.Parse(split[j]);
                        if (FloatUtil.TryParse(split[j].Trim(), out val))
                        {
                            Write(val, data, offset);
                            offset += sizeof(float);
                        }
                    }
                    break;
            }
        }

        private static void TraverseArray(XmlNode node, MetaBuilder mb, MetaStructureEntryInfo_s arrEntry, int offset, ArrayResults results)
        {
            switch (arrEntry.DataType)
            {
                case MetaStructureEntryDataType.Structure:
                    {
                        results.Structures[offset] = TraverseArrayStructure(node, mb, arrEntry.ReferenceKey);
                        break;
                    }

                case MetaStructureEntryDataType.StructurePointer:
                    {
                        results.StructurePointers[offset] = TraverseArrayStructurePointer(node, mb);
                        break;
                    }

                case MetaStructureEntryDataType.UnsignedInt:
                    {
                        results.UInts[offset] = TraverseRawUIntArray(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.UnsignedShort:
                    {
                        results.UShorts[offset] = TraverseRawUShortArray(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.UnsignedByte:
                    {
                        results.UBytes[offset] = TraverseRawUByteArray(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.Float:
                    {
                        results.Floats[offset] = TraverseRawFloatArray(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.Float_XYZ:
                    {
                        results.Float_XYZs[offset] = TraverseRawVector3Array(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.Hash:
                    {
                        results.Hashes[offset] = TraverseHashArray(node, mb);
                        break;
                    }
                case MetaStructureEntryDataType.CharPointer:
                    {
                        // TODO
                        break;
                    }
                case MetaStructureEntryDataType.DataBlockPointer:
                    {
                        // TODO
                        break;
                    }

                default: break;
            }

        }

        private static Array_Structure TraverseArrayStructure(XmlNode node, MetaBuilder mb, MetaName type)
        {
            List<byte[]> strucs = new List<byte[]>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;

                byte[] struc = Traverse(cnode, mb, type);

                if (struc != null)
                {
                    strucs.Add(struc);
                }
            }

            return mb.AddItemArrayPtr(type, strucs.ToArray());
        }

        private static Array_StructurePointer TraverseArrayStructurePointer(XmlNode node, MetaBuilder mb)
        {
            List<MetaPOINTER> ptrs = new List<MetaPOINTER>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;

                MetaName type = (MetaName)(uint)GetHash(cnode.Attributes["type"].Value);
                byte[] struc = Traverse(cnode, mb, type);

                if(struc != null)
                {
                    MetaPOINTER ptr = mb.AddItemPtr(type, struc);
                    ptrs.Add(ptr);
                }

            }

            return mb.AddPointerArray(ptrs.ToArray());

        }

        private static Array_uint TraverseRawUIntArray(XmlNode node, MetaBuilder mb)
        {
            List<uint> data = new List<uint>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if(!string.IsNullOrEmpty(split[i]))
                    {
                        uint val = Convert.ToUInt32(split[i]);
                        data.Add(val);
                    }

                }
            }

            return mb.AddUintArrayPtr(data.ToArray());
        }

        private static Array_ushort TraverseRawUShortArray(XmlNode node, MetaBuilder mb)
        {
            List<ushort> data = new List<ushort>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if (!string.IsNullOrEmpty(split[i]))
                    {
                        ushort val = Convert.ToUInt16(split[i]);
                        data.Add(val);
                    }
                }
            }

            return mb.AddUshortArrayPtr(data.ToArray());
        }

        private static Array_byte TraverseRawUByteArray(XmlNode node, MetaBuilder mb)
        {
            List<byte> data = new List<byte>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if (!string.IsNullOrEmpty(split[i]))
                    {
                        byte val = Convert.ToByte(split[i]);
                        data.Add(val);
                    }
                }
            }

            return mb.AddByteArrayPtr(data.ToArray());
        }

        private static Array_float TraverseRawFloatArray(XmlNode node, MetaBuilder mb)
        {
            List<float> data = new List<float>();

            if(node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    string ts = split[i]?.Trim();
                    if (!string.IsNullOrEmpty(ts))
                    {
                        float val = FloatUtil.Parse(ts);// Convert.ToSingle(split[i]);
                        data.Add(val);
                    }
                }
            }

            return mb.AddFloatArrayPtr(data.ToArray());
        }

        private static Array_Vector3 TraverseRawVector3Array(XmlNode node, MetaBuilder mb)
        {
            List<Vector4> items = new List<Vector4>();

            float x = 0f;
            float y = 0f;
            float z = 0f;
            float w = 0f;

            XmlNodeList cnodes = node.SelectNodes("Item");
            if (cnodes.Count > 0)
            {
                foreach (XmlNode cnode in cnodes)
                {
                    string str = cnode.InnerText;
                    string[] strs = str.Split(',');
                    if (strs.Length >= 3)
                    {
                        x = FloatUtil.Parse(strs[0].Trim());
                        y = FloatUtil.Parse(strs[1].Trim());
                        z = FloatUtil.Parse(strs[2].Trim());
                        if (strs.Length >= 4)
                        {
                            w = FloatUtil.Parse(strs[3].Trim());
                        }
                        Vector4 val = new Vector4(x, y, z, w);
                        items.Add(val);
                    }
                }
            }
            else
            {
                string[] split = node.InnerText.Split('\n');// Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    string s = split[i]?.Trim();
                    if (string.IsNullOrEmpty(s)) continue;
                    string[] split2 = Regex.Split(s, @"[\s\t]");
                    int c = 0;
                    x = 0f; y = 0f; z = 0f;
                    for (int n = 0; n < split2.Length; n++)
                    {
                        string ts = split2[n]?.Trim();
                        if (string.IsNullOrEmpty(ts)) continue;
                        float f = FloatUtil.Parse(ts);
                        switch (c)
                        {
                            case 0: x = f; break;
                            case 1: y = f; break;
                            case 2: z = f; break;
                        }
                        c++;
                    }
                    if (c >= 3)
                    {
                        Vector4 val = new Vector4(x, y, z, w);
                        items.Add(val);
                    }
                }
            }


            return mb.AddPaddedVector3ArrayPtr(items.ToArray());
        }

        private static Array_uint TraverseHashArray(XmlNode node, MetaBuilder mb)
        {
            List<MetaHash> items = new List<MetaHash>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;
                MetaHash val = GetHash(cnode.InnerText);
                items.Add(val);
            }

            return mb.AddHashArrayPtr(items.ToArray());
        }

        private static void Write(int val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(int));
        }

        private static void Write(uint val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(uint));
        }

        private static void Write(short val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(short));
        }

        private static void Write(ushort val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(ushort));
        }

        private static void Write(float val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(float));
        }

        public static MetaHash GetHash(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            if (str.StartsWith("hash_"))
            {
                return (MetaHash) Convert.ToUInt32(str.Substring(5), 16);
            }
            else
            {
                JenkIndex.Ensure(str);
                return JenkHash.GenHash(str);
            }
        }

        private static XmlNode GetEntryNode(XmlNodeList nodes, MetaStructureEntryInfo_s entry)
        {
            foreach (XmlNode node in nodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (GetHash(node.Name) == (uint)entry.EntryNameHash)
                {
                    return node;
                }
            }

            return null;
        }

        private static string[] Split(string str, int maxChunkSize)
        {
            List<string> chunks = new List<String>();

            for (int i = 0; i < str.Length; i += maxChunkSize)
            {
                chunks.Add(str.Substring(i, Math.Min(maxChunkSize, str.Length - i)));
            }

            return chunks.ToArray();
        }

        private static int GetEnumInt(MetaName type, string enumString, MetaStructureEntryDataType dataType)
        {
            int intval = 0;
            if (int.TryParse(enumString, out intval))
            {
                return intval; //it's already an int.... maybe enum not found or has no entries... or original value didn't match anything
            }

            MetaEnumInfo infos = MetaTypes.GetEnumInfo(type);
            if (infos == null)
            {
                return 0;
            }


            bool isFlags = (dataType == MetaStructureEntryDataType.IntFlags1) ||
                           (dataType == MetaStructureEntryDataType.IntFlags2);// ||
                           //(dataType == MetaStructureEntryDataType.ShortFlags);

            if (isFlags)
            {
                //flags enum. (multiple values, comma-separated)
                string[] split = enumString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int enumVal = 0;

                for (int i = 0; i < split.Length; i++)
                {
                    MetaName enumName = (MetaName)(uint)GetHash(split[i].Trim());

                    for (int j = 0; j < infos.Entries.Length; j++)
                    {
                        MetaEnumEntryInfo_s entry = infos.Entries[j];
                        if (entry.EntryNameHash == enumName)
                        {
                            enumVal += (1 << entry.EntryValue);
                            break;
                        }
                    }
                }

                return enumVal;
            }
            else
            {
                //single value enum.

                MetaName enumName = (MetaName)(uint)GetHash(enumString);

                for (int j = 0; j < infos.Entries.Length; j++)
                {
                    MetaEnumEntryInfo_s entry = infos.Entries[j];

                    if (entry.EntryNameHash == enumName)
                    {
                        return entry.EntryValue;
                    }
                }
            }

            return 0;
        }



        public static T[] ReadItemArray<T>(XmlNode node, string name) where T : IMetaXmlItem, new()
        {
            XmlNode vnode2 = node.SelectSingleNode(name);
            if (vnode2 != null)
            {
                XmlNodeList inodes = vnode2.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    List<T> vlist = new List<T>();
                    foreach (XmlNode inode in inodes)
                    {
                        T v = new T();
                        v.ReadXml(inode);
                        vlist.Add(v);
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }

        public static T[] ReadItemArrayNullable<T>(XmlNode node, string name) where T : IMetaXmlItem, new()
        {
            XmlNode vnode2 = node.SelectSingleNode(name);
            if (vnode2 != null)
            {
                XmlNodeList inodes = vnode2.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    List<T> vlist = new List<T>();
                    foreach (XmlNode inode in inodes)
                    {
                        if (inode.HasChildNodes)
                        {
                            T v = new T();
                            v.ReadXml(inode);
                            vlist.Add(v);
                        }
                        else
                        {
                            vlist.Add(default(T));
                        }
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }


        public static MetaHash[] ReadHashItemArray(XmlNode node, string name)
        {
            XmlNode vnode = node.SelectSingleNode(name);
            if (vnode != null)
            {
                XmlNodeList inodes = vnode.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    List<MetaHash> vlist = new List<MetaHash>();
                    foreach (XmlNode inode in inodes)
                    {
                        vlist.Add(GetHash(inode.InnerText));
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }
        public static string[] ReadStringItemArray(XmlNode node, string name)
        {
            XmlNode vnode = node.SelectSingleNode(name);
            if (vnode != null)
            {
                XmlNodeList inodes = vnode.SelectNodes("Item");
                if (inodes?.Count > 0)
                {
                    List<string> vlist = new List<string>();
                    foreach (XmlNode inode in inodes)
                    {
                        vlist.Add(inode.InnerText);
                    }
                    return vlist.ToArray();
                }
            }
            return null;
        }

    }

    struct ArrayResults
    {
        public Dictionary<int, Array_Structure> Structures;
        public Dictionary<int, Array_StructurePointer> StructurePointers;
        public Dictionary<int, Array_uint> UInts;
        public Dictionary<int, Array_ushort> UShorts;
        public Dictionary<int, Array_byte> UBytes;
        public Dictionary<int, Array_float> Floats;
        public Dictionary<int, Array_Vector3> Float_XYZs;
        public Dictionary<int, Array_uint> Hashes;

        public void WriteArrays(byte[] data)
        {
            foreach (KeyValuePair<int, Array_Structure> ptr in Structures)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_StructurePointer> ptr in StructurePointers)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_uint> ptr in UInts)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_ushort> ptr in UShorts)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_byte> ptr in UBytes)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_float> ptr in Floats)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_Vector3> ptr in Float_XYZs)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_uint> ptr in Hashes)
            {
                byte[] _data = MetaTypes.ConvertToBytes(ptr.Value);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }
        }
    }
}
