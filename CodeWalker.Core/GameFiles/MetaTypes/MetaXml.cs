using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace CodeWalker.GameFiles
{
    public class MetaXml : MetaXmlBase
    {

        public static string GetXml(RpfFileEntry e, byte[] data, out string filename, string outputfolder = "")
        {
            string fn = e.Name;
            string fnl = fn.ToLowerInvariant();

            if (!string.IsNullOrEmpty(outputfolder))
            {
                outputfolder = Path.Combine(outputfolder, e.GetShortName());
            }

            if (fnl.EndsWith(".ymt"))
            {
                YmtFile ymt = RpfFile.GetFile<YmtFile>(e, data);
                return GetXml(ymt, out filename);
            }
            else if (fnl.EndsWith(".ymf"))
            {
                YmfFile ymf = RpfFile.GetFile<YmfFile>(e, data);
                return GetXml(ymf, out filename);
            }
            else if (fnl.EndsWith(".ymap"))
            {
                YmapFile ymap = RpfFile.GetFile<YmapFile>(e, data);
                return GetXml(ymap, out filename);
            }
            else if (fnl.EndsWith(".ytyp"))
            {
                YtypFile ytyp = RpfFile.GetFile<YtypFile>(e, data);
                return GetXml(ytyp, out filename);
            }
            else if (fnl.EndsWith(".pso"))
            {
                JPsoFile pso = RpfFile.GetFile<JPsoFile>(e, data);
                return GetXml(pso, out filename);
            }
            else if (fnl.EndsWith(".cut"))
            {
                CutFile cut = RpfFile.GetFile<CutFile>(e, data);
                return GetXml(cut, out filename);
            }
            else if (fnl.EndsWith(".rel"))
            {
                RelFile rel = RpfFile.GetFile<RelFile>(e, data);
                return GetXml(rel, out filename);
            }
            else if (fnl.EndsWith(".ynd"))
            {
                YndFile ynd = RpfFile.GetFile<YndFile>(e, data);
                return GetXml(ynd, out filename);
            }
            else if (fnl.EndsWith(".ynv"))
            {
                YnvFile ynv = RpfFile.GetFile<YnvFile>(e, data);
                return GetXml(ynv, out filename);
            }
            else if (fnl.EndsWith(".ycd"))
            {
                YcdFile ycd = RpfFile.GetFile<YcdFile>(e, data);
                return GetXml(ycd, out filename);
            }
            else if (fnl.EndsWith(".ybn"))
            {
                YbnFile ybn = RpfFile.GetFile<YbnFile>(e, data);
                return GetXml(ybn, out filename);
            }
            else if (fnl.EndsWith(".ytd"))
            {
                YtdFile ytd = RpfFile.GetFile<YtdFile>(e, data);
                return GetXml(ytd, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".ydr"))
            {
                YdrFile ydr = RpfFile.GetFile<YdrFile>(e, data);
                return GetXml(ydr, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".ydd"))
            {
                YddFile ydd = RpfFile.GetFile<YddFile>(e, data);
                return GetXml(ydd, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".yft"))
            {
                YftFile yft = RpfFile.GetFile<YftFile>(e, data);
                return GetXml(yft, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".ypt"))
            {
                YptFile ypt = RpfFile.GetFile<YptFile>(e, data);
                return GetXml(ypt, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".yld"))
            {
                YldFile yld = RpfFile.GetFile<YldFile>(e, data);
                return GetXml(yld, out filename);
            }
            else if (fnl.EndsWith(".yed"))
            {
                YedFile yed = RpfFile.GetFile<YedFile>(e, data);
                return GetXml(yed, out filename);
            }
            else if (fnl.EndsWith(".ywr"))
            {
                YwrFile ywr = RpfFile.GetFile<YwrFile>(e, data);
                return GetXml(ywr, out filename);
            }
            else if (fnl.EndsWith(".yvr"))
            {
                YvrFile yvr = RpfFile.GetFile<YvrFile>(e, data);
                return GetXml(yvr, out filename);
            }
            else if (fnl.EndsWith(".ypdb"))
            {
                YpdbFile ypdb = RpfFile.GetFile<YpdbFile>(e, data);
                return GetXml(ypdb, out filename);
            }
            else if (fnl.EndsWith(".yfd"))
            {
                YfdFile yfd = RpfFile.GetFile<YfdFile>(e, data);
                return GetXml(yfd, out filename);
            }
            else if (fnl.EndsWith(".awc"))
            {
                AwcFile awc = RpfFile.GetFile<AwcFile>(e, data);
                return GetXml(awc, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".fxc"))
            {
                FxcFile fxc = RpfFile.GetFile<FxcFile>(e, data);
                return GetXml(fxc, out filename, outputfolder);
            }
            else if (fnl.EndsWith("cache_y.dat"))
            {
                CacheDatFile cdf = RpfFile.GetFile<CacheDatFile>(e, data);
                return GetXml(cdf, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".dat") && fnl.StartsWith("heightmap"))
            {
                HeightmapFile hmf = RpfFile.GetFile<HeightmapFile>(e, data);
                return GetXml(hmf, out filename, outputfolder);
            }
            else if (fnl.EndsWith(".mrf"))
            {
                MrfFile mrf = RpfFile.GetFile<MrfFile>(e, data);
                return GetXml(mrf, out filename, outputfolder);
            }
            filename = fn;
            return string.Empty;
        }
        public static string GetXml(YmtFile ymt, out string filename)
        {
            string fn = (ymt?.RpfFileEntry?.Name) ?? "";
            if (ymt.Meta != null) { filename = fn + ".xml"; return GetXml(ymt.Meta); }
            else if (ymt.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(ymt.Pso); }
            else if (ymt.Rbf != null) { filename = fn + ".rbf.xml"; return RbfXml.GetXml(ymt.Rbf); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(YmfFile ymf, out string filename)
        {
            string fn = (ymf?.FileEntry?.Name) ?? "";
            if (ymf.Meta != null) { filename = fn + ".xml"; return GetXml(ymf.Meta); }
            else if (ymf.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(ymf.Pso); }
            else if (ymf.Rbf != null) { filename = fn + ".rbf.xml"; return RbfXml.GetXml(ymf.Rbf); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(YmapFile ymap, out string filename)
        {
            string fn = (ymap?.RpfFileEntry?.Name) ?? "";
            if (ymap.Meta != null) { filename = fn + ".xml"; return GetXml(ymap.Meta); }
            else if (ymap.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(ymap.Pso); }
            else if (ymap.Rbf != null) { filename = fn + ".rbf.xml"; return RbfXml.GetXml(ymap.Rbf); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(YtypFile ytyp, out string filename)
        {
            string fn = (ytyp?.RpfFileEntry?.Name) ?? "";
            if (ytyp.Meta != null) { filename = fn + ".xml"; return GetXml(ytyp.Meta); }
            else if (ytyp.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(ytyp.Pso); }
            else if (ytyp.Rbf != null) { filename = fn + ".rbf.xml"; return RbfXml.GetXml(ytyp.Rbf); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(JPsoFile pso, out string filename)
        {
            string fn = (pso?.FileEntry?.Name) ?? "";
            if (pso.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(pso.Pso); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(CutFile cut, out string filename)
        {
            string fn = (cut?.FileEntry?.Name) ?? "";
            if (cut.Pso != null) { filename = fn + ".pso.xml"; return PsoXml.GetXml(cut.Pso); }
            filename = string.Empty;
            return string.Empty;
        }
        public static string GetXml(RelFile rel, out string filename)
        {
            string fn = (rel?.RpfFileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return RelXml.GetXml(rel);
        }
        public static string GetXml(YndFile ynd, out string filename)
        {
            string fn = (ynd?.RpfFileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return YndXml.GetXml(ynd);
        }
        public static string GetXml(YnvFile ynv, out string filename)
        {
            string fn = (ynv?.RpfFileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return YnvXml.GetXml(ynv);
        }
        public static string GetXml(YcdFile ycd, out string filename)
        {
            string fn = (ycd?.RpfFileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return YcdXml.GetXml(ycd);
        }
        public static string GetXml(YbnFile ybn, out string filename)
        {
            string fn = (ybn?.RpfFileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return YbnXml.GetXml(ybn);
        }
        public static string GetXml(YtdFile ytd, out string filename, string outputfolder)
        {
            string fn = (ytd?.Name) ?? "";
            filename = fn + ".xml";
            return YtdXml.GetXml(ytd, outputfolder);
        }
        public static string GetXml(YdrFile ydr, out string filename, string outputfolder)
        {
            string fn = (ydr?.Name) ?? "";
            filename = fn + ".xml";
            return YdrXml.GetXml(ydr, outputfolder);
        }
        public static string GetXml(YddFile ydd, out string filename, string outputfolder)
        {
            string fn = (ydd?.Name) ?? "";
            filename = fn + ".xml";
            return YddXml.GetXml(ydd, outputfolder);
        }
        public static string GetXml(YftFile yft, out string filename, string outputfolder)
        {
            string fn = (yft?.Name) ?? "";
            filename = fn + ".xml";
            return YftXml.GetXml(yft, outputfolder);
        }
        public static string GetXml(YptFile ypt, out string filename, string outputfolder)
        {
            string fn = (ypt?.Name) ?? "";
            filename = fn + ".xml";
            return YptXml.GetXml(ypt, outputfolder);
        }
        public static string GetXml(YldFile yld, out string filename)
        {
            string fn = (yld?.Name) ?? "";
            filename = fn + ".xml";
            return YldXml.GetXml(yld);
        }
        public static string GetXml(YedFile yed, out string filename)
        {
            string fn = (yed?.Name) ?? "";
            filename = fn + ".xml";
            return YedXml.GetXml(yed);
        }
        public static string GetXml(YwrFile ywr, out string filename)
        {
            string fn = (ywr?.Name) ?? "";
            filename = fn + ".xml";
            return YwrXml.GetXml(ywr);
        }
        public static string GetXml(YvrFile yvr, out string filename)
        {
            string fn = (yvr?.Name) ?? "";
            filename = fn + ".xml";
            return YvrXml.GetXml(yvr);
        }
        public static string GetXml(YpdbFile ypdb, out string filename)
        {
            string fn = (ypdb?.Name) ?? "";
            filename = fn + ".xml";
            return YpdbXml.GetXml(ypdb);
        }
        public static string GetXml(YfdFile yfd, out string filename)
        {
            string fn = (yfd?.Name) ?? "";
            filename = fn + ".xml";
            return YfdXml.GetXml(yfd);
        }
        public static string GetXml(AwcFile awc, out string filename, string outputfolder)
        {
            string fn = (awc?.Name) ?? "";
            filename = fn + ".xml";
            return AwcXml.GetXml(awc, outputfolder);
        }
        public static string GetXml(FxcFile fxc, out string filename, string outputfolder)
        {
            string fn = (fxc?.Name) ?? "";
            filename = fn + ".xml";
            return FxcXml.GetXml(fxc, outputfolder);
        }
        public static string GetXml(CacheDatFile cdf, out string filename, string outputfolder)
        {
            string fn = (cdf?.FileEntry?.Name) ?? "";
            filename = fn + ".xml";
            return CacheDatXml.GetXml(cdf);
        }
        public static string GetXml(HeightmapFile hmf, out string filename, string outputfolder)
        {
            string fn = (hmf?.Name) ?? "";
            filename = fn + ".xml";
            return HmapXml.GetXml(hmf);
        }
        public static string GetXml(MrfFile mrf, out string filename, string outputfolder)
        {
            string fn = (mrf?.Name) ?? "";
            filename = fn + ".xml";
            return MrfXml.GetXml(mrf);
        }









        public static string GetXml(Meta meta)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(XmlHeader);

            if (meta != null)
            {
                MetaCont cont = new MetaCont(meta);

                WriteNode(sb, 0, cont, meta.RootBlockIndex, 0, XmlTagMode.Structure, 0, (string)meta.Name);
            }

            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, int indent, MetaCont cont, int blockId, int offset, XmlTagMode tagMode = XmlTagMode.None, MetaName structName = 0, string metaName = "")
        {

            MetaDataBlock block = cont.Meta.GetBlock(blockId);
            if (block == null)
            {
                ErrorXml(sb, indent, "Couldn't find block " + blockId + "!");
                return;
            }

            if (structName == 0)
            {
                structName = block.StructureNameHash;
            }

            string name = HashString(structName);
            byte[] data = block.Data;

            MetaStructureInfo structInfo = cont.GetStructureInfo(structName);
            if (structInfo == null)
            {
                ErrorXml(sb, indent, "Couldn't find structure info " + name + "!");
                return;
            }
            if (structInfo.Entries == null)
            {
                ErrorXml(sb, indent, "Couldn't find structure info entries for " + name + "!");
                return;
            }


            switch (tagMode)
            {
                case XmlTagMode.Structure:
                    OpenTag(sb, indent, name, true, metaName);
                    break;
                case XmlTagMode.Item:
                    OpenTag(sb, indent, "Item", true, metaName);
                    break;
                case XmlTagMode.ItemAndType:
                    OpenTag(sb, indent, "Item type=\"" + name + "\"", true, metaName);
                    break;
            }

            int cind = indent + 1;
            MetaStructureEntryInfo_s arrEntry = new MetaStructureEntryInfo_s();
            for (int i = 0; i < structInfo.Entries.Length; i++)
            {
                MetaStructureEntryInfo_s entry = structInfo.Entries[i];
                if (entry.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO)
                {
                    arrEntry = entry;
                    continue;
                }
                string ename = HashString(entry.EntryNameHash);
                int eoffset = offset + entry.DataOffset;
                switch (entry.DataType)
                {
                    default:
                        ErrorXml(sb, cind, ename + ": Unexpected entry DataType: " + entry.DataType.ToString());
                        break;
                    case MetaStructureEntryDataType.Array:

                        WriteArrayNode(sb, cind, cont, data, arrEntry, ename, eoffset);

                        break;
                    case MetaStructureEntryDataType.ArrayOfBytes:

                        WriteParsedArrayOfBytesNode(sb, cind, data, ename, eoffset, entry, arrEntry);

                        break;
                    case MetaStructureEntryDataType.ArrayOfChars:
                        OpenTag(sb, cind, ename, false);
                        uint charArrLen = (uint)entry.ReferenceKey;
                        for (int n = 0; n < charArrLen; n++)
                        {
                            int bidx = eoffset + n;
                            if ((bidx >= 0) && (bidx < data.Length))
                            {
                                byte b = data[bidx];
                                if (b == 0) break;
                                sb.Append((char)b);
                            }
                        }
                        CloseTag(sb, 0, ename);
                        break;
                    case MetaStructureEntryDataType.Boolean:
                        bool boolVal = BitConverter.ToBoolean(data, eoffset);
                        ValueTag(sb, cind, ename, boolVal?"true":"false");
                        break;
                    case MetaStructureEntryDataType.ByteEnum:
                        byte byteEnumVal = data[eoffset];
                        ValueTag(sb, cind, ename, byteEnumVal.ToString());
                        break;
                    case MetaStructureEntryDataType.CharPointer:
                        CharPointer charPtr = MetaTypes.ConvertData<CharPointer>(data, eoffset);
                        string charStr = MetaTypes.GetString(cont.Meta, charPtr);
                        OneLineTag(sb, cind, ename, charStr);
                        break;
                    case MetaStructureEntryDataType.DataBlockPointer:
                        DataBlockPointer dataPtr = MetaTypes.ConvertData<DataBlockPointer>(data, eoffset);
                        //need to just get all the data from that block, since this pointer is referring to the whole block! it should be of type BYTE!
                        MetaDataBlock dblock = cont.Meta.GetBlock((int)dataPtr.PointerDataId);
                        WriteRawArray(sb, dblock.Data, cind, ename, "ByteArray", FormatHexByte, 32);
                        break;
                    case MetaStructureEntryDataType.Float:
                        float floatVal = BitConverter.ToSingle(data, eoffset);
                        ValueTag(sb, cind, ename, FloatUtil.ToString(floatVal));
                        break;
                    case MetaStructureEntryDataType.Float_XYZ:
                        Vector3 v3 = MetaTypes.ConvertData<Vector3>(data, eoffset);
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v3.X) + "\" y=\"" + FloatUtil.ToString(v3.Y) + "\" z=\"" + FloatUtil.ToString(v3.Z) + "\"");
                        break;
                    case MetaStructureEntryDataType.Float_XYZW:
                        Vector4 v4 = MetaTypes.ConvertData<Vector4>(data, eoffset);
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v4.X) + "\" y=\"" + FloatUtil.ToString(v4.Y) + "\" z=\"" + FloatUtil.ToString(v4.Z) + "\" w=\"" + FloatUtil.ToString(v4.W) + "\"");
                        break;
                    case MetaStructureEntryDataType.Hash:
                        MetaHash hashVal = MetaTypes.ConvertData<MetaHash>(data, eoffset);
                        string hashStr = HashString(hashVal);
                        StringTag(sb, cind, ename, hashStr);
                        break;
                    case MetaStructureEntryDataType.IntEnum:
                        int intEnumVal = BitConverter.ToInt32(data, eoffset);
                        string intEnumStr = GetEnumString(cont, entry, intEnumVal);
                        StringTag(sb, cind, ename, intEnumStr);
                        break;
                    case MetaStructureEntryDataType.IntFlags1:
                        int intFlags1Val = BitConverter.ToInt32(data, eoffset);
                        string intFlags1Str = GetEnumString(cont, entry, intFlags1Val);
                        StringTag(sb, cind, ename, intFlags1Str);
                        break;
                    case MetaStructureEntryDataType.IntFlags2:
                        int intFlags2Val = BitConverter.ToInt32(data, eoffset);
                        string intFlags2Str = GetEnumString(cont, entry, intFlags2Val);
                        StringTag(sb, cind, ename, intFlags2Str);
                        break;
                    case MetaStructureEntryDataType.ShortFlags:
                        short shortFlagsVal = BitConverter.ToInt16(data, eoffset);
                        string shortFlagsStr = GetEnumString(cont, entry, shortFlagsVal);
                        StringTag(sb, cind, ename, shortFlagsStr);
                        break;
                    case MetaStructureEntryDataType.SignedByte:
                        sbyte sbyteVal = (sbyte)data[eoffset];
                        ValueTag(sb, cind, ename, sbyteVal.ToString());
                        break;
                    case MetaStructureEntryDataType.SignedInt:
                        int intVal = BitConverter.ToInt32(data, eoffset);
                        ValueTag(sb, cind, ename, intVal.ToString());
                        break;
                    case MetaStructureEntryDataType.SignedShort:
                        short shortVal = BitConverter.ToInt16(data, eoffset);
                        ValueTag(sb, cind, ename, shortVal.ToString());
                        break;
                    case MetaStructureEntryDataType.Structure:
                        OpenTag(sb, cind, ename);
                        WriteNode(sb, cind, cont, blockId, eoffset, XmlTagMode.None, entry.ReferenceKey);
                        CloseTag(sb, cind, ename);
                        break;
                    case MetaStructureEntryDataType.StructurePointer:
                        OpenTag(sb, cind, ename);
                        ErrorXml(sb, cind + 1, "StructurePointer not supported here! Tell dexy!");
                        CloseTag(sb, cind, ename);
                        break;
                    case MetaStructureEntryDataType.UnsignedByte:
                        byte byteVal = data[eoffset];
                        ValueTag(sb, cind, ename, byteVal.ToString());
                        //ValueTag(sb, cind, ename, "0x" + byteVal.ToString("X").PadLeft(2, '0'));
                        break;
                    case MetaStructureEntryDataType.UnsignedInt:
                        uint uintVal = BitConverter.ToUInt32(data, eoffset);
                        switch (entry.EntryNameHash)
                        {
                            default:
                                ValueTag(sb, cind, ename, uintVal.ToString());
                                break;
                            case MetaName.color:
                                ValueTag(sb, cind, ename, "0x" + uintVal.ToString("X").PadLeft(8, '0'));
                                break;
                        }

                        break;
                    case MetaStructureEntryDataType.UnsignedShort:
                        ushort ushortVal = BitConverter.ToUInt16(data, eoffset);
                        ValueTag(sb, cind, ename, ushortVal.ToString());// "0x" + ushortVal.ToString("X").PadLeft(4, '0'));
                        break;
                }
            }

            switch (tagMode)
            {
                case XmlTagMode.Structure:
                    CloseTag(sb, indent, name);
                    break;
                case XmlTagMode.Item:
                case XmlTagMode.ItemAndType:
                    CloseTag(sb, indent, "Item");
                    break;
            }

        }

        private static void WriteArrayNode(StringBuilder sb, int indent, MetaCont cont, byte[] data, MetaStructureEntryInfo_s arrEntry, string ename, int eoffset)
        {
            int aCount = 0;
            int aind = indent + 1;
            string arrTag = ename;
            switch (arrEntry.DataType)
            {
                default:
                    ErrorXml(sb, indent, ename + ": Unexpected array entry DataType: " + arrEntry.DataType.ToString());
                    break;
                case MetaStructureEntryDataType.Structure:
                    Array_Structure arrStruc = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                    int aBlockId = (int)arrStruc.PointerDataId;
                    int aOffset = (int)arrStruc.PointerDataOffset;
                    aCount = arrStruc.Count1;
                    arrTag += " itemType=\"" + HashString(arrEntry.ReferenceKey) + "\"";
                    if (aCount > 0)
                    {
                        OpenTag(sb, indent, arrTag);
                        MetaStructureInfo atyp = cont.GetStructureInfo(arrEntry.ReferenceKey);
                        MetaDataBlock aBlock = cont.Meta.GetBlock(aBlockId);
                        for (int n = 0; n < aCount; n++)
                        {
                            WriteNode(sb, aind, cont, aBlockId, aOffset, XmlTagMode.Item, arrEntry.ReferenceKey);
                            aOffset += atyp.StructureSize;

                            if ((n < (aCount - 1)) && (aBlock != null) && (aOffset >= aBlock.DataLength))
                            {
                                aOffset = 0;
                                aBlockId++;
                                aBlock = cont.Meta.GetBlock(aBlockId);
                            }
                        }
                        CloseTag(sb, indent, ename);
                    }
                    else
                    {
                        SelfClosingTag(sb, indent, arrTag);
                    }
                    break;
                case MetaStructureEntryDataType.StructurePointer:
                    Array_StructurePointer arrStrucP = MetaTypes.ConvertData<Array_StructurePointer>(data, eoffset);
                    MetaPOINTER[] ptrArr = MetaTypes.GetPointerArray(cont.Meta, arrStrucP);
                    aCount = ptrArr?.Length ?? 0;
                    if (aCount > 0)
                    {
                        OpenTag(sb, indent, arrTag);
                        for (int n = 0; n < aCount; n++)
                        {
                            MetaPOINTER ptr = ptrArr[n];
                            int offset = ptr.Offset;
                            WriteNode(sb, aind, cont, ptr.BlockID, offset, XmlTagMode.ItemAndType);
                        }
                        CloseTag(sb, indent, ename);
                    }
                    else
                    {
                        SelfClosingTag(sb, indent, arrTag);
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedInt:
                    Array_uint arrUint = MetaTypes.ConvertData<Array_uint>(data, eoffset);
                    uint[] uintArr = MetaTypes.GetUintArray(cont.Meta, arrUint);
                    WriteRawArray(sb, uintArr, indent, ename, "uint");
                    break;
                case MetaStructureEntryDataType.UnsignedShort:
                    Array_ushort arrUshort = MetaTypes.ConvertData<Array_ushort>(data, eoffset);
                    ushort[] ushortArr = MetaTypes.GetUshortArray(cont.Meta, arrUshort);
                    WriteRawArray(sb, ushortArr, indent, ename, "ushort");
                    break;
                case MetaStructureEntryDataType.UnsignedByte:
                    Array_byte arrUbyte = MetaTypes.ConvertData<Array_byte>(data, eoffset);
                    byte[] byteArr = MetaTypes.GetByteArray(cont.Meta, arrUbyte);
                    WriteRawArray(sb, byteArr, indent, ename, "byte");
                    break;
                case MetaStructureEntryDataType.Float:
                    Array_float arrFloat = MetaTypes.ConvertData<Array_float>(data, eoffset);
                    float[] floatArr = MetaTypes.GetFloatArray(cont.Meta, arrFloat);
                    WriteRawArray(sb, floatArr, indent, ename, "float");
                    break;
                case MetaStructureEntryDataType.Float_XYZ:
                    Array_Vector3 arrV3 = MetaTypes.ConvertData<Array_Vector3>(data, eoffset);
                    Vector4[] v4Arr = MetaTypes.ConvertDataArray<Vector4>(cont.Meta, (MetaName)MetaTypeName.VECTOR4, arrV3.Pointer, arrV3.Count1);
                    WriteItemArray(sb, v4Arr, indent, ename, "Vector3/4", FormatVector4);
                    break;
                case MetaStructureEntryDataType.CharPointer:
                    ErrorXml(sb, indent, "CharPointer ARRAY not supported here! Tell dexy!");
                    break;
                case MetaStructureEntryDataType.DataBlockPointer:
                    ErrorXml(sb, indent, "DataBlockPointer ARRAY not supported here! Tell dexy!");
                    break;
                case MetaStructureEntryDataType.Hash:
                    Array_uint arrHash = MetaTypes.ConvertData<Array_uint>(data, eoffset);
                    MetaHash[] hashArr = MetaTypes.GetHashArray(cont.Meta, arrHash);
                    WriteItemArray(sb, hashArr, indent, ename, "Hash", FormatHash);
                    break;
            }
        }

        private static void WriteParsedArrayOfBytesNode(StringBuilder sb, int indent, byte[] data, string ename, int eoffset, MetaStructureEntryInfo_s entry, MetaStructureEntryInfo_s arrEntry)
        {
            OpenTag(sb, indent, ename, false);

            int byteArrLen = ((int)entry.ReferenceKey);

            switch (arrEntry.DataType)
            {
                default:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n;
                        byte b = ((bidx >= 0) && (bidx < data.Length)) ? data[bidx] : (byte)0;
                        sb.Append(b.ToString("X").PadLeft(2, '0'));
                    }
                    break;
                case MetaStructureEntryDataType.SignedByte:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n;
                        sbyte b = ((bidx >= 0) && (bidx < data.Length)) ? (sbyte)data[bidx] : (sbyte)0;
                        sb.Append(b.ToString()); //sb.Append(b.ToString("X").PadLeft(2, '0')); to show HEX values
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;

                case MetaStructureEntryDataType.UnsignedByte:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n;
                        byte b = ((bidx >= 0) && (bidx < data.Length)) ? data[bidx] : (byte)0;
                        sb.Append(b.ToString());
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
                case MetaStructureEntryDataType.SignedShort:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n * 2;
                        short b = ((bidx >= 0) && (bidx < data.Length)) ? BitConverter.ToInt16(data, bidx) : (short)0;
                        sb.Append(b.ToString());
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedShort:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n * 2;
                        ushort b = ((bidx >= 0) && (bidx < data.Length)) ? BitConverter.ToUInt16(data, bidx) : (ushort)0;
                        sb.Append(b.ToString());
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
                case MetaStructureEntryDataType.SignedInt:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n * 4;
                        int b = ((bidx >= 0) && (bidx < data.Length)) ? BitConverter.ToInt32(data, bidx) : (int)0;
                        sb.Append(b.ToString());
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
                case MetaStructureEntryDataType.UnsignedInt:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n * 4;
                        uint b = ((bidx >= 0) && (bidx < data.Length)) ? BitConverter.ToUInt32(data, bidx) : (uint)0;
                        sb.Append(b.ToString());
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
                case MetaStructureEntryDataType.Float:
                    for (int n = 0; n < byteArrLen; n++)
                    {
                        int bidx = eoffset + n * 4;
                        float b = ((bidx >= 0) && (bidx < data.Length)) ? BitConverter.ToSingle(data, bidx) : (float)0;
                        sb.Append(FloatUtil.ToString(b));
                        if (n < byteArrLen - 1) sb.Append(" ");
                    }
                    break;
            }

            CloseTag(sb, 0, ename);
        }



        private static string GetEnumString(MetaCont cont, MetaStructureEntryInfo_s entry, int value)
        {
            MetaName eName = entry.ReferenceKey;
            MetaEnumInfo eInfo = cont.GetEnumInfo(eName);
            if ((eInfo == null) || (eInfo.Entries == null))
            {
                return value.ToString();
            }

            bool isFlags = (entry.DataType == MetaStructureEntryDataType.IntFlags1) ||
                           (entry.DataType == MetaStructureEntryDataType.IntFlags2);// ||
                           //(entry.DataType == MetaStructureEntryDataType.ShortFlags);

            if (isFlags)
            {
                StringBuilder sb = new StringBuilder();
                foreach (MetaEnumEntryInfo_s ev in eInfo.Entries)
                {
                    int v = ev.EntryValue;
                    int m = 1 << v;
                    if ((value & m) > 0)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(HashString(ev.EntryNameHash));
                    }
                }
                return sb.ToString();
            }
            else
            {
                foreach (MetaEnumEntryInfo_s ev in eInfo.Entries)
                {
                    if (ev.EntryValue == value)
                    {
                        return HashString(ev.EntryNameHash);
                    }
                }
                return value.ToString(); //if we got here, there was no match...
            }
        }




        private class MetaCont
        {
            public Meta Meta { get; set; }

            Dictionary<MetaName, MetaStructureInfo> structInfos = new Dictionary<MetaName, MetaStructureInfo>();
            Dictionary<MetaName, MetaEnumInfo> enumInfos = new Dictionary<MetaName, MetaEnumInfo>();

            public MetaCont(Meta meta)
            {
                Meta = meta;

                if (meta.StructureInfos?.Data != null)
                {
                    foreach (MetaStructureInfo si in meta.StructureInfos)
                    {
                        structInfos[si.StructureNameHash] = si;
                    }
                }
                if (meta.EnumInfos?.Data != null)
                {
                    foreach (MetaEnumInfo ei in meta.EnumInfos)
                    {
                        enumInfos[ei.EnumNameHash] = ei;
                    }
                }
            }

            public MetaStructureInfo GetStructureInfo(MetaName name)
            {
                MetaStructureInfo i = null;
                structInfos.TryGetValue(name, out i);
                return i;
            }
            public MetaEnumInfo GetEnumInfo(MetaName name)
            {
                MetaEnumInfo i = null;
                enumInfos.TryGetValue(name, out i);
                return i;
            }

        }

    }

    public class PsoXml : MetaXmlBase
    {

        public static string GetXml(PsoFile pso)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(XmlHeader);

            if ((pso != null) && (pso.DataSection != null) && (pso.DataMapSection != null))
            {
                PsoCont cont = new PsoCont(pso);

                WriteNode(sb, 0, cont, pso.DataMapSection.RootId, 0, XmlTagMode.Structure);
            }

            return sb.ToString();
        }


        private static void WriteNode(StringBuilder sb, int indent, PsoCont cont, int blockId, int offset, XmlTagMode tagMode = XmlTagMode.None, MetaName structName = 0)
        {

            PsoDataMappingEntry block = cont.Pso.GetBlock(blockId);
            if (block == null)
            {
                ErrorXml(sb, indent, "Couldn't find block " + blockId + "!");
                return;
            }

            if (offset >= block.Length)
            {
                offset = offset >> 8; //how to tell when to do this??
            }

            int boffset = offset + block.Offset;

            if (structName == 0)
            {
                structName = block.NameHash;
            }

            string name = HashString(structName);
            byte[] data = cont.Pso.DataSection.Data;

            PsoStructureInfo structInfo = cont.GetStructureInfo(structName);
            if (structInfo == null)
            {
                structInfo = PsoTypes.GetStructureInfo(structName);//fallback to builtin...
            }
            if (structInfo == null)
            {
                ErrorXml(sb, indent, "Couldn't find structure info " + name + "!");
                return;
            }
            if (structInfo.Entries == null)
            {
                ErrorXml(sb, indent, "Couldn't find structure info entries for " + name + "!");
                return;
            }

            switch (tagMode)
            {
                case XmlTagMode.Structure:
                    OpenTag(sb, indent, name);
                    break;
                case XmlTagMode.Item:
                    OpenTag(sb, indent, "Item");
                    break;
                case XmlTagMode.ItemAndType:
                    OpenTag(sb, indent, "Item type=\"" + name + "\"");
                    break;
            }


            int cind = indent + 1;
            for (int i = 0; i < structInfo.Entries.Length; i++)
            {
                PsoStructureEntryInfo entry = structInfo.Entries[i];
                if (entry.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO)
                {
                    continue;
                }
                string ename = HashString(entry.EntryNameHash);
                int eoffset = boffset + entry.DataOffset;
                switch (entry.Type)
                {
                    default:
                        ErrorXml(sb, cind, ename + ": Unexpected entry DataType: " + entry.Type.ToString());
                        break;
                    case PsoDataType.Array:

                        WriteArrayNode(sb, cind, cont, blockId, offset, entry, structInfo, ename);

                        break;
                    case PsoDataType.Bool:
                        bool boolVal = BitConverter.ToBoolean(data, eoffset);
                        ValueTag(sb, cind, ename, boolVal?"true":"false");
                        break;
                    case PsoDataType.SByte: //was LONG_01h //signed byte?
                        //var long1Val = MetaTypes.SwapBytes(BitConverter.ToUInt64(data, eoffset));
                        //ValueTag(sb, cind, ename, long1Val.ToString());
                        sbyte byte1Val = (sbyte)data[eoffset];
                        ValueTag(sb, cind, ename, byte1Val.ToString());
                        break;
                    case PsoDataType.UByte:
                        byte byte2Val = data[eoffset];
                        ValueTag(sb, cind, ename, byte2Val.ToString());
                        break;
                    case PsoDataType.Enum:
                        PsoEnumInfo enumInfo = cont.GetEnumInfo((MetaName)entry.ReferenceKey);
                        switch (entry.Unk_5h)
                        {
                            default:
                                ErrorXml(sb, cind, ename + ": Unexpected Enum subtype: " + entry.Unk_5h.ToString());
                                break;
                            case 0: //int enum
                                int intEVal = MetaTypes.SwapBytes(BitConverter.ToInt32(data, eoffset));
                                PsoEnumEntryInfo intE = enumInfo.FindEntry(intEVal);
                                string intH = HashString(intE?.EntryNameHash ?? 0);
                                if (string.IsNullOrEmpty(intH))
                                { }
                                StringTag(sb, cind, ename, intH);
                                break;
                            case 2: //byte enum
                                byte byteEVal = data[eoffset];
                                PsoEnumEntryInfo byteE = enumInfo.FindEntry(byteEVal);
                                StringTag(sb, cind, ename, HashString(byteE?.EntryNameHash ?? 0));
                                break;
                        }
                        break;
                    case PsoDataType.Flags:
                        //uint fCount = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                        uint fEntry = (entry.ReferenceKey & 0xFFF);
                        PsoStructureEntryInfo fEnt = (fEntry != 0xFFF) ? structInfo.GetEntry((int)fEntry) : null;
                        PsoEnumInfo flagsInfo = null;
                        if ((fEnt != null) && (fEnt.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO))
                        {
                            flagsInfo = cont.GetEnumInfo((MetaName)fEnt.ReferenceKey);
                        }
                        if (flagsInfo == null)
                        {
                            if (fEntry != 0xFFF)
                            { }
                            //flagsInfo = cont.GetEnumInfo(entry.EntryNameHash);
                        }
                        uint? flagsVal = null;
                        switch (entry.Unk_5h)
                        {
                            default:
                                ErrorXml(sb, cind, ename + ": Unexpected Flags subtype: " + entry.Unk_5h.ToString());
                                break;
                            case 0: //int flags
                                flagsVal = MetaTypes.SwapBytes(BitConverter.ToUInt32(data, eoffset));
                                break;
                            case 1: //short flags
                                flagsVal = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset));
                                break;
                            case 2: //byte flags
                                flagsVal = data[eoffset];
                                break;
                        }
                        if (flagsVal.HasValue)
                        {
                            uint fv = flagsVal.Value;
                            if (flagsInfo != null)
                            {
                                string fstr = "";
                                for (int n = 0; n < flagsInfo.EntriesCount; n++)
                                {
                                    PsoEnumEntryInfo fentry = flagsInfo.Entries[n];
                                    int fmask = (1 << fentry.EntryKey);
                                    if ((fv & fmask) > 0)
                                    {
                                        if (fstr != "") fstr += " ";
                                        fstr += HashString(fentry.EntryNameHash);
                                    }
                                }
                                StringTag(sb, cind, ename, fstr);
                            }
                            else
                            {
                                if (fv != 0) ValueTag(sb, cind, ename, fv.ToString());
                                else SelfClosingTag(sb, cind, ename);
                            }
                        }
                        break;
                    case PsoDataType.Float:
                        float floatVal = MetaTypes.SwapBytes(BitConverter.ToSingle(data, eoffset));
                        ValueTag(sb, cind, ename, FloatUtil.ToString(floatVal));
                        break;
                    case PsoDataType.Float2:
                        Vector2 v2 = MetaTypes.SwapBytes(MetaTypes.ConvertData<Vector2>(data, eoffset));
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v2.X) + "\" y=\"" + FloatUtil.ToString(v2.Y) + "\"");
                        break;
                    case PsoDataType.Float3:
                        Vector3 v3 = MetaTypes.SwapBytes(MetaTypes.ConvertData<Vector3>(data, eoffset));
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v3.X) + "\" y=\"" + FloatUtil.ToString(v3.Y) + "\" z=\"" + FloatUtil.ToString(v3.Z) + "\"");
                        break;
                    case PsoDataType.Float3a: //TODO: check this!
                        Vector3 v3a = MetaTypes.SwapBytes(MetaTypes.ConvertData<Vector3>(data, eoffset));
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v3a.X) + "\" y=\"" + FloatUtil.ToString(v3a.Y) + "\" z=\"" + FloatUtil.ToString(v3a.Z) + "\"");
                        break;
                    case PsoDataType.Float4a: //TODO: check this! //...why are there 3 different types of float3?
                        Vector3 v3b = MetaTypes.SwapBytes(MetaTypes.ConvertData<Vector3>(data, eoffset));
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v3b.X) + "\" y=\"" + FloatUtil.ToString(v3b.Y) + "\" z=\"" + FloatUtil.ToString(v3b.Z) + "\"");
                        break;
                    case PsoDataType.Float4:
                        Vector4 v4 = MetaTypes.SwapBytes(MetaTypes.ConvertData<Vector4>(data, eoffset));
                        SelfClosingTag(sb, cind, ename + " x=\"" + FloatUtil.ToString(v4.X) + "\" y=\"" + FloatUtil.ToString(v4.Y) + "\" z=\"" + FloatUtil.ToString(v4.Z) + "\" w=\"" + FloatUtil.ToString(v4.W) + "\"");
                        break;
                    case PsoDataType.SInt: //TODO: convert hashes?
                        int int5Val = MetaTypes.SwapBytes(BitConverter.ToInt32(data, eoffset));
                        ValueTag(sb, cind, ename, int5Val.ToString());
                        break;
                    case PsoDataType.UInt:
                        switch (entry.Unk_5h)
                        {
                            default:
                                ErrorXml(sb, cind, ename + ": Unexpected Integer subtype: " + entry.Unk_5h.ToString());
                                break;
                            case 0: //signed int (? flags?)
                                int int6aVal = MetaTypes.SwapBytes(BitConverter.ToInt32(data, eoffset));
                                ValueTag(sb, cind, ename, int6aVal.ToString());
                                break;
                            case 1: //unsigned int
                                uint int6bVal = MetaTypes.SwapBytes(BitConverter.ToUInt32(data, eoffset));
                                //ValueTag(sb, cind, ename, int6bVal.ToString());
                                ValueTag(sb, cind, ename, "0x" + int6bVal.ToString("X").PadLeft(8, '0'));
                                break;
                        }
                        break;
                    case PsoDataType.Long:
                        ulong long2Val = MetaTypes.SwapBytes(BitConverter.ToUInt64(data, eoffset));
                        ValueTag(sb, cind, ename, long2Val.ToString());
                        break;
                    case PsoDataType.Map:

                        WriteMapNode(sb, indent, cont, eoffset, entry, structInfo, ename);

                        break;
                    case PsoDataType.SShort:
                        short short3Val = (short)MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset));
                        ValueTag(sb, cind, ename, short3Val.ToString());
                        break;
                    case PsoDataType.UShort:
                        ushort short4Val = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset));
                        ValueTag(sb, cind, ename, short4Val.ToString());
                        break;
                    case PsoDataType.HFloat://half float?
                        short short1EVal = MetaTypes.SwapBytes(BitConverter.ToInt16(data, eoffset));
                        ValueTag(sb, cind, ename, short1EVal.ToString());
                        break;
                    case PsoDataType.String:
                        string str0 = XmlEscape(GetStringValue(cont.Pso, entry, data, eoffset));
                        //if (str0 == null)
                        //{
                        //    ErrorXml(sb, cind, ename + ": Unexpected String subtype: " + entry.Unk_5h.ToString());
                        //}
                        //else
                        {
                            StringTag(sb, cind, ename, str0);
                        }
                        break;
                    case PsoDataType.Structure:
                        switch (entry.Unk_5h)
                        {
                            default:
                                ErrorXml(sb, cind, ename + ": Unexpected Structure subtype: " + entry.Unk_5h.ToString());
                                break;
                            case 0: //default structure
                                OpenTag(sb, cind, ename);
                                WriteNode(sb, cind, cont, blockId, offset + entry.DataOffset, XmlTagMode.None, (MetaName)entry.ReferenceKey);
                                CloseTag(sb, cind, ename);
                                break;
                            case 3: //structure pointer...
                            case 4: //also pointer? what's the difference?
                                PsoPOINTER ptrVal = MetaTypes.ConvertData<PsoPOINTER>(data, eoffset);
                                ptrVal.SwapEnd();
                                ushort pbid = ptrVal.BlockID;
                                bool pbok = true;
                                if (pbid <= 0)
                                {
                                    pbok = false; //no block specified?
                                }
                                if (pbid > cont.Pso.DataMapSection.EntriesCount)
                                {
                                    pbok = false; //bad pointer? different type..? should output an error message here?
                                }
                                if (pbok)
                                {
                                    string typename = HashString(cont.Pso.GetBlock(pbid).NameHash);
                                    OpenTag(sb, cind, ename + " type=\"" + typename + "\"");
                                    WriteNode(sb, cind, cont, ptrVal.BlockID, (int)ptrVal.ItemOffset, XmlTagMode.None, (MetaName)entry.ReferenceKey);
                                    CloseTag(sb, cind, ename);
                                }
                                else
                                {
                                    SelfClosingTag(sb, cind, ename);
                                }
                                break;
                        }
                        break;
                }
            }




            switch (tagMode)
            {
                case XmlTagMode.Structure:
                    CloseTag(sb, indent, name);
                    break;
                case XmlTagMode.Item:
                case XmlTagMode.ItemAndType:
                    CloseTag(sb, indent, "Item");
                    break;
            }
        }

        private static void WriteArrayNode(StringBuilder sb, int indent, PsoCont cont, int blockId, int offset, PsoStructureEntryInfo entry, PsoStructureInfo estruct, string ename)
        {


            PsoDataMappingEntry block = cont.Pso.GetBlock(blockId);
            int boffset = offset + block.Offset;
            int eoffset = boffset + entry.DataOffset;
            int aOffset = offset + entry.DataOffset;
            int abOffset = aOffset + block.Offset;
            int aBlockId = blockId;
            uint aCount = (entry.ReferenceKey >> 16) & 0x0000FFFF;
            Array_Structure arrStruc = new Array_Structure();
            arrStruc.PointerDataId = (uint)aBlockId;
            arrStruc.PointerDataOffset = (uint)aOffset;
            arrStruc.Count1 = arrStruc.Count2 = (ushort)aCount;
            int aind = indent + 1;
            string arrTag = ename;
            uint arrEntInd = (entry.ReferenceKey & 0xFFFF);
            if (arrEntInd >= estruct.EntriesCount)
            {
                arrEntInd = (entry.ReferenceKey & 0xFFF);
            }
            PsoStructureEntryInfo arrEntry = estruct.GetEntry((int)arrEntInd);
            if (arrEntry == null)
            {
                ErrorXml(sb, indent, "ARRAYINFO not found for " + ename + "!");
                return;
            }

            byte[] data = cont.Pso.DataSection.Data;

            bool embedded = true;
            switch (entry.Unk_5h)
            {
                default:
                    ErrorXml(sb, indent, ename + ": WIP! Unsupported Array subtype: " + entry.Unk_5h.ToString());
                    break;
                case 0: //Array_Structure
                    arrStruc = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                    arrStruc.SwapEnd();
                    aBlockId = (int)arrStruc.PointerDataId;
                    aOffset = (int)arrStruc.PointerDataOffset;
                    aCount = arrStruc.Count1;
                    PsoDataMappingEntry aBlock = cont.Pso.GetBlock(aBlockId);
                    if (aBlock != null)
                    {
                        abOffset = aOffset + aBlock.Offset;
                    }
                    embedded = false;
                    break;
                case 1: //Raw in-line array
                    break;
                case 2: //also raw in-line array, but how different from above?
                    break;
                case 4: //pointer array? default array?
                    if (arrEntry.Unk_5h == 3) //pointers...
                    {
                        arrStruc = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                        arrStruc.SwapEnd();
                        aBlockId = (int)arrStruc.PointerDataId;
                        aOffset = (int)arrStruc.PointerDataOffset;
                        aCount = arrStruc.Count1;
                        PsoDataMappingEntry aBlock2 = cont.Pso.GetBlock(aBlockId);
                        if (aBlock2 != null)
                        {
                            abOffset = aOffset + aBlock2.Offset;
                        }
                        embedded = false;
                    }
                    else
                    {
                    }
                    break;
                case 129: //also raw inline array? in junctions.pso
                    break;
            }

            switch (arrEntry.Type)
            {
                default:
                    ErrorXml(sb, indent, ename + ": WIP! Unsupported array entry DataType: " + arrEntry.Type.ToString());
                    break;
                case PsoDataType.Array:
                    uint rk0 = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                    //var rk1 = entry.ReferenceKey & 0x0000FFFF;
                    //var rk3 = (arrEntry.ReferenceKey >> 16) & 0x0000FFFF;
                    //var rk4 = arrEntry.ReferenceKey & 0x0000FFFF;
                    if (rk0 > 0)
                    {
                        aOffset = offset + entry.DataOffset;

                        OpenTag(sb, indent, arrTag);
                        for (int n = 0; n < rk0; n++) //ARRAY ARRAY!
                        {
                            WriteArrayNode(sb, aind, cont, blockId, aOffset, arrEntry, estruct, "Item");

                            aOffset += 16;//ptr size... todo: what if not pointer array?
                        }
                        CloseTag(sb, indent, ename);
                    }
                    else
                    {
                        SelfClosingTag(sb, indent, arrTag);
                    }
                    break;
                case PsoDataType.Structure:
                    switch (arrEntry.Unk_5h)
                    {
                        case 0:
                            break;
                        case 3://structure pointer array
                            Array_StructurePointer arrStrucPtr = MetaTypes.ConvertData<Array_StructurePointer>(data, eoffset);
                            arrStrucPtr.SwapEnd();
                            aBlockId = (int)arrStrucPtr.PointerDataId;
                            aOffset = (int)arrStrucPtr.PointerDataOffset;
                            aCount = arrStrucPtr.Count1;
                            if (aCount > 0)
                            {
                                PsoPOINTER[] ptrArr = PsoTypes.GetPointerArray(cont.Pso, arrStrucPtr);
                                OpenTag(sb, indent, arrTag);
                                for (int n = 0; n < aCount; n++)
                                {
                                    PsoPOINTER ptrVal = ptrArr[n];
                                    if (ptrVal.Pointer == 0)
                                    {
                                        SelfClosingTag(sb, aind, "Item"); //"null" entry...
                                    }
                                    else
                                    {
                                        WriteNode(sb, aind, cont, ptrVal.BlockID, (int)ptrVal.ItemOffset, XmlTagMode.ItemAndType);
                                    }
                                }
                                CloseTag(sb, indent, ename);
                            }
                            break;
                        default:
                            break;
                    }
                    arrTag += " itemType=\"" + HashString((MetaName)arrEntry.ReferenceKey) + "\"";
                    if (aCount > 0)
                    {
                        PsoDataMappingEntry aBlock = cont.Pso.GetBlock(aBlockId);
                        PsoStructureInfo atyp = cont.GetStructureInfo((MetaName)arrEntry.ReferenceKey);
                        if (aBlock == null)
                        {
                            ErrorXml(sb, indent, ename + ": Array block not found: " + aBlockId.ToString());
                        }
                        else if (aBlock.NameHash != (MetaName)MetaTypeName.PsoPOINTER)
                        {
                            OpenTag(sb, indent, arrTag);
                            if (atyp == null)
                            {
                                ErrorXml(sb, indent, ename + ": Array type not found: " + HashString((MetaHash)arrEntry.ReferenceKey));
                            }
                            else
                            {
                                for (int n = 0; n < aCount; n++)
                                {
                                    WriteNode(sb, aind, cont, aBlockId, aOffset, XmlTagMode.Item, (MetaName)arrEntry.ReferenceKey);
                                    aOffset += atyp.StructureLength;
                                    if ((n < (aCount - 1)) && (aBlock != null) && (aOffset >= aBlock.Length))
                                    {
                                        break;
                                    }
                                }
                            }
                            CloseTag(sb, indent, ename);
                        }
                        else
                        { } //pointer array should get here, but it's already handled above. should improve this.
                    }
                    else
                    {
                        SelfClosingTag(sb, indent, arrTag);
                    }
                    break;
                case PsoDataType.String:
                    switch (arrEntry.Unk_5h)
                    {
                        default:
                            ErrorXml(sb, indent, ename + ": Unexpected String array subtype: " + entry.Unk_5h.ToString());
                            break;
                        case 7: //hash array...
                        case 8:
                            if (embedded)
                            { }
                            Array_uint arrHash = MetaTypes.ConvertData<Array_uint>(data, eoffset);
                            arrHash.SwapEnd();
                            MetaHash[] hashArr = PsoTypes.GetHashArray(cont.Pso, arrHash);
                            WriteItemArray(sb, hashArr, indent, ename, "Hash", HashString);
                            break;
                        case 2: //string array  (array of pointers)
                            Array_Structure arrStrs = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                            arrStrs.SwapEnd();
                            DataBlockPointer[] strPtrArr = PsoTypes.GetItemArrayRaw<DataBlockPointer>(cont.Pso, arrStrs);
                            string[] strs = (strPtrArr != null) ? new string[strPtrArr.Length] : null;
                            int cnt = strPtrArr?.Length ?? 0;
                            for (int i = 0; i < cnt; i++)
                            {
                                strPtrArr[i].SwapEnd();
                                strs[i] = PsoTypes.GetString(cont.Pso, strPtrArr[i]);
                            }
                            WriteStringItemArray(sb, strs, indent, ename);
                            break;
                        case 3: //char array array  (array of CharPointer)
                            Array_Structure arrChars = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                            arrChars.SwapEnd();
                            CharPointer[] charPtrArr = PsoTypes.GetItemArrayRaw<CharPointer>(cont.Pso, arrChars);//namehash 200
                            string[] strs2 = (charPtrArr != null) ? new string[charPtrArr.Length] : null;
                            int cnt2 = charPtrArr?.Length ?? 0;
                            for (int i = 0; i < cnt2; i++)
                            {
                                charPtrArr[i].SwapEnd();
                                strs2[i] = PsoTypes.GetString(cont.Pso, charPtrArr[i]);
                            }
                            WriteStringItemArray(sb, strs2, indent, ename);
                            break;
                    }
                    break;
                case PsoDataType.Float2:
                    aCount = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                    arrTag += " itemType=\"Vector2\"";
                    Vector2[] v2Arr = MetaTypes.ConvertDataArray<Vector2>(data, eoffset, (int)aCount);
                    WriteRawArray(sb, v2Arr, indent, ename, "Vector2", FormatVector2Swap, 1);
                    break;
                case PsoDataType.Float3:
                    if (!embedded)
                    { }
                    arrTag += " itemType=\"Vector3\""; //this is actually aligned as vector4, the W values are crazy in places
                    Vector4[] v4Arr = MetaTypes.ConvertDataArray<Vector4>(data, eoffset, (int)aCount);
                    WriteRawArray(sb, v4Arr, indent, ename, "Vector3", FormatVector4SwapXYZOnly, 1);
                    break;
                case PsoDataType.UByte:
                    if (embedded)
                    { }
                    else
                    { } //block type 2
                    byte[] barr = new byte[aCount];
                    if (aCount > 0)
                    {
                        //var bblock = cont.Pso.GetBlock(aBlockId);
                        //var boffs = bblock.Offset + aOffset;
                        Buffer.BlockCopy(data, abOffset /*boffs*/, barr, 0, (int)aCount);
                    }
                    WriteRawArray(sb, barr, indent, ename, "byte");
                    break;
                case PsoDataType.Bool:
                    if (embedded)
                    { }
                    else
                    { }
                    byte[] barr2 = new byte[aCount];
                    if (aCount > 0)
                    {
                        //var bblock = cont.Pso.GetBlock(aBlockId);
                        //var boffs = bblock.Offset + aOffset;
                        Buffer.BlockCopy(data, abOffset /*boffs*/, barr2, 0, (int)aCount);
                    }
                    WriteRawArray(sb, barr2, indent, ename, "boolean"); //todo: true/false output
                    break;
                case PsoDataType.Float:
                    if (embedded)
                    { }
                    Array_float arrFloat = new Array_float(arrStruc.Pointer, arrStruc.Count1); //block type 7
                    float[] floatArr = PsoTypes.GetFloatArray(cont.Pso, arrFloat);
                    WriteRawArray(sb, floatArr, indent, ename, "float", FloatUtil.ToString);
                    break;
                case PsoDataType.UShort:
                    if (embedded)
                    { }
                    ushort[] shortArr = PsoTypes.GetUShortArray(cont.Pso, arrStruc); //block type 4
                    WriteRawArray(sb, shortArr, indent, ename, "ushort");
                    break;
                case PsoDataType.UInt:
                    if (embedded)
                    { }
                    Array_uint arrUint = new Array_uint(arrStruc.Pointer, arrStruc.Count1); //block type 6
                    uint[] intArr = PsoTypes.GetUintArray(cont.Pso, arrUint);
                    WriteRawArray(sb, intArr, indent, ename, "int");
                    break;
                case PsoDataType.SInt:
                    if (embedded)
                    { }
                    Array_uint arrUint2 = new Array_uint(arrStruc.Pointer, arrStruc.Count1); //block type 5
                    uint[] intArr2 = PsoTypes.GetUintArray(cont.Pso, arrUint2);
                    WriteRawArray(sb, intArr2, indent, ename, "int");
                    break;
                case PsoDataType.Enum:
                    if (embedded)
                    { }
                    Array_uint arrEnum = MetaTypes.ConvertData<Array_uint>(data, eoffset);
                    arrEnum.SwapEnd();
                    uint[] enumArr = PsoTypes.GetUintArray(cont.Pso, arrEnum);
                    PsoEnumInfo enumDef = cont.GetEnumInfo((MetaName)arrEntry.ReferenceKey);
                    WriteItemArray(sb, enumArr, indent, ename, "enum", (ie)=> {
                        PsoEnumEntryInfo eval = enumDef?.FindEntry((int)ie);
                        return HashString(eval?.EntryNameHash ?? 0);
                    });
                    break;
            }

        }

        private static void WriteMapNode(StringBuilder sb, int indent, PsoCont cont, int eoffset, PsoStructureEntryInfo entry, PsoStructureInfo structInfo, string ename)
        {
            int cind = indent + 1;
            byte[] data = cont.Pso.DataSection.Data;
            switch (entry.Unk_5h)
            {
                default:
                    ErrorXml(sb, cind, ename + ": Unexpected Map subtype: " + entry.Unk_5h.ToString());
                    break;
                case 1:
                    uint mapidx1 = entry.ReferenceKey & 0x0000FFFF;
                    uint mapidx2 = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                    PsoStructureEntryInfo mapreftype1 = structInfo.Entries[mapidx2];
                    PsoStructureEntryInfo mapreftype2 = structInfo.Entries[mapidx1];
                    int x1 = MetaTypes.SwapBytes(BitConverter.ToInt32(data, eoffset));
                    int x2 = MetaTypes.SwapBytes(BitConverter.ToInt32(data, eoffset + 4));
                    Array_Structure sptr = MetaTypes.ConvertData<Array_Structure>(data, eoffset + 8);
                    sptr.SwapEnd();


                    if (x1 != 0x1000000)
                    {
                        ushort c1 = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset));
                        ushort c2 = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset + 2));
                        uint u1 = MetaTypes.SwapBytes(BitConverter.ToUInt32(data, eoffset + 4));
                        ushort c3 = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset + 8));
                        ushort c4 = MetaTypes.SwapBytes(BitConverter.ToUInt16(data, eoffset + 10));
                        uint u3 = MetaTypes.SwapBytes(BitConverter.ToUInt32(data, eoffset + 12));
                        ulong ptr = MetaTypes.SwapBytes(BitConverter.ToUInt64(data, eoffset + 16));
                        sptr = new Array_Structure(ptr, c2);
                        if (c3 != 256)
                        { }
                        if (c1 != c2)
                        { }
                    }
                    if (x2 != 0)
                    { }
                    if (mapreftype2.ReferenceKey != 0)
                    { }

                    int xBlockId = (int)sptr.PointerDataId;// x3 & 0xFFF;
                    uint xOffset = sptr.PointerDataOffset;// (x3 >> 12) & 0xFFFFF;
                    ushort xCount1 = sptr.Count1;// x5 & 0xFFFF;
                    ushort xCount2 = sptr.Count2;// (x5 >> 16) & 0xFFFF;

                    PsoDataMappingEntry xBlock = cont.Pso.GetBlock(xBlockId);
                    if ((xBlock == null) && (xCount1 > 0))
                    {
                        ErrorXml(sb, cind, ename + ": Couldn't find Map xBlock: " + xBlockId.ToString());
                    }
                    else
                    {
                        if (xCount1 != xCount2)
                        { }
                        if (xCount1 > 0)
                        {
                            PsoStructureInfo xStruct = cont.GetStructureInfo(xBlock.NameHash);
                            int xind = indent + 1;
                            int aind = indent + 2;
                            PsoStructureEntryInfo kEntry = xStruct?.FindEntry(MetaName.Key);
                            PsoStructureEntryInfo iEntry = xStruct?.FindEntry(MetaName.Item);

                            if (xOffset >= xBlock.Length)
                            {
                                xOffset = xOffset >> 8; //how to tell when to do this??
                            }

                            if ((xStruct == null) && (xBlock.NameHash == 0))
                            {
                                SelfClosingTag(sb, cind, ename);
                            }
                            else if (xStruct == null)
                            {
                                ErrorXml(sb, aind, ename + ": Map struct type not found: " + HashString(xBlock.NameHash));
                            }
                            else if ((xStruct.IndexInfo == null))// || (xStruct.IndexInfo.NameHash != (MetaName)MetaTypeName.ARRAYINFO))
                            {
                                ErrorXml(sb, aind, ename + ": Map struct was missing IndexInfo! " + (xStruct == null ? "" : xStruct.ToString()));
                            }
                            else if ((kEntry == null) || (iEntry == null))
                            {
                                ErrorXml(sb, aind, ename + ": Map Key/Item entries not found!");
                            }
                            else if (kEntry.Type != PsoDataType.String)
                            {
                                ErrorXml(sb, aind, ename + ": Map Key was not a string!");
                            }
                            else if ((iEntry.Type != PsoDataType.Structure) && (iEntry.Type != PsoDataType.String))
                            {
                                ErrorXml(sb, aind, ename + ": Map Item was not a structure or string!");
                            }
                            //else if (iEntry.Unk_5h != 3)
                            //{
                            //    ErrorXml(sb, aind, ename + ": Map Item was not a structure pointer - TODO!");
                            //}
                            else
                            {
                                OpenTag(sb, xind, ename);

                                for (int n = 0; n < xCount1; n++)
                                {
                                    if (xOffset >= xBlock.Length)
                                    {
                                        ErrorXml(sb, aind, "Offset out of range! Count is " + xCount1.ToString());
                                        break; //out of range...
                                    }
                                    //WriteNode(sb, aind, cont, xBlockId, xOffset, XmlTagMode.Item, xStruct.IndexInfo.NameHash);

                                    int sOffset = (int)xOffset + xBlock.Offset;
                                    int kOffset = sOffset + kEntry.DataOffset;
                                    int iOffset = sOffset + iEntry.DataOffset;
                                    string kStr = GetStringValue(cont.Pso, kEntry, data, kOffset);
                                    if (iEntry.Type == PsoDataType.String)
                                    {
                                        string iStr = GetStringValue(cont.Pso, iEntry, data, iOffset);
                                        OpenTag(sb, aind, "Item type=\"String\" key=\"" + kStr + "\"", false);
                                        sb.Append(XmlEscape(iStr));
                                        CloseTag(sb, 0, "Item");
                                    }
                                    else if (iEntry.ReferenceKey != 0)//(xBlock.NameHash != (MetaName)MetaTypeName.ARRAYINFO)//257,258,259
                                    {
                                        //embedded map values
                                        int vOffset = (int)xOffset + iEntry.DataOffset;
                                        OpenTag(sb, aind, "Item type=\"" + HashString((MetaName)iEntry.ReferenceKey) + "\" key=\"" + kStr + "\"");
                                        WriteNode(sb, aind, cont, xBlockId, vOffset, XmlTagMode.None, (MetaName)iEntry.ReferenceKey);
                                        CloseTag(sb, aind, "Item");
                                    }
                                    else
                                    {
                                        PsoPOINTER iPtr = MetaTypes.ConvertData<PsoPOINTER>(data, iOffset);
                                        iPtr.SwapEnd();
                                        PsoDataMappingEntry iBlock = cont.Pso.GetBlock(iPtr.BlockID);
                                        if (iBlock == null)
                                        {
                                            ErrorXml(sb, aind, ename + ": Could not find iBlock for Map entry!");
                                        }
                                        else
                                        {
                                            string iStr = "Item type=\"" + HashString(iBlock.NameHash) + "\" key=\"" + kStr + "\"";
                                            PsoStructureInfo iStruc = cont.GetStructureInfo(iBlock.NameHash);
                                            if (iStruc?.EntriesCount == 0)
                                            {
                                                //SelfClosingTag(sb, aind, iStr);
                                                OpenTag(sb, aind, iStr);
                                                CloseTag(sb, aind, "Item");
                                            }
                                            else
                                            {
                                                int iOff = (int)iPtr.ItemOffset;
                                                OpenTag(sb, aind, iStr);
                                                WriteNode(sb, aind, cont, iPtr.BlockID, iOff, XmlTagMode.None);//, (MetaName)entry.ReferenceKey);
                                                CloseTag(sb, aind, "Item");
                                            }
                                        }
                                    }
                                    xOffset += (uint)xStruct.StructureLength;
                                }
                                CloseTag(sb, xind, ename);
                            }
                        }
                        else
                        {
                            SelfClosingTag(sb, cind, ename);
                        }
                    }
                    break;
            }
        }




        private static string GetStringValue(PsoFile pso, PsoStructureEntryInfo entry, byte[] data, int eoffset)
        {
            switch (entry.Unk_5h)
            {
                default:
                    return null;
                case 0:
                    int str0len = (int)((entry.ReferenceKey >> 16) & 0xFFFF);
                    return Encoding.ASCII.GetString(data, eoffset, str0len).Replace("\0", "");
                case 1:
                case 2:
                    DataBlockPointer dataPtr2 = MetaTypes.ConvertData<DataBlockPointer>(data, eoffset);
                    dataPtr2.SwapEnd();
                    return PsoTypes.GetString(pso, dataPtr2);
                case 3:
                    CharPointer charPtr3 = MetaTypes.ConvertData<CharPointer>(data, eoffset);
                    charPtr3.SwapEnd();
                    string strval = PsoTypes.GetString(pso, charPtr3);
                    return strval ?? "";
                case 7:
                case 8:
                    MetaHash hashVal = (MetaHash)MetaTypes.SwapBytes(MetaTypes.ConvertData<MetaHash>(data, eoffset));
                    return HashString(hashVal);
            }

        }


        public class PsoCont
        {
            public PsoFile Pso { get; set; }

            public Dictionary<MetaName, PsoEnumInfo> EnumDict = new Dictionary<MetaName, PsoEnumInfo>();
            public Dictionary<MetaName, PsoStructureInfo> StructDict = new Dictionary<MetaName, PsoStructureInfo>();


            public PsoCont(PsoFile pso)
            {
                Pso = pso;

                if ((pso.SchemaSection == null) || (pso.SchemaSection.Entries == null) || (pso.SchemaSection.EntriesIdx == null))
                {
                    return;
                }


                for (int i = 0; i < pso.SchemaSection.Entries.Length; i++)
                {
                    PsoElementInfo entry = pso.SchemaSection.Entries[i];
                    PsoEnumInfo enuminfo = entry as PsoEnumInfo;
                    PsoStructureInfo structinfo = entry as PsoStructureInfo;

                    if (enuminfo != null)
                    {
                        if (!EnumDict.ContainsKey(enuminfo.IndexInfo.NameHash))
                        {
                            EnumDict.Add(enuminfo.IndexInfo.NameHash, enuminfo);
                        }
                        else
                        {
                            //PsoEnumInfo oldei = EnumDict[enuminfo.IndexInfo.NameHash];
                            //if (!ComparePsoEnumInfos(oldei, enuminfo))
                            //{
                            //}
                        }
                    }
                    else if (structinfo != null)
                    {
                        if (!StructDict.ContainsKey(structinfo.IndexInfo.NameHash))
                        {
                            StructDict.Add(structinfo.IndexInfo.NameHash, structinfo);
                        }
                        else
                        {
                            //PsoStructureInfo oldsi = StructDict[structinfo.IndexInfo.NameHash];
                            //if (!ComparePsoStructureInfos(oldsi, structinfo))
                            //{
                            //}
                        }
                    }

                }
            }


            public PsoStructureInfo GetStructureInfo(MetaName name)
            {
                PsoStructureInfo i = null;
                StructDict.TryGetValue(name, out i);
                return i;
            }
            public PsoEnumInfo GetEnumInfo(MetaName name)
            {
                PsoEnumInfo i = null;
                EnumDict.TryGetValue(name, out i);
                return i;
            }

        }

    }

    public class RbfXml : MetaXmlBase
    {

        public static string GetXml(RbfFile rbf)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(XmlHeader);

            WriteNode(sb, 0, rbf.current);

            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, int indent, RbfStructure rs)
        {
            string attStr = "";
            if (rs.Attributes.Count > 0)
            {
                StringBuilder asb = new StringBuilder();
                foreach (IRbfType attr in rs.Attributes)
                {
                    if (attr is RbfString str)
                    {
                        asb.Append($" {attr.Name}=\"{str.Value}\"");
                    }
                    else if (attr is RbfFloat flt)
                    {
                        asb.Append($" {attr.Name}=\"{FloatUtil.ToString(flt.Value)}\"");
                    }
                    else if (attr is RbfUint32 unt)
                    {
                        asb.Append($" {attr.Name}=\"{unt.Value.ToString()}\"");
                    }
                    else if (attr is RbfBoolean bln)
                    {
                        asb.Append($" {attr.Name}=\"{bln.Value.ToString()}\"");
                    }
                    else
                    { }
                }
                attStr = $"{asb.ToString()}";
            }

            if (rs.Children.Count == 0)
            {
                SelfClosingTag(sb, indent, rs.Name + attStr);
                return;
            }

            int cind = indent + 1;

            bool oneline = ((rs.Children.Count == 1) && (rs.Children[0].Name == null) && (rs.Attributes.Count == 0));

            OpenTag(sb, indent, rs.Name + attStr, !oneline);


            foreach (IRbfType child in rs.Children)
            {
                if (child is RbfBytes)
                {
                    RbfBytes bytesChild = (RbfBytes)child;
                    RbfString contentField = rs.FindAttribute("content") as RbfString;//TODO: fix this to output nicer XML!
                    if (contentField != null)
                    {
                        if (contentField.Value == "char_array")
                        {
                            foreach (byte k in bytesChild.Value)
                            {
                                Indent(sb, cind);
                                sb.AppendLine(k.ToString());
                            }
                        }
                        else if (contentField.Value.Equals("short_array"))
                        {
                            DataReader valueReader = new DataReader(new MemoryStream(bytesChild.Value));
                            while (valueReader.Position < valueReader.Length)
                            {
                                Indent(sb, cind);
                                ushort y = valueReader.ReadUInt16();
                                sb.AppendLine(y.ToString());
                            }
                        }
                        else
                        {
                            ErrorXml(sb, cind, "Unexpected content type: " + contentField.Value);
                        }
                    }
                    else
                    {
                        string stringValue = Encoding.ASCII.GetString(bytesChild.Value);
                        string str = stringValue.Substring(0, stringValue.Length - 1); //removes null terminator
                        sb.Append(str);
                    }
                }
                if (child is RbfFloat)
                {
                    RbfFloat floatChild = (RbfFloat)child;
                    ValueTag(sb, cind, child.Name, FloatUtil.ToString(floatChild.Value));
                }
                if (child is RbfString)
                {
                    ////// this doesn't seem to be used! it's always using RbfBytes child...

                    RbfString stringChild = (RbfString)child;
                    StringTag(sb, cind, stringChild.Name, stringChild.Value);

                    //if (stringChild.Name.Equals("content"))
                    //else if (stringChild.Name.Equals("type"))
                    //else throw new Exception("Unexpected string content");
                }
                if (child is RbfStructure)
                {
                    WriteNode(sb, cind, child as RbfStructure);
                }
                if (child is RbfUint32)
                {
                    RbfUint32 intChild = (RbfUint32)child;
                    ValueTag(sb, cind, intChild.Name, UintString(intChild.Value));
                }
                if (child is RbfBoolean)
                {
                    RbfBoolean booleanChild = (RbfBoolean)child;
                    ValueTag(sb, cind, booleanChild.Name, booleanChild.Value.ToString());
                }
                if (child is RbfFloat3)
                {
                    RbfFloat3 v3 = child as RbfFloat3;
                    SelfClosingTag(sb, cind, v3.Name + " x=\"" + FloatUtil.ToString(v3.X) + "\" y=\"" + FloatUtil.ToString(v3.Y) + "\" z=\"" + FloatUtil.ToString(v3.Z) + "\"");
                }


            }

            CloseTag(sb, oneline ? 0 : indent, rs.Name);
        }



    }


    public class MetaXmlBase
    {

        public const string XmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";



        public static void Indent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++)
            {
                sb.Append(" ");
            }
        }
        public static void ErrorXml(StringBuilder sb, int indent, string msg)
        {
            Indent(sb, indent);
            sb.Append("<error>");
            sb.Append(msg);
            sb.Append("</error>");
            sb.AppendLine();
        }
        public static void OpenTag(StringBuilder sb, int indent, string name, bool appendLine = true, string metaName = "")
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            if (string.IsNullOrWhiteSpace(metaName))
            {
                sb.Append(">");
            }
            else
            {
                sb.Append(" name=\"" + metaName + "\">");
            }
            if (appendLine) sb.AppendLine();
        }
        public static void CloseTag(StringBuilder sb, int indent, string name, bool appendLine = true)
        {
            Indent(sb, indent);
            sb.Append("</");
            sb.Append(name);
            sb.Append(">");
            if (appendLine) sb.AppendLine();
        }
        public static void ValueTag(StringBuilder sb, int indent, string name, string val, string attr = "value")
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            sb.Append(" ");
            sb.Append(attr);
            sb.Append("=\"");
            sb.Append(val);
            sb.Append("\" />");
            sb.AppendLine();
        }
        public static void OneLineTag(StringBuilder sb, int indent, string name, string text)
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(name);
            sb.Append(">");
            sb.Append(text);
            sb.Append("</");
            sb.Append(name);
            sb.Append(">");
            sb.AppendLine();
        }
        public static void SelfClosingTag(StringBuilder sb, int indent, string val)
        {
            Indent(sb, indent);
            sb.Append("<");
            sb.Append(val);
            sb.Append(" />");
            sb.AppendLine();
        }
        public static void StringTag(StringBuilder sb, int indent, string name, string text)
        {
            if (!string.IsNullOrEmpty(text)) OneLineTag(sb, indent, name, text);
            else SelfClosingTag(sb, indent, name);
        }

        public static void WriteRawArrayContent<T>(StringBuilder sb, T[] arr, int ind, Func<T, string> formatter = null, int arrRowSize = 10) where T : struct
        {
            int aCount = arr?.Length ?? 0;
            for (int n = 0; n < aCount; n++)
            {
                int col = n % arrRowSize;
                if (col == 0) Indent(sb, ind);
                if (col > 0) sb.Append(" ");
                string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                sb.Append(str);
                bool lastcol = (col == (arrRowSize - 1));
                bool lastn = (n == (aCount - 1));
                if (lastcol || lastn) sb.AppendLine();
            }
        }
        public static void WriteRawArray<T>(StringBuilder sb, T[] arr, int ind, string name, string typeName, Func<T, string> formatter = null, int arrRowSize = 10) where T : struct
        {
            int aCount = arr?.Length ?? 0;
            //var arrRowSize = 10;
            int aind = ind + 1;
            string arrTag = name;// + " itemType=\"" + typeName + "\"";
            if (aCount > 0)
            {
                if (aCount <= arrRowSize)
                {
                    OpenTag(sb, ind, arrTag, false);
                    for (int n = 0; n < aCount; n++)
                    {
                        if (n > 0) sb.Append(" ");
                        string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                        sb.Append(str);
                    }
                    CloseTag(sb, 0, name);
                }
                else
                {
                    OpenTag(sb, ind, arrTag);
                    for (int n = 0; n < aCount; n++)
                    {
                        int col = n % arrRowSize;
                        if (col == 0) Indent(sb, aind);
                        if (col > 0) sb.Append(" ");
                        string str = (formatter != null) ? formatter(arr[n]) : arr[n].ToString();
                        sb.Append(str);
                        bool lastcol = (col == (arrRowSize - 1));
                        bool lastn = (n == (aCount - 1));
                        if (lastcol || lastn) sb.AppendLine();
                    }
                    CloseTag(sb, ind, name);
                }
            }
            else
            {
                SelfClosingTag(sb, ind, arrTag);
            }
        }
        public static void WriteItemArray<T>(StringBuilder sb, T[] arr, int ind, string name, string typeName, Func<T, string> formatter) where T : struct
        {
            int aCount = arr?.Length ?? 0;
            string arrTag = name;// + " itemType=\"Hash\"";
            int aind = ind + 1;
            if (aCount > 0)
            {
                OpenTag(sb, ind, arrTag);
                for (int n = 0; n < aCount; n++)
                {
                    Indent(sb, aind);
                    sb.Append("<Item>");
                    sb.Append(formatter(arr[n]));
                    sb.AppendLine("</Item>");
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, arrTag);
            }
        }
        public static void WriteItemArray<T>(StringBuilder sb, T[] arr, int ind, string name) where T : IMetaXmlItem
        {
            int itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                int cind = ind + 1;
                int cind2 = ind + 2;
                for (int i = 0; i < itemCount; i++)
                {
                    if (arr[i] != null)
                    {
                        OpenTag(sb, cind, "Item");
                        arr[i].WriteXml(sb, cind2);
                        CloseTag(sb, cind, "Item");
                    }
                    else
                    {
                        SelfClosingTag(sb, cind, "Item");
                    }
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }
        public static void WriteCustomItemArray<T>(StringBuilder sb, T[] arr, int ind, string name) where T : IMetaXmlItem
        {
            int itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                int cind = ind + 1;
                for (int i = 0; i < itemCount; i++)
                {
                    if (arr[i] != null)
                    {
                        arr[i].WriteXml(sb, cind);
                    }
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }
        public static void WriteHashItemArray(StringBuilder sb, MetaHash[] arr, int ind, string name)
        {
            int itemCount = arr?.Length ?? 0;
            if (itemCount > 0)
            {
                OpenTag(sb, ind, name);
                int cind = ind + 1;
                for (int i = 0; i < itemCount; i++)
                {
                    string iname = HashString(arr[i]);
                    StringTag(sb, cind, "Item", iname);
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, name);
            }
        }
        public static void WriteStringItemArray(StringBuilder sb, string[] arr, int ind, string name)
        {
            int aCount = arr?.Length ?? 0;
            string arrTag = name;// + " itemType=\"Hash\"";
            int aind = ind + 1;
            if (aCount > 0)
            {
                OpenTag(sb, ind, arrTag);
                for (int n = 0; n < aCount; n++)
                {
                    Indent(sb, aind);
                    sb.Append("<Item>");
                    sb.Append(arr[n]);//TODO: escape this??!
                    sb.AppendLine("</Item>");
                }
                CloseTag(sb, ind, name);
            }
            else
            {
                SelfClosingTag(sb, ind, arrTag);
            }
        }

        public static string FormatHash(MetaHash h) //for use with WriteItemArray
        {
            string str = JenkIndex.TryGetString(h);
            if (!string.IsNullOrEmpty(str)) return str;
            str = GlobalText.TryGetString(h);
            if (!string.IsNullOrEmpty(str)) return str;
            return HashString(h);// "hash_" + h.Hex;
            //return h.ToString();
        }
        public static string FormatVector2(Vector2 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector2String(v);
        }
        public static string FormatVector3(Vector3 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector3String(v);
        }
        public static string FormatVector4(Vector4 v) //for use with WriteItemArray
        {
            return FloatUtil.GetVector4String(v);
        }
        public static string FormatHexByte(byte b)
        {
            return Convert.ToString(b, 16).ToUpperInvariant().PadLeft(2, '0'); //hex byte array
        }

        public static string FormatHashSwap(MetaHash h) //for use with WriteItemArray, swaps endianness
        {
            return MetaTypes.SwapBytes(h).ToString();
        }
        public static string FormatVector2Swap(Vector2 v) //for use with WriteItemArray, swaps endianness
        {
            return FloatUtil.GetVector2String(MetaTypes.SwapBytes(v));
        }
        public static string FormatVector3Swap(Vector3 v) //for use with WriteItemArray, swaps endianness
        {
            return FloatUtil.GetVector3String(MetaTypes.SwapBytes(v));
        }
        public static string FormatVector4Swap(Vector4 v) //for use with WriteItemArray, swaps endianness
        {
            return FloatUtil.GetVector4String(MetaTypes.SwapBytes(v));
        }
        public static string FormatVector4SwapXYZOnly(Vector4 v) //for use with WriteItemArray, swaps endianness, and outputs only XYZ components
        {
            return FloatUtil.GetVector3String(MetaTypes.SwapBytes(v.XYZ()));
        }



        public static string HashString(MetaName h)
        {
            uint uh = (uint)h;
            if (uh == 0) return "";

            string str;
            if (MetaNames.TryGetString(uh, out str)) return str;

            str = JenkIndex.TryGetString(uh);
            if (!string.IsNullOrEmpty(str)) return str;

            //TODO: do extra hash lookup here
            //if(Lookup.TryGetValue(uh, out str)) ...

            return "hash_" + uh.ToString("X").PadLeft(8, '0');

        }
        public static string HashString(MetaHash h)
        {
            if (h == 0) return "";

            string str = JenkIndex.TryGetString(h);

            if (string.IsNullOrEmpty(str))
            {
                if (MetaNames.TryGetString(h, out str)) return str;
            }

            //todo: make sure JenkIndex is built!
            //todo: do extra hash lookup here


            if (!string.IsNullOrEmpty(str)) return str;
            return "hash_" + h.Hex;
        }
        public static string HashString(TextHash h)
        {
            uint uh = h.Hash;
            if (uh == 0) return "";

            string str = GlobalText.TryGetString(uh);
            if (!string.IsNullOrEmpty(str)) return str;

            //TODO: do extra hash lookup here
            //if(Lookup.TryGetValue(uh, out str)) ...

            return "hash_" + uh.ToString("X").PadLeft(8, '0');
        }


        public static string UintString(uint h)
        {
            //string str;
            //if (MetaNames.TryGetString(h, out str)) return str;

            //str = JenkIndex.TryGetString(h);
            //if (!string.IsNullOrEmpty(str)) return str;

            ////TODO: do extra hash lookup here
            ////if(Lookup.TryGetValue(uh, out str)) ...


            //if (h == 0) return "";
            return "0x" + h.ToString("X");

        }




        public static string XmlEscape(string unescaped)
        {
            if (unescaped == null) return null;
            XmlDocument doc = new XmlDocument();
            XmlNode node = doc.CreateElement("root");
            node.InnerText = unescaped;
            string escaped = node.InnerXml.Replace("\"", "&quot;");
            if (escaped != unescaped)
            { }
            return escaped;
        }




        public enum XmlTagMode
        {
            None = 0,
            Structure = 1,
            Item = 2,
            ItemAndType = 3,
        }
    }

    public interface IMetaXmlItem
    {
        void WriteXml(StringBuilder sb, int indent);
        void ReadXml(XmlNode node);
    }



    public enum MetaFormat
    {
        XML = 0,
        RSC = 1,
        PSO = 2,
        RBF = 3,
        CacheFile = 4,
        AudioRel = 5,
        Ynd = 6,
        Ynv = 7,
        Ycd = 8,
        Ybn = 9,
        Ytd = 10,
        Ydr = 11,
        Ydd = 12,
        Yft = 13,
        Ypt = 14,
        Yld = 15,
        Yed = 16,
        Ywr = 17,
        Yvr = 18,
        Awc = 19,
        Fxc = 20,
        Heightmap = 21,
        Ypdb = 22,
        Mrf = 23,
        Yfd = 24,
    }

}
