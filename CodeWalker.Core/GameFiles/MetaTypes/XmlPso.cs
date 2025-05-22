using SharpDX;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace CodeWalker.GameFiles
{
    public class XmlPso
    {

        public static PsoFile GetPso(XmlDocument doc)
        {
            PsoBuilder pb = new PsoBuilder();

            Traverse(doc.DocumentElement, pb, 0, true);

            PsoFile pso = pb.GetPso();

            return pso;
        }



        private static byte[] Traverse(XmlNode node, PsoBuilder pb, MetaName type = 0, bool isRoot = false)
        {
            if (type == 0)
            {
                type = (MetaName)(uint)GetHash(node.Name);
            }

            PsoStructureInfo infos = PsoTypes.GetStructureInfo(type);
            if (infos != null)
            {
                byte[] data = new byte[infos.StructureLength];
                PsoArrayResults arrayResults = new PsoArrayResults();

                arrayResults.Structures = new Dictionary<int, Array_Structure>();
                arrayResults.StructurePointers = new Dictionary<int, Array_StructurePointer>();
                arrayResults.UInts = new Dictionary<int, Array_uint>();
                arrayResults.UShorts = new Dictionary<int, Array_ushort>();
                arrayResults.UBytes = new Dictionary<int, Array_byte>();
                arrayResults.Floats = new Dictionary<int, Array_float>();
                arrayResults.Float_XYZs = new Dictionary<int, Array_Vector3>();
                arrayResults.Hashes = new Dictionary<int, Array_uint>();

                Array.Clear(data, 0, infos.StructureLength); //shouldn't really be necessary...

                PsoStructureEntryInfo arrEntry = null;


                //if (isRoot)
                //{
                //    pb.EnsureBlock(type);
                //}

                for (int i = 0; i < infos.Entries.Length; i++)
                {
                    PsoStructureEntryInfo entry = infos.Entries[i];
                    XmlNode cnode = GetEntryNode(node.ChildNodes, entry.EntryNameHash);

                    if (entry.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO)
                    {
                        arrEntry = entry;
                        continue;
                    }

                    if (cnode == null)
                    {
                        //warning: node not found in XML for this entry!
                        continue;
                    }

                    switch (entry.Type)
                    {
                        case PsoDataType.Array:
                            {
                                TraverseArray(cnode, pb, entry, arrEntry, arrayResults, data, infos);
                                break;
                            }
                        case PsoDataType.Structure:
                            {
                                MetaName stype = (MetaName)entry.ReferenceKey;
                                if (stype == 0)
                                {
                                    string stypestr = Xml.GetStringAttribute(cnode, "type");
                                    if (!string.IsNullOrEmpty(stypestr))
                                    {
                                        stype = (MetaName)(uint)GetHash(stypestr);
                                    }
                                }
                                byte[] struc = Traverse(cnode, pb, stype);
                                if (struc != null)
                                {
                                    switch (entry.Unk_5h)
                                    {
                                        default:
                                            //ErrorXml(sb, cind, ename + ": Unexpected Structure subtype: " + entry.Unk_5h.ToString());
                                            break;
                                        case 0: //default structure

                                            Buffer.BlockCopy(struc, 0, data, entry.DataOffset, struc.Length);

                                            break;
                                        case 3: //structure pointer...
                                        case 4: //also pointer? what's the difference?

                                            PsoBuilderPointer bptr = pb.AddItem(stype, struc);
                                            PsoPOINTER ptr = new PsoPOINTER(bptr.BlockID, bptr.Offset);
                                            ptr.SwapEnd();
                                            byte[] ptrb = MetaTypes.ConvertToBytes(ptr);

                                            Buffer.BlockCopy(ptrb, 0, data, entry.DataOffset, ptrb.Length);

                                            break;
                                    }
                                }
                                else
                                { }
                                break;
                            }
                        case PsoDataType.Map:
                            {
                                TraverseMap(cnode, pb, entry, infos, data, arrayResults);

                                break;
                            }

                        case PsoDataType.Bool:
                            {
                                byte val = (cnode.Attributes["value"].Value == "false") ? (byte)0 : (byte)1;
                                data[entry.DataOffset] = val;
                                break;
                            }
                        case PsoDataType.SByte:
                            {
                                sbyte val = Convert.ToSByte(cnode.Attributes["value"].Value);
                                data[entry.DataOffset] = (byte)val;
                                break;
                            }
                        case PsoDataType.UByte:
                            {
                                byte val = Convert.ToByte(cnode.Attributes["value"].Value);
                                data[entry.DataOffset] = val;
                                break;
                            }
                        case PsoDataType.SShort:
                            {
                                short val = Convert.ToInt16(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }
                        case PsoDataType.UShort:
                            {
                                ushort val = Convert.ToUInt16(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }
                        case PsoDataType.SInt:
                            {
                                int val = Convert.ToInt32(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }
                        case PsoDataType.UInt:
                            {
                                switch (entry.Unk_5h)
                                {
                                    default:
                                        //ErrorXml(sb, cind, ename + ": Unexpected Integer subtype: " + entry.Unk_5h.ToString());
                                        break;
                                    case 0: //signed int (? flags?)
                                        int sval = Convert.ToInt32(cnode.Attributes["value"].Value);
                                        Write(sval, data, entry.DataOffset);
                                        break;
                                    case 1: //unsigned int
                                        string ustr = cnode.Attributes["value"].Value;
                                        uint uval = 0;
                                        if (ustr.StartsWith("0x"))
                                        {
                                            ustr = ustr.Substring(2);
                                            uval = Convert.ToUInt32(ustr, 16);
                                        }
                                        else
                                        {
                                            uval = Convert.ToUInt32(ustr);
                                        }
                                        Write(uval, data, entry.DataOffset);
                                        break;
                                }

                                break;
                            }
                        case PsoDataType.Float:
                            {
                                float val = FloatUtil.Parse(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }
                        case PsoDataType.Float2:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                break;
                            }
                        case PsoDataType.Float3:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                float z = FloatUtil.Parse(cnode.Attributes["z"].Value);
                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                Write(z, data, entry.DataOffset + sizeof(float) * 2);
                                break;
                            }
                        case PsoDataType.Float4:
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
                        case PsoDataType.String:
                            {
                                TraverseString(cnode, pb, entry, data);
                                break;
                            }
                        case PsoDataType.Enum:
                            {
                                pb.AddEnumInfo((MetaName)entry.ReferenceKey);
                                switch (entry.Unk_5h)
                                {
                                    default:
                                        //ErrorXml(sb, cind, ename + ": Unexpected Enum subtype: " + entry.Unk_5h.ToString());
                                        break;
                                    case 0: //int enum
                                        int ival = GetEnumInt((MetaName)entry.ReferenceKey, cnode.InnerText, entry.Type);
                                        Write(ival, data, entry.DataOffset);
                                        break;
                                    case 1: //short enum?
                                        short sval = (short)GetEnumInt((MetaName)entry.ReferenceKey, cnode.InnerText, entry.Type);
                                        Write(sval, data, entry.DataOffset);
                                        break;
                                    case 2: //byte enum
                                        byte bval = (byte)GetEnumInt((MetaName)entry.ReferenceKey, cnode.InnerText, entry.Type);
                                        data[entry.DataOffset] = bval;
                                        break;
                                }
                                break;
                            }
                        case PsoDataType.Flags:
                            {
                                //uint fCount = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                                uint fEntry = (entry.ReferenceKey & 0xFFF);
                                PsoStructureEntryInfo fEnt = (fEntry != 0xFFF) ? infos.GetEntry((int)fEntry) : null;
                                PsoEnumInfo flagsInfo = null;
                                MetaName fEnum = (MetaName)(fEnt?.ReferenceKey ?? 0);
                                if ((fEnt != null) && (fEnt.EntryNameHash == (MetaName)MetaTypeName.ARRAYINFO))
                                {
                                    flagsInfo = PsoTypes.GetEnumInfo(fEnum);
                                }
                                if (flagsInfo == null)
                                {
                                    if (fEntry != 0xFFF)
                                    { }
                                    //flagsInfo = PsoTypes.GetEnumInfo(entry.EntryNameHash);
                                }
                                if (flagsInfo != null)
                                {
                                    pb.AddEnumInfo(flagsInfo.IndexInfo.NameHash);
                                }
                                else
                                { }//error?

                                switch (entry.Unk_5h)
                                {
                                    default:
                                        //ErrorXml(sb, cind, ename + ": Unexpected Flags subtype: " + entry.Unk_5h.ToString());
                                        break;
                                    case 0: //int flags
                                        int ival = GetEnumInt(fEnum, cnode.InnerText, entry.Type);
                                        Write(ival, data, entry.DataOffset);
                                        break;
                                    case 1: //short flags
                                        short sval = (short)GetEnumInt(fEnum, cnode.InnerText, entry.Type);
                                        Write(sval, data, entry.DataOffset);
                                        break;
                                    case 2: //byte flags
                                        byte bval = (byte)GetEnumInt(fEnum, cnode.InnerText, entry.Type);
                                        data[entry.DataOffset] = bval;
                                        break;
                                }
                                break;
                            }
                        case PsoDataType.Float3a:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                float z = FloatUtil.Parse(cnode.Attributes["z"].Value);
                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                Write(z, data, entry.DataOffset + sizeof(float) * 2);
                                break;
                            }
                        case PsoDataType.Float4a:
                            {
                                float x = FloatUtil.Parse(cnode.Attributes["x"].Value);
                                float y = FloatUtil.Parse(cnode.Attributes["y"].Value);
                                float z = FloatUtil.Parse(cnode.Attributes["z"].Value);
                                //float w = FloatUtil.Parse(cnode.Attributes["w"].Value);
                                Write(x, data, entry.DataOffset);
                                Write(y, data, entry.DataOffset + sizeof(float));
                                Write(z, data, entry.DataOffset + sizeof(float) * 2);
                                //Write(w, data, entry.DataOffset + sizeof(float) * 3);
                                break;
                            }
                        case PsoDataType.HFloat:
                            {
                                short val = Convert.ToInt16(cnode.Attributes["value"].Value);
                                Write(val, data, entry.DataOffset);
                                break;
                            }
                        case PsoDataType.Long:
                            {
                                ulong uval = Convert.ToUInt64(cnode.Attributes["value"].Value);
                                Write(uval, data, entry.DataOffset);
                                break;
                            }


                        default:
                            break;

                    }


                }


                arrayResults.WriteArrays(data);

                pb.AddStructureInfo(infos.IndexInfo.NameHash);

                if (isRoot)
                {
                    pb.RootPointer = pb.AddItem(type, data);
                }

                return data;

            }
            else
            { }//info not found

            return null;
        }

        private static void TraverseMap(XmlNode node, PsoBuilder pb, PsoStructureEntryInfo entry, PsoStructureInfo infos, byte[] data, PsoArrayResults arrayResults)
        {
            uint mapidx1 = entry.ReferenceKey & 0x0000FFFF;
            uint mapidx2 = (entry.ReferenceKey >> 16) & 0x0000FFFF;
            PsoStructureEntryInfo mapreftype1 = infos.Entries[mapidx2];
            PsoStructureEntryInfo mapreftype2 = infos.Entries[mapidx1];

            if (mapreftype2.ReferenceKey != 0)
            { }

            PsoStructureInfo xStruct = pb.AddMapNodeStructureInfo((MetaName)mapreftype2.ReferenceKey);
            MetaName xName = xStruct.IndexInfo.NameHash;
            PsoStructureEntryInfo kEntry = xStruct?.FindEntry(MetaName.Key);
            PsoStructureEntryInfo iEntry = xStruct?.FindEntry(MetaName.Item);

            if (kEntry.Type != PsoDataType.String)
            { }



            List<byte[]> nodesData = new List<byte[]>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;
                string kattr = cnode.Attributes["key"].Value;
                string tattr = cnode.Attributes["type"].Value;//CW invention for convenience..!
                MetaName khash = (MetaName)(uint)GetHash(kattr);
                MetaName thash = (MetaName)(uint)GetHash(tattr);

                byte[] strucBytes = Traverse(cnode, pb, thash);
                byte[] nodeBytes = new byte[xStruct.StructureLength];

                TraverseStringRaw(kattr, pb, kEntry, nodeBytes); //write the key

                if (xName != (MetaName)MetaTypeName.ARRAYINFO)// (mapreftype2.ReferenceKey != 0)
                {
                    //value struct embedded in ARRAYINFO node
                    Buffer.BlockCopy(strucBytes, 0, nodeBytes, iEntry.DataOffset, strucBytes.Length);
                }
                else
                {
                    //normal ARRAYINFO with pointer value
                    PsoPOINTER itemptr = pb.AddItemPtr(thash, strucBytes);
                    itemptr.SwapEnd(); //big schmigg
                    byte[] ptrbytes = MetaTypes.ConvertToBytes(itemptr);
                    Buffer.BlockCopy(ptrbytes, 0, nodeBytes, iEntry.DataOffset, ptrbytes.Length);
                }

                nodesData.Add(nodeBytes);

            }



            Write(0x1000000, data, entry.DataOffset);
            Write(0, data, entry.DataOffset + 4);

            arrayResults.Structures[entry.DataOffset + 8] = pb.AddItemArrayPtr(xName, nodesData.ToArray());  //pb.AddPointerArray(nodeptrs);
        }

        private static void TraverseArray(XmlNode node, PsoBuilder pb, PsoStructureEntryInfo entry, PsoStructureEntryInfo arrEntry, PsoArrayResults results, byte[] data, PsoStructureInfo structInfo)
        {
            int offset = entry.DataOffset;
            uint aCount = (entry.ReferenceKey >> 16) & 0x0000FFFF;
            uint aPtr = (entry.ReferenceKey) & 0x0000FFFF;
            byte[] adata = null;

            //how do we know when it's an "embedded" array?
            bool embedded = true;
            switch (entry.Unk_5h)
            {
                default:
                    //ErrorXml(sb, indent, ename + ": WIP! Unsupported Array subtype: " + entry.Unk_5h.ToString());
                    break;
                case 0: //Array_Structure
                    //var arrStruc = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                    embedded = false;
                    break;
                case 1: //Raw in-line array
                    break;
                case 2: //also raw in-line array, but how different from above?
                    break;
                case 4: //pointer array? default array?
                    if (arrEntry.Unk_5h == 3) //pointers...
                    {
                        //var arrStruc4 = MetaTypes.ConvertData<Array_Structure>(data, eoffset);
                        embedded = false;
                    }
                    else
                    {
                    }
                    break;
                case 129: //also raw inline array? in junctions.pso  (AutoJunctionAdjustments)
                    break;
            }




            switch (arrEntry.Type)
            {
                case PsoDataType.Structure:
                    {
                        if (embedded)
                        {
                            if (arrEntry.ReferenceKey != 0)
                            {
                                byte[][] datas = TraverseArrayStructureRaw(node, pb, (MetaName)arrEntry.ReferenceKey);
                                int aoffset = offset;
                                for (int i = 0; i < datas.Length; i++)
                                {
                                    Buffer.BlockCopy(datas[i], 0, data, aoffset, datas[i].Length);
                                    aoffset += datas[i].Length;
                                }
                            }
                            else
                            {
                                PsoPOINTER[] ptrs = TraverseArrayStructurePointerRaw(node, pb);
                                adata = MetaTypes.ConvertArrayToBytes(ptrs);
                            }
                        }
                        else
                        {
                            if (arrEntry.ReferenceKey != 0)
                            {
                                results.Structures[offset] = TraverseArrayStructure(node, pb, (MetaName)arrEntry.ReferenceKey);
                            }
                            else
                            {
                                results.StructurePointers[offset] = TraverseArrayStructurePointer(node, pb);
                            }
                        }
                        break;
                    }

                case PsoDataType.Float2:
                    {
                        Vector2[] arr = TraverseVector2ArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 8;
                        }
                        else
                        {
                            results.Float_XYZs[offset] = pb.AddVector2ArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.Float3:
                    {
                        Vector4[] arr = TraverseVector3ArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 16;
                        }
                        else
                        {
                            results.Float_XYZs[offset] = pb.AddPaddedVector3ArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.UByte:
                    {
                        byte[] arr = TraverseUByteArrayRaw(node);
                        if (embedded)
                        {
                            adata = arr;
                        }
                        else
                        {
                            results.UBytes[offset] = pb.AddByteArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.Bool:
                    {
                        byte[] arr = TraverseUByteArrayRaw(node);
                        if (embedded)
                        {
                            adata = arr;
                        }
                        else
                        {
                            results.UBytes[offset] = pb.AddByteArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.UInt:
                    {
                        uint[] arr = TraverseUIntArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 4;
                        }
                        else
                        {
                            results.UInts[offset] = pb.AddUIntArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.SInt:
                    {
                        int[] arr = TraverseSIntArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 4;
                        }
                        else
                        {
                            results.UInts[offset] = pb.AddSIntArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.Float:
                    {
                        float[] arr = TraverseFloatArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 4;
                        }
                        else
                        {
                            results.Floats[offset] = pb.AddFloatArrayPtr(arr);
                        }
                        break;
                    }
                case PsoDataType.UShort:
                    {
                        ushort[] arr = TraverseUShortArrayRaw(node);
                        if (embedded)
                        {
                            adata = MetaTypes.ConvertArrayToBytes(arr);
                            aCount *= 2;
                        }
                        else
                        {
                            results.UShorts[offset] = pb.AddUShortArrayPtr(arr);
                        }
                        break;
                    }

                case PsoDataType.String:
                    {
                        switch (arrEntry.Unk_5h)
                        {
                            default:
                                //ErrorXml(sb, indent, ename + ": Unexpected String array subtype: " + entry.Unk_5h.ToString());
                                break;
                            case 7: //hash array...
                            case 8:
                                MetaHash[] hashes = TraverseHashArrayRaw(node);
                                if (embedded)
                                {
                                    adata = MetaTypes.ConvertArrayToBytes(hashes);
                                    aCount *= 4;
                                }
                                else
                                {
                                    results.Hashes[offset] = pb.AddHashArrayPtr(hashes);
                                }
                                break;
                            case 2: //string array  (array of pointers)
                                string[] strs = TraverseStringArrayRaw(node);
                                int cnt = strs?.Length ?? 0;
                                DataBlockPointer[] ptrs = (cnt > 0) ? new DataBlockPointer[strs.Length] : null;
                                for (int i = 0; i < cnt; i++)
                                {
                                    string str = strs[i];
                                    if (string.IsNullOrEmpty(str)) continue;
                                    PsoBuilderPointer bptr = pb.AddString(str);
                                    DataBlockPointer ptr = new DataBlockPointer(bptr.BlockID, bptr.Offset);
                                    ptr.SwapEnd();
                                    ptrs[i] = ptr;
                                }
                                PsoBuilderPointer aptr = (cnt > 0) ? pb.AddItemArray((MetaName)MetaTypeName.PsoPOINTER, ptrs) : new PsoBuilderPointer();
                                results.Structures[offset] = (cnt > 0) ? new Array_Structure(aptr.Pointer, aptr.Length) : new Array_Structure();
                                break;
                            case 3: //char array array  (array of CharPointer)
                                string[] strs2 = TraverseStringArrayRaw(node);
                                int cnt2 = strs2?.Length ?? 0;
                                CharPointer[] ptrs2 = (cnt2 > 0) ? new CharPointer[strs2.Length] : null;
                                for (int i = 0; i < cnt2; i++)
                                {
                                    string str = strs2[i];
                                    if (string.IsNullOrEmpty(str)) continue;
                                    PsoBuilderPointer bptr = pb.AddString(str);
                                    CharPointer ptr = new CharPointer(bptr.Pointer, str.Length);
                                    ptr.Count1 += 1;
                                    ptr.SwapEnd();
                                    ptrs2[i] = ptr;
                                }
                                PsoBuilderPointer aptr2 = (cnt2 > 0) ? pb.AddItemArray((MetaName)200, ptrs2) : new PsoBuilderPointer();
                                results.Structures[offset] = (cnt2 > 0) ? new Array_Structure(aptr2.Pointer, aptr2.Length) : new Array_Structure();
                                break;
                        }


                        break;
                    }


                case PsoDataType.Enum:
                    {
                        MetaHash[] hashes = TraverseHashArrayRaw(node);

                        if (arrEntry.ReferenceKey != 0)
                        {
                            PsoEnumInfo _infos = PsoTypes.GetEnumInfo((MetaName)arrEntry.ReferenceKey);
                            pb.AddEnumInfo(_infos.IndexInfo.NameHash);

                            uint[] values = new uint[hashes.Length];
                            for (int i = 0; i < hashes.Length; i++)
                            {
                                MetaName enumname = (MetaName)MetaTypes.SwapBytes(hashes[i]);//yeah swap it back to little endian..!
                                PsoEnumEntryInfo enuminf = _infos.FindEntryByName(enumname);
                                if (enuminf != null)
                                {
                                    values[i] = MetaTypes.SwapBytes((uint)enuminf.EntryKey);
                                }
                                else
                                { } //error?
                            }

                            if (embedded)
                            { } //TODO?
                            else
                            {
                                results.UInts[offset] = pb.AddUIntArrayPtr(values);
                            }

                        }
                        else
                        { }


                        break;
                    }


                case PsoDataType.Array:
                    {
                        //array array...
                        uint rk0 = (entry.ReferenceKey >> 16) & 0x0000FFFF;
                        uint rk1 = arrEntry.ReferenceKey & 0x0000FFFF;
                        if (rk0 > 0) //should be count of items
                        {
                            PsoStructureEntryInfo subarrEntry = structInfo.GetEntry((int)rk1);
                            MetaName subarrType = (MetaName)subarrEntry.ReferenceKey;

                            ushort origOffset = arrEntry.DataOffset;
                            arrEntry.DataOffset = entry.DataOffset;//slight hack for traversing array array
                            foreach (XmlNode cnode in node.ChildNodes)
                            {
                                if (cnode.NodeType != XmlNodeType.Element) continue;

                                TraverseArray(cnode, pb, arrEntry, subarrEntry, results, data, structInfo);

                                arrEntry.DataOffset += 16;//ptr size... todo: what if not pointer array?
                            }
                            arrEntry.DataOffset = origOffset;


                        }


                        break;
                    }




                default:
                    break;
            }

            if (embedded)
            {
                if (adata?.Length > 0)
                {
                    if (adata.Length > aCount)
                    { }//bad! old data won't fit in new slot...

                    Buffer.BlockCopy(adata, 0, data, offset, adata.Length);
                }
                else
                { }
            }
        }

        private static void TraverseString(XmlNode node, PsoBuilder pb, PsoStructureEntryInfo entry, byte[] data)
        {
            TraverseStringRaw(node.InnerText, pb, entry, data);
        }
        private static void TraverseStringRaw(string str, PsoBuilder pb, PsoStructureEntryInfo entry, byte[] data)
        {
            switch (entry.Unk_5h)
            {
                default:
                    break;
                case 0:
                    int str0len = (int)((entry.ReferenceKey >> 16) & 0xFFFF);
                    if (!string.IsNullOrEmpty(str))
                    {
                        byte[] strdata = Encoding.ASCII.GetBytes(str);
                        Buffer.BlockCopy(strdata, 0, data, entry.DataOffset, strdata.Length);
                        if (strdata.Length > str0len)
                        { }
                    }
                    break;
                case 1:
                case 2:
                    if (!string.IsNullOrEmpty(str))
                    {
                        PsoBuilderPointer bptr = pb.AddString(str);
                        PsoPOINTER ptr = new PsoPOINTER(bptr.BlockID, bptr.Offset);
                        ptr.SwapEnd();
                        byte[] val = MetaTypes.ConvertToBytes(ptr);
                        Buffer.BlockCopy(val, 0, data, entry.DataOffset, val.Length);
                    }
                    break;
                case 3:
                    if (!string.IsNullOrEmpty(str))
                    {
                        PsoBuilderPointer bptr = pb.AddString(str);
                        CharPointer ptr = new CharPointer(bptr.Pointer, str.Length);
                        ptr.SwapEnd();
                        byte[] val = MetaTypes.ConvertToBytes(ptr);
                        Buffer.BlockCopy(val, 0, data, entry.DataOffset, val.Length);
                    }
                    break;
                case 7://hash only?
                case 8://hash with STRF entry?

                    MetaHash hashVal = string.IsNullOrEmpty(str) ? 0 : GetHash(str);
                    Write(hashVal, data, entry.DataOffset);

                    if (entry.Unk_5h == 8)
                    {
                        pb.AddStringToSTRF(str);
                    }
                    break;
            }
        }


        private static byte[][] TraverseArrayStructureRaw(XmlNode node, PsoBuilder pb, MetaName type)
        {
            List<byte[]> strucs = new List<byte[]>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;

                byte[] struc = Traverse(cnode, pb, type);

                if (struc != null)
                {
                    strucs.Add(struc);
                }
            }

            return strucs.ToArray();
        }
        private static Array_Structure TraverseArrayStructure(XmlNode node, PsoBuilder pb, MetaName type)
        {
            byte[][] bytes = TraverseArrayStructureRaw(node, pb, type);

            return pb.AddItemArrayPtr(type, bytes);
        }

        private static PsoPOINTER[] TraverseArrayStructurePointerRaw(XmlNode node, PsoBuilder pb)
        {
            List<PsoPOINTER> ptrs = new List<PsoPOINTER>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;

                MetaName type = (MetaName)(uint)GetHash(cnode.Attributes["type"]?.Value ?? "");
                if (type != 0)
                {
                    byte[] struc = Traverse(cnode, pb, type);

                    if (struc != null)
                    {
                        PsoPOINTER ptr = pb.AddItemPtr(type, struc);
                        ptr.SwapEnd(); //big schmigg
                        ptrs.Add(ptr);
                    }
                    else
                    { } //error?
                }
                else
                {
                    ptrs.Add(new PsoPOINTER());//null value?
                }
            }

            return ptrs.ToArray();
        }
        private static Array_StructurePointer TraverseArrayStructurePointer(XmlNode node, PsoBuilder pb)
        {
            PsoPOINTER[] ptrs = TraverseArrayStructurePointerRaw(node, pb);

            return pb.AddPointerArray(ptrs);

        }

        private static int[] TraverseSIntArrayRaw(XmlNode node)
        {
            List<int> data = new List<int>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if (!string.IsNullOrEmpty(split[i]))
                    {
                        int val = Convert.ToInt32(split[i]);
                        data.Add(MetaTypes.SwapBytes(val));
                    }

                }
            }

            return data.ToArray();
        }
        private static uint[] TraverseUIntArrayRaw(XmlNode node)
        {
            List<uint> data = new List<uint>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if (!string.IsNullOrEmpty(split[i]))
                    {
                        uint val = Convert.ToUInt32(split[i]);
                        data.Add(MetaTypes.SwapBytes(val));
                    }

                }
            }

            return data.ToArray();
        }
        private static byte[] TraverseUByteArrayRaw(XmlNode node)
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

            return data.ToArray();
        }
        private static ushort[] TraverseUShortArrayRaw(XmlNode node)
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
                        data.Add(MetaTypes.SwapBytes(val));
                    }

                }
            }

            return data.ToArray();
        }
        private static float[] TraverseFloatArrayRaw(XmlNode node)
        {
            List<float> data = new List<float>();

            if (node.InnerText != "")
            {
                string[] split = Regex.Split(node.InnerText, @"[\s\r\n\t]");

                for (int i = 0; i < split.Length; i++)
                {
                    if (!string.IsNullOrEmpty(split[i]))
                    {
                        float val = FloatUtil.Parse(split[i]);
                        data.Add(MetaTypes.SwapBytes(val));
                    }

                }
            }

            return data.ToArray();
        }
        private static Vector4[] TraverseVector3ArrayRaw(XmlNode node)
        {
            List<Vector4> items = new List<Vector4>();


            string[] split = node.InnerText.Split('\n');// Regex.Split(node.InnerText, @"[\s\r\n\t]");


            float x = 0f;
            float y = 0f;
            float z = 0f;
            float w = 0f;

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
                    if (ts.EndsWith(",")) ts = ts.Substring(0, ts.Length - 1);
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
                    items.Add(MetaTypes.SwapBytes(val)); //big schmig
                }
            }


            return items.ToArray();
        }
        private static Vector2[] TraverseVector2ArrayRaw(XmlNode node)
        {
            List<Vector2> items = new List<Vector2>();


            string[] split = node.InnerText.Split('\n');// Regex.Split(node.InnerText, @"[\s\r\n\t]");


            float x = 0f;
            float y = 0f;

            for (int i = 0; i < split.Length; i++)
            {
                string s = split[i]?.Trim();
                if (string.IsNullOrEmpty(s)) continue;
                string[] split2 = Regex.Split(s, @"[\s\t]");
                int c = 0;
                x = 0f; y = 0f;
                for (int n = 0; n < split2.Length; n++)
                {
                    string ts = split2[n]?.Trim();
                    if (ts.EndsWith(",")) ts = ts.Substring(0, ts.Length - 1);
                    if (string.IsNullOrEmpty(ts)) continue;
                    float f = FloatUtil.Parse(ts);
                    switch (c)
                    {
                        case 0: x = f; break;
                        case 1: y = f; break;
                    }
                    c++;
                }
                if (c >= 3)
                {
                    Vector2 val = new Vector2(x, y);
                    items.Add(MetaTypes.SwapBytes(val)); //big schmig
                }
            }


            return items.ToArray();
        }
        private static MetaHash[] TraverseHashArrayRaw(XmlNode node)
        {
            List<MetaHash> items = new List<MetaHash>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;
                MetaHash val = GetHash(cnode.InnerText);
                items.Add(MetaTypes.SwapBytes(val));
            }

            return items.ToArray();
        }
        private static string[] TraverseStringArrayRaw(XmlNode node)
        {
            List<string> items = new List<string>();

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode.NodeType != XmlNodeType.Element) continue;
                items.Add(cnode.InnerText);
            }

            return items.ToArray();
        }




        private static void Write(int val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(int));
        }

        private static void Write(uint val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(uint));
        }

        private static void Write(short val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(short));
        }

        private static void Write(ushort val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(ushort));
        }

        private static void Write(float val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));//big fkn end
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(float));
        }

        private static void Write(ulong val, byte[] data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(MetaTypes.SwapBytes(val));
            Buffer.BlockCopy(bytes, 0, data, offset, sizeof(ulong));
        }

        private static MetaHash GetHash(string str)
        {
            if (str.StartsWith("hash_"))
            {
                return (MetaHash)Convert.ToUInt32(str.Substring(5), 16);
            }
            else
            {
                JenkIndex.Ensure(str);
                return JenkHash.GenHash(str);
            }
        }

        private static XmlNode GetEntryNode(XmlNodeList nodes, MetaName name)
        {
            foreach (XmlNode node in nodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (GetHash(node.Name) == (uint)name)
                {
                    return node;
                }
            }
            return null;
        }


        private static int GetEnumInt(MetaName type, string enumString, PsoDataType dataType)
        {
            PsoEnumInfo infos = PsoTypes.GetEnumInfo(type);

            if (infos == null)
            {
                return 0;
            }


            bool isFlags = (dataType == PsoDataType.Flags);// ||
                           //(dataType == PsoDataType.IntFlags2);// ||
                            //(dataType == PsoDataType.ShortFlags);

            if (isFlags)
            {
                //flags enum. (multiple values, comma-separated)
                string[] split = enumString.Split(new[] { ',' ,' '}, StringSplitOptions.RemoveEmptyEntries);
                int enumVal = 0;

                for (int i = 0; i < split.Length; i++)
                {
                    MetaName enumName = (MetaName)(uint)GetHash(split[i].Trim());

                    for (int j = 0; j < infos.Entries.Length; j++)
                    {
                        PsoEnumEntryInfo entry = infos.Entries[j];
                        if (entry.EntryNameHash == enumName)
                        {
                            enumVal += (1 << entry.EntryKey);
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
                    PsoEnumEntryInfo entry = infos.Entries[j];

                    if (entry.EntryNameHash == enumName)
                    {
                        return entry.EntryKey;
                    }
                }
            }

            return -1;
        }

    }

    struct PsoArrayResults
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
                Array_Structure val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_StructurePointer> ptr in StructurePointers)
            {
                Array_StructurePointer val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_uint> ptr in UInts)
            {
                Array_uint val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_ushort> ptr in UShorts)
            {
                Array_ushort val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_byte> ptr in UBytes)
            {
                Array_byte val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_float> ptr in Floats)
            {
                Array_float val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_Vector3> ptr in Float_XYZs)
            {
                Array_Vector3 val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }

            foreach (KeyValuePair<int, Array_uint> ptr in Hashes)
            {
                Array_uint val = ptr.Value;
                val.SwapEnd();
                byte[] _data = MetaTypes.ConvertToBytes(val);
                Buffer.BlockCopy(_data, 0, data, ptr.Key, _data.Length);
            }
        }
    }

}
