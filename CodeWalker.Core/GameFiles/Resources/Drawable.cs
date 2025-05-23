using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using SharpDX;

namespace CodeWalker.GameFiles
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderGroup : ResourceSystemBlock
    {
        public uint Unknown_1Ch; // 0x00000000
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public uint Unknown_34h; // 0x00000000
        public ulong Unknown_38h; // 0x0000000000000000
        public uint Unknown_4h = 1; // 0x00000001
        public override long BlockLength => 64;

        // structure data
        public uint VFT { get; set; } = 1080113136;
        public ulong TextureDictionaryPointer { get; set; }
        public ulong ShadersPointer { get; set; }
        public ushort ShadersCount1 { get; set; }
        public ushort ShadersCount2 { get; set; }
        public uint ShaderGroupBlocksSize { get; set; } // divided by 16

        // reference data
        public TextureDictionary TextureDictionary { get; set; }
        public ResourcePointerArray64<ShaderFX> Shaders { get; set; }


        public int TotalParameters
        {
            get
            {
                int c = 0;
                if (Shaders?.data_items != null)
                    foreach (ShaderFX s in Shaders.data_items)
                        c += s.ParameterCount;

                return c;
            }
        }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            TextureDictionaryPointer = reader.ReadUInt64();
            ShadersPointer = reader.ReadUInt64();
            ShadersCount1 = reader.ReadUInt16();
            ShadersCount2 = reader.ReadUInt16();
            Unknown_1Ch = reader.ReadUInt32();
            Unknown_20h = reader.ReadUInt64();
            Unknown_28h = reader.ReadUInt64();
            ShaderGroupBlocksSize = reader.ReadUInt32();
            Unknown_34h = reader.ReadUInt32();
            Unknown_38h = reader.ReadUInt64();

            // read reference data
            TextureDictionary = reader.ReadBlockAt<TextureDictionary>(
                TextureDictionaryPointer // offset
            );
            Shaders = reader.ReadBlockAt<ResourcePointerArray64<ShaderFX>>(
                ShadersPointer, // offset
                ShadersCount1
            );

            //if (Unknown_4h != 1)
            //{ }
            //if (Unknown_1Ch != 0)
            //{ }
            //if (Unknown_20h != 0)
            //{ }
            //if (Unknown_28h != 0)
            //{ }
            //if (Unknown_34h != 0)
            //{ }
            //if (Unknown_38h != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            TextureDictionaryPointer = (ulong)(TextureDictionary != null ? TextureDictionary.FilePosition : 0);
            ShadersPointer = (ulong)(Shaders != null ? Shaders.FilePosition : 0);
            ShadersCount1 = (ushort)(Shaders != null ? Shaders.Count : 0);
            ShadersCount2 = ShadersCount1;
            // In vanilla files this includes the size of the Shaders array, ShaderFX blocks and, sometimes,
            // ShaderParametersBlocks since they are placed contiguously after the ShaderGroup in the file.
            // But CW doesn't always do this so we only include the ShaderGroup size.
            //(ignore for gen9)
            ShaderGroupBlocksSize = writer.IsGen9 ? 0 : (uint)BlockLength / 16;

            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(TextureDictionaryPointer);
            writer.Write(ShadersPointer);
            writer.Write(ShadersCount1);
            writer.Write(ShadersCount2);
            writer.Write(Unknown_1Ch);
            writer.Write(Unknown_20h);
            writer.Write(Unknown_28h);
            writer.Write(ShaderGroupBlocksSize);
            writer.Write(Unknown_34h);
            writer.Write(Unknown_38h);
        }

        public void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            if (TextureDictionary != null) TextureDictionary.WriteXmlNode(TextureDictionary, sb, indent, ddsfolder);
            YdrXml.WriteItemArray(sb, Shaders?.data_items, indent, "Shaders");
        }

        public void ReadXml(XmlNode node, string ddsfolder)
        {
            XmlNode tnode = node.SelectSingleNode("TextureDictionary");
            if (tnode != null) TextureDictionary = TextureDictionary.ReadXmlNode(tnode, ddsfolder);
            ShaderFX[] shaders = XmlMeta.ReadItemArray<ShaderFX>(node, "Shaders");
            if (shaders != null)
            {
                Shaders = new ResourcePointerArray64<ShaderFX>();
                Shaders.data_items = shaders;
            }


            if (shaders != null && TextureDictionary != null)
                foreach (ShaderFX shader in shaders)
                {
                    ShaderParameter[] sparams = shader?.ParametersList?.Parameters;
                    if (sparams != null)
                        foreach (ShaderParameter sparam in sparams)
                            if (sparam.Data is TextureBase tex)
                            {
                                Texture tex2 = TextureDictionary.Lookup(tex.NameHash);
                                if (tex2 != null) sparam.Data = tex2; //swap the parameter out for the embedded texture
                            }
                }
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (TextureDictionary != null) list.Add(TextureDictionary);
            if (Shaders != null) list.Add(Shaders);
            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderFX : ResourceSystemBlock, IMetaXmlItem
    {
        public ulong G9_Unknown_28h;
        public ulong G9_Unknown_30h;
        public byte G9_Unknown_38h;
        public uint Unknown_1Ch; // 0x00000000
        public ushort Unknown_24h; // 0x0000
        public byte Unknown_26h; // 0x00
        public ulong Unknown_28h; // 0x0000000000000000
        public uint Unknown_Ch; // 0x00000000
        public override long BlockLength => 48;
        public override long BlockLength_Gen9 => 64;

        // structure data
        public ulong ParametersPointer { get; set; }
        public MetaHash Name { get; set; } //decal_emissive_only, emissive, spec
        public byte ParameterCount { get; set; }
        public byte RenderBucket { get; set; } // 2, 0, 
        public ushort Unknown_12h { get; set; } = 32768; // 32768    HasComment?
        public ushort ParameterSize { get; set; } //112, 208, 320    (with 16h) 10485872, 17826000, 26214720
        public ushort ParameterDataSize { get; set; } //160, 272, 400 
        public MetaHash FileName { get; set; } //decal_emissive_only.sps, emissive.sps, spec.sps
        public uint RenderBucketMask { get; set; } //65284, 65281  DrawBucketMask?   (1<<bucket) | 0xFF00
        public byte TextureParametersCount { get; set; }

        // reference data
        public ShaderParametersBlock ParametersList { get; set; }

        // gen9 structure data
        public MetaHash G9_Preset { get; set; } = 0x6D657461;
        public ulong G9_TextureRefsPointer { get; set; }
        public ulong G9_UnknownParamsPointer { get; set; }
        public ulong G9_ParamInfosPointer { get; set; }
        public ShaderParamInfosG9 G9_ParamInfos { get; set; }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.StringTag(sb, indent, "Name", YdrXml.HashString(Name));
            YdrXml.StringTag(sb, indent, "FileName", YdrXml.HashString(FileName));
            YdrXml.ValueTag(sb, indent, "RenderBucket", RenderBucket.ToString());
            if (ParametersList != null)
            {
                YdrXml.OpenTag(sb, indent, "Parameters");
                ParametersList.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "Parameters");
            }
        }

        public void ReadXml(XmlNode node)
        {
            Name = XmlMeta.GetHash(Xml.GetChildInnerText(node, "Name"));
            FileName = XmlMeta.GetHash(Xml.GetChildInnerText(node, "FileName"));
            RenderBucket = (byte)Xml.GetChildUIntAttribute(node, "RenderBucket");
            RenderBucketMask = (1u << RenderBucket) | 0xFF00u;
            XmlNode pnode = node.SelectSingleNode("Parameters");
            if (pnode != null)
            {
                ParametersList = new ShaderParametersBlock();
                ParametersList.Owner = this;
                ParametersList.ReadXml(pnode);

                ParameterCount = (byte)ParametersList.Count;
                ParameterSize = ParametersList.ParametersSize;
                ParameterDataSize = ParametersList.ParametersDataSize; //is it right?
                TextureParametersCount = ParametersList.TextureParamsCount;
            }
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            if (reader.IsGen9)
            {
                Name = new MetaHash(reader.ReadUInt32());
                G9_Preset = reader.ReadUInt32();
                ParametersPointer = reader.ReadUInt64(); // m_parameters
                G9_TextureRefsPointer = reader.ReadUInt64();
                G9_UnknownParamsPointer = reader.ReadUInt64(); //something to do with grass_batch (instance data?)
                G9_ParamInfosPointer = reader.ReadUInt64(); // m_parameterData (sgaShaderParamData)
                G9_Unknown_28h = reader.ReadUInt64(); //pad
                G9_Unknown_30h = reader.ReadUInt64(); //pad
                G9_Unknown_38h = reader.ReadByte();
                RenderBucket = reader.ReadByte();
                ParameterDataSize = reader.ReadUInt16(); //==ParametersList.G9_DataSize
                RenderBucketMask = reader.ReadUInt32();

                G9_ParamInfos = reader.ReadBlockAt<ShaderParamInfosG9>(G9_ParamInfosPointer);
                ParametersList = reader.ReadBlockAt<ShaderParametersBlock>(ParametersPointer, 0, this);
                FileName = JenkHash.GenHash(Name.ToCleanString() +
                                            ".sps"); //TODO: get mapping from G9_Preset to legacy FileName

                if (G9_UnknownParamsPointer != 0)
                {
                }

                if (G9_Unknown_28h != 0)
                {
                }

                if (G9_Unknown_30h != 0)
                {
                }

                if (G9_Unknown_38h != 0)
                {
                }

                switch (G9_Preset)
                {
                    case 0x6D657461:
                        break;
                }
            }
            else
            {
                // read structure data
                ParametersPointer = reader.ReadUInt64();
                Name = new MetaHash(reader.ReadUInt32());
                Unknown_Ch = reader.ReadUInt32();
                ParameterCount = reader.ReadByte();
                RenderBucket = reader.ReadByte();
                Unknown_12h = reader.ReadUInt16();
                ParameterSize = reader.ReadUInt16();
                ParameterDataSize = reader.ReadUInt16();
                FileName = new MetaHash(reader.ReadUInt32());
                Unknown_1Ch = reader.ReadUInt32();
                RenderBucketMask = reader.ReadUInt32();
                Unknown_24h = reader.ReadUInt16();
                Unknown_26h = reader.ReadByte();
                TextureParametersCount = reader.ReadByte();
                Unknown_28h = reader.ReadUInt64();

                // read reference data
                ParametersList = reader.ReadBlockAt<ShaderParametersBlock>(
                    ParametersPointer, // offset
                    ParameterCount,
                    this
                );

                //// just testing...
                //if (Unknown_12h != 32768)
                //{
                //    if (Unknown_12h != 0)//des_aquaduct_root, rig_root_skin.... destructions?
                //    { }//no hit
                //}
                //if (RenderBucketMask != ((1 << RenderBucket) | 0xFF00))
                //{ }//no hit
                //if (ParameterSize != ParametersList?.ParametersSize)
                //{ }//no hit
                ////if (ParameterDataSize != ParametersList?.ParametersDataSize)
                //{
                //    var diff = ParameterDataSize - (ParametersList?.BlockLength ?? 0);
                //    switch (diff)
                //    {
                //        case 32:
                //        case 36:
                //        case 40:
                //        case 44:
                //            break;
                //        default:
                //            break;//no hit
                //    }
                //}
                //if (Unknown_24h != 0)
                //{ }//no hit
                //if (Unknown_26h != 0)
                //{ }//no hit
                //if (Unknown_Ch != 0)
                //{ }//no hit
                //if (Unknown_1Ch != 0)
                //{ }//no hit
                //if (Unknown_28h != 0)
                //{ }//no hit
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            if (writer.IsGen9)
            {
                ParameterCount = (byte)(ParametersList?.Count ?? 0);
                ParameterDataSize = (ushort)ParametersList.G9_DataSize;
                ParametersPointer = (ulong)(ParametersList?.FilePosition ?? 0);
                G9_ParamInfosPointer = (ulong)(G9_ParamInfos?.FilePosition ?? 0);
                G9_TextureRefsPointer =
                    ParametersPointer != 0 ? ParametersPointer + ParametersList.G9_TexturesOffset : 0;
                G9_UnknownParamsPointer =
                    ParametersPointer != 0 ? ParametersPointer + ParametersList.G9_UnknownsOffset : 0;

                writer.Write(Name);
                writer.Write(G9_Preset);
                writer.Write(ParametersPointer);
                writer.Write(G9_TextureRefsPointer);
                writer.Write(G9_UnknownParamsPointer);
                writer.Write(G9_ParamInfosPointer);
                writer.Write(G9_Unknown_28h);
                writer.Write(G9_Unknown_30h);
                writer.Write(G9_Unknown_38h);
                writer.Write(RenderBucket);
                writer.Write(ParameterDataSize);
                writer.Write(RenderBucketMask);
            }
            else
            {
                // update structure data
                ParametersPointer = (ulong)(ParametersList != null ? ParametersList.FilePosition : 0);
                ParameterCount = (byte)(ParametersList != null ? ParametersList.Count : 0);

                // write structure data
                writer.Write(ParametersPointer);
                writer.Write(Name.Hash);
                writer.Write(Unknown_Ch);
                writer.Write(ParameterCount);
                writer.Write(RenderBucket);
                writer.Write(Unknown_12h);
                writer.Write(ParameterSize);
                writer.Write(ParameterDataSize);
                writer.Write(FileName.Hash);
                writer.Write(Unknown_1Ch);
                writer.Write(RenderBucketMask);
                writer.Write(Unknown_24h);
                writer.Write(Unknown_26h);
                writer.Write(TextureParametersCount);
                writer.Write(Unknown_28h);
            }
        }


        public void EnsureGen9()
        {
            if (ParametersList == null) return; //need this
            //get G9_ParamInfos from GameFileCache.ShadersGen9ConversionData
            //calculate ParametersList.G9_DataSize
            //build ParametersList.G9_ fields from G9_ParamInfos


            GameFileCache.EnsureShadersGen9ConversionData();
            GameFileCache.ShadersGen9ConversionData.TryGetValue(Name, out GameFileCache.ShaderGen9XmlDataCollection dc);

            if (dc == null)
            {
            }

            int[] bsizs = dc?.BufferSizes;
            ShaderParamInfoG9[] pinfs = dc?.ParamInfos;
            int tc = 0;
            int uc = 0;
            int sc = 0;
            int bs = 0;
            int multi = 1;
            uint[] bsizsu = new uint[bsizs?.Length ?? 0];
            if (bsizs != null)
            {
                multi = 3;
                for (int i = 0; i < bsizs.Length; i++)
                {
                    int bsiz = bsizs[i];
                    bsizsu[i] = (uint)bsiz;
                    bs += bsiz;
                }
            }

            if (pinfs != null)
            {
                multi = multi << 2;
                foreach (ShaderParamInfoG9 pinf in pinfs)
                    switch (pinf.Type)
                    {
                        case ShaderParamTypeG9.Texture: tc++; break;
                        case ShaderParamTypeG9.Unknown: uc++; break;
                        case ShaderParamTypeG9.Sampler: sc++; break;
                    }
            }

            ShaderParamInfosG9 pinfos = new ShaderParamInfosG9();
            pinfos.Params = pinfs;
            pinfos.NumBuffers = (byte)(bsizs?.Length ?? 0);
            pinfos.NumParams = (byte)(pinfs?.Length ?? 0);
            pinfos.NumTextures = (byte)tc;
            pinfos.NumUnknowns = (byte)uc;
            pinfos.NumSamplers = (byte)sc;
            pinfos.Unknown0 = 0x00;
            pinfos.Unknown1 = 0x01;
            pinfos.Unknown2 = (byte)multi;


            int ptrslen = pinfos.NumBuffers * 8 * multi;
            int bufslen = bs * multi;
            int texslen = tc * 8 * multi;
            int unkslen = uc * 8 * multi;
            int smpslen = sc;
            int totlen = ptrslen + bufslen + texslen + unkslen + smpslen;
            ParametersList.G9_BuffersDataSize = (uint)bufslen;
            ParametersList.G9_TexturesOffset = (uint)(ptrslen + bufslen);
            ParametersList.G9_UnknownsOffset = (uint)(ptrslen + bufslen + texslen);
            ParametersList.G9_DataSize = totlen;
            ParameterDataSize = (ushort)totlen;


            if (G9_ParamInfos != null)
            {
            }

            G9_ParamInfos = pinfos;
            ParametersList.G9_ParamInfos = pinfos;

            if (ParametersList.G9_Samplers != null)
            {
            }

            ParametersList.G9_Samplers = dc?.SamplerValues;

            if (ParametersList.G9_BufferSizes != null)
            {
            }

            ParametersList.G9_BufferSizes = bsizsu;


            ShaderParameter[] parr = ParametersList.Parameters;
            if (parr != null)
                foreach (ShaderParameter p in parr)
                    if (p.Data is Texture etex) //in case embedded textures are actual texture refs, convert them to TextureBase
                    {
                        TextureBase btex = new TextureBase();
                        btex.Name = etex.Name;
                        btex.NameHash = etex.NameHash;
                        btex.G9_Dimension = etex.G9_Dimension;
                        btex.G9_Flags = 0x00260000;
                        //btex.G9_SRV = new ShaderResourceViewG9();
                        //btex.G9_SRV.Dimension = etex.G9_SRV?.Dimension ?? ShaderResourceViewDimensionG9.Texture2D;
                        p.Data = btex;
                    }
                    else if (p.Data is TextureBase btex)
                    {
                        btex.VFT = 0;
                        btex.Unknown_4h = 1;
                        if (btex.G9_Flags == 0) btex.G9_Flags = 0x00260000;
                        //if (btex.G9_SRV == null)//make sure the SRVs for these params exist
                        //{
                        //    btex.G9_SRV = new ShaderResourceViewG9();
                        //    switch (btex.G9_Dimension)
                        //    {
                        //        case TextureDimensionG9.Texture2D: btex.G9_SRV.Dimension = ShaderResourceViewDimensionG9.Texture2D; break;
                        //        case TextureDimensionG9.Texture3D: btex.G9_SRV.Dimension = ShaderResourceViewDimensionG9.Texture3D; break;
                        //        case TextureDimensionG9.TextureCube: btex.G9_SRV.Dimension = ShaderResourceViewDimensionG9.TextureCube; break;
                        //    }
                        //}
                    }
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (G9_ParamInfos != null) list.Add(G9_ParamInfos);
            if (ParametersList != null) list.Add(ParametersList);
            return list.ToArray();
        }


        public override string ToString()
        {
            return Name + " (" + FileName + ")";
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderParameter
    {
        public ushort Unknown_2h; // 0x0000
        public uint Unknown_4h; // 0x00000000
        public byte DataType { get; set; } //0: texture, 1: vector4
        public byte Unknown_1h { get; set; }
        public ulong DataPointer { get; set; }

        public object Data { get; set; }

        public void Read(ResourceDataReader reader)
        {
            DataType = reader.ReadByte();
            Unknown_1h = reader.ReadByte();
            Unknown_2h = reader.ReadUInt16();
            Unknown_4h = reader.ReadUInt32();
            DataPointer = reader.ReadUInt64();
        }

        public void Write(ResourceDataWriter writer)
        {
            writer.Write(DataType);
            writer.Write(Unknown_1h);
            writer.Write(Unknown_2h);
            writer.Write(Unknown_4h);
            writer.Write(DataPointer);
        }

        public override string ToString()
        {
            return Data != null ? Data.ToString() : DataType + ": " + DataPointer;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderParametersBlock : ResourceSystemBlock
    {
        private ResourceSystemStructBlock<Vector4>[] ParameterDataBlocks;

        public override long BlockLength
        {
            get
            {
                long bsize = BaseSize;
                ushort psize = ParametersDataSize;
                return bsize + psize * 4;
            }
        }

        public override long BlockLength_Gen9 => G9_DataSize;

        public long BaseSize
        {
            get
            {
                long offset = 32;
                if (Parameters != null)
                {
                    foreach (ShaderParameter x in Parameters)
                    {
                        offset += 16;
                        offset += 16 * x.DataType;
                    }

                    offset += Parameters.Length * 4;
                }

                return offset;
            }
        }

        public ushort ParametersSize
        {
            get
            {
                ushort size = (ushort)((Parameters?.Length ?? 0) * 16);
                foreach (ShaderParameter x in Parameters) size += (ushort)(16 * x.DataType);
                return size;
            }
        }

        public ushort ParametersDataSize
        {
            get
            {
                long size = BaseSize;
                if (size % 16 != 0) size += 16 - size % 16;
                return (ushort)size;
            }
        }

        public byte TextureParamsCount
        {
            get
            {
                byte c = 0;
                foreach (ShaderParameter x in Parameters)
                    if (x.DataType == 0)
                        c++;
                return c;
            }
        }

        public ShaderParameter[] Parameters { get; set; }
        public MetaName[] Hashes { get; set; }
        public int Count { get; set; }

        public ShaderFX Owner { get; set; }


        // gen9 data
        public long G9_DataSize { get; set; }
        public ShaderParamInfosG9 G9_ParamInfos { get; set; }

        public ulong[]
            G9_BufferPtrs { get; set; } //4x copies of buffers.. buffer data immediately follows pointers array

        public uint[] G9_BufferSizes { get; set; } //sizes of all buffers
        public uint G9_BuffersDataSize { get; set; }
        public uint G9_TexturesOffset { get; set; }
        public uint G9_UnknownsOffset { get; set; }
        public ulong[] G9_TexturePtrs { get; set; }
        public ulong[] G9_UnknownData { get; set; }
        public byte[] G9_Samplers { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            Count = Convert.ToInt32(parameters[0]);
            Owner = parameters[1] as ShaderFX;

            if (reader.IsGen9)
            {
                GameFileCache.EnsureShadersGen9ConversionData();
                GameFileCache.ShadersGen9ConversionData.TryGetValue(Owner.Name, out GameFileCache.ShaderGen9XmlDataCollection dc);
                Dictionary<MetaHash, MetaHash> paramap = dc?.ParamsMapGen9ToLegacy;

                G9_ParamInfos = Owner.G9_ParamInfos;
                int multi = G9_ParamInfos.Unknown2; //12  ... wtf
                uint mult = (uint)multi;

                byte bcnt = G9_ParamInfos.NumBuffers;
                long spos = reader.Position;
                G9_BufferPtrs = reader.ReadStructs<ulong>(bcnt * mult); //12x copies of buffers...... !!
                G9_BufferSizes =
                    new uint[bcnt]; //this might affect load performance slightly, but needed for XML and saving
                for (int i = 0; i < bcnt; i++) G9_BufferSizes[i] = (uint)(G9_BufferPtrs[i + 1] - G9_BufferPtrs[i]);
                ulong p0 = 0ul;
                ulong p1 = 0ul;
                if (G9_BufferPtrs != null && G9_BufferPtrs.Length > bcnt)
                {
                    p0 = G9_BufferPtrs[0];
                    p1 = G9_BufferPtrs[bcnt];
                }

                int ptrslen = bcnt * 8 * multi;
                int bufslen = (int)(p1 - p0) * multi;
                int texslen = G9_ParamInfos.NumTextures * 8 * multi;
                int unkslen = G9_ParamInfos.NumUnknowns * 8 * multi;
                byte smpslen = G9_ParamInfos.NumSamplers;
                int totlen = ptrslen + bufslen + texslen + unkslen + smpslen;
                G9_BuffersDataSize = (uint)bufslen;
                G9_TexturesOffset = (uint)(ptrslen + bufslen);
                G9_UnknownsOffset = (uint)(ptrslen + bufslen + texslen);
                G9_DataSize = totlen;

                if (Owner.G9_TextureRefsPointer != 0)
                    G9_TexturePtrs = reader.ReadUlongsAt(Owner.G9_TextureRefsPointer, G9_ParamInfos.NumTextures * mult,
                        false);
                if (Owner.G9_UnknownParamsPointer != 0)
                    G9_UnknownData = reader.ReadUlongsAt(Owner.G9_UnknownParamsPointer,
                        G9_ParamInfos.NumUnknowns * mult, false);
                if (G9_ParamInfos.NumSamplers > 0)
                    G9_Samplers = reader.ReadBytesAt((ulong)(spos + (ptrslen + bufslen + texslen + unkslen)),
                        G9_ParamInfos.NumSamplers, false);

                List<ShaderParameter> paras = new List<ShaderParameter>();
                List<MetaName> hashes = new List<MetaName>();
                foreach (ShaderParamInfoG9 info in G9_ParamInfos.Params)
                {
                    uint hash = info.Name.Hash;
                    if (paramap != null && paramap.TryGetValue(hash, out MetaHash oldhash)) hash = oldhash;

                    if (info.Type == ShaderParamTypeG9.Texture)
                    {
                        ShaderParameter p = new ShaderParameter();
                        p.DataType = 0;
                        p.DataPointer = G9_TexturePtrs[info.TextureIndex];
                        p.Data = reader.ReadBlockAt<TextureBase>(p.DataPointer);
                        paras.Add(p);
                        hashes.Add((MetaName)hash);
                        if (p.Data is TextureBase ptex)
                            if (ptex.G9_SRV != null)
                            {
                            }
                    }
                    else if (info.Type == ShaderParamTypeG9.CBuffer)
                    {
                        uint fcnt = info.ParamLength / 4u;
                        uint arrsiz = info.ParamLength / 16u;
                        ShaderParameter p = new ShaderParameter();
                        p.DataType = (byte)Math.Max(arrsiz, 1);
                        if (info.ParamLength % 4 != 0)
                        {
                        }

                        byte cbi = info.CBufferIndex;
                        long baseptr = G9_BufferPtrs != null && G9_BufferPtrs.Length > cbi
                            ? (long)G9_BufferPtrs[cbi]
                            : 0;
                        if (baseptr != 0)
                        {
                            long ptr = baseptr + info.ParamOffset;
                            switch (fcnt)
                            {
                                case 0:
                                    break;
                                case 1: p.Data = new Vector4(reader.ReadStructAt<float>(ptr), 0, 0, 0); break;
                                case 2: p.Data = new Vector4(reader.ReadStructAt<Vector2>(ptr), 0, 0); break;
                                case 3: p.Data = new Vector4(reader.ReadStructAt<Vector3>(ptr), 0); break;
                                case 4: p.Data = reader.ReadStructAt<Vector4>(ptr); break;
                                default:
                                    if (arrsiz == 0)
                                    {
                                    }

                                    p.Data = reader.ReadStructsAt<Vector4>((ulong)ptr, arrsiz, false);
                                    break;
                            }
                        }

                        paras.Add(p);
                        hashes.Add((MetaName)hash);
                    } //todo?
                }

                Parameters = paras.ToArray();
                Hashes = hashes.ToArray();
                Count = paras.Count;
            }
            else
            {
                List<ShaderParameter> paras = new List<ShaderParameter>();
                for (int i = 0; i < Count; i++)
                {
                    ShaderParameter p = new ShaderParameter();
                    p.Read(reader);
                    paras.Add(p);
                }

                int offset = 0;
                for (int i = 0; i < Count; i++)
                {
                    ShaderParameter p = paras[i];

                    // read reference data
                    switch (p.DataType)
                    {
                        case 0:
                            offset += 0;
                            p.Data = reader.ReadBlockAt<TextureBase>(p.DataPointer);
                            break;
                        case 1:
                            offset += 16;
                            p.Data = reader.ReadStructAt<Vector4>((long)p.DataPointer);
                            break;
                        default:
                            offset += 16 * p.DataType;
                            p.Data = reader.ReadStructsAt<Vector4>(p.DataPointer, p.DataType, false);
                            break;
                    }
                }


                reader.Position += offset; //Vector4 data gets embedded here... but why pointers in params also???

                List<MetaName> hashes = new List<MetaName>();
                for (int i = 0; i < Count; i++) hashes.Add((MetaName)reader.ReadUInt32());

                Parameters = paras.ToArray();
                Hashes = hashes.ToArray();


                ////testing padding area at the end of the block...
                //var psiz1 = Owner.ParameterDataSize;
                //var psiz2 = ParametersDataSize;
                //if (psiz1 != psiz2)
                //{ }//no hit
                //var unk0 = reader.ReadStructs<MetaHash>(8);
                //foreach (var u0i in unk0)
                //{
                //    if (u0i != 0)
                //    { }//no hit
                //}
                //if (Owner.Unknown_12h != 0)
                //{
                //    var unk1 = reader.ReadStructs<MetaHash>(psiz1);
                //    foreach (var u1i in unk1)
                //    {
                //        if (u1i != 0)
                //        { break; }//no hit
                //    }
                //}


                //// just testing...
                //for (int i = 0; i < Parameters.Length; i++)
                //{
                //    var param = Parameters[i];
                //    if (param.DataType == 0)
                //    {
                //        if (param.Unknown_1h != ((param.Data == null) ? 10 : (i + 2)))
                //        { }
                //    }
                //    else
                //    {
                //        if (param.Unknown_1h != (160 + ((Parameters.Length - 1) - i)))
                //        { }
                //    }
                //}
                //if (Parameters.Length > 0)
                //{
                //    var lparam = Parameters[Parameters.Length - 1];
                //    switch(lparam.Unknown_1h)
                //    {
                //        case 192:
                //        case 160:
                //        case 177:
                //        case 161:
                //        case 156:
                //        case 162:
                //        case 157:
                //        case 149:
                //        case 178:
                //        case 72:
                //        case 153:
                //        case 133:
                //            break;
                //        case 64://in ydd's
                //        case 130:
                //        case 180:
                //            break;
                //        default:
                //            break;
                //    }
                //}
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            if (writer.IsGen9)
            {
                GameFileCache.EnsureShadersGen9ConversionData();
                GameFileCache.ShadersGen9ConversionData.TryGetValue(Owner.Name, out GameFileCache.ShaderGen9XmlDataCollection dc);
                Dictionary<MetaHash, MetaHash> paramap = dc?.ParamsMapLegacyToGen9;

                if (G9_ParamInfos == null) G9_ParamInfos = Owner.G9_ParamInfos;
                int multi = G9_ParamInfos.Unknown2;
                uint mult = (uint)multi;
                byte bcnt = G9_ParamInfos.NumBuffers;
                byte tcnt = G9_ParamInfos.NumTextures;
                byte ucnt = G9_ParamInfos.NumUnknowns;
                long spos = FilePosition; //this position should definitely be assigned by this point
                uint[] bsizes = G9_BufferSizes;
                uint[] boffs = new uint[bsizes?.Length ?? 0];
                int ptrslen = bcnt * 8 * multi;
                ulong[] bptrs = new ulong[bcnt * mult];
                ulong bptr = (ulong)(spos + ptrslen);
                for (int i = 0; i < mult; i++)
                for (int j = 0; j < bcnt; j++)
                {
                    bptrs[i * bcnt + j] = bptr;
                    bptr += bsizes[j];
                }

                uint boff = 0u;
                for (int i = 0; i < bsizes.Length; i++)
                {
                    boffs[i] = boff;
                    boff += bsizes[i];
                }

                if (G9_BufferPtrs != null)
                {
                }

                G9_BufferPtrs = bptrs;

                writer.WriteUlongs(bptrs);


                int buf0len = (int)(G9_BuffersDataSize / mult);
                byte[] buf0 = new byte[buf0len];
                ulong[] texptrs = new ulong[tcnt * mult];
                if (Parameters != null && paramap != null)
                {
                    Dictionary<uint, ShaderParameter> exmap = new Dictionary<uint, ShaderParameter>();
                    int excnt = Math.Min(Parameters.Length, Hashes?.Length ?? 0);
                    for (int i = 0; i < excnt; i++)
                    {
                        uint exhash = (uint)Hashes[i];
                        if (paramap.TryGetValue(exhash, out MetaHash g9hash) == false) g9hash = exhash;
                        if (g9hash != 0) exmap[g9hash] = Parameters[i];
                    }

                    void writeStruct<T>(int o, T val) where T : struct
                    {
                        int size = Marshal.SizeOf(typeof(T));
                        IntPtr ptr = Marshal.AllocHGlobal(size);
                        Marshal.StructureToPtr(val, ptr, true);
                        Marshal.Copy(ptr, buf0, o, size);
                        Marshal.FreeHGlobal(ptr);
                    }

                    void writeStructs<T>(int o, T[] val) where T : struct
                    {
                        if (val == null) return;
                        int size = Marshal.SizeOf(typeof(T));
                        foreach (T v in val)
                        {
                            writeStruct(o, v);
                            o += size;
                        }
                    }

                    foreach (ShaderParamInfoG9 info in G9_ParamInfos.Params)
                    {
                        exmap.TryGetValue(info.Name, out ShaderParameter exparam);
                        if (info.Type == ShaderParamTypeG9.Texture)
                        {
                            TextureBase btex = exparam?.Data as TextureBase;
                            texptrs[info.TextureIndex] = (ulong)(btex?.FilePosition ?? 0);
                        }
                        else if (info.Type == ShaderParamTypeG9.CBuffer)
                        {
                            object data = exparam?.Data;
                            uint fcnt = info.ParamLength / 4u;
                            uint arrsiz = info.ParamLength / 16u;
                            byte cbi = info.CBufferIndex;
                            uint baseoff = boffs != null && boffs.Length > cbi ? boffs[cbi] : 0;
                            int off = (int)(baseoff + info.ParamOffset);
                            Vector4 v = data is Vector4 ? (Vector4)data : Vector4.Zero;
                            switch (fcnt)
                            {
                                case 0: break;
                                case 1: writeStruct(off, v.X); break;
                                case 2: writeStruct(off, new Vector2(v.X, v.Y)); break;
                                case 3: writeStruct(off, new Vector3(v.X, v.Y, v.Z)); break;
                                case 4: writeStruct(off, v); break;
                                default:
                                    if (arrsiz == 0)
                                    {
                                    }

                                    writeStructs(off, data as Vector4[]);
                                    break;
                            }
                        }
                    }
                }

                for (int i = 0; i < mult; i++) writer.Write(buf0);

                if (G9_TexturePtrs != null)
                {
                }

                G9_TexturePtrs = texptrs.Length > 0 ? texptrs : null;
                if (G9_TexturePtrs != null) writer.WriteUlongs(G9_TexturePtrs);

                if (G9_UnknownData != null)
                {
                }

                G9_UnknownData = ucnt > 0 ? new ulong[ucnt * mult] : null;
                if (G9_UnknownData != null) writer.WriteUlongs(G9_UnknownData);

                if (G9_Samplers != null) writer.Write(G9_Samplers);
            }
            else
            {
                // update pointers...
                for (int i = 0; i < Parameters.Length; i++)
                {
                    ShaderParameter param = Parameters[i];
                    if (param.DataType == 0)
                    {
                        param.DataPointer = (ulong)((param.Data as TextureBase)?.FilePosition ?? 0);
                    }
                    else
                    {
                        ResourceSystemStructBlock<Vector4> block = i < ParameterDataBlocks?.Length
                            ? ParameterDataBlocks[i]
                            : null;
                        if (block != null)
                            param.DataPointer = (ulong)block.FilePosition;
                        else
                            param.DataPointer = 0; //shouldn't happen!
                    }
                }


                // write parameter infos
                foreach (ShaderParameter f in Parameters) f.Write(writer);

                // write vector data
                for (int i = 0; i < Parameters.Length; i++)
                {
                    ShaderParameter param = Parameters[i];
                    if (param.DataType != 0)
                    {
                        ResourceSystemStructBlock<Vector4> block = i < ParameterDataBlocks?.Length
                            ? ParameterDataBlocks[i]
                            : null;
                        if (block != null) writer.WriteBlock(block);
                        //shouldn't happen!
                    }
                }

                // write hashes
                foreach (MetaName h in Hashes) writer.Write((uint)h);


                //write end padding stuff
                ushort psiz = ParametersDataSize;
                writer.Write(new byte[32 + psiz * 4]);
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            int cind = indent + 1;
            for (int i = 0; i < Count; i++)
            {
                ShaderParameter param = Parameters[i];
                ShaderParamNames name = (ShaderParamNames)Hashes[i];
                string typestr = "";
                if (param.DataType == 0) typestr = "Texture";
                else if (param.DataType == 1) typestr = "Vector";
                else if (param.DataType > 1) typestr = "Array";
                string otstr = "Item name=\"" + name + "\" type=\"" + typestr + "\"";

                if (param.DataType == 0)
                {
                    if (param.Data is TextureBase tex)
                    {
                        YdrXml.OpenTag(sb, indent, otstr);
                        YdrXml.StringTag(sb, cind, "Name", YdrXml.XmlEscape(tex.Name));
                        YdrXml.CloseTag(sb, indent, "Item");
                    }
                    else
                    {
                        YdrXml.SelfClosingTag(sb, indent, otstr);
                    }
                }
                else if (param.DataType == 1)
                {
                    if (param.Data is Vector4 vec)
                        YdrXml.SelfClosingTag(sb, indent, otstr + " " + FloatUtil.GetVector4XmlString(vec));
                    else
                        YdrXml.SelfClosingTag(sb, indent, otstr);
                }
                else
                {
                    if (param.Data is Vector4[] arr)
                    {
                        YdrXml.OpenTag(sb, indent, otstr);
                        foreach (Vector4 vec in arr)
                            YdrXml.SelfClosingTag(sb, cind, "Value " + FloatUtil.GetVector4XmlString(vec));
                        YdrXml.CloseTag(sb, indent, "Item");
                    }
                    else
                    {
                        YdrXml.SelfClosingTag(sb, indent, otstr);
                    }
                }
            }
        }

        public void ReadXml(XmlNode node)
        {
            List<ShaderParameter> plist = new List<ShaderParameter>();
            List<MetaName> hlist = new List<MetaName>();
            XmlNodeList pnodes = node.SelectNodes("Item");
            foreach (XmlNode pnode in pnodes)
            {
                ShaderParameter p = new ShaderParameter();
                MetaName h = (MetaName)(uint)XmlMeta.GetHash(Xml.GetStringAttribute(pnode, "name")?.ToLowerInvariant());
                string type = Xml.GetStringAttribute(pnode, "type");
                if (type == "Texture")
                {
                    p.DataType = 0;
                    if (pnode.SelectSingleNode("Name") != null)
                    {
                        TextureBase tex = new TextureBase();
                        tex.ReadXml(pnode, null); //embedded textures will get replaced in ShaderFX ReadXML
                        tex.Unknown_32h = 2;
                        p.Data = tex;
                    }
                }
                else if (type == "Vector")
                {
                    p.DataType = 1;
                    float fx = Xml.GetFloatAttribute(pnode, "x");
                    float fy = Xml.GetFloatAttribute(pnode, "y");
                    float fz = Xml.GetFloatAttribute(pnode, "z");
                    float fw = Xml.GetFloatAttribute(pnode, "w");
                    p.Data = new Vector4(fx, fy, fz, fw);
                }
                else if (type == "Array")
                {
                    List<Vector4> vecs = new List<Vector4>();
                    XmlNodeList inodes = pnode.SelectNodes("Value");
                    foreach (XmlNode inode in inodes)
                    {
                        float fx = Xml.GetFloatAttribute(inode, "x");
                        float fy = Xml.GetFloatAttribute(inode, "y");
                        float fz = Xml.GetFloatAttribute(inode, "z");
                        float fw = Xml.GetFloatAttribute(inode, "w");
                        vecs.Add(new Vector4(fx, fy, fz, fw));
                    }

                    p.Data = vecs.ToArray();
                    p.DataType = (byte)vecs.Count;
                }

                plist.Add(p);
                hlist.Add(h);
            }

            Parameters = plist.ToArray();
            Hashes = hlist.ToArray();
            Count = plist.Count;

            for (int i = 0; i < Parameters.Length; i++)
            {
                ShaderParameter param = Parameters[i];
                if (param.DataType == 0) param.Unknown_1h = (byte)(i + 2); //wtf and why
            }

            int offset = 160;
            for (int i = Parameters.Length - 1; i >= 0; i--)
            {
                ShaderParameter param = Parameters[i];
                if (param.DataType != 0)
                {
                    param.Unknown_1h = (byte)offset; //wtf and why
                    offset += param.DataType;
                }
            }
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            list.AddRange(base.GetReferences());

            foreach (ShaderParameter x in Parameters)
                if (x.DataType == 0)
                    list.Add(x.Data as TextureBase);

            return list.ToArray();
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            List<Tuple<long, IResourceBlock>> list = new List<Tuple<long, IResourceBlock>>();
            list.AddRange(base.GetParts());

            long offset = Parameters.Length * 16;

            List<ResourceSystemStructBlock<Vector4>> blist = new List<ResourceSystemStructBlock<Vector4>>();
            foreach (ShaderParameter x in Parameters)
            {
                if (x.DataType != 0)
                {
                    Vector4[] vecs = x.Data as Vector4[];
                    if (vecs == null) vecs = new[] { (Vector4)x.Data };
                    if (vecs == null)
                    {
                    }

                    ResourceSystemStructBlock<Vector4> block = new ResourceSystemStructBlock<Vector4>(vecs);
                    list.Add(new Tuple<long, IResourceBlock>(offset, block));
                    blist.Add(block);
                }
                else
                {
                    blist.Add(null);
                }

                offset += 16 * x.DataType;
            }

            ParameterDataBlocks = blist.ToArray();

            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderParamInfosG9 : ResourceSystemBlock
    {
        public override long BlockLength => 8 + NumParams * 8;

        public byte NumBuffers { get; set; }
        public byte NumTextures { get; set; }
        public byte NumUnknowns { get; set; }
        public byte NumSamplers { get; set; }
        public byte NumParams { get; set; }
        public byte Unknown0 { get; set; }
        public byte Unknown1 { get; set; }
        public byte Unknown2 { get; set; } = 0xc; //12  threads buffer copy count..?
        public ShaderParamInfoG9[] Params { get; set; }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            NumBuffers = reader.ReadByte();
            NumTextures = reader.ReadByte();
            NumUnknowns = reader.ReadByte();
            NumSamplers = reader.ReadByte();
            NumParams = reader.ReadByte();
            Unknown0 = reader.ReadByte();
            Unknown1 = reader.ReadByte();
            Unknown2 = reader.ReadByte();
            Params = reader.ReadStructs<ShaderParamInfoG9>(NumParams);

            if (Unknown0 != 0)
            {
            }

            if (Unknown1 != 0)
            {
            }

            if (Unknown2 != 0xc)
            {
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            writer.Write(NumBuffers);
            writer.Write(NumTextures);
            writer.Write(NumUnknowns);
            writer.Write(NumSamplers);
            writer.Write(NumParams);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.WriteStructs(Params);
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct ShaderParamInfoG9
    {
        public MetaHash Name { get; set; }
        public uint Data { get; set; }

        public ShaderParamTypeG9 Type
        {
            get => (ShaderParamTypeG9)(Data & 0x3);
            set => Data = (Data & 0xFFFFFFF8) + ((uint)value & 0x3);
        }

        public byte TextureIndex
        {
            get => (byte)((Data >> 2) & 0xFF);
            set => Data = (Data & 0xFFFFFC03) + (((uint)value & 0xFF) << 2);
        }

        public byte SamplerIndex
        {
            get => (byte)((Data >> 2) & 0xFF);
            set => Data = (Data & 0xFFFFFC03) + (((uint)value & 0xFF) << 2);
        }

        public byte CBufferIndex
        {
            get => (byte)((Data >> 2) & 0x3F);
            set => Data = (Data & 0xFFFFFF03) + (((uint)value & 0x3F) << 2);
        }

        public ushort ParamOffset
        {
            get => (ushort)((Data >> 8) & 0xFFF);
            set => Data = (Data & 0xFFF000FF) + (((uint)value & 0xFFF) << 8);
        }

        public ushort ParamLength
        {
            get => (ushort)((Data >> 20) & 0xFFF);
            set => Data = (Data & 0x000FFFFF) + (((uint)value & 0xFFF) << 20);
        }

        public override string ToString()
        {
            return $"{Name}: {Type}, {TextureIndex}, {ParamOffset}, {ParamLength}";
        }
    }

    public enum ShaderParamTypeG9 : byte
    {
        Texture = 0,
        Unknown = 1,
        Sampler = 2,
        CBuffer = 3
    }


    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Skeleton : ResourceSystemBlock
    {
        public Matrix3_s[] BoneTransforms; //for rendering
        private ResourceSystemStructBlock<short> ChildIndicesBlock;
        private ResourceSystemStructBlock<short> ParentIndicesBlock;
        private ResourceSystemStructBlock<Matrix> TransformationsBlock;

        private ResourceSystemStructBlock<Matrix> TransformationsInvertedBlock; //for saving only
        public ulong Unknown_48h; // 0x0000000000000000
        public ushort Unknown_62h; // 0x0000
        public uint Unknown_64h; // 0x00000000
        public ulong Unknown_68h; // 0x0000000000000000
        public ulong Unknown_8h; // 0x0000000000000000
        public override long BlockLength => 112;

        // structure data
        public uint VFT { get; set; } = 1080114336;
        public uint Unknown_4h { get; set; } = 1; // 0x00000001
        public ulong BoneTagsPointer { get; set; }
        public ushort BoneTagsCapacity { get; set; }
        public ushort BoneTagsCount { get; set; }
        public FlagsUint Unknown_1Ch { get; set; }
        public ulong BonesPointer { get; set; }
        public ulong TransformationsInvertedPointer { get; set; }
        public ulong TransformationsPointer { get; set; }
        public ulong ParentIndicesPointer { get; set; }
        public ulong ChildIndicesPointer { get; set; }
        public MetaHash Unknown_50h { get; set; }
        public MetaHash Unknown_54h { get; set; }
        public MetaHash Unknown_58h { get; set; }
        public ushort Unknown_5Ch { get; set; } = 1; // 0x0001
        public ushort BonesCount { get; set; }
        public ushort ChildIndicesCount { get; set; }

        // reference data
        public ResourcePointerArray64<SkeletonBoneTag> BoneTags { get; set; }
        public SkeletonBonesBlock Bones { get; set; }

        public Matrix[] TransformationsInverted { get; set; }
        public Matrix[] Transformations { get; set; }
        public short[] ParentIndices { get; set; }
        public short[] ChildIndices { get; set; } //mapping child->parent indices, first child index, then parent


        public Dictionary<ushort, Bone> BonesMap { get; set; } //for convienience finding bones by tag

        public Bone[]
            BonesSorted { get; set; } //sometimes bones aren't in parent>child order in the files! (eg player chars)


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt64();
            BoneTagsPointer = reader.ReadUInt64();
            BoneTagsCapacity = reader.ReadUInt16();
            BoneTagsCount = reader.ReadUInt16();
            Unknown_1Ch = reader.ReadUInt32();
            BonesPointer = reader.ReadUInt64();
            TransformationsInvertedPointer = reader.ReadUInt64();
            TransformationsPointer = reader.ReadUInt64();
            ParentIndicesPointer = reader.ReadUInt64();
            ChildIndicesPointer = reader.ReadUInt64();
            Unknown_48h = reader.ReadUInt64();
            Unknown_50h = new MetaHash(reader.ReadUInt32());
            Unknown_54h = new MetaHash(reader.ReadUInt32());
            Unknown_58h = new MetaHash(reader.ReadUInt32());
            Unknown_5Ch = reader.ReadUInt16();
            BonesCount = reader.ReadUInt16();
            ChildIndicesCount = reader.ReadUInt16();
            Unknown_62h = reader.ReadUInt16();
            Unknown_64h = reader.ReadUInt32();
            Unknown_68h = reader.ReadUInt64();

            // read reference data
            BoneTags = reader.ReadBlockAt<ResourcePointerArray64<SkeletonBoneTag>>(BoneTagsPointer, BoneTagsCapacity);
            Bones = reader.ReadBlockAt<SkeletonBonesBlock>(BonesPointer != 0 ? BonesPointer - 16 : 0, (uint)BonesCount);
            TransformationsInverted = reader.ReadStructsAt<Matrix>(TransformationsInvertedPointer, BonesCount);
            Transformations = reader.ReadStructsAt<Matrix>(TransformationsPointer, BonesCount);
            ParentIndices = reader.ReadShortsAt(ParentIndicesPointer, BonesCount);
            ChildIndices = reader.ReadShortsAt(ChildIndicesPointer, ChildIndicesCount);


            AssignBoneParents();

            BuildBonesMap();

            //BuildIndices();//testing!
            //BuildBoneTags();//testing!
            //BuildTransformations();//testing!
            //if (BoneTagsCount != Math.Min(BonesCount, BoneTagsCapacity))
            //{ }//no hits

            //if (BonesPointer != 0)
            //{
            //    var bhdr = reader.ReadStructAt<ResourcePointerListHeader>((long)BonesPointer - 16);
            //    if (bhdr.Pointer != BonesCount)
            //    { }//no hit
            //    if ((bhdr.Count != 0) || (bhdr.Capacity != 0) || (bhdr.Unknown != 0))
            //    { }//no hit
            //}

            //if (Unknown_8h != 0)
            //{ }
            //if (Unknown_48h != 0)
            //{ }
            //if (Unknown_62h != 0)
            //{ }
            //if (Unknown_64h != 0)
            //{ }
            //if (Unknown_68h != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            BoneTagsPointer = (ulong)(BoneTags != null ? BoneTags.FilePosition : 0);
            BoneTagsCapacity = (ushort)(BoneTags != null ? BoneTags.Count : 0);
            BonesPointer = (ulong)(Bones != null ? Bones.FilePosition + 16 : 0);
            TransformationsInvertedPointer =
                (ulong)(TransformationsInvertedBlock != null ? TransformationsInvertedBlock.FilePosition : 0);
            TransformationsPointer = (ulong)(TransformationsBlock != null ? TransformationsBlock.FilePosition : 0);
            ParentIndicesPointer = (ulong)(ParentIndicesBlock != null ? ParentIndicesBlock.FilePosition : 0);
            ChildIndicesPointer = (ulong)(ChildIndicesBlock != null ? ChildIndicesBlock.FilePosition : 0);
            BonesCount = (ushort)(Bones?.Items != null ? Bones.Items.Length : 0);
            ChildIndicesCount = (ushort)(ChildIndicesBlock != null ? ChildIndicesBlock.ItemCount : 0);
            BoneTagsCount = Math.Min(BonesCount, BoneTagsCapacity);


            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(Unknown_8h);
            writer.Write(BoneTagsPointer);
            writer.Write(BoneTagsCapacity);
            writer.Write(BoneTagsCount);
            writer.Write(Unknown_1Ch);
            writer.Write(BonesPointer);
            writer.Write(TransformationsInvertedPointer);
            writer.Write(TransformationsPointer);
            writer.Write(ParentIndicesPointer);
            writer.Write(ChildIndicesPointer);
            writer.Write(Unknown_48h);
            writer.Write(Unknown_50h);
            writer.Write(Unknown_54h);
            writer.Write(Unknown_58h);
            writer.Write(Unknown_5Ch);
            writer.Write(BonesCount);
            writer.Write(ChildIndicesCount);
            writer.Write(Unknown_62h);
            writer.Write(Unknown_64h);
            writer.Write(Unknown_68h);
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "Unknown1C", Unknown_1Ch.Value.ToString());
            YdrXml.ValueTag(sb, indent, "Unknown50", Unknown_50h.Hash.ToString());
            YdrXml.ValueTag(sb, indent, "Unknown54", Unknown_54h.Hash.ToString());
            YdrXml.ValueTag(sb, indent, "Unknown58", Unknown_58h.Hash.ToString());

            if (Bones?.Items != null) YdrXml.WriteItemArray(sb, Bones.Items, indent, "Bones");
        }

        public void ReadXml(XmlNode node)
        {
            Unknown_1Ch = Xml.GetChildUIntAttribute(node, "Unknown1C");
            Unknown_50h = Xml.GetChildUIntAttribute(node, "Unknown50");
            Unknown_54h = Xml.GetChildUIntAttribute(node, "Unknown54");
            Unknown_58h = Xml.GetChildUIntAttribute(node, "Unknown58");

            Bone[] bones = XmlMeta.ReadItemArray<Bone>(node, "Bones");
            if (bones != null)
            {
                Bones = new SkeletonBonesBlock();
                Bones.Items = bones;
            }

            BuildIndices();
            BuildBoneTags();
            AssignBoneParents();
            BuildTransformations();
            BuildBonesMap();
        }

        public override IResourceBlock[] GetReferences()
        {
            BuildTransformations();

            List<IResourceBlock> list = new List<IResourceBlock>();
            if (BoneTags != null) list.Add(BoneTags);
            if (Bones != null) list.Add(Bones);
            if (TransformationsInverted != null)
            {
                TransformationsInvertedBlock = new ResourceSystemStructBlock<Matrix>(TransformationsInverted);
                list.Add(TransformationsInvertedBlock);
            }

            if (Transformations != null)
            {
                TransformationsBlock = new ResourceSystemStructBlock<Matrix>(Transformations);
                list.Add(TransformationsBlock);
            }

            if (ParentIndices != null)
            {
                ParentIndicesBlock = new ResourceSystemStructBlock<short>(ParentIndices);
                list.Add(ParentIndicesBlock);
            }

            if (ChildIndices != null)
            {
                ChildIndicesBlock = new ResourceSystemStructBlock<short>(ChildIndices);
                list.Add(ChildIndicesBlock);
            }

            return list.ToArray();
        }


        public void AssignBoneParents()
        {
            if (Bones?.Items != null && ParentIndices != null)
            {
                int maxcnt = Math.Min(Bones.Items.Length, ParentIndices.Length);
                for (int i = 0; i < maxcnt; i++)
                {
                    Bone bone = Bones.Items[i];
                    short pind = ParentIndices[i];
                    if (pind >= 0 && pind < Bones.Items.Length) bone.Parent = Bones.Items[pind];
                }
            }
        }

        public void BuildBonesMap()
        {
            BonesMap = new Dictionary<ushort, Bone>();
            if (Bones?.Items != null)
            {
                List<Bone> bonesSorted = new List<Bone>();
                for (int i = 0; i < Bones.Items.Length; i++)
                {
                    Bone bone = Bones.Items[i];
                    BonesMap[bone.Tag] = bone;
                    bonesSorted.Add(bone);

                    bone.UpdateAnimTransform();
                    bone.AbsTransform = bone.AnimTransform;
                    bone.BindTransformInv = i < (TransformationsInverted?.Length ?? 0)
                        ? TransformationsInverted[i]
                        : Matrix.Invert(bone.AnimTransform);
                    bone.BindTransformInv.M44 = 1.0f;
                    bone.UpdateSkinTransform();
                    bone.TransformUnk =
                        i < (Transformations?.Length ?? 0)
                            ? Transformations[i].Column4
                            : Vector4.Zero; //still dont know what this is
                }

                bonesSorted.Sort((a, b) => a.Index.CompareTo(b.Index));
                BonesSorted = bonesSorted.ToArray();
            }
        }

        public void BuildIndices()
        {
            List<short> parents = new List<short>();
            List<short> childs = new List<short>();
            if (Bones?.Items != null)
            {
                //crazy breadth-wise limited to 4 algorithm for generating the ChildIndices

                List<Bone> tbones = Bones.Items.ToList();
                List<Bone> rootbones = tbones.Where(b => b.ParentIndex < 0).ToList();
                for (int i = 0; i < tbones.Count; i++)
                {
                    Bone bone = Bones.Items[i];
                    short pind = bone.ParentIndex;
                    parents.Add(pind);
                }

                List<Bone> getChildren(Bone b)
                {
                    List<Bone> r = new List<Bone>();
                    if (b == null) return r;
                    for (int i = 0; i < tbones.Count; i++)
                    {
                        Bone tb = tbones[i];
                        if (tb.ParentIndex == b.Index) r.Add(tb);
                    }

                    return r;
                }

                List<Bone> getAllChildren(List<Bone> bones)
                {
                    List<Bone> l = new List<Bone>();
                    foreach (Bone b in bones)
                    {
                        List<Bone> children = getChildren(b);
                        l.AddRange(children);
                    }

                    return l;
                }

                List<List<Bone>> layers = new List<List<Bone>>();
                List<Bone> layer = getAllChildren(rootbones);
                while (layer.Count > 0)
                {
                    int numbones = Math.Min(layer.Count, 4);
                    List<Bone> inslayer = layer.GetRange(0, numbones);
                    List<Bone> extlayer = getAllChildren(inslayer);
                    layers.Add(inslayer);
                    layer.RemoveRange(0, numbones);
                    layer.InsertRange(0, extlayer);
                }


                foreach (List<Bone> l in layers)
                {
                    Bone lastbone = null;
                    foreach (Bone b in l)
                    {
                        childs.Add(b.Index);
                        childs.Add(b.ParentIndex);
                        lastbone = b;
                    }

                    if (lastbone != null)
                    {
                        int npad = 8 - childs.Count % 8;
                        if (npad < 8)
                            for (int i = 0; i < npad; i += 2)
                            {
                                childs.Add(lastbone.Index);
                                childs.Add(lastbone.ParentIndex);
                            }
                    }
                }


                //////just testing
                //var numchilds = ChildIndices?.Length ?? 0;
                //int diffstart = -1;
                //int diffend = -1;
                //int ndiff = Math.Abs(numchilds - childs.Count);
                //int maxchilds = Math.Min(numchilds, childs.Count);
                //for (int i = 0; i < maxchilds; i++)
                //{
                //    var oc = ChildIndices[i];
                //    var nc = childs[i];
                //    if (nc != oc)
                //    {
                //        if (diffstart < 0) diffstart = i;
                //        diffend = i;
                //        ndiff++; 
                //    }
                //}
                //if (ndiff > 0)
                //{
                //    var difffrac = ((float)ndiff) / ((float)numchilds);
                //}
                //if (numchilds != childs.Count)
                //{ }


                //var numbones = Bones.Items.Length;
                //var numchilds = ChildIndices?.Length ?? 0;
                //for (int i = 0; i < numchilds; i += 2)
                //{
                //    var bind = ChildIndices[i];
                //    var pind = ChildIndices[i + 1];
                //    if (bind > numbones)
                //    { continue; }//shouldn't happen
                //    var bone = Bones.Items[bind];
                //    if (bone == null)
                //    { continue; }//shouldn't happen
                //    if (pind != bone.ParentIndex)
                //    { }//shouldn't happen?
                //}
            }

            ParentIndices = parents.Count > 0 ? parents.ToArray() : null;
            ChildIndices = childs.Count > 0 ? childs.ToArray() : null;
        }

        public void BuildBoneTags()
        {
            List<SkeletonBoneTag> tags = new List<SkeletonBoneTag>();
            if (Bones?.Items != null)
                for (int i = 0; i < Bones.Items.Length; i++)
                {
                    Bone bone = Bones.Items[i];
                    SkeletonBoneTag tag = new SkeletonBoneTag();
                    tag.BoneTag = bone.Tag;
                    tag.BoneIndex = (uint)i;
                    tags.Add(tag);
                }

            if (tags.Count < 2)
            {
                if (BoneTags != null)
                {
                }

                BoneTags = null;
                return;
            }

            uint numbuckets = GetNumHashBuckets(tags.Count);

            List<SkeletonBoneTag>[] buckets = new List<SkeletonBoneTag>[numbuckets];
            foreach (SkeletonBoneTag tag in tags)
            {
                uint b = tag.BoneTag % numbuckets;
                List<SkeletonBoneTag> bucket = buckets[b];
                if (bucket == null)
                {
                    bucket = new List<SkeletonBoneTag>();
                    buckets[b] = bucket;
                }

                bucket.Add(tag);
            }

            List<SkeletonBoneTag> newtags = new List<SkeletonBoneTag>();
            foreach (List<SkeletonBoneTag> b in buckets)
                if ((b?.Count ?? 0) == 0)
                {
                    newtags.Add(null);
                }
                else
                {
                    b.Reverse();
                    newtags.Add(b[0]);
                    SkeletonBoneTag p = b[0];
                    for (int i = 1; i < b.Count; i++)
                    {
                        SkeletonBoneTag c = b[i];
                        c.Next = null;
                        p.Next = c;
                        p = c;
                    }
                }


            //if (BoneTags?.data_items != null) //just testing - all ok
            //{
            //    var numtags = BoneTags.data_items.Length;
            //    if (numbuckets != numtags)
            //    { }
            //    else
            //    {
            //        for (int i = 0; i < numtags; i++)
            //        {
            //            var ot = BoneTags.data_items[i];
            //            var nt = newtags[i];
            //            if ((ot == null) != (nt == null))
            //            { }
            //            else if (ot != null)
            //            {
            //                if (ot.BoneIndex != nt.BoneIndex)
            //                { }
            //                if (ot.BoneTag != nt.BoneTag)
            //                { }
            //            }
            //        }
            //    }
            //}


            BoneTags = new ResourcePointerArray64<SkeletonBoneTag>();
            BoneTags.data_items = newtags.ToArray();
        }

        public void BuildTransformations()
        {
            List<Matrix> transforms = new List<Matrix>();
            List<Matrix> transformsinv = new List<Matrix>();
            if (Bones?.Items != null)
                foreach (Bone bone in Bones.Items)
                {
                    Vector3 pos = bone.Translation;
                    Quaternion ori = bone.Rotation;
                    Vector3 sca = bone.Scale;
                    Matrix m = Matrix.AffineTransformation(1.0f, ori, pos); //(local transform)
                    m.ScaleVector *= sca;
                    m.Column4 = bone.TransformUnk; // new Vector4(0, 4, -3, 0);//???

                    Bone pbone = bone.Parent;
                    while (pbone != null)
                    {
                        pos = pbone.Rotation.Multiply(pos /** pbone.Scale*/) + pbone.Translation;
                        ori = pbone.Rotation * ori;
                        pbone = pbone.Parent;
                    }

                    Matrix m2 = Matrix.AffineTransformation(1.0f, ori, pos); //(global transform)
                    Matrix mi = Matrix.Invert(m2);
                    mi.Column4 = Vector4.Zero;

                    transforms.Add(m);
                    transformsinv.Add(mi);
                }

            //if (Transformations != null) //just testing! - all ok
            //{
            //    if (Transformations.Length != transforms.Count)
            //    { }
            //    else
            //    {
            //        for (int i = 0; i < Transformations.Length; i++)
            //        {
            //            if (Transformations[i].Column1 != transforms[i].Column1)
            //            { }
            //            if (Transformations[i].Column2 != transforms[i].Column2)
            //            { }
            //            if (Transformations[i].Column3 != transforms[i].Column3)
            //            { }
            //            if (Transformations[i].Column4 != transforms[i].Column4)
            //            { }
            //        }
            //    }
            //    if (TransformationsInverted.Length != transformsinv.Count)
            //    { }
            //    else
            //    {
            //        for (int i = 0; i < TransformationsInverted.Length; i++)
            //        {
            //            if (TransformationsInverted[i].Column4 != transformsinv[i].Column4)
            //            { }
            //        }
            //    }
            //}

            Transformations = transforms.Count > 0 ? transforms.ToArray() : null;
            TransformationsInverted = transformsinv.Count > 0 ? transformsinv.ToArray() : null;
        }


        public static uint GetNumHashBuckets(int nHashes)
        {
            //todo: refactor with same in Clip.cs?
            if (nHashes < 11) return 11;
            if (nHashes < 29) return 29;
            if (nHashes < 59) return 59;
            if (nHashes < 107) return 107;
            if (nHashes < 191) return 191;
            if (nHashes < 331) return 331;
            if (nHashes < 563) return 563;
            if (nHashes < 953) return 953;
            if (nHashes < 1609) return 1609;
            if (nHashes < 2729) return 2729;
            if (nHashes < 4621) return 4621;
            if (nHashes < 7841) return 7841;
            if (nHashes < 13297) return 13297;
            if (nHashes < 22571) return 22571;
            if (nHashes < 38351) return 38351;
            if (nHashes < 65167) return 65167;
            /*if (nHashes < 65521)*/
            return 65521;
            //return ((uint)nHashes / 4) * 4 + 3;
        }


        public void ResetBoneTransforms()
        {
            if (Bones?.Items == null) return;
            foreach (Bone bone in Bones.Items) bone.ResetAnimTransform();
            UpdateBoneTransforms();
        }

        public void UpdateBoneTransforms()
        {
            if (Bones?.Items == null) return;
            if (BoneTransforms == null || BoneTransforms.Length != Bones.Items.Length)
                BoneTransforms = new Matrix3_s[Bones.Items.Length];
            for (int i = 0; i < Bones.Items.Length; i++)
            {
                Bone bone = Bones.Items[i];
                Matrix b = bone.SkinTransform;
                Matrix3_s bt = new Matrix3_s();
                bt.Row1 = b.Column1;
                bt.Row2 = b.Column2;
                bt.Row3 = b.Column3;
                BoneTransforms[i] = bt;
            }
        }


        public Skeleton Clone()
        {
            Skeleton skel = new Skeleton();

            skel.BoneTagsCapacity = BoneTagsCapacity;
            skel.BoneTagsCount = BoneTagsCount;
            skel.Unknown_1Ch = Unknown_1Ch;
            skel.Unknown_50h = Unknown_50h;
            skel.Unknown_54h = Unknown_54h;
            skel.Unknown_58h = Unknown_58h;
            skel.BonesCount = BonesCount;
            skel.ChildIndicesCount = ChildIndicesCount;

            if (BoneTags != null)
            {
                skel.BoneTags = new ResourcePointerArray64<SkeletonBoneTag>();
                if (BoneTags.data_items != null)
                {
                    skel.BoneTags.data_items = new SkeletonBoneTag[BoneTags.data_items.Length];
                    for (int i = 0; i < BoneTags.data_items.Length; i++)
                    {
                        SkeletonBoneTag obt = BoneTags.data_items[i];
                        SkeletonBoneTag nbt = new SkeletonBoneTag();
                        skel.BoneTags.data_items[i] = nbt;
                        while (obt != null)
                        {
                            nbt.BoneTag = obt.BoneTag;
                            nbt.BoneIndex = obt.BoneIndex;
                            obt = obt.Next;
                            if (obt != null)
                            {
                                SkeletonBoneTag nxt = new SkeletonBoneTag();
                                nbt.Next = nxt;
                                nbt = nxt;
                            }
                        }
                    }
                }
            }

            if (Bones != null)
            {
                skel.Bones = new SkeletonBonesBlock();
                if (Bones.Items != null)
                {
                    skel.Bones.Items = new Bone[Bones.Items.Length];
                    for (int i = 0; i < Bones.Items.Length; i++)
                    {
                        Bone ob = Bones.Items[i];
                        Bone nb = new Bone();
                        nb.Rotation = ob.Rotation;
                        nb.Translation = ob.Translation;
                        nb.Scale = ob.Scale;
                        nb.NextSiblingIndex = ob.NextSiblingIndex;
                        nb.ParentIndex = ob.ParentIndex;
                        nb.Flags = ob.Flags;
                        nb.Index = ob.Index;
                        nb.Tag = ob.Tag;
                        nb.Index2 = ob.Index2;
                        nb.Name = ob.Name;
                        nb.AnimRotation = ob.AnimRotation;
                        nb.AnimTranslation = ob.AnimTranslation;
                        nb.AnimScale = ob.AnimScale;
                        nb.AnimTransform = ob.AnimTransform;
                        nb.BindTransformInv = ob.BindTransformInv;
                        nb.SkinTransform = ob.SkinTransform;
                        skel.Bones.Items[i] = nb;
                    }
                }
            }

            skel.TransformationsInverted = (Matrix[])TransformationsInverted?.Clone();
            skel.Transformations = (Matrix[])Transformations?.Clone();
            skel.ParentIndices = (short[])ParentIndices?.Clone();
            skel.ChildIndices = (short[])ChildIndices?.Clone();

            skel.AssignBoneParents();
            skel.BuildBonesMap();

            return skel;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SkeletonBonesBlock : ResourceSystemBlock
    {
        public uint Unk0; // 0
        public uint Unk1; // 0
        public uint Unk2; // 0

        public override long BlockLength
        {
            get
            {
                long length = 16;
                if (Items != null)
                    foreach (Bone b in Items)
                        length += b.BlockLength;

                return length;
            }
        }

        public uint Count { get; set; }
        public Bone[] Items { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            Count = reader.ReadUInt32();
            Unk0 = reader.ReadUInt32();
            Unk1 = reader.ReadUInt32();
            Unk2 = reader.ReadUInt32();

            uint count = (uint)parameters[0];
            Bone[] items = new Bone[count];
            for (uint i = 0; i < count; i++) items[i] = reader.ReadBlock<Bone>();
            Items = items;


            //if (Count != count)
            //{ }//no hit
            //if (Unk0 != 0)
            //{ }//no hit
            //if (Unk1 != 0)
            //{ }//no hit
            //if (Unk2 != 0)
            //{ }//no hit
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            Count = (uint)(Items?.Length ?? 0);

            writer.Write(Count);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(Unk2);

            foreach (Bone b in Items) b.Write(writer);
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            List<Tuple<long, IResourceBlock>> list = new List<Tuple<long, IResourceBlock>>();
            long length = 16;
            if (Items != null)
                foreach (Bone b in Items)
                {
                    list.Add(new Tuple<long, IResourceBlock>(length, b));
                    length += b.BlockLength;
                }

            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SkeletonBoneTag : ResourceSystemBlock
    {
        public override long BlockLength => 16;

        // structure data
        public uint BoneTag { get; set; }
        public uint BoneIndex { get; set; }
        public ulong NextPointer { get; set; }

        // reference data
        public SkeletonBoneTag Next { get; set; } //don't know why it's linked here

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            BoneTag = reader.ReadUInt32();
            BoneIndex = reader.ReadUInt32();
            NextPointer = reader.ReadUInt64();

            // read reference data
            Next = reader.ReadBlockAt<SkeletonBoneTag>(
                NextPointer // offset
            );
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            NextPointer = (ulong)(Next != null ? Next.FilePosition : 0);

            // write structure data
            writer.Write(BoneTag);
            writer.Write(BoneIndex);
            writer.Write(NextPointer);
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (Next != null) list.Add(Next);
            return list.ToArray();
        }

        public override string ToString()
        {
            return BoneTag + ": " + BoneIndex;
        }
    }

    [Flags]
    public enum EBoneFlags : ushort
    {
        None = 0,
        RotX = 0x1,
        RotY = 0x2,
        RotZ = 0x4,
        LimitRotation = 0x8,
        TransX = 0x10,
        TransY = 0x20,
        TransZ = 0x40,
        LimitTranslation = 0x80,
        ScaleX = 0x100,
        ScaleY = 0x200,
        ScaleZ = 0x400,
        LimitScale = 0x800,
        Unk0 = 0x1000,
        Unk1 = 0x2000,
        Unk2 = 0x4000,
        Unk3 = 0x8000
    }

    // List of BoneTags which are hardcoded/not calculated using ElfHash and CalculateBoneHash
    internal enum EPedBoneId : ushort
    {
        SKEL_ROOT = 0x0,
        SKEL_Pelvis = 0x2E28,
        SKEL_L_Thigh = 0xE39F,
        SKEL_L_Calf = 0xF9BB,
        SKEL_L_Foot = 0x3779,
        SKEL_L_Toe0 = 0x83C,
        EO_L_Foot = 0x84C5,
        EO_L_Toe = 0x68BD,
        IK_L_Foot = 0xFEDD,
        PH_L_Foot = 0xE175,
        MH_L_Knee = 0xB3FE,
        SKEL_R_Thigh = 0xCA72,
        SKEL_R_Calf = 0x9000,
        SKEL_R_Foot = 0xCC4D,
        SKEL_R_Toe0 = 0x512D,
        EO_R_Foot = 0x1096,
        EO_R_Toe = 0x7163,
        IK_R_Foot = 0x8AAE,
        PH_R_Foot = 0x60E6,
        MH_R_Knee = 0x3FCF,
        RB_L_ThighRoll = 0x5C57,
        RB_R_ThighRoll = 0x192A,
        SKEL_Spine_Root = 0xE0FD,
        SKEL_Spine0 = 0x5C01,
        SKEL_Spine1 = 0x60F0,
        SKEL_Spine2 = 0x60F1,
        SKEL_Spine3 = 0x60F2,
        SKEL_L_Clavicle = 0xFCD9,
        SKEL_L_UpperArm = 0xB1C5,
        SKEL_L_Forearm = 0xEEEB,
        SKEL_L_Hand = 0x49D9,
        SKEL_L_Finger00 = 0x67F2,
        SKEL_L_Finger01 = 0xFF9,
        SKEL_L_Finger02 = 0xFFA,
        SKEL_L_Finger10 = 0x67F3,
        SKEL_L_Finger11 = 0x1049,
        SKEL_L_Finger12 = 0x104A,
        SKEL_L_Finger20 = 0x67F4,
        SKEL_L_Finger21 = 0x1059,
        SKEL_L_Finger22 = 0x105A,
        SKEL_L_Finger30 = 0x67F5,
        SKEL_L_Finger31 = 0x1029,
        SKEL_L_Finger32 = 0x102A,
        SKEL_L_Finger40 = 0x67F6,
        SKEL_L_Finger41 = 0x1039,
        SKEL_L_Finger42 = 0x103A,
        PH_L_Hand = 0xEB95,
        IK_L_Hand = 0x8CBD,
        RB_L_ForeArmRoll = 0xEE4F,
        RB_L_ArmRoll = 0x1470,
        MH_L_Elbow = 0x58B7,
        SKEL_R_Clavicle = 0x29D2,
        SKEL_R_UpperArm = 0x9D4D,
        SKEL_R_Forearm = 0x6E5C,
        SKEL_R_Hand = 0xDEAD,
        SKEL_R_Finger00 = 0xE5F2,
        SKEL_R_Finger01 = 0xFA10,
        SKEL_R_Finger02 = 0xFA11,
        SKEL_R_Finger10 = 0xE5F3,
        SKEL_R_Finger11 = 0xFA60,
        SKEL_R_Finger12 = 0xFA61,
        SKEL_R_Finger20 = 0xE5F4,
        SKEL_R_Finger21 = 0xFA70,
        SKEL_R_Finger22 = 0xFA71,
        SKEL_R_Finger30 = 0xE5F5,
        SKEL_R_Finger31 = 0xFA40,
        SKEL_R_Finger32 = 0xFA41,
        SKEL_R_Finger40 = 0xE5F6,
        SKEL_R_Finger41 = 0xFA50,
        SKEL_R_Finger42 = 0xFA51,
        PH_R_Hand = 0x6F06,
        IK_R_Hand = 0x188E,
        RB_R_ForeArmRoll = 0xAB22,
        RB_R_ArmRoll = 0x90FF,
        MH_R_Elbow = 0xBB0,
        SKEL_Neck_1 = 0x9995,
        SKEL_Head = 0x796E,
        IK_Head = 0x322C,
        FACIAL_facialRoot = 0xFE2C,
        FB_L_Brow_Out_000 = 0xE3DB,
        FB_L_Lid_Upper_000 = 0xB2B6,
        FB_L_Eye_000 = 0x62AC,
        FB_L_CheekBone_000 = 0x542E,
        FB_L_Lip_Corner_000 = 0x74AC,
        FB_R_Lid_Upper_000 = 0xAA10,
        FB_R_Eye_000 = 0x6B52,
        FB_R_CheekBone_000 = 0x4B88,
        FB_R_Brow_Out_000 = 0x54C,
        FB_R_Lip_Corner_000 = 0x2BA6,
        FB_Brow_Centre_000 = 0x9149,
        FB_UpperLipRoot_000 = 0x4ED2,
        FB_UpperLip_000 = 0xF18F,
        FB_L_Lip_Top_000 = 0x4F37,
        FB_R_Lip_Top_000 = 0x4537,
        FB_Jaw_000 = 0xB4A0,
        FB_LowerLipRoot_000 = 0x4324,
        FB_LowerLip_000 = 0x508F,
        FB_L_Lip_Bot_000 = 0xB93B,
        FB_R_Lip_Bot_000 = 0xC33B,
        FB_Tongue_000 = 0xB987,
        RB_Neck_1 = 0x8B93,
        SPR_L_Breast = 0xFC8E,
        SPR_R_Breast = 0x885F,
        IK_Root = 0xDD1C,
        SKEL_Neck_2 = 0x5FD4,
        SKEL_Pelvis1 = 0xD003,
        SKEL_PelvisRoot = 0x45FC,
        SKEL_SADDLE = 0x9524,
        MH_L_CalfBack = 0x1013,
        MH_L_ThighBack = 0x600D,
        SM_L_Skirt = 0xC419,
        MH_R_CalfBack = 0xB013,
        MH_R_ThighBack = 0x51A3,
        SM_R_Skirt = 0x7712,
        SM_M_BackSkirtRoll = 0xDBB,
        SM_L_BackSkirtRoll = 0x40B2,
        SM_R_BackSkirtRoll = 0xC141,
        SM_M_FrontSkirtRoll = 0xCDBB,
        SM_L_FrontSkirtRoll = 0x9B69,
        SM_R_FrontSkirtRoll = 0x86F1,
        SM_CockNBalls_ROOT = 0xC67D,
        SM_CockNBalls = 0x9D34,
        MH_L_Finger00 = 0x8C63,
        MH_L_FingerBulge00 = 0x5FB8,
        MH_L_Finger10 = 0x8C53,
        MH_L_FingerTop00 = 0xA244,
        MH_L_HandSide = 0xC78A,
        MH_Watch = 0x2738,
        MH_L_Sleeve = 0x933C,
        MH_R_Finger00 = 0x2C63,
        MH_R_FingerBulge00 = 0x69B8,
        MH_R_Finger10 = 0x2C53,
        MH_R_FingerTop00 = 0xEF4B,
        MH_R_HandSide = 0x68FB,
        MH_R_Sleeve = 0x92DC,
        FACIAL_jaw = 0xB21,
        FACIAL_underChin = 0x8A95,
        FACIAL_L_underChin = 0x234E,
        FACIAL_chin = 0xB578,
        FACIAL_chinSkinBottom = 0x98BC,
        FACIAL_L_chinSkinBottom = 0x3E8F,
        FACIAL_R_chinSkinBottom = 0x9E8F,
        FACIAL_tongueA = 0x4A7C,
        FACIAL_tongueB = 0x4A7D,
        FACIAL_tongueC = 0x4A7E,
        FACIAL_tongueD = 0x4A7F,
        FACIAL_tongueE = 0x4A80,
        FACIAL_L_tongueE = 0x35F2,
        FACIAL_R_tongueE = 0x2FF2,
        FACIAL_L_tongueD = 0x35F1,
        FACIAL_R_tongueD = 0x2FF1,
        FACIAL_L_tongueC = 0x35F0,
        FACIAL_R_tongueC = 0x2FF0,
        FACIAL_L_tongueB = 0x35EF,
        FACIAL_R_tongueB = 0x2FEF,
        FACIAL_L_tongueA = 0x35EE,
        FACIAL_R_tongueA = 0x2FEE,
        FACIAL_chinSkinTop = 0x7226,
        FACIAL_L_chinSkinTop = 0x3EB3,
        FACIAL_chinSkinMid = 0x899A,
        FACIAL_L_chinSkinMid = 0x4427,
        FACIAL_L_chinSide = 0x4A5E,
        FACIAL_R_chinSkinMid = 0xF5AF,
        FACIAL_R_chinSkinTop = 0xF03B,
        FACIAL_R_chinSide = 0xAA5E,
        FACIAL_R_underChin = 0x2BF4,
        FACIAL_L_lipLowerSDK = 0xB9E1,
        FACIAL_L_lipLowerAnalog = 0x244A,
        FACIAL_L_lipLowerThicknessV = 0xC749,
        FACIAL_L_lipLowerThicknessH = 0xC67B,
        FACIAL_lipLowerSDK = 0x7285,
        FACIAL_lipLowerAnalog = 0xD97B,
        FACIAL_lipLowerThicknessV = 0xC5BB,
        FACIAL_lipLowerThicknessH = 0xC5ED,
        FACIAL_R_lipLowerSDK = 0xA034,
        FACIAL_R_lipLowerAnalog = 0xC2D9,
        FACIAL_R_lipLowerThicknessV = 0xC6E9,
        FACIAL_R_lipLowerThicknessH = 0xC6DB,
        FACIAL_nose = 0x20F1,
        FACIAL_L_nostril = 0x7322,
        FACIAL_L_nostrilThickness = 0xC15F,
        FACIAL_noseLower = 0xE05A,
        FACIAL_L_noseLowerThickness = 0x79D5,
        FACIAL_R_noseLowerThickness = 0x7975,
        FACIAL_noseTip = 0x6A60,
        FACIAL_R_nostril = 0x7922,
        FACIAL_R_nostrilThickness = 0x36FF,
        FACIAL_noseUpper = 0xA04F,
        FACIAL_L_noseUpper = 0x1FB8,
        FACIAL_noseBridge = 0x9BA3,
        FACIAL_L_nasolabialFurrow = 0x5ACA,
        FACIAL_L_nasolabialBulge = 0xCD78,
        FACIAL_L_cheekLower = 0x6907,
        FACIAL_L_cheekLowerBulge1 = 0xE3FB,
        FACIAL_L_cheekLowerBulge2 = 0xE3FC,
        FACIAL_L_cheekInner = 0xE7AB,
        FACIAL_L_cheekOuter = 0x8161,
        FACIAL_L_eyesackLower = 0x771B,
        FACIAL_L_eyeball = 0x1744,
        FACIAL_L_eyelidLower = 0x998C,
        FACIAL_L_eyelidLowerOuterSDK = 0xFE4C,
        FACIAL_L_eyelidLowerOuterAnalog = 0xB9AA,
        FACIAL_L_eyelashLowerOuter = 0xD7F6,
        FACIAL_L_eyelidLowerInnerSDK = 0xF151,
        FACIAL_L_eyelidLowerInnerAnalog = 0x8242,
        FACIAL_L_eyelashLowerInner = 0x4CCF,
        FACIAL_L_eyelidUpper = 0x97C1,
        FACIAL_L_eyelidUpperOuterSDK = 0xAF15,
        FACIAL_L_eyelidUpperOuterAnalog = 0x67FA,
        FACIAL_L_eyelashUpperOuter = 0x27B7,
        FACIAL_L_eyelidUpperInnerSDK = 0xD341,
        FACIAL_L_eyelidUpperInnerAnalog = 0xF092,
        FACIAL_L_eyelashUpperInner = 0x9B1F,
        FACIAL_L_eyesackUpperOuterBulge = 0xA559,
        FACIAL_L_eyesackUpperInnerBulge = 0x2F2A,
        FACIAL_L_eyesackUpperOuterFurrow = 0xC597,
        FACIAL_L_eyesackUpperInnerFurrow = 0x52A7,
        FACIAL_forehead = 0x9218,
        FACIAL_L_foreheadInner = 0x843,
        FACIAL_L_foreheadInnerBulge = 0x767C,
        FACIAL_L_foreheadOuter = 0x8DCB,
        FACIAL_skull = 0x4221,
        FACIAL_foreheadUpper = 0xF7D6,
        FACIAL_L_foreheadUpperInner = 0xCF13,
        FACIAL_L_foreheadUpperOuter = 0x509B,
        FACIAL_R_foreheadUpperInner = 0xCEF3,
        FACIAL_R_foreheadUpperOuter = 0x507B,
        FACIAL_L_temple = 0xAF79,
        FACIAL_L_ear = 0x19DD,
        FACIAL_L_earLower = 0x6031,
        FACIAL_L_masseter = 0x2810,
        FACIAL_L_jawRecess = 0x9C7A,
        FACIAL_L_cheekOuterSkin = 0x14A5,
        FACIAL_R_cheekLower = 0xF367,
        FACIAL_R_cheekLowerBulge1 = 0x599B,
        FACIAL_R_cheekLowerBulge2 = 0x599C,
        FACIAL_R_masseter = 0x810,
        FACIAL_R_jawRecess = 0x93D4,
        FACIAL_R_ear = 0x1137,
        FACIAL_R_earLower = 0x8031,
        FACIAL_R_eyesackLower = 0x777B,
        FACIAL_R_nasolabialBulge = 0xD61E,
        FACIAL_R_cheekOuter = 0xD32,
        FACIAL_R_cheekInner = 0x737C,
        FACIAL_R_noseUpper = 0x1CD6,
        FACIAL_R_foreheadInner = 0xE43,
        FACIAL_R_foreheadInnerBulge = 0x769C,
        FACIAL_R_foreheadOuter = 0x8FCB,
        FACIAL_R_cheekOuterSkin = 0xB334,
        FACIAL_R_eyesackUpperInnerFurrow = 0x9FAE,
        FACIAL_R_eyesackUpperOuterFurrow = 0x140F,
        FACIAL_R_eyesackUpperInnerBulge = 0xA359,
        FACIAL_R_eyesackUpperOuterBulge = 0x1AF9,
        FACIAL_R_nasolabialFurrow = 0x2CAA,
        FACIAL_R_temple = 0xAF19,
        FACIAL_R_eyeball = 0x1944,
        FACIAL_R_eyelidUpper = 0x7E14,
        FACIAL_R_eyelidUpperOuterSDK = 0xB115,
        FACIAL_R_eyelidUpperOuterAnalog = 0xF25A,
        FACIAL_R_eyelashUpperOuter = 0xE0A,
        FACIAL_R_eyelidUpperInnerSDK = 0xD541,
        FACIAL_R_eyelidUpperInnerAnalog = 0x7C63,
        FACIAL_R_eyelashUpperInner = 0x8172,
        FACIAL_R_eyelidLower = 0x7FDF,
        FACIAL_R_eyelidLowerOuterSDK = 0x1BD,
        FACIAL_R_eyelidLowerOuterAnalog = 0x457B,
        FACIAL_R_eyelashLowerOuter = 0xBE49,
        FACIAL_R_eyelidLowerInnerSDK = 0xF351,
        FACIAL_R_eyelidLowerInnerAnalog = 0xE13,
        FACIAL_R_eyelashLowerInner = 0x3322,
        FACIAL_L_lipUpperSDK = 0x8F30,
        FACIAL_L_lipUpperAnalog = 0xB1CF,
        FACIAL_L_lipUpperThicknessH = 0x37CE,
        FACIAL_L_lipUpperThicknessV = 0x38BC,
        FACIAL_lipUpperSDK = 0x1774,
        FACIAL_lipUpperAnalog = 0xE064,
        FACIAL_lipUpperThicknessH = 0x7993,
        FACIAL_lipUpperThicknessV = 0x7981,
        FACIAL_L_lipCornerSDK = 0xB1C,
        FACIAL_L_lipCornerAnalog = 0xE568,
        FACIAL_L_lipCornerThicknessUpper = 0x7BC,
        FACIAL_L_lipCornerThicknessLower = 0xDD42,
        FACIAL_R_lipUpperSDK = 0x7583,
        FACIAL_R_lipUpperAnalog = 0x51CF,
        FACIAL_R_lipUpperThicknessH = 0x382E,
        FACIAL_R_lipUpperThicknessV = 0x385C,
        FACIAL_R_lipCornerSDK = 0xB3C,
        FACIAL_R_lipCornerAnalog = 0xEE0E,
        FACIAL_R_lipCornerThicknessUpper = 0x54C3,
        FACIAL_R_lipCornerThicknessLower = 0x2BBA,
        MH_MulletRoot = 0x3E73,
        MH_MulletScaler = 0xA1C2,
        MH_Hair_Scale = 0xC664,
        MH_Hair_Crown = 0x1675,
        SM_Torch = 0x8D6,
        FX_Light = 0x8959,
        FX_Light_Scale = 0x5038,
        FX_Light_Switch = 0xE18E,
        BagRoot = 0xAD09,
        BagPivotROOT = 0xB836,
        BagPivot = 0x4D11,
        BagBody = 0xAB6D,
        BagBone_R = 0x937,
        BagBone_L = 0x991,
        SM_LifeSaver_Front = 0x9420,
        SM_R_Pouches_ROOT = 0x2962,
        SM_R_Pouches = 0x4141,
        SM_L_Pouches_ROOT = 0x2A02,
        SM_L_Pouches = 0x4B41,
        SM_Suit_Back_Flapper = 0xDA2D,
        SPR_CopRadio = 0x8245,
        SM_LifeSaver_Back = 0x2127,
        MH_BlushSlider = 0xA0CE,
        SKEL_Tail_01 = 0x347,
        SKEL_Tail_02 = 0x348,
        MH_L_Concertina_B = 0xC988,
        MH_L_Concertina_A = 0xC987,
        MH_R_Concertina_B = 0xC8E8,
        MH_R_Concertina_A = 0xC8E7,
        MH_L_ShoulderBladeRoot = 0x8711,
        MH_L_ShoulderBlade = 0x4EAF,
        MH_R_ShoulderBladeRoot = 0x3A0A,
        MH_R_ShoulderBlade = 0x54AF,
        FB_R_Ear_000 = 0x6CDF,
        SPR_R_Ear = 0x63B6,
        FB_L_Ear_000 = 0x6439,
        SPR_L_Ear = 0x5B10,
        FB_TongueA_000 = 0x4206,
        FB_TongueB_000 = 0x4207,
        FB_TongueC_000 = 0x4208,
        SKEL_L_Toe1 = 0x1D6B,
        SKEL_R_Toe1 = 0xB23F,
        SKEL_Tail_03 = 0x349,
        SKEL_Tail_04 = 0x34A,
        SKEL_Tail_05 = 0x34B,
        SPR_Gonads_ROOT = 0xBFDE,
        SPR_Gonads = 0x1C00,
        FB_L_Brow_Out_001 = 0xE3DB,
        FB_L_Lid_Upper_001 = 0xB2B6,
        FB_L_Eye_001 = 0x62AC,
        FB_L_CheekBone_001 = 0x542E,
        FB_L_Lip_Corner_001 = 0x74AC,
        FB_R_Lid_Upper_001 = 0xAA10,
        FB_R_Eye_001 = 0x6B52,
        FB_R_CheekBone_001 = 0x4B88,
        FB_R_Brow_Out_001 = 0x54C,
        FB_R_Lip_Corner_001 = 0x2BA6,
        FB_Brow_Centre_001 = 0x9149,
        FB_UpperLipRoot_001 = 0x4ED2,
        FB_UpperLip_001 = 0xF18F,
        FB_L_Lip_Top_001 = 0x4F37,
        FB_R_Lip_Top_001 = 0x4537,
        FB_Jaw_001 = 0xB4A0,
        FB_LowerLipRoot_001 = 0x4324,
        FB_LowerLip_001 = 0x508F,
        FB_L_Lip_Bot_001 = 0xB93B,
        FB_R_Lip_Bot_001 = 0xC33B,
        FB_Tongue_001 = 0xB987
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Bone : ResourceSystemBlock, IMetaXmlItem
    {
        public Matrix AbsTransform; //original absolute transform from loaded file, calculated from bones hierarchy


        //used by CW for animating skeletons.
        public Quaternion AnimRotation; //relative to parent
        public Vector3 AnimScale;
        public Matrix AnimTransform; //absolute world transform, animated
        public Vector3 AnimTranslation; //relative to parent
        public Matrix BindTransformInv; //inverse of bind pose transform

        private string_r NameBlock;
        public Matrix SkinTransform; //transform to use for skin meshes
        public uint Unknown_1Ch; // 0x00000000 RHW?
        public uint Unknown_34h; // 0x00000000
        public ulong Unknown_48h; // 0x0000000000000000
        public override long BlockLength => 80;

        // structure data
        public Quaternion Rotation { get; set; }
        public Vector3 Translation { get; set; }
        public Vector3 Scale { get; set; }
        public float Unknown_2Ch { get; set; } = 1.0f; // 1.0  RHW?
        public short NextSiblingIndex { get; set; } //limb end index? IK chain?
        public short ParentIndex { get; set; }
        public ulong NamePointer { get; set; }
        public EBoneFlags Flags { get; set; }
        public short Index { get; set; }
        public ushort Tag { get; set; }
        public short Index2 { get; set; } //always same as Index

        // reference data
        public string Name { get; set; }

        public Bone Parent { get; set; }

        public Vector4
            TransformUnk { get; set; } //unknown value (column 4) from skeleton's transform array, used for IO purposes

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.StringTag(sb, indent, "Name", Name);
            YdrXml.ValueTag(sb, indent, "Tag", Tag.ToString());
            YdrXml.ValueTag(sb, indent, "Index", Index.ToString());
            YdrXml.ValueTag(sb, indent, "ParentIndex", ParentIndex.ToString());
            YdrXml.ValueTag(sb, indent, "SiblingIndex", NextSiblingIndex.ToString());
            YdrXml.StringTag(sb, indent, "Flags", Flags.ToString());
            YdrXml.SelfClosingTag(sb, indent, "Translation " + FloatUtil.GetVector3XmlString(Translation));
            YdrXml.SelfClosingTag(sb, indent, "Rotation " + FloatUtil.GetVector4XmlString(Rotation.ToVector4()));
            YdrXml.SelfClosingTag(sb, indent, "Scale " + FloatUtil.GetVector3XmlString(Scale));
            YdrXml.SelfClosingTag(sb, indent, "TransformUnk " + FloatUtil.GetVector4XmlString(TransformUnk));
        }

        public void ReadXml(XmlNode node)
        {
            Name = Xml.GetChildInnerText(node, "Name");
            Tag = (ushort)Xml.GetChildUIntAttribute(node, "Tag");
            Index = (short)Xml.GetChildIntAttribute(node, "Index");
            Index2 = Index;
            ParentIndex = (short)Xml.GetChildIntAttribute(node, "ParentIndex");
            NextSiblingIndex = (short)Xml.GetChildIntAttribute(node, "SiblingIndex");
            Flags = Xml.GetChildEnumInnerText<EBoneFlags>(node, "Flags");
            Translation = Xml.GetChildVector3Attributes(node, "Translation");
            Rotation = Xml.GetChildVector4Attributes(node, "Rotation").ToQuaternion();
            Scale = Xml.GetChildVector3Attributes(node, "Scale");
            TransformUnk = Xml.GetChildVector4Attributes(node, "TransformUnk");
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            Rotation = new Quaternion(reader.ReadVector4());
            Translation = reader.ReadVector3();
            Unknown_1Ch = reader.ReadUInt32();
            Scale = reader.ReadVector3();
            Unknown_2Ch = reader.ReadSingle();
            NextSiblingIndex = reader.ReadInt16();
            ParentIndex = reader.ReadInt16();
            Unknown_34h = reader.ReadUInt32();
            NamePointer = reader.ReadUInt64();
            Flags = (EBoneFlags)reader.ReadUInt16();
            Index = reader.ReadInt16();
            Tag = reader.ReadUInt16();
            Index2 = reader.ReadInt16();
            Unknown_48h = reader.ReadUInt64();

            // read reference data
            Name = reader.ReadStringAt( //BlockAt<string_r>(
                NamePointer // offset
            );

            //if (Index2 != Index)
            //{ }//no hits

            AnimRotation = Rotation;
            AnimTranslation = Translation;
            AnimScale = Scale;


            //if (Unknown_1Ch != 0)
            //{ }
            //if (Unknown_34h != 0)
            //{ }
            //if (Unknown_48h != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            NamePointer = (ulong)(NameBlock != null ? NameBlock.FilePosition : 0);

            // write structure data
            writer.Write(Rotation.ToVector4());
            writer.Write(Translation);
            writer.Write(Unknown_1Ch);
            writer.Write(Scale);
            writer.Write(Unknown_2Ch);
            writer.Write(NextSiblingIndex);
            writer.Write(ParentIndex);
            writer.Write(Unknown_34h);
            writer.Write(NamePointer);
            writer.Write((ushort)Flags);
            writer.Write(Index);
            writer.Write(Tag);
            writer.Write(Index2);
            writer.Write(Unknown_48h);
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (Name != null)
            {
                NameBlock = (string_r)Name;
                list.Add(NameBlock);
            }

            return list.ToArray();
        }

        public override string ToString()
        {
            return Tag + ": " + Name;
        }


        public void UpdateAnimTransform()
        {
            AnimTransform = Matrix.AffineTransformation(1.0f, AnimRotation, AnimTranslation);
            AnimTransform.ScaleVector *= AnimScale;
            if (Parent != null) AnimTransform = AnimTransform * Parent.AnimTransform;

            ////AnimTransform = Matrix.AffineTransformation(1.0f, AnimRotation, AnimTranslation);//(local transform)

            //var pos = AnimTranslation;
            //var ori = AnimRotation;
            //var sca = AnimScale;
            //var pbone = Parent;
            //while (pbone != null)
            //{
            //    pos = pbone.AnimRotation.Multiply(pos /** pbone.AnimScale*/) + pbone.AnimTranslation;
            //    ori = pbone.AnimRotation * ori;
            //    pbone = pbone.Parent;
            //}
            //AnimTransform = Matrix.AffineTransformation(1.0f, ori, pos);//(global transform)
            //AnimTransform.ScaleVector *= sca;
        }

        public void UpdateSkinTransform()
        {
            SkinTransform = BindTransformInv * AnimTransform;
            //SkinTransform = Matrix.Identity;//(for testing)
        }

        public void ResetAnimTransform()
        {
            AnimRotation = Rotation;
            AnimTranslation = Translation;
            AnimScale = Scale;
            UpdateAnimTransform();
            UpdateSkinTransform();
        }

        public static uint ElfHash_Uppercased(string str)
        {
            uint hash = 0;
            uint x = 0;
            uint i = 0;

            for (i = 0; i < str.Length; i++)
            {
                byte c = (byte)str[(int)i];
                if ((byte)(c - 'a') <= 25u) // to uppercase
                    c -= 32;

                hash = (hash << 4) + c;

                if ((x = hash & 0xF0000000) != 0) hash ^= x >> 24;

                hash &= ~x;
            }

            return hash;
        }

        public static ushort CalculateBoneHash(string boneName)
        {
            return (ushort)(ElfHash_Uppercased(boneName) % 0xFE8F + 0x170);
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Joints : ResourceSystemBlock
    {
        private ResourceSystemStructBlock<JointRotationLimit_s> RotationLimitsBlock; //for saving only
        private ResourceSystemStructBlock<JointTranslationLimit_s> TranslationLimitsBlock;
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ushort Unknown_34h; // 0x0000
        public ushort Unknown_36h = 1; // 0x0001
        public ulong Unknown_38h; // 0x0000000000000000
        public uint Unknown_4h = 1; // 0x00000001
        public ulong Unknown_8h; // 0x0000000000000000
        public override long BlockLength => 64;

        // structure data
        public uint VFT { get; set; } = 1080130656;
        public ulong RotationLimitsPointer { get; set; }
        public ulong TranslationLimitsPointer { get; set; }
        public ushort RotationLimitsCount { get; set; }
        public ushort TranslationLimitsCount { get; set; }

        // reference data
        public JointRotationLimit_s[] RotationLimits { get; set; }
        public JointTranslationLimit_s[] TranslationLimits { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt64();
            RotationLimitsPointer = reader.ReadUInt64();
            TranslationLimitsPointer = reader.ReadUInt64();
            Unknown_20h = reader.ReadUInt64();
            Unknown_28h = reader.ReadUInt64();
            RotationLimitsCount = reader.ReadUInt16();
            TranslationLimitsCount = reader.ReadUInt16();
            Unknown_34h = reader.ReadUInt16();
            Unknown_36h = reader.ReadUInt16();
            Unknown_38h = reader.ReadUInt64();

            // read reference data
            RotationLimits = reader.ReadStructsAt<JointRotationLimit_s>(RotationLimitsPointer, RotationLimitsCount);
            TranslationLimits =
                reader.ReadStructsAt<JointTranslationLimit_s>(TranslationLimitsPointer, TranslationLimitsCount);

            //if (Unknown_4h != 1)
            //{ }
            //if (Unknown_8h != 0)
            //{ }
            //if (Unknown_20h != 0)
            //{ }
            //if (Unknown_28h != 0)
            //{ }
            //if (Unknown_34h != 0)
            //{ }
            //if (Unknown_36h != 1)
            //{ }
            //if (Unknown_38h != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            RotationLimitsPointer = (ulong)(RotationLimitsBlock != null ? RotationLimitsBlock.FilePosition : 0);
            TranslationLimitsPointer =
                (ulong)(TranslationLimitsBlock != null ? TranslationLimitsBlock.FilePosition : 0);
            RotationLimitsCount = (ushort)(RotationLimitsBlock != null ? RotationLimitsBlock.ItemCount : 0);
            TranslationLimitsCount = (ushort)(TranslationLimitsBlock != null ? TranslationLimitsBlock.ItemCount : 0);


            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(Unknown_8h);
            writer.Write(RotationLimitsPointer);
            writer.Write(TranslationLimitsPointer);
            writer.Write(Unknown_20h);
            writer.Write(Unknown_28h);
            writer.Write(RotationLimitsCount);
            writer.Write(TranslationLimitsCount);
            writer.Write(Unknown_34h);
            writer.Write(Unknown_36h);
            writer.Write(Unknown_38h);
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            if (RotationLimits != null) YdrXml.WriteItemArray(sb, RotationLimits, indent, "RotationLimits");
            if (TranslationLimits != null) YdrXml.WriteItemArray(sb, TranslationLimits, indent, "TranslationLimits");
        }

        public void ReadXml(XmlNode node)
        {
            RotationLimits = XmlMeta.ReadItemArray<JointRotationLimit_s>(node, "RotationLimits");
            TranslationLimits = XmlMeta.ReadItemArray<JointTranslationLimit_s>(node, "TranslationLimits");
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (RotationLimits != null)
            {
                RotationLimitsBlock = new ResourceSystemStructBlock<JointRotationLimit_s>(RotationLimits);
                list.Add(RotationLimitsBlock);
            }

            if (TranslationLimits != null)
            {
                TranslationLimitsBlock = new ResourceSystemStructBlock<JointTranslationLimit_s>(TranslationLimits);
                list.Add(TranslationLimitsBlock);
            }

            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct JointRotationLimit_s : IMetaXmlItem
    {
        // structure data
        public uint Unknown_0h { get; set; } // 0x00000000
        public uint Unknown_4h { get; set; } // 0x00000000
        public ushort BoneId { get; set; }
        public ushort Unknown_Ah { get; set; }
        public uint Unknown_Ch { get; set; } // 0x00000001
        public uint Unknown_10h { get; set; } // 0x00000003
        public uint Unknown_14h { get; set; } // 0x00000000
        public uint Unknown_18h { get; set; } // 0x00000000
        public uint Unknown_1Ch { get; set; } // 0x00000000
        public uint Unknown_20h { get; set; } // 0x00000000
        public uint Unknown_24h { get; set; } // 0x00000000
        public uint Unknown_28h { get; set; } // 0x00000000
        public float Unknown_2Ch { get; set; } // 1.0
        public uint Unknown_30h { get; set; } // 0x00000000
        public uint Unknown_34h { get; set; } // 0x00000000
        public uint Unknown_38h { get; set; } // 0x00000000
        public uint Unknown_3Ch { get; set; } // 0x00000000
        public float Unknown_40h { get; set; } // 1.0
        public uint Unknown_44h { get; set; } // 0x00000000
        public uint Unknown_48h { get; set; } // 0x00000000
        public uint Unknown_4Ch { get; set; } // 0x00000000
        public float Unknown_50h { get; set; } // -pi
        public float Unknown_54h { get; set; } // pi
        public float Unknown_58h { get; set; } // 1.0
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
        public float Unknown_74h { get; set; } // pi
        public float Unknown_78h { get; set; } // -pi
        public float Unknown_7Ch { get; set; } // pi
        public float Unknown_80h { get; set; } // pi
        public float Unknown_84h { get; set; } // -pi
        public float Unknown_88h { get; set; } // pi
        public float Unknown_8Ch { get; set; } // pi
        public float Unknown_90h { get; set; } // -pi
        public float Unknown_94h { get; set; } // pi
        public float Unknown_98h { get; set; } // pi
        public float Unknown_9Ch { get; set; } // -pi
        public float Unknown_A0h { get; set; } // pi
        public float Unknown_A4h { get; set; } // pi
        public float Unknown_A8h { get; set; } // -pi
        public float Unknown_ACh { get; set; } // pi
        public float Unknown_B0h { get; set; } // pi
        public float Unknown_B4h { get; set; } // -pi
        public float Unknown_B8h { get; set; } // pi
        public uint Unknown_BCh { get; set; } // 0x00000100

        private void Init()
        {
            float pi = (float)Math.PI;
            Unknown_0h = 0;
            Unknown_4h = 0;
            BoneId = 0;
            Unknown_Ah = 0;
            Unknown_Ch = 1;
            Unknown_10h = 3;
            Unknown_14h = 0;
            Unknown_18h = 0;
            Unknown_1Ch = 0;
            Unknown_20h = 0;
            Unknown_24h = 0;
            Unknown_28h = 0;
            Unknown_2Ch = 1.0f;
            Unknown_30h = 0;
            Unknown_34h = 0;
            Unknown_38h = 0;
            Unknown_3Ch = 0;
            Unknown_40h = 1.0f;
            Unknown_44h = 0;
            Unknown_48h = 0;
            Unknown_4Ch = 0;
            Unknown_50h = -pi;
            Unknown_54h = pi;
            Unknown_58h = 1.0f;
            Min = Vector3.Zero;
            Max = Vector3.Zero;
            Unknown_74h = pi;
            Unknown_78h = -pi;
            Unknown_7Ch = pi;
            Unknown_80h = pi;
            Unknown_84h = -pi;
            Unknown_88h = pi;
            Unknown_8Ch = pi;
            Unknown_90h = -pi;
            Unknown_94h = pi;
            Unknown_98h = pi;
            Unknown_9Ch = -pi;
            Unknown_A0h = pi;
            Unknown_A4h = pi;
            Unknown_A8h = -pi;
            Unknown_ACh = pi;
            Unknown_B0h = pi;
            Unknown_B4h = -pi;
            Unknown_B8h = pi;
            Unknown_BCh = 0x100;
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "BoneId", BoneId.ToString());
            YdrXml.ValueTag(sb, indent, "UnknownA", Unknown_Ah.ToString());
            YdrXml.SelfClosingTag(sb, indent, "Min " + FloatUtil.GetVector3XmlString(Min));
            YdrXml.SelfClosingTag(sb, indent, "Max " + FloatUtil.GetVector3XmlString(Max));
        }

        public void ReadXml(XmlNode node)
        {
            Init();
            BoneId = (ushort)Xml.GetChildUIntAttribute(node, "BoneId");
            Unknown_Ah = (ushort)Xml.GetChildUIntAttribute(node, "UnknownA");
            Min = Xml.GetChildVector3Attributes(node, "Min");
            Max = Xml.GetChildVector3Attributes(node, "Max");
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct JointTranslationLimit_s : IMetaXmlItem
    {
        public uint Unknown_0h { get; set; } // 0x00000000
        public uint Unknown_4h { get; set; } // 0x00000000
        public uint BoneId { get; set; }
        public uint Unknown_Ch { get; set; } // 0x00000000
        public uint Unknown_10h { get; set; } // 0x00000000
        public uint Unknown_14h { get; set; } // 0x00000000
        public uint Unknown_18h { get; set; } // 0x00000000
        public uint Unknown_1Ch { get; set; } // 0x00000000
        public Vector3 Min { get; set; }
        public uint Unknown_2Ch { get; set; } // 0x00000000
        public Vector3 Max { get; set; }
        public uint Unknown_3Ch { get; set; } // 0x00000000

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "BoneId", BoneId.ToString());
            YdrXml.SelfClosingTag(sb, indent, "Min " + FloatUtil.GetVector3XmlString(Min));
            YdrXml.SelfClosingTag(sb, indent, "Max " + FloatUtil.GetVector3XmlString(Max));
        }

        public void ReadXml(XmlNode node)
        {
            BoneId = (ushort)Xml.GetChildUIntAttribute(node, "BoneId");
            Min = Xml.GetChildVector3Attributes(node, "Min");
            Max = Xml.GetChildVector3Attributes(node, "Max");
        }
    }


    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawableModelsBlock : ResourceSystemBlock
    {
        public DrawableBase Owner;

        public override long BlockLength
        {
            get
            {
                long len = 0;
                len += ListLength(High, len);
                len += ListLength(Med, len);
                len += ListLength(Low, len);
                len += ListLength(VLow, len);
                len += ListLength(Extra, len);
                return len;
            }
        }

        public DrawableModel[] High { get; set; }
        public DrawableModel[] Med { get; set; }
        public DrawableModel[] Low { get; set; }
        public DrawableModel[] VLow { get; set; }
        public DrawableModel[] Extra { get; set; } //shouldn't be used

        public ResourcePointerListHeader HighHeader { get; set; }
        public ResourcePointerListHeader MedHeader { get; set; }
        public ResourcePointerListHeader LowHeader { get; set; }
        public ResourcePointerListHeader VLowHeader { get; set; }
        public ResourcePointerListHeader ExtraHeader { get; set; }

        public ulong[] HighPointers { get; set; }
        public ulong[] MedPointers { get; set; }
        public ulong[] LowPointers { get; set; }
        public ulong[] VLowPointers { get; set; }
        public ulong[] ExtraPointers { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            Owner = parameters[0] as DrawableBase;
            ulong pos = (ulong)reader.Position;
            ulong highPointer = Owner?.DrawableModelsHighPointer ?? 0;
            ulong medPointer = Owner?.DrawableModelsMediumPointer ?? 0;
            ulong lowPointer = Owner?.DrawableModelsLowPointer ?? 0;
            ulong vlowPointer = Owner?.DrawableModelsVeryLowPointer ?? 0;
            ulong extraPointer = pos != highPointer ? pos : 0;

            if (highPointer != 0)
            {
                HighHeader = reader.ReadStructAt<ResourcePointerListHeader>((long)highPointer);
                HighPointers = reader.ReadUlongsAt(HighHeader.Pointer, HighHeader.Capacity, false);
                High = reader.ReadBlocks<DrawableModel>(HighPointers);
            }

            if (medPointer != 0)
            {
                MedHeader = reader.ReadStructAt<ResourcePointerListHeader>((long)medPointer);
                MedPointers = reader.ReadUlongsAt(MedHeader.Pointer, MedHeader.Capacity, false);
                Med = reader.ReadBlocks<DrawableModel>(MedPointers);
            }

            if (lowPointer != 0)
            {
                LowHeader = reader.ReadStructAt<ResourcePointerListHeader>((long)lowPointer);
                LowPointers = reader.ReadUlongsAt(LowHeader.Pointer, LowHeader.Capacity, false);
                Low = reader.ReadBlocks<DrawableModel>(LowPointers);
            }

            if (vlowPointer != 0)
            {
                VLowHeader = reader.ReadStructAt<ResourcePointerListHeader>((long)vlowPointer);
                VLowPointers = reader.ReadUlongsAt(VLowHeader.Pointer, VLowHeader.Capacity, false);
                VLow = reader.ReadBlocks<DrawableModel>(VLowPointers);
            }

            if (extraPointer != 0)
            {
                ExtraHeader = reader.ReadStructAt<ResourcePointerListHeader>((long)extraPointer);
                ExtraPointers = reader.ReadUlongsAt(ExtraHeader.Pointer, ExtraHeader.Capacity, false);
                Extra = reader.ReadBlocks<DrawableModel>(ExtraPointers);
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            ResourcePointerListHeader makeHeader(ref long p, int c)
            {
                p += Pad(p);
                ResourcePointerListHeader h = new ResourcePointerListHeader
                    { Pointer = (ulong)(p + 16), Count = (ushort)c, Capacity = (ushort)c };
                p += HeaderLength(c);
                return h;
            }

            ulong[] makePointers(ref long p, DrawableModel[] a)
            {
                ulong[] ptrs = new ulong[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    p += Pad(p);
                    ptrs[i] = (ulong)p;
                    p += a[i].BlockLength;
                }

                return ptrs;
            }

            void write(ResourcePointerListHeader h, ulong[] p, DrawableModel[] a)
            {
                writer.WritePadding(16);
                writer.WriteStruct(h);
                writer.WriteUlongs(p);
                for (int i = 0; i < a.Length; i++)
                {
                    writer.WritePadding(16);
                    writer.WriteBlock(a[i]);
                }
            }

            long ptr = writer.Position;
            if (High != null)
            {
                HighHeader = makeHeader(ref ptr, High.Length);
                HighPointers = makePointers(ref ptr, High);
                write(HighHeader, HighPointers, High);
            }

            if (Med != null)
            {
                MedHeader = makeHeader(ref ptr, Med.Length);
                MedPointers = makePointers(ref ptr, Med);
                write(MedHeader, MedPointers, Med);
            }

            if (Low != null)
            {
                LowHeader = makeHeader(ref ptr, Low.Length);
                LowPointers = makePointers(ref ptr, Low);
                write(LowHeader, LowPointers, Low);
            }

            if (VLow != null)
            {
                VLowHeader = makeHeader(ref ptr, VLow.Length);
                VLowPointers = makePointers(ref ptr, VLow);
                write(VLowHeader, VLowPointers, VLow);
            }

            if (Extra != null)
            {
                ExtraHeader = makeHeader(ref ptr, Extra.Length);
                ExtraPointers = makePointers(ref ptr, Extra);
                write(ExtraHeader, ExtraPointers, Extra);
            }
        }


        private long Pad(long o)
        {
            return (16 - o % 16) % 16;
        }

        private long HeaderLength(int listlength)
        {
            return 16 + listlength * 8;
        }

        private long ListLength(DrawableModel[] list, long o)
        {
            if (list == null) return 0;
            long l = 0;
            l += HeaderLength(list.Length);
            foreach (DrawableModel m in list) l += Pad(l) + m.BlockLength;
            return Pad(o) + l;
        }


        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            List<Tuple<long, IResourceBlock>> parts = new List<Tuple<long, IResourceBlock>>();
            parts.AddRange(base.GetParts());

            void addParts(ref long p, DrawableModel[] a)
            {
                if (a == null) return;
                p += Pad(p);
                p += HeaderLength(a.Length);
                foreach (DrawableModel m in a)
                {
                    p += Pad(p);
                    parts.Add(new Tuple<long, IResourceBlock>(p, m));
                    p += m.BlockLength;
                }
            }

            long ptr = 0;
            addParts(ref ptr, High);
            addParts(ref ptr, Med);
            addParts(ref ptr, Low);
            addParts(ref ptr, VLow);
            addParts(ref ptr, Extra);

            return parts.ToArray();
        }


        public long GetHighPointer()
        {
            if (High == null) return 0;
            return FilePosition;
        }

        public long GetMedPointer()
        {
            if (Med == null) return 0;
            long p = FilePosition;
            p += ListLength(High, p);
            p += Pad(p);
            return p;
        }

        public long GetLowPointer()
        {
            if (Low == null) return 0;
            long p = GetMedPointer();
            p += ListLength(Med, p);
            p += Pad(p);
            return p;
        }

        public long GetVLowPointer()
        {
            if (VLow == null) return 0;
            long p = GetLowPointer();
            p += ListLength(Low, p);
            p += Pad(p);
            return p;
        }

        public long GetExtraPointer()
        {
            if (Extra == null) return 0;
            long p = GetVLowPointer();
            p += ListLength(VLow, p);
            p += Pad(p);
            return p;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawableModel : ResourceSystemBlock, IMetaXmlItem
    {
        public uint Unknown_14h; // 0x00000000
        public uint Unknown_4h = 1; // 0x00000001

        public override long BlockLength
        {
            get
            {
                long off = 48;
                off += GeometriesCount1 * 2; //ShaderMapping
                if (GeometriesCount1 == 1) off += 6;
                else off += (16 - off % 16) % 16;
                off += GeometriesCount1 * 8; //Geometries pointers
                off += (16 - off % 16) % 16;
                off += (GeometriesCount1 + (GeometriesCount1 > 1 ? 1 : 0)) * 32; //BoundsData
                for (int i = 0; i < GeometriesCount1; i++)
                {
                    DrawableGeometry geom = Geometries != null ? Geometries[i] : null;
                    if (geom != null)
                    {
                        off += (16 - off % 16) % 16;
                        off += geom.BlockLength; //Geometries
                    }
                }

                return off;
            }
        }

        // structure data
        public uint VFT { get; set; } = 1080101528;
        public ulong GeometriesPointer { get; set; }
        public ushort GeometriesCount1 { get; set; }
        public ushort GeometriesCount2 { get; set; } //always equal to GeometriesCount1
        public ulong BoundsPointer { get; set; }
        public ulong ShaderMappingPointer { get; set; }
        public uint SkeletonBinding { get; set; } //4th byte is bone index, 2nd byte for skin meshes
        public ushort RenderMaskFlags { get; set; } //First byte is called "Mask" in GIMS EVO
        public ushort GeometriesCount3 { get; set; } //always equal to GeometriesCount1, is it ShaderMappingCount?
        public ushort[] ShaderMapping { get; set; }
        public ulong[] GeometryPointers { get; set; }
        public AABB_s[] BoundsData { get; set; }
        public DrawableGeometry[] Geometries { get; set; }

        public byte BoneIndex
        {
            get => (byte)((SkeletonBinding >> 24) & 0xFF);
            set => SkeletonBinding = (SkeletonBinding & 0x00FFFFFF) + ((value & 0xFFu) << 24);
        }

        public byte SkeletonBindUnk2 //always 0
        {
            get => (byte)((SkeletonBinding >> 16) & 0xFF);
            set => SkeletonBinding = (SkeletonBinding & 0xFF00FFFF) + ((value & 0xFFu) << 16);
        }

        public byte HasSkin //only 0 or 1
        {
            get => (byte)((SkeletonBinding >> 8) & 0xFF);
            set => SkeletonBinding = (SkeletonBinding & 0xFFFF00FF) + ((value & 0xFFu) << 8);
        }

        public byte SkeletonBindUnk1 //only 0 or 43 (in rare cases, see below)
        {
            get => (byte)((SkeletonBinding >> 0) & 0xFF);
            set => SkeletonBinding = (SkeletonBinding & 0xFFFFFF00) + ((value & 0xFFu) << 0);
        }

        public byte RenderMask
        {
            get => (byte)((RenderMaskFlags >> 0) & 0xFF);
            set => RenderMaskFlags = (ushort)((RenderMaskFlags & 0xFF00u) + ((value & 0xFFu) << 0));
        }

        public byte Flags
        {
            get => (byte)((RenderMaskFlags >> 8) & 0xFF);
            set => RenderMaskFlags = (ushort)((RenderMaskFlags & 0xFFu) + ((value & 0xFFu) << 8));
        }


        public long MemoryUsage
        {
            get
            {
                long val = 0;
                if (Geometries != null)
                    foreach (DrawableGeometry geom in Geometries)
                    {
                        if (geom == null) continue;
                        if (geom.VertexData != null) val += geom.VertexData.MemoryUsage;
                        if (geom.IndexBuffer != null) val += geom.IndexBuffer.IndicesCount * 4;
                        if (geom.VertexBuffer != null)
                        {
                            if (geom.VertexBuffer.Data1 != null && geom.VertexBuffer.Data1 != geom.VertexData)
                                val += geom.VertexBuffer.Data1.MemoryUsage;
                            if (geom.VertexBuffer.Data2 != null && geom.VertexBuffer.Data2 != geom.VertexData)
                                val += geom.VertexBuffer.Data2.MemoryUsage;
                        }
                    }

                if (BoundsData != null) val += BoundsData.Length * 32;
                return val;
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "RenderMask", RenderMask.ToString());
            YdrXml.ValueTag(sb, indent, "Flags", Flags.ToString());
            YdrXml.ValueTag(sb, indent, "HasSkin", HasSkin.ToString());
            YdrXml.ValueTag(sb, indent, "BoneIndex", BoneIndex.ToString());
            YdrXml.ValueTag(sb, indent, "Unknown1", SkeletonBindUnk1.ToString());

            if (Geometries != null) YdrXml.WriteItemArray(sb, Geometries, indent, "Geometries");
        }

        public void ReadXml(XmlNode node)
        {
            RenderMask = (byte)Xml.GetChildUIntAttribute(node, "RenderMask");
            Flags = (byte)Xml.GetChildUIntAttribute(node, "Flags");
            HasSkin = (byte)Xml.GetChildUIntAttribute(node, "HasSkin");
            BoneIndex = (byte)Xml.GetChildUIntAttribute(node, "BoneIndex");
            SkeletonBindUnk1 = (byte)Xml.GetChildUIntAttribute(node, "Unknown1");

            List<AABB_s> aabbs = new List<AABB_s>();
            List<ushort> shids = new List<ushort>();
            Vector4 min = new Vector4(float.MaxValue);
            Vector4 max = new Vector4(float.MinValue);
            DrawableGeometry[] geoms = XmlMeta.ReadItemArray<DrawableGeometry>(node, "Geometries");
            if (geoms != null)
            {
                Geometries = geoms;
                foreach (DrawableGeometry geom in geoms)
                {
                    aabbs.Add(geom.AABB);
                    shids.Add(geom.ShaderID);
                    min = Vector4.Min(min, geom.AABB.Min);
                    max = Vector4.Max(max, geom.AABB.Max);
                }

                GeometriesCount1 = GeometriesCount2 = GeometriesCount3 = (ushort)geoms.Length;
            }

            if (aabbs.Count > 1)
            {
                AABB_s outeraabb = new AABB_s { Min = min, Max = max };
                aabbs.Insert(0, outeraabb);
            }

            BoundsData = aabbs.Count > 0 ? aabbs.ToArray() : null;
            ShaderMapping = shids.Count > 0 ? shids.ToArray() : null;
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            GeometriesPointer = reader.ReadUInt64();
            GeometriesCount1 = reader.ReadUInt16();
            GeometriesCount2 = reader.ReadUInt16();
            Unknown_14h = reader.ReadUInt32();
            BoundsPointer = reader.ReadUInt64();
            ShaderMappingPointer = reader.ReadUInt64();
            SkeletonBinding = reader.ReadUInt32();
            RenderMaskFlags = reader.ReadUInt16();
            GeometriesCount3 = reader.ReadUInt16();

            ShaderMapping = reader.ReadUshortsAt(ShaderMappingPointer, GeometriesCount1, false);
            GeometryPointers = reader.ReadUlongsAt(GeometriesPointer, GeometriesCount1, false);
            BoundsData = reader.ReadStructsAt<AABB_s>(BoundsPointer,
                (uint)(GeometriesCount1 > 1 ? GeometriesCount1 + 1 : GeometriesCount1), false);
            Geometries = reader.ReadBlocks<DrawableGeometry>(GeometryPointers);

            if (Geometries != null)
                for (int i = 0; i < Geometries.Length; i++)
                {
                    DrawableGeometry geom = Geometries[i];
                    if (geom != null)
                    {
                        geom.ShaderID = ShaderMapping != null && i < ShaderMapping.Length
                            ? ShaderMapping[i]
                            : (ushort)0;
                        geom.AABB = BoundsData != null
                            ? BoundsData.Length > 1 && i + 1 < BoundsData.Length ? BoundsData[i + 1] : BoundsData[0]
                            : new AABB_s();
                    }
                }


            ////just testing!
            /*
            //var pos = (ulong)reader.Position;
            //var off = (ulong)0;
            //if (ShaderMappingPointer != (pos + off))
            //{ }//no hit
            //off += (ulong)(GeometriesCount1 * 2); //ShaderMapping
            //if (GeometriesCount1 == 1) off += 6;
            //else off += ((16 - (off % 16)) % 16);
            //if (GeometriesPointer != (pos + off))
            //{ }//no hit
            //off += (ulong)(GeometriesCount1 * 8); //Geometries pointers
            //off += ((16 - (off % 16)) % 16);
            //if (BoundsPointer != (pos + off))
            //{ }//no hit
            //off += (ulong)((GeometriesCount1 + ((GeometriesCount1 > 1) ? 1 : 0)) * 32); //BoundsData
            //if ((GeometryPointers != null) && (Geometries != null))
            //{
            //    for (int i = 0; i < GeometriesCount1; i++)
            //    {
            //        var geomptr = GeometryPointers[i];
            //        var geom = Geometries[i];
            //        if (geom != null)
            //        {
            //            off += ((16 - (off % 16)) % 16);
            //            if (geomptr != (pos + off))
            //            { }//no hit
            //            off += (ulong)geom.BlockLength;
            //        }
            //        else
            //        { }//no hit
            //    }
            //}
            //else
            //{ }//no hit

            //if (SkeletonBindUnk2 != 0)
            //{ }//no hit
            //switch (SkeletonBindUnk1)
            //{
            //    case 0:
            //        break;
            //    case 43://des_plog_light_root.ydr, des_heli_scrapyard_skin002.ydr, v_74_it1_ceiling_smoke_02_skin.ydr, buzzard2.yft, vader.yft, zombiea.yft
            //        break;
            //    default:
            //        break;//no hit
            //}
            //switch (HasSkin)
            //{
            //    case 0:
            //    case 1:
            //        break;
            //    default:
            //        break;//no hit
            //}
            //if (Unknown_4h != 1)
            //{ }//no hit
            //if (Unknown_14h != 0)
            //{ }//no hit
            */
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            GeometriesCount1 = (ushort)(Geometries != null ? Geometries.Length : 0);
            GeometriesCount2 = GeometriesCount1; //is this correct?
            GeometriesCount3 = GeometriesCount1; //is this correct?

            long pad(long o)
            {
                return (16 - o % 16) % 16;
            }

            long off = writer.Position + 48;
            ShaderMappingPointer = (ulong)off;
            off += GeometriesCount1 * 2; //ShaderMapping
            if (GeometriesCount1 == 1) off += 6;
            else off += pad(off);
            GeometriesPointer = (ulong)off;
            off += GeometriesCount1 * 8; //Geometries pointers
            off += pad(off);
            BoundsPointer = (ulong)off;
            off += BoundsData.Length * 32; //BoundsData
            GeometryPointers = new ulong[GeometriesCount1];
            for (int i = 0; i < GeometriesCount1; i++)
            {
                DrawableGeometry geom = Geometries != null ? Geometries[i] : null;
                if (geom != null)
                {
                    off += pad(off);
                    GeometryPointers[i] = (ulong)off;
                    off += geom.BlockLength; //Geometries
                }
            }


            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(GeometriesPointer);
            writer.Write(GeometriesCount1);
            writer.Write(GeometriesCount2);
            writer.Write(Unknown_14h);
            writer.Write(BoundsPointer);
            writer.Write(ShaderMappingPointer);
            writer.Write(SkeletonBinding);
            writer.Write(RenderMaskFlags);
            writer.Write(GeometriesCount3);


            for (int i = 0; i < GeometriesCount1; i++) writer.Write(ShaderMapping[i]);
            if (GeometriesCount1 == 1)
                writer.Write(new byte[6]);
            else
                writer.WritePadding(16);
            for (int i = 0; i < GeometriesCount1; i++) writer.Write(GeometryPointers[i]);
            writer.WritePadding(16);
            for (int i = 0; i < BoundsData.Length; i++) writer.WriteStruct(BoundsData[i]);
            for (int i = 0; i < GeometriesCount1; i++)
            {
                DrawableGeometry geom = Geometries != null ? Geometries[i] : null;
                if (geom != null)
                {
                    writer.WritePadding(16);
                    writer.WriteBlock(geom);
                }
            }
        }


        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            List<Tuple<long, IResourceBlock>> parts = new List<Tuple<long, IResourceBlock>>();
            parts.AddRange(base.GetParts());

            long off = 48;
            off += GeometriesCount1 * 2; //ShaderMapping
            if (GeometriesCount1 == 1) off += 6;
            else off += (16 - off % 16) % 16;
            off += GeometriesCount1 * 8; //Geometries pointers
            off += (16 - off % 16) % 16;
            off += (GeometriesCount1 + (GeometriesCount1 > 1 ? 1 : 0)) * 32; //BoundsData
            for (int i = 0; i < GeometriesCount1; i++)
            {
                DrawableGeometry geom = Geometries != null ? Geometries[i] : null;
                if (geom != null)
                {
                    off += (16 - off % 16) % 16;
                    parts.Add(new Tuple<long, IResourceBlock>(off, geom));
                    off += geom.BlockLength; //Geometries
                }
            }

            return parts.ToArray();
        }

        public override string ToString()
        {
            return "(" + (Geometries?.Length ?? 0) + " geometr" + ((Geometries?.Length ?? 0) != 1 ? "ies)" : "y)");
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawableGeometry : ResourceSystemBlock, IMetaXmlItem
    {
        public ulong Unknown_10h; // 0x0000000000000000
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ulong Unknown_30h; // 0x0000000000000000
        public ulong Unknown_40h; // 0x0000000000000000
        public ulong Unknown_48h; // 0x0000000000000000
        public uint Unknown_4h = 1; // 0x00000001
        public ulong Unknown_50h; // 0x0000000000000000
        public ushort Unknown_62h = 3; // 0x0003 // indices per primitive (triangle)
        public uint Unknown_64h; // 0x00000000
        public uint Unknown_74h; // 0x00000000
        public ulong Unknown_80h; // 0x0000000000000000
        public ulong Unknown_88h; // 0x0000000000000000
        public ulong Unknown_8h; // 0x0000000000000000
        public ulong Unknown_90h; // 0x0000000000000000


        public bool UpdateRenderableParameters = false; //used by model material editor...

        public override long BlockLength
        {
            get
            {
                long l = 152;
                if (BoneIds != null)
                {
                    if (BoneIds.Length > 4) l += 8;
                    l += BoneIds.Length * 2;
                }

                return l;
            }
        }

        // structure data
        public uint VFT { get; set; } = 1080133528;
        public ulong VertexBufferPointer { get; set; }
        public ulong IndexBufferPointer { get; set; }
        public uint IndicesCount { get; set; }
        public uint TrianglesCount { get; set; }
        public ushort VerticesCount { get; set; }
        public ulong BoneIdsPointer { get; set; }
        public ushort VertexStride { get; set; }
        public ushort BoneIdsCount { get; set; }
        public ulong VertexDataPointer { get; set; }

        // reference data
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public VertexData VertexData { get; set; }
        public ushort[] BoneIds { get; set; } //embedded at the end of this struct
        public ShaderFX Shader { get; set; } //written by parent DrawableBase, using ShaderID
        public ushort ShaderID { get; set; } //read/written by parent model
        public AABB_s AABB { get; set; } //read/written by parent model

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "ShaderIndex", ShaderID.ToString());
            YdrXml.SelfClosingTag(sb, indent, "BoundingBoxMin " + FloatUtil.GetVector4XmlString(AABB.Min));
            YdrXml.SelfClosingTag(sb, indent, "BoundingBoxMax " + FloatUtil.GetVector4XmlString(AABB.Max));
            if (BoneIds != null)
            {
                StringBuilder ids = new StringBuilder();
                foreach (ushort id in BoneIds)
                {
                    if (ids.Length > 0) ids.Append(", ");
                    ids.Append(id.ToString());
                }

                YdrXml.StringTag(sb, indent, "BoneIDs", ids.ToString());
            }

            if (VertexBuffer != null)
            {
                YdrXml.OpenTag(sb, indent, "VertexBuffer");
                VertexBuffer.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "VertexBuffer");
            }

            if (IndexBuffer != null)
            {
                YdrXml.OpenTag(sb, indent, "IndexBuffer");
                IndexBuffer.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "IndexBuffer");
            }
        }

        public void ReadXml(XmlNode node)
        {
            ShaderID = (ushort)Xml.GetChildUIntAttribute(node, "ShaderIndex");
            AABB_s aabb = new AABB_s();
            aabb.Min = Xml.GetChildVector4Attributes(node, "BoundingBoxMin");
            aabb.Max = Xml.GetChildVector4Attributes(node, "BoundingBoxMax");
            AABB = aabb;
            XmlNode bnode = node.SelectSingleNode("BoneIDs");
            if (bnode != null)
            {
                string astr = bnode.InnerText;
                string[] arr = astr.Split(',');
                List<ushort> blist = new List<ushort>();
                foreach (string bstr in arr)
                {
                    string tstr = bstr?.Trim();
                    if (string.IsNullOrEmpty(tstr)) continue;
                    if (ushort.TryParse(tstr, out ushort u)) blist.Add(u);
                }

                BoneIds = blist.Count > 0 ? blist.ToArray() : null;
            }

            XmlNode vnode = node.SelectSingleNode("VertexBuffer");
            if (vnode != null)
            {
                VertexBuffer = new VertexBuffer();
                VertexBuffer.ReadXml(vnode);
                VertexData = VertexBuffer.Data1 ?? VertexBuffer.Data2;
            }

            XmlNode inode = node.SelectSingleNode("IndexBuffer");
            if (inode != null)
            {
                IndexBuffer = new IndexBuffer();
                IndexBuffer.ReadXml(inode);
            }
        }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt64();
            Unknown_10h = reader.ReadUInt64();
            VertexBufferPointer = reader.ReadUInt64();
            Unknown_20h = reader.ReadUInt64();
            Unknown_28h = reader.ReadUInt64();
            Unknown_30h = reader.ReadUInt64();
            IndexBufferPointer = reader.ReadUInt64();
            Unknown_40h = reader.ReadUInt64();
            Unknown_48h = reader.ReadUInt64();
            Unknown_50h = reader.ReadUInt64();
            IndicesCount = reader.ReadUInt32();
            TrianglesCount = reader.ReadUInt32();
            VerticesCount = reader.ReadUInt16();
            Unknown_62h = reader.ReadUInt16();
            Unknown_64h = reader.ReadUInt32();
            BoneIdsPointer = reader.ReadUInt64();
            VertexStride = reader.ReadUInt16();
            BoneIdsCount = reader.ReadUInt16();
            Unknown_74h = reader.ReadUInt32();
            VertexDataPointer = reader.ReadUInt64();
            Unknown_80h = reader.ReadUInt64();
            Unknown_88h = reader.ReadUInt64();
            Unknown_90h = reader.ReadUInt64();

            // read reference data
            VertexBuffer = reader.ReadBlockAt<VertexBuffer>(
                VertexBufferPointer // offset
            );
            IndexBuffer = reader.ReadBlockAt<IndexBuffer>(
                IndexBufferPointer // offset
            );
            BoneIds = reader.ReadUshortsAt(BoneIdsPointer, BoneIdsCount, false);
            if (BoneIds != null) //skinned mesh bones to use? peds, also yft props...
            {
            }
            //if (BoneIdsPointer != 0)
            //{
            //    var pos = (ulong)reader.Position;
            //    if (BoneIdsCount > 4) pos += 8;
            //    if (BoneIdsPointer != pos)
            //    { }//no hit - interesting alignment, boneids array always packed after this struct
            //}

            if (VertexBuffer != null)
            {
                VertexData = VertexBuffer.Data1 ?? VertexBuffer.Data2;

                if (VerticesCount == 0) VerticesCount = (ushort)(VertexData?.VertexCount ?? 0);

                //if (VertexBuffer.Data1 != VertexBuffer.Data2)
                //{ }//no hit
                //if (VertexDataPointer == 0)
                //{ }//no hit
                //else if (VertexDataPointer != VertexBuffer.DataPointer1)
                //{
                //    ////some mods hit here!
                //    //try
                //    //{
                //    //    this.VertexData = reader.ReadBlockAt<VertexData>(
                //    //        this.VertexDataPointer, // offset
                //    //        this.VertexStride,
                //    //        this.VerticesCount,
                //    //        this.VertexBuffer.Info
                //    //    );
                //    //}
                //    //catch
                //    //{ }
                //}
                //if (VertexStride != VertexBuffer.VertexStride)
                //{ }//no hit
                //if (VertexStride != (VertexBuffer.Info?.Stride ?? 0))
                //{ }//no hit
            }
            //else
            //{ }//no hit


            //if (Unknown_4h != 1)
            //{ }
            //if (Unknown_8h != 0)
            //{ }
            //if (Unknown_10h != 0)
            //{ }
            //if (Unknown_20h != 0)
            //{ }
            //if (Unknown_28h != 0)
            //{ }
            //if (Unknown_30h != 0)
            //{ }
            //if (Unknown_40h != 0)
            //{ }
            //if (Unknown_48h != 0)
            //{ }
            //if (Unknown_50h != 0)
            //{ }
            //if (Unknown_64h != 0)
            //{ }
            //if (Unknown_74h != 0)
            //{ }
            //if (Unknown_80h != 0)
            //{ }
            //if (Unknown_88h != 0)
            //{ }
            //if (Unknown_90h != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            VertexBufferPointer = (ulong)(VertexBuffer != null ? VertexBuffer.FilePosition : 0);
            IndexBufferPointer = (ulong)(IndexBuffer != null ? IndexBuffer.FilePosition : 0);
            VertexDataPointer = (ulong)(VertexData != null ? VertexData.FilePosition : 0);
            VerticesCount = (ushort)(VertexData != null ? VertexData.VertexCount : 0); //TODO: fix?
            VertexStride = (ushort)(VertexBuffer != null ? VertexBuffer.VertexStride : 0); //TODO: fix?
            IndicesCount = IndexBuffer != null ? IndexBuffer.IndicesCount : 0; //TODO: fix?
            TrianglesCount = IndicesCount / 3; //TODO: fix?
            BoneIdsPointer = BoneIds != null ? (ulong)(writer.Position + 152 + (BoneIds.Length > 4 ? 8 : 0)) : 0;
            BoneIdsCount = (ushort)(BoneIds?.Length ?? 0);


            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(Unknown_8h);
            writer.Write(Unknown_10h);
            writer.Write(VertexBufferPointer);
            writer.Write(Unknown_20h);
            writer.Write(Unknown_28h);
            writer.Write(Unknown_30h);
            writer.Write(IndexBufferPointer);
            writer.Write(Unknown_40h);
            writer.Write(Unknown_48h);
            writer.Write(Unknown_50h);
            writer.Write(IndicesCount);
            writer.Write(TrianglesCount);
            writer.Write(VerticesCount);
            writer.Write(Unknown_62h);
            writer.Write(Unknown_64h);
            writer.Write(BoneIdsPointer);
            writer.Write(VertexStride);
            writer.Write(BoneIdsCount);
            writer.Write(Unknown_74h);
            writer.Write(VertexDataPointer);
            writer.Write(Unknown_80h);
            writer.Write(Unknown_88h);
            writer.Write(Unknown_90h);

            if (BoneIds != null)
            {
                if (BoneIds.Length > 4) writer.Write((ulong)0);
                for (int i = 0; i < BoneIds.Length; i++) writer.Write(BoneIds[i]);
            }
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (VertexBuffer != null) list.Add(VertexBuffer);
            if (IndexBuffer != null) list.Add(IndexBuffer);
            if (VertexData != null) list.Add(VertexData);
            return list.ToArray();
        }

        public override string ToString()
        {
            return VerticesCount + " verts, " + Shader;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VertexBuffer : ResourceSystemBlock
    {
        public uint G9_Unknown_14h;
        public ulong G9_Unknown_20h;

        // gen9 structure data
        public ushort G9_Unknown_Eh;
        public uint Unknown_1Ch; // 0x00000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ulong Unknown_38h; // 0x0000000000000000
        public ulong Unknown_40h; // 0x0000000000000000
        public ulong Unknown_48h; // 0x0000000000000000
        public uint Unknown_4h = 1; // 0x00000001
        public ulong Unknown_50h; // 0x0000000000000000
        public ulong Unknown_58h; // 0x0000000000000000
        public ulong Unknown_60h; // 0x0000000000000000
        public ulong Unknown_68h; // 0x0000000000000000
        public ulong Unknown_70h; // 0x0000000000000000
        public ulong Unknown_78h; // 0x0000000000000000
        public uint Unknown_Ch; // 0x00000000
        public override long BlockLength => 128;
        public override long BlockLength_Gen9 => 64;

        // structure data
        public uint VFT { get; set; } = 1080153080;
        public ushort VertexStride { get; set; }
        public ushort Flags { get; set; } //only 0 or 1024
        public ulong DataPointer1 { get; set; }
        public uint VertexCount { get; set; }
        public ulong DataPointer2 { get; set; }
        public ulong InfoPointer { get; set; }
        public uint G9_BindFlags { get; set; } // m_bindFlags    0x00580409 or 0x00586409
        public ulong G9_SRVPointer { get; set; }
        public ShaderResourceViewG9 G9_SRV { get; set; }
        public VertexDeclarationG9 G9_Info { get; set; }


        // reference data
        public VertexData Data1 { get; set; }
        public VertexData Data2 { get; set; }
        public VertexDeclaration Info { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();

            if (reader.IsGen9)
            {
                VertexCount = reader.ReadUInt32();
                VertexStride = reader.ReadUInt16(); // m_vertexSize
                G9_Unknown_Eh = reader.ReadUInt16();
                G9_BindFlags = reader.ReadUInt32();
                G9_Unknown_14h = reader.ReadUInt32();
                DataPointer1 = reader.ReadUInt64(); // m_vertexData
                G9_Unknown_20h = reader.ReadUInt64(); // m_pad
                Unknown_28h = reader.ReadUInt64(); // m_pad2
                G9_SRVPointer = reader.ReadUInt64(); // m_srv
                InfoPointer = reader.ReadUInt64(); // m_vertexFormat (rage::grcFvf)

                G9_SRV = reader.ReadBlockAt<ShaderResourceViewG9>(G9_SRVPointer);
                G9_Info = reader.ReadBlockAt<VertexDeclarationG9>(InfoPointer);

                uint datalen = VertexCount * VertexStride;
                byte[] vertexBytes = reader.ReadBytesAt(DataPointer1, datalen);
                InitVertexDataFromGen9Data(vertexBytes);

                if (G9_Unknown_Eh != 0)
                {
                }

                switch (G9_BindFlags)
                {
                    case 0x00580409:
                    case 0x00586409:
                        break;
                }

                if (G9_Unknown_14h != 0)
                {
                }

                if (G9_Unknown_20h != 0)
                {
                }

                if (Unknown_28h != 0)
                {
                }
            }
            else
            {
                VertexStride = reader.ReadUInt16();
                Flags = reader.ReadUInt16();
                Unknown_Ch = reader.ReadUInt32();
                DataPointer1 = reader.ReadUInt64();
                VertexCount = reader.ReadUInt32();
                Unknown_1Ch = reader.ReadUInt32();
                DataPointer2 = reader.ReadUInt64();
                Unknown_28h = reader.ReadUInt64();
                InfoPointer = reader.ReadUInt64();
                Unknown_38h = reader.ReadUInt64();
                Unknown_40h = reader.ReadUInt64();
                Unknown_48h = reader.ReadUInt64();
                Unknown_50h = reader.ReadUInt64();
                Unknown_58h = reader.ReadUInt64();
                Unknown_60h = reader.ReadUInt64();
                Unknown_68h = reader.ReadUInt64();
                Unknown_70h = reader.ReadUInt64();
                Unknown_78h = reader.ReadUInt64();

                // read reference data
                Info = reader.ReadBlockAt<VertexDeclaration>(
                    InfoPointer // offset
                );
                Data1 = reader.ReadBlockAt<VertexData>(
                    DataPointer1, // offset
                    VertexStride,
                    VertexCount,
                    Info
                );
                Data2 = reader.ReadBlockAt<VertexData>(
                    DataPointer2, // offset
                    VertexStride,
                    VertexCount,
                    Info
                );


                //switch (Flags)
                //{
                //    case 0:
                //        break;
                //    case 1024://micro flag? //micro_brow_down.ydr, micro_chin_pointed.ydr
                //        break;
                //    default:
                //        break;
                //}

                //if (Unknown_4h != 1)
                //{ }
                //if (Unknown_Ch != 0)
                //{ }
                //if (Unknown_1Ch != 0)
                //{ }
                //if (Unknown_28h != 0)
                //{ }
                //if (Unknown_38h != 0)
                //{ }
                //if (Unknown_40h != 0)
                //{ }
                //if (Unknown_48h != 0)
                //{ }
                //if (Unknown_50h != 0)
                //{ }
                //if (Unknown_58h != 0)
                //{ }
                //if (Unknown_60h != 0)
                //{ }
                //if (Unknown_68h != 0)
                //{ }
                //if (Unknown_70h != 0)
                //{ }
                //if (Unknown_78h != 0)
                //{ }
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            VertexCount = (uint)(Data1 != null ? Data1.VertexCount : Data2 != null ? Data2.VertexCount : 0);
            DataPointer1 = (ulong)(Data1 != null ? Data1.FilePosition : 0);
            DataPointer2 = (ulong)(Data2 != null ? Data2.FilePosition : 0);
            InfoPointer = (ulong)(Info != null ? Info.FilePosition : 0);

            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);

            if (writer.IsGen9)
            {
                G9_SRVPointer = (ulong)(G9_SRV != null ? G9_SRV.FilePosition : 0);
                InfoPointer = (ulong)(G9_Info != null ? G9_Info.FilePosition : 0);

                if (G9_BindFlags == 0) G9_BindFlags = 0x00580409;
                //G9_BindFlags = //TODO?

                writer.Write(VertexCount);
                writer.Write(VertexStride); // m_vertexSize
                writer.Write(G9_Unknown_Eh);
                writer.Write(G9_BindFlags);
                writer.Write(G9_Unknown_14h);
                writer.Write(DataPointer1); // m_vertexData
                writer.Write(G9_Unknown_20h); // m_pad
                writer.Write(Unknown_28h); // m_pad2
                writer.Write(G9_SRVPointer); // m_srv
                writer.Write(InfoPointer); // m_vertexFormat (rage::grcFvf)
            }
            else
            {
                writer.Write(VertexStride);
                writer.Write(Flags);
                writer.Write(Unknown_Ch);
                writer.Write(DataPointer1);
                writer.Write(VertexCount);
                writer.Write(Unknown_1Ch);
                writer.Write(DataPointer2);
                writer.Write(Unknown_28h);
                writer.Write(InfoPointer);
                writer.Write(Unknown_38h);
                writer.Write(Unknown_40h);
                writer.Write(Unknown_48h);
                writer.Write(Unknown_50h);
                writer.Write(Unknown_58h);
                writer.Write(Unknown_60h);
                writer.Write(Unknown_68h);
                writer.Write(Unknown_70h);
                writer.Write(Unknown_78h);
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.ValueTag(sb, indent, "Flags", Flags.ToString());

            if (Info != null) Info.WriteXml(sb, indent, "Layout");
            if (Data1 != null)
            {
                YdrXml.OpenTag(sb, indent, "Data");
                Data1.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "Data");
            }

            if (Data2 != null && Data2 != Data1)
            {
                YdrXml.OpenTag(sb, indent, "Data2");
                Data2.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "Data2");
            }
        }

        public void ReadXml(XmlNode node)
        {
            Flags = (ushort)Xml.GetChildUIntAttribute(node, "Flags");

            XmlNode inode = node.SelectSingleNode("Layout");
            if (inode != null)
            {
                Info = new VertexDeclaration();
                Info.ReadXml(inode);
                VertexStride = Info.Stride;
            }

            XmlNode dnode = node.SelectSingleNode("Data");
            if (dnode != null)
            {
                Data1 = new VertexData();
                Data1.ReadXml(dnode, Info);
                Data2 = Data1;
                VertexCount = (uint)Data1.VertexCount;
            }

            XmlNode dnode2 = node.SelectSingleNode("Data2");
            if (dnode2 != null)
            {
                Data2 = new VertexData();
                Data2.ReadXml(dnode2, Info);
            }
        }


        public void InitVertexDataFromGen9Data(byte[] gen9bytes)
        {
            if (gen9bytes == null) return;
            if (G9_Info == null) return;

            //create VertexDeclaration (Info) from G9_Info
            //and remap vertex data into Data1.VertexBytes (and Data2)

            byte[] g9types = G9_Info.Types;
            byte[]
                g9sizes = G9_Info
                    .Sizes; //these seem to just contain the vertex stride - not sizes but offsets to next item
            uint[] g9offs = G9_Info.Offsets;
            VertexDeclaration vd = G9_Info.GetLegacyDeclaration();
            VertexDeclarationTypes vdtypes = vd.Types;
            VertexType vtype = (VertexType)vd.Flags;

            //this really sucks that we have to rebuild the vertex data, but component ordering is different!
            //maybe some layouts still have the same ordering so this could be bypassed, but probably not many.
            byte[] buf = new byte[gen9bytes.Length];
            for (int i = 0; i < g9types.Length; i++) //52
            {
                byte t = g9types[i];
                if (t == 0) continue;
                int lci = VertexDeclarationG9.GetLegacyComponentIndex(i, vdtypes);
                if (lci < 0) continue;
                int cssize = g9sizes[i];
                int csoff = (int)g9offs[i];
                int cdoff = vd.GetComponentOffset(lci);
                VertexComponentType cdtype = vd.GetComponentType(lci);
                int cdsize = VertexComponentTypes.GetSizeInBytes(cdtype);
                for (int v = 0; v < VertexCount; v++)
                {
                    int srcoff = csoff + cssize * v;
                    int dstoff = cdoff + VertexStride * v;
                    Buffer.BlockCopy(gen9bytes, srcoff, buf, dstoff, cdsize);
                }
            }

            VertexData data = new VertexData();
            data.VertexStride = VertexStride;
            data.VertexCount = (int)VertexCount;
            data.Info = vd;
            data.VertexType = vtype;
            data.VertexBytes = buf;

            Data1 = data;
            Data2 = data;
            Info = vd;
        }

        public byte[] InitGen9DataFromVertexData()
        {
            if (Info == null) return null;
            if (Data1?.VertexBytes == null) return null;

            //create G9_Info from Info
            //and remap vertex data from Data1.VertexBytes into the result

            VertexDeclaration vd = Info;
            VertexDeclarationTypes vdtypes = vd.Types;
            VertexDeclarationG9 info = VertexDeclarationG9.FromLegacyDeclaration(vd);
            uint[] g9offs = info.Offsets;
            byte[]
                g9sizes = info
                    .Sizes; //these seem to just contain the vertex stride - not sizes but offsets to next item
            byte[] g9types = info.Types;

            if (G9_Info != null) //sanity check with existing layout
            {
                if (info.VertexSize != G9_Info.VertexSize)
                {
                }

                if (info.VertexCount != G9_Info.VertexCount)
                {
                }

                if (info.ElementCount != G9_Info.ElementCount)
                {
                }

                for (int i = 0; i < 52; i++)
                {
                    if (info.Offsets[i] != G9_Info.Offsets[i])
                    {
                    }

                    if (info.Sizes[i] != G9_Info.Sizes[i])
                    {
                    }

                    if (info.Types[i] != G9_Info.Types[i])
                    {
                    }
                }
            }

            G9_Info = info;

            byte[] legabytes = Data1.VertexBytes;
            byte[] buf = new byte[legabytes.Length];
            for (int i = 0; i < g9types.Length; i++) //52
            {
                byte t = g9types[i];
                if (t == 0) continue;
                int lci = VertexDeclarationG9.GetLegacyComponentIndex(i, vdtypes);
                if (lci < 0) continue;
                int cssize = g9sizes[i];
                int csoff = (int)g9offs[i];
                int cdoff = vd.GetComponentOffset(lci);
                VertexComponentType cdtype = vd.GetComponentType(lci);
                int cdsize = VertexComponentTypes.GetSizeInBytes(cdtype);
                for (int v = 0; v < VertexCount; v++)
                {
                    int srcoff = cdoff + VertexStride * v;
                    int dstoff = csoff + cssize * v;
                    Buffer.BlockCopy(legabytes, srcoff, buf, dstoff, cdsize);
                }
            }

            return buf;
        }

        public void EnsureGen9()
        {
            VFT = 1080153080;
            Unknown_4h = 1;

            if (Data1 == null && Data2 != null) Data1 = Data2;
            if (Data1 != null) Data1.G9_VertexBytes = InitGen9DataFromVertexData();
            Data2 = Data1;

            if (G9_SRV == null)
            {
                G9_SRV = new ShaderResourceViewG9();
                G9_SRV.Dimension = ShaderResourceViewDimensionG9.Buffer;
            }
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (Data1 != null) list.Add(Data1);
            if (Data2 != null) list.Add(Data2);
            if (G9_SRV != null) list.Add(G9_SRV);
            if (G9_Info != null)
            {
                list.Add(G9_Info);
            }
            else
            {
                if (Info != null) list.Add(Info);
            }

            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VertexData : ResourceSystemBlock
    {
        //private int length = 0;
        public override long BlockLength => VertexBytes?.Length ?? 0; //this.length;


        public int VertexStride { get; set; }
        public int VertexCount { get; set; }
        public VertexDeclaration Info { get; set; }
        public VertexType VertexType { get; set; }

        public byte[] VertexBytes { get; set; }
        public byte[] G9_VertexBytes { get; set; } //only use when saving


        public long MemoryUsage => VertexCount * (long)VertexStride;

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            //not used by gen9 reader

            VertexStride = Convert.ToInt32(parameters[0]);
            VertexCount = Convert.ToInt32(parameters[1]);
            Info = (VertexDeclaration)parameters[2];
            VertexType = (VertexType)Info.Flags;

            VertexBytes = reader.ReadBytes(VertexCount * VertexStride);

            switch (Info.Types)
            {
                case VertexDeclarationTypes.GTAV1: //YDR - 0x7755555555996996
                    break;
                case VertexDeclarationTypes.GTAV2: //YFT - 0x030000000199A006
                    switch (Info.Flags)
                    {
                        case 16473: VertexType = VertexType.PCCH2H4; break; //  PCCH2H4 
                    }

                    break;
                case VertexDeclarationTypes.GTAV3: //YFT - 0x0300000001996006  PNCH2H4
                    switch (Info.Flags)
                    {
                        case 89: VertexType = VertexType.PNCH2; break; //  PNCH2
                    }

                    break;
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            if (writer.IsGen9)
            {
                if (G9_VertexBytes != null) writer.Write(G9_VertexBytes);
            }
            else
            {
                if (VertexBytes != null)
                    writer.Write(VertexBytes); //not dealing with individual vertex data here any more!
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            uint flags = Info?.Flags ?? 0;
            StringBuilder row = new StringBuilder();
            for (int v = 0; v < VertexCount; v++)
            {
                row.Clear();
                for (int k = 0; k < 16; k++)
                    if (((flags >> k) & 0x1) == 1)
                    {
                        if (row.Length > 0) row.Append("   ");
                        string str = GetString(v, k, " ");
                        row.Append(str);
                    }

                YdrXml.Indent(sb, indent);
                sb.AppendLine(row.ToString());
            }
        }

        public void ReadXml(XmlNode node, VertexDeclaration info)
        {
            Info = info;
            VertexType = (VertexType)(info?.Flags ?? 0);

            if (Info != null)
            {
                uint flags = Info.Flags;
                ushort stride = Info.Stride;
                List<string[]> vstrs = new List<string[]>();
                char[] coldelim = { ' ', '\t' };
                char[] rowdelim = { '\n' };
                string[] rows = node?.InnerText?.Trim()?.Split(rowdelim, StringSplitOptions.RemoveEmptyEntries);
                if (rows != null)
                    foreach (string row in rows)
                    {
                        string rowt = row.Trim();
                        if (string.IsNullOrEmpty(rowt)) continue;
                        string[] cols = row.Split(coldelim, StringSplitOptions.RemoveEmptyEntries);
                        vstrs.Add(cols);
                    }

                if (vstrs.Count > 0)
                {
                    AllocateData(vstrs.Count);
                    for (int v = 0; v < vstrs.Count; v++)
                    {
                        string[] vstr = vstrs[v];
                        int sind = 0;
                        for (int k = 0; k < 16; k++)
                            if (((flags >> k) & 0x1) == 1)
                                SetString(v, k, vstr, ref sind);
                    }
                }
            }
        }


        public void AllocateData(int vertexCount)
        {
            if (Info != null)
            {
                ushort stride = Info.Stride;
                int byteCount = vertexCount * stride;
                VertexBytes = new byte[byteCount];
                VertexCount = vertexCount;
            }
        }

        public void SetString(int v, int c, string[] strs, ref int sind)
        {
            if (Info != null && VertexBytes != null && strs != null)
            {
                int ind = sind;

                float f(int i)
                {
                    return FloatUtil.Parse(strs[ind + i].Trim());
                }

                byte b(int i)
                {
                    if (byte.TryParse(strs[ind + i].Trim(), out byte x)) return x;
                    return 0;
                }

                VertexComponentType ct = Info.GetComponentType(c);
                int cc = VertexComponentTypes.GetComponentCount(ct);
                if (sind + cc > strs.Length) return;
                switch (ct)
                {
                    case VertexComponentType.Float: SetFloat(v, c, f(0)); break;
                    case VertexComponentType.Float2: SetVector2(v, c, new Vector2(f(0), f(1))); break;
                    case VertexComponentType.Float3: SetVector3(v, c, new Vector3(f(0), f(1), f(2))); break;
                    case VertexComponentType.Float4: SetVector4(v, c, new Vector4(f(0), f(1), f(2), f(3))); break;
                    case VertexComponentType.RGBA8SNorm:
                        SetRGBA8SNorm(v, c, new Vector4(f(0), f(1), f(2), f(3))); break;
                    case VertexComponentType.Half2: SetHalf2(v, c, new Half2(f(0), f(1))); break;
                    case VertexComponentType.Half4: SetHalf4(v, c, new Half4(f(0), f(1), f(2), f(3))); break;
                    case VertexComponentType.Colour: SetColour(v, c, new Color(b(0), b(1), b(2), b(3))); break;
                    case VertexComponentType.UByte4: SetUByte4(v, c, new Color(b(0), b(1), b(2), b(3))); break;
                }

                sind += cc;
            }
        }

        public void SetFloat(int v, int c, float val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(float)
                if (e <= VertexBytes.Length)
                {
                    byte[] b = BitConverter.GetBytes(val);
                    Buffer.BlockCopy(b, 0, VertexBytes, o, 4);
                }
            }
        }

        public void SetVector2(int v, int c, Vector2 val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 8; //sizeof(Vector2)
                if (e <= VertexBytes.Length)
                {
                    byte[] x = BitConverter.GetBytes(val.X);
                    byte[] y = BitConverter.GetBytes(val.Y);
                    Buffer.BlockCopy(x, 0, VertexBytes, o + 0, 4);
                    Buffer.BlockCopy(y, 0, VertexBytes, o + 4, 4);
                }
            }
        }

        public void SetVector3(int v, int c, Vector3 val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 12; //sizeof(Vector3)
                if (e <= VertexBytes.Length)
                {
                    byte[] x = BitConverter.GetBytes(val.X);
                    byte[] y = BitConverter.GetBytes(val.Y);
                    byte[] z = BitConverter.GetBytes(val.Z);
                    Buffer.BlockCopy(x, 0, VertexBytes, o + 0, 4);
                    Buffer.BlockCopy(y, 0, VertexBytes, o + 4, 4);
                    Buffer.BlockCopy(z, 0, VertexBytes, o + 8, 4);
                }
            }
        }

        public void SetVector4(int v, int c, Vector4 val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 16; //sizeof(Vector4)
                if (e <= VertexBytes.Length)
                {
                    byte[] x = BitConverter.GetBytes(val.X);
                    byte[] y = BitConverter.GetBytes(val.Y);
                    byte[] z = BitConverter.GetBytes(val.Z);
                    byte[] w = BitConverter.GetBytes(val.W);
                    Buffer.BlockCopy(x, 0, VertexBytes, o + 0, 4);
                    Buffer.BlockCopy(y, 0, VertexBytes, o + 4, 4);
                    Buffer.BlockCopy(z, 0, VertexBytes, o + 8, 4);
                    Buffer.BlockCopy(w, 0, VertexBytes, o + 12, 4);
                }
            }
        }

        public void SetRGBA8SNorm(int v, int c, Vector4 val)
        {
            // Equivalent to DXGI_FORMAT_R8G8B8A8_SNORM
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(RGBA8SNorm)
                if (e <= VertexBytes.Length)
                {
                    byte x = (byte)Math.Max(-127.0f, Math.Min(val.X * 127.0f, 127.0f));
                    byte y = (byte)Math.Max(-127.0f, Math.Min(val.Y * 127.0f, 127.0f));
                    byte z = (byte)Math.Max(-127.0f, Math.Min(val.Z * 127.0f, 127.0f));
                    byte w = (byte)Math.Max(-127.0f, Math.Min(val.W * 127.0f, 127.0f));
                    int u = x | (y << 8) | (z << 16) | (w << 24);
                    byte[] b = BitConverter.GetBytes(u);
                    Buffer.BlockCopy(b, 0, VertexBytes, o, 4);
                }
            }
        }

        public void SetHalf2(int v, int c, Half2 val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(Half2)
                if (e <= VertexBytes.Length)
                {
                    byte[] x = BitConverter.GetBytes(val.X.RawValue);
                    byte[] y = BitConverter.GetBytes(val.Y.RawValue);
                    Buffer.BlockCopy(x, 0, VertexBytes, o + 0, 2);
                    Buffer.BlockCopy(y, 0, VertexBytes, o + 2, 2);
                }
            }
        }

        public void SetHalf4(int v, int c, Half4 val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 8; //sizeof(Half4)
                if (e <= VertexBytes.Length)
                {
                    byte[] x = BitConverter.GetBytes(val.X.RawValue);
                    byte[] y = BitConverter.GetBytes(val.Y.RawValue);
                    byte[] z = BitConverter.GetBytes(val.Z.RawValue);
                    byte[] w = BitConverter.GetBytes(val.W.RawValue);
                    Buffer.BlockCopy(x, 0, VertexBytes, o + 0, 2);
                    Buffer.BlockCopy(y, 0, VertexBytes, o + 2, 2);
                    Buffer.BlockCopy(z, 0, VertexBytes, o + 4, 2);
                    Buffer.BlockCopy(w, 0, VertexBytes, o + 6, 2);
                }
            }
        }

        public void SetColour(int v, int c, Color val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(Color)
                if (e <= VertexBytes.Length)
                {
                    int u = val.ToRgba();
                    byte[] b = BitConverter.GetBytes(u);
                    Buffer.BlockCopy(b, 0, VertexBytes, o, 4);
                }
            }
        }

        public void SetUByte4(int v, int c, Color val)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(UByte4)
                if (e <= VertexBytes.Length)
                {
                    int u = val.ToRgba();
                    byte[] b = BitConverter.GetBytes(u);
                    Buffer.BlockCopy(b, 0, VertexBytes, o, 4);
                }
            }
        }

        public string GetString(int v, int c, string d = ", ")
        {
            if (Info != null && VertexBytes != null)
            {
                VertexComponentType ct = Info.GetComponentType(c);
                switch (ct)
                {
                    case VertexComponentType.Float: return FloatUtil.ToString(GetFloat(v, c));
                    case VertexComponentType.Float2: return FloatUtil.GetVector2String(GetVector2(v, c), d);
                    case VertexComponentType.Float3: return FloatUtil.GetVector3String(GetVector3(v, c), d);
                    case VertexComponentType.Float4: return FloatUtil.GetVector4String(GetVector4(v, c), d);
                    case VertexComponentType.RGBA8SNorm: return FloatUtil.GetVector4String(GetRGBA8SNorm(v, c), d);
                    case VertexComponentType.Half2: return FloatUtil.GetHalf2String(GetHalf2(v, c), d);
                    case VertexComponentType.Half4: return FloatUtil.GetHalf4String(GetHalf4(v, c), d);
                    case VertexComponentType.Colour: return FloatUtil.GetColourString(GetColour(v, c), d);
                    case VertexComponentType.UByte4: return FloatUtil.GetColourString(GetUByte4(v, c), d);
                }
            }

            return string.Empty;
        }

        public float GetFloat(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(float)
                if (e <= VertexBytes.Length)
                {
                    float f = BitConverter.ToSingle(VertexBytes, o);
                    return f;
                }
            }

            return 0;
        }

        public Vector2 GetVector2(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 8; //sizeof(Vector2)
                if (e <= VertexBytes.Length)
                {
                    float x = BitConverter.ToSingle(VertexBytes, o + 0);
                    float y = BitConverter.ToSingle(VertexBytes, o + 4);
                    return new Vector2(x, y);
                }
            }

            return Vector2.Zero;
        }

        public Vector3 GetVector3(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 12; //sizeof(Vector3)
                if (e <= VertexBytes.Length)
                {
                    float x = BitConverter.ToSingle(VertexBytes, o + 0);
                    float y = BitConverter.ToSingle(VertexBytes, o + 4);
                    float z = BitConverter.ToSingle(VertexBytes, o + 8);
                    return new Vector3(x, y, z);
                }
            }

            return Vector3.Zero;
        }

        public Vector4 GetVector4(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 16; //sizeof(Vector4)
                if (e <= VertexBytes.Length)
                {
                    float x = BitConverter.ToSingle(VertexBytes, o + 0);
                    float y = BitConverter.ToSingle(VertexBytes, o + 4);
                    float z = BitConverter.ToSingle(VertexBytes, o + 8);
                    float w = BitConverter.ToSingle(VertexBytes, o + 12);
                    return new Vector4(x, y, z, w);
                }
            }

            return Vector4.Zero;
        }

        public Vector4 GetRGBA8SNorm(int v, int c)
        {
            // Equivalent to DXGI_FORMAT_R8G8B8A8_SNORM
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(RGBA8SNorm)
                if (e <= VertexBytes.Length)
                {
                    uint xyzw = BitConverter.ToUInt32(VertexBytes, o);
                    float x = (sbyte)(xyzw & 0xFF) / 127.0f;
                    float y = (sbyte)((xyzw >> 8) & 0xFF) / 127.0f;
                    float z = (sbyte)((xyzw >> 16) & 0xFF) / 127.0f;
                    float w = (sbyte)((xyzw >> 24) & 0xFF) / 127.0f;
                    return new Vector4(x, y, z, w);
                }
            }

            return Vector4.Zero;
        }

        public Half2 GetHalf2(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(Half2)
                if (e <= VertexBytes.Length)
                {
                    ushort x = BitConverter.ToUInt16(VertexBytes, o + 0);
                    ushort y = BitConverter.ToUInt16(VertexBytes, o + 2);
                    return new Half2(x, y);
                }
            }

            return new Half2(0, 0);
        }

        public Half4 GetHalf4(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 8; //sizeof(Half4)
                if (e <= VertexBytes.Length)
                {
                    ushort x = BitConverter.ToUInt16(VertexBytes, o + 0);
                    ushort y = BitConverter.ToUInt16(VertexBytes, o + 2);
                    ushort z = BitConverter.ToUInt16(VertexBytes, o + 4);
                    ushort w = BitConverter.ToUInt16(VertexBytes, o + 6);
                    return new Half4(x, y, z, w);
                }
            }

            return new Half4(0, 0, 0, 0);
        }

        public Color GetColour(int v, int c)
        {
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(Color)
                if (e <= VertexBytes.Length)
                {
                    uint rgba = BitConverter.ToUInt32(VertexBytes, o);
                    return new Color(rgba);
                }
            }

            return Color.Black;
        }

        public Color GetUByte4(int v, int c)
        {
            //Color is the same as UByte4 really
            if (Info != null && VertexBytes != null)
            {
                ushort s = Info.Stride;
                int co = Info.GetComponentOffset(c);
                int o = v * s + co;
                int e = o + 4; //sizeof(UByte4)
                if (e <= VertexBytes.Length)
                {
                    uint rgba = BitConverter.ToUInt32(VertexBytes, o);
                    return new Color(rgba);
                }
            }

            return new Color(0, 0, 0, 0);
        }


        public override string ToString()
        {
            return "Type: " + VertexType + ", Count: " + VertexCount;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VertexDeclaration : ResourceSystemBlock
    {
        public override long BlockLength => 16;

        // structure data
        public uint Flags { get; set; }
        public ushort Stride { get; set; }
        public byte Unknown_6h { get; set; } //0
        public byte Count { get; set; }
        public VertexDeclarationTypes Types { get; set; }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            Flags = reader.ReadUInt32();
            Stride = reader.ReadUInt16();
            Unknown_6h = reader.ReadByte();
            Count = reader.ReadByte();
            Types = (VertexDeclarationTypes)reader.ReadUInt64();

            ////just testing!
            //UpdateCountAndStride();
            //if (Unknown_6h != 0)
            //{ }//no hit
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // write structure data
            writer.Write(Flags);
            writer.Write(Stride);
            writer.Write(Unknown_6h);
            writer.Write(Count);
            writer.Write((ulong)Types);
        }

        public void WriteXml(StringBuilder sb, int indent, string name)
        {
            YdrXml.OpenTag(sb, indent, name + " type=\"" + Types + "\"");

            for (int k = 0; k < 16; k++)
                if (((Flags >> k) & 0x1) == 1)
                {
                    VertexSemantics componentSemantic = (VertexSemantics)k;
                    string tag = componentSemantic.ToString();
                    YdrXml.SelfClosingTag(sb, indent + 1, tag);
                }

            YdrXml.CloseTag(sb, indent, name);
        }

        public void ReadXml(XmlNode node)
        {
            if (node == null) return;

            Types = Xml.GetEnumValue<VertexDeclarationTypes>(Xml.GetStringAttribute(node, "type"));

            uint f = 0;
            foreach (XmlNode cnode in node.ChildNodes)
                if (cnode is XmlElement celem)
                {
                    VertexSemantics componentSematic = Xml.GetEnumValue<VertexSemantics>(celem.Name);
                    int idx = (int)componentSematic;
                    f = f | (1u << idx);
                }

            Flags = f;

            UpdateCountAndStride();
        }

        public ulong GetDeclarationId()
        {
            ulong res = 0;
            for (int i = 0; i < 16; i++)
                if (((Flags >> i) & 1) == 1)
                    res += (ulong)Types & (0xFu << (i * 4));

            return res;
        }

        public VertexComponentType GetComponentType(int index)
        {
            //index is the flags bit index
            return (VertexComponentType)(((ulong)Types >> (index * 4)) & 0x0000000F);
        }

        public int GetComponentOffset(int index)
        {
            //index is the flags bit index
            int offset = 0;
            for (int k = 0; k < index; k++)
                if (((Flags >> k) & 0x1) == 1)
                {
                    VertexComponentType componentType = GetComponentType(k);
                    offset += VertexComponentTypes.GetSizeInBytes(componentType);
                }

            return offset;
        }

        public void UpdateCountAndStride()
        {
            int cnt = 0;
            int str = 0;
            for (int k = 0; k < 16; k++)
                if (((Flags >> k) & 0x1) == 1)
                {
                    VertexComponentType componentType = GetComponentType(k);
                    str += VertexComponentTypes.GetSizeInBytes(componentType);
                    cnt++;
                }

            ////just testing
            //if (Count != cnt)
            //{ }//no hit
            //if (Stride != str)
            //{ }//no hit

            Count = (byte)cnt;
            Stride = (ushort)str;
        }

        public override string ToString()
        {
            return Stride + ": " + Count + ": " + Flags + ": " + Types;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VertexDeclarationG9 : ResourceSystemBlock
    {
        public override long BlockLength => 320; //316;
        public uint[] Offsets { get; set; } //[52]
        public byte[] Sizes { get; set; } //[52]
        public byte[] Types { get; set; } //[52] //(VertexDeclarationG9ElementFormat)
        public ulong Data { get; set; }

        public bool HasSOA => (Data & 1) > 0; //seems to always be false for GTAV gen9  (but true for RDR2)

        public bool Flag => ((Data >> 1) & 1) > 0; //seems to always be false

        public byte VertexSize
        {
            get => (byte)((Data >> 2) & 0xFF);
            set => Data = (Data & 0xFFFFFC03) + ((value & 0xFFu) << 2);
        }

        public uint VertexCount
        {
            get => (uint)((Data >> 10) & 0x3FFFFF);
            set => Data = (Data & 0x3FF) + ((value & 0x3FFFFF) << 10);
        }

        public uint ElementCount
        {
            get
            {
                if (Types == null) return 0;
                uint n = 0u;
                foreach (byte t in Types)
                    if (t != 0)
                        n++;
                return n;
            }
        }

        public VertexDeclarationG9ElementFormat[] G9Formats
        {
            get
            {
                if (Types == null) return null;
                uint n = ElementCount;
                VertexDeclarationG9ElementFormat[] a = new VertexDeclarationG9ElementFormat[n];
                int c = 0;
                foreach (byte t in Types)
                {
                    if (t == 0) continue;
                    a[c] = (VertexDeclarationG9ElementFormat)t;
                    c++;
                }

                return a;
            }
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            Offsets = reader.ReadStructs<uint>(52);
            Sizes = reader.ReadBytes(52);
            Types = reader.ReadBytes(52);
            Data = reader.ReadUInt64();


            //if (Types != null)
            //{
            //    foreach (var t in Types)
            //    {
            //        if (t == 0) continue;
            //        var f = (VertexDeclarationG9ElementFormat)t;
            //        switch (f)
            //        {
            //            case VertexDeclarationG9ElementFormat.R32G32B32_FLOAT:
            //            case VertexDeclarationG9ElementFormat.R32G32B32A32_FLOAT:
            //            case VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM:
            //            case VertexDeclarationG9ElementFormat.R32G32_TYPELESS:
            //            case VertexDeclarationG9ElementFormat.R8G8B8A8_UINT:
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //}
            //if (HasSOA == true)
            //{ }
            //if (Flag == true)
            //{ }
            //if ((Data >> 32) != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            writer.WriteStructs(Offsets);
            writer.Write(Sizes);
            writer.Write(Types);
            writer.Write(Data);
        }


        public VertexDeclaration GetLegacyDeclaration(VertexDeclarationTypes vdtypes = VertexDeclarationTypes.GTAV1)
        {
            uint vdflags = 0u;
            byte[] g9types = Types;
            byte[] g9sizes = Sizes; //these seem to just contain the vertex stride - not sizes but offsets to next item
            uint[] g9offs = Offsets;
            uint g9cnt = ElementCount;
            for (int i = 0; i < g9types.Length; i++) //52
            {
                byte t = g9types[i];
                if (t == 0) continue;
                int lci = GetLegacyComponentIndex(i, vdtypes);
                if (lci < 0)
                    //this component type won't work for the given type...
                    //TODO: try a different type! eg GTAV4
                    continue;
                vdflags = BitUtil.SetBit(vdflags, lci);
            }

            VertexType vtype = (VertexType)vdflags;
            switch (vtype) //just testing converted flags
            {
                case VertexType.Default:
                case VertexType.DefaultEx:
                case VertexType.PCTT:
                case VertexType.PNCCT:
                case VertexType.PNCCTTTX:
                case VertexType.PNCTTX:
                case VertexType.PNCCTT:
                case VertexType.PNCTTTX:
                case VertexType.PNCCTTX_2:
                case VertexType.PNCCTX:
                case VertexType.PNCCTTX:
                case VertexType.PBBNCTX:
                case VertexType.PBBNCT:
                case VertexType.PBBNCCTX:
                case VertexType.PBBCCT:
                case VertexType.PBBNCTTX:
                case VertexType.PNC:
                case VertexType.PCT:
                case VertexType.PNCTTTX_2:
                case VertexType.PNCTTTTX:
                case VertexType.PBBNCCT:
                case VertexType.PT:
                case VertexType.PNCCTTTT:
                case VertexType.PNCTTTX_3:
                case VertexType.PCC:
                case (VertexType)113: //PCCT: decal_diff_only_um, ch_chint02_floor_mural_01.ydr
                case (VertexType)1: //P: farlods.ydd
                case VertexType.PTT:
                case VertexType.PC:
                case VertexType.PBBNCCTTX:
                case VertexType.PBBNCCTT:
                case VertexType.PBBNCTT:
                case VertexType.PBBNCTTT:
                case VertexType.PNCTT:
                case VertexType.PNCTTT:
                case VertexType.PBBNCTTTX:
                    break;
            }

            VertexDeclaration vd = new VertexDeclaration();
            vd.Types = vdtypes;
            vd.Flags = vdflags;
            vd.UpdateCountAndStride();
            if (vd.Count != g9cnt)
            {
            } //just testing converted component count actually matches

            if (vd.Stride != VertexSize)
            {
            } //just testing converted stride actually matches

            return vd;
        }

        public static VertexDeclarationG9 FromLegacyDeclaration(VertexDeclaration vd)
        {
            byte vstride = (byte)vd.Stride;
            VertexDeclarationTypes vdtypes = vd.Types; // VertexDeclarationTypes.GTAV1;
            uint vdflags = vd.Flags;
            uint[] g9offs = new uint[52];
            byte[]
                g9sizes = new byte[52]; //these seem to just contain the vertex stride - not sizes but offsets to next item
            byte[] g9types = new byte[52];

            if (vdtypes != VertexDeclarationTypes.GTAV1)
            {
                //there might be some issues with converting other types
                //for example, if a component doesn't have a (known) equivalent gen9 type
            }

            uint offset = 0u;
            for (int i = 0; i < 52; i++)
            {
                g9offs[i] = offset;
                int lci = GetLegacyComponentIndex(i, vdtypes);
                if (lci < 0) continue; //can't be used, unavailable for GTAV1
                if ((vdflags & (1u << lci)) == 0) continue;
                VertexComponentType ctype = (VertexComponentType)(((ulong)vdtypes >> (lci * 4)) & 0xF);
                int csize = VertexComponentTypes.GetSizeInBytes(ctype);
                offset += (uint)csize;
                g9sizes[i] = vstride;
                g9types[i] = (byte)GetGen9ComponentType(lci, vdtypes);
            }

            VertexDeclarationG9 info = new VertexDeclarationG9();
            info.Offsets = g9offs;
            info.Sizes = g9sizes;
            info.Types = g9types;
            info.VertexSize = vstride;
            info.VertexCount = 0; //it's actually supposed to be 0

            return info;
        }


        public static VertexComponentType GetLegacyComponentType(VertexDeclarationG9ElementFormat f)
        {
            switch (f)
            {
                case VertexDeclarationG9ElementFormat.R32G32B32_FLOAT: return VertexComponentType.Float3;
                case VertexDeclarationG9ElementFormat.R32G32B32A32_FLOAT: return VertexComponentType.Float4;
                case VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM: return VertexComponentType.Colour;
                case VertexDeclarationG9ElementFormat.R32G32_TYPELESS: return VertexComponentType.Float2;
                case VertexDeclarationG9ElementFormat.R8G8B8A8_UINT: return VertexComponentType.Colour; //for bone inds
                default: return VertexComponentType.Float4;
            }
        }

        public static int GetLegacyComponentIndex(int i, VertexDeclarationTypes vdtypes)
        {
            if (vdtypes != VertexDeclarationTypes.GTAV1)
            {
            } //TODO: is this ok? are component indices (semantics?) always the same?

            //GTAV1 = 0x7755555555996996, // GTAV - used by most drawables
            switch (i)
            {
                case 0: return 0; //POSITION0
                case 4: return 3; //NORMAL0
                case 8: return 14; //TANGENT0
                case 16: return 1; //BLENDWEIGHTS0
                case 20: return 2; //BLENDINDICES0
                case 24: return 4; //COLOR0
                case 25: return 5; //COLOR1
                case 28: return 6; //TEXCOORD0
                case 29: return 7; //TEXCOORD1
                case 30: return 8; //TEXCOORD2
                case 31: return 9; //TEXCOORD3
                case 32: return 10; //TEXCOORD4
                case 33: return 11; //TEXCOORD5
                default: return -1;
            }
            /*
            private static string[] RageSemanticNames =
            {
                00"POSITION",
                01"POSITION1",
                02"POSITION2",
                03"POSITION3",
                04"NORMAL",
                05"NORMAL1",
                06"NORMAL2",
                07"NORMAL3",
                08"TANGENT",
                09"TANGENT1",
                10"TANGENT2",
                11"TANGENT3",
                12"BINORMAL",
                13"BINORMAL1",
                14"BINORMAL2",
                15"BINORMAL3",
                16"BLENDWEIGHT",
                17"BLENDWEIGHT1",
                18"BLENDWEIGHT2",
                19"BLENDWEIGHT3",
                20"BLENDINDICIES",
                21"BLENDINDICIES1",
                22"BLENDINDICIES2",
                23"BLENDINDICIES3",
                24"COLOR0",
                25"COLOR1",
                26"COLOR2",
                27"COLOR3",
                28"TEXCOORD0",
                29"TEXCOORD1",
                30"TEXCOORD2",
                31"TEXCOORD3",
                32"TEXCOORD4",
                33"TEXCOORD5",
                34"TEXCOORD6",
                35"TEXCOORD7",
                36"TEXCOORD8",
                37"TEXCOORD9",
                38"TEXCOORD10",
                39"TEXCOORD11",
                40"TEXCOORD12",
                41"TEXCOORD13",
                42"TEXCOORD14",
                43"TEXCOORD15",
                44"TEXCOORD16",
                45"TEXCOORD17",
                46"TEXCOORD18",
                47"TEXCOORD19",
                48"TEXCOORD20",
                49"TEXCOORD21",
                50"TEXCOORD22",
                51"TEXCOORD23",
            };
             */
        }

        public static VertexDeclarationG9ElementFormat GetGen9ComponentTypeGTAV1(int lci)
        {
            //(lci=legacy component index)
            //GTAV1 = 0x7755555555996996, // GTAV - used by most drawables
            switch (lci)
            {
                case 0: return VertexDeclarationG9ElementFormat.R32G32B32_FLOAT;
                case 1: return VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM;
                case 2: return VertexDeclarationG9ElementFormat.R8G8B8A8_UINT;
                case 3: return VertexDeclarationG9ElementFormat.R32G32B32_FLOAT;
                case 4:
                case 5: return VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM;
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13: return VertexDeclarationG9ElementFormat.R32G32_TYPELESS;
                case 14:
                case 15: return VertexDeclarationG9ElementFormat.R32G32B32A32_FLOAT;
                default: return VertexDeclarationG9ElementFormat.R32G32B32A32_FLOAT;
            }
        }

        public static VertexDeclarationG9ElementFormat GetGen9ComponentType(int lci, VertexDeclarationTypes vdtypes)
        {
            if (vdtypes == VertexDeclarationTypes.GTAV1) return GetGen9ComponentTypeGTAV1(lci);

            switch (lci)
            {
                case 1: return VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM; //boneweights
                case 2: return VertexDeclarationG9ElementFormat.R8G8B8A8_UINT; //boneinds
            }

            VertexComponentType t = (VertexComponentType)(((ulong)vdtypes >> (lci * 4)) & 0xF);
            switch (t)
            {
                case VertexComponentType.Half2: return VertexDeclarationG9ElementFormat.R16G16_FLOAT;
                case VertexComponentType.Float: return VertexDeclarationG9ElementFormat.NONE;
                case VertexComponentType.Half4: return VertexDeclarationG9ElementFormat.R16G16B16A16_FLOAT;
                case VertexComponentType.FloatUnk: return VertexDeclarationG9ElementFormat.NONE;
                case VertexComponentType.Float2: return VertexDeclarationG9ElementFormat.R32G32_TYPELESS;
                case VertexComponentType.Float3: return VertexDeclarationG9ElementFormat.R32G32B32_FLOAT;
                case VertexComponentType.Float4: return VertexDeclarationG9ElementFormat.R32G32B32A32_FLOAT;
                case VertexComponentType.UByte4: return VertexDeclarationG9ElementFormat.R8G8B8A8_UINT;
                case VertexComponentType.Colour: return VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM;
                case VertexComponentType.RGBA8SNorm: return VertexDeclarationG9ElementFormat.R8G8B8A8_UNORM; //close?!
                default: return VertexDeclarationG9ElementFormat.NONE;
            }
        }
    }

    public enum VertexDeclarationG9ElementFormat : byte
    {
        NONE = 0,
        R32G32B32A32_FLOAT = 2,
        R32G32B32_FLOAT = 6,
        R16G16B16A16_FLOAT = 10,
        R32G32_TYPELESS = 16,
        D3DX_R10G10B10A2 = 24,
        R8G8B8A8_UNORM = 28,
        R8G8B8A8_UINT = 30,
        R16G16_FLOAT = 34
    }


    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class IndexBuffer : ResourceSystemBlock
    {
        public uint G9_Unknown_14h;
        public ushort G9_Unknown_Eh;


        private ResourceSystemStructBlock<ushort> IndicesBlock; //only used when saving
        public ulong Unknown_18h; // 0x0000000000000000
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ulong Unknown_30h; // 0x0000000000000000
        public ulong Unknown_38h; // 0x0000000000000000
        public ulong Unknown_40h; // 0x0000000000000000
        public ulong Unknown_48h; // 0x0000000000000000
        public uint Unknown_4h = 1; // 0x00000001
        public ulong Unknown_50h; // 0x0000000000000000
        public ulong Unknown_58h; // 0x0000000000000000
        public uint Unknown_Ch; // 0x00000000
        public override long BlockLength => 96;
        public override long BlockLength_Gen9 => 64;

        // structure data
        public uint VFT { get; set; } = 1080152408;
        public uint IndicesCount { get; set; }
        public ulong IndicesPointer { get; set; }

        // gen9 structure data
        public ushort G9_IndexSize { get; set; } = 2; // m_indexSize  //TODO: do we need to support 32bit indices?
        public uint G9_BindFlags { get; set; } // m_bindFlags
        public ulong G9_SRVPointer { get; set; }
        public ShaderResourceViewG9 G9_SRV { get; set; }


        // reference data
        //public ResourceSimpleArray<ushort_r> Indices;
        public ushort[] Indices { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            IndicesCount = reader.ReadUInt32();

            if (reader.IsGen9)
            {
                G9_IndexSize = reader.ReadUInt16();
                G9_Unknown_Eh = reader.ReadUInt16();
                G9_BindFlags = reader.ReadUInt32();
                G9_Unknown_14h = reader.ReadUInt32();
                IndicesPointer = reader.ReadUInt64();
                Unknown_20h = reader.ReadUInt64();
                Unknown_28h = reader.ReadUInt64();
                G9_SRVPointer = reader.ReadUInt64();
                Unknown_38h = reader.ReadUInt64();

                Indices = reader.ReadUshortsAt(IndicesPointer, IndicesCount);
                G9_SRV = reader.ReadBlockAt<ShaderResourceViewG9>(G9_SRVPointer);

                if (G9_IndexSize != 2)
                {
                }

                if (G9_Unknown_Eh != 0)
                {
                }

                switch (G9_BindFlags)
                {
                    case 0x0058020a:
                        break;
                }

                if (G9_Unknown_14h != 0)
                {
                }

                if (Unknown_20h != 0)
                {
                }

                if (Unknown_28h != 0)
                {
                }

                if (Unknown_38h != 0)
                {
                }
            }
            else
            {
                Unknown_Ch = reader.ReadUInt32();
                IndicesPointer = reader.ReadUInt64();
                Unknown_18h = reader.ReadUInt64();
                Unknown_20h = reader.ReadUInt64();
                Unknown_28h = reader.ReadUInt64();
                Unknown_30h = reader.ReadUInt64();
                Unknown_38h = reader.ReadUInt64();
                Unknown_40h = reader.ReadUInt64();
                Unknown_48h = reader.ReadUInt64();
                Unknown_50h = reader.ReadUInt64();
                Unknown_58h = reader.ReadUInt64();

                // read reference data
                //this.Indices = reader.ReadBlockAt<ResourceSimpleArray<ushort_r>>(
                //    this.IndicesPointer, // offset
                //    this.IndicesCount
                //);
                Indices = reader.ReadUshortsAt(IndicesPointer, IndicesCount);


                //if (Unknown_4h != 1)
                //{ }
                //if (Unknown_Ch != 0)
                //{ }
                //if (Unknown_18h != 0)
                //{ }
                //if (Unknown_20h != 0)
                //{ }
                //if (Unknown_28h != 0)
                //{ }
                //if (Unknown_30h != 0)
                //{ }
                //if (Unknown_38h != 0)
                //{ }
                //if (Unknown_40h != 0)
                //{ }
                //if (Unknown_48h != 0)
                //{ }
                //if (Unknown_50h != 0)
                //{ }
                //if (Unknown_58h != 0)
                //{ }
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update structure data
            IndicesCount = (uint)(IndicesBlock != null ? IndicesBlock.ItemCount : 0);
            IndicesPointer = (ulong)(IndicesBlock != null ? IndicesBlock.FilePosition : 0);

            // write structure data
            writer.Write(VFT);
            writer.Write(Unknown_4h);
            writer.Write(IndicesCount);

            if (writer.IsGen9)
            {
                G9_SRVPointer = (ulong)(G9_SRV != null ? G9_SRV.FilePosition : 0);

                if (G9_BindFlags == 0) G9_BindFlags = 0x0058020a;

                writer.Write(G9_IndexSize);
                writer.Write(G9_Unknown_Eh);
                writer.Write(G9_BindFlags);
                writer.Write(G9_Unknown_14h);
                writer.Write(IndicesPointer);
                writer.Write(Unknown_20h);
                writer.Write(Unknown_28h);
                writer.Write(G9_SRVPointer);
                writer.Write(Unknown_38h);
            }
            else
            {
                writer.Write(Unknown_Ch);
                writer.Write(IndicesPointer);
                writer.Write(Unknown_18h);
                writer.Write(Unknown_20h);
                writer.Write(Unknown_28h);
                writer.Write(Unknown_30h);
                writer.Write(Unknown_38h);
                writer.Write(Unknown_40h);
                writer.Write(Unknown_48h);
                writer.Write(Unknown_50h);
                writer.Write(Unknown_58h);
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            if (Indices != null) YdrXml.WriteRawArray(sb, Indices, indent, "Data", "", null, 24);
        }

        public void ReadXml(XmlNode node)
        {
            XmlNode inode = node.SelectSingleNode("Data");
            if (inode != null)
            {
                Indices = Xml.GetRawUshortArray(node);
                IndicesCount = (uint)(Indices?.Length ?? 0);
            }
        }


        public void EnsureGen9()
        {
            VFT = 1080152408;
            Unknown_4h = 1;

            if (G9_SRV == null)
            {
                G9_SRV = new ShaderResourceViewG9();
                G9_SRV.Dimension = ShaderResourceViewDimensionG9.Buffer;
            }
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>();
            if (Indices != null)
            {
                IndicesBlock = new ResourceSystemStructBlock<ushort>(Indices);
                list.Add(IndicesBlock);
            }

            if (G9_SRV != null) list.Add(G9_SRV);
            return list.ToArray();
        }
    }


    public enum LightType : byte
    {
        Point = 1,
        Spot = 2,
        Capsule = 4
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class LightAttributes : ResourceSystemBlock, IMetaXmlItem
    {
        public bool UpdateRenderable = false; //used by model light form
        public override long BlockLength => 168;

        // structure data
        public uint Unknown_0h { get; set; } // 0x00000000
        public uint Unknown_4h { get; set; } // 0x00000000
        public Vector3 Position { get; set; }
        public uint Unknown_14h { get; set; } // 0x00000000
        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }
        public byte Flashiness { get; set; }
        public float Intensity { get; set; }
        public uint Flags { get; set; }
        public ushort BoneId { get; set; }
        public LightType Type { get; set; }
        public byte GroupId { get; set; }
        public uint TimeFlags { get; set; }
        public float Falloff { get; set; }
        public float FalloffExponent { get; set; }
        public Vector3 CullingPlaneNormal { get; set; }
        public float CullingPlaneOffset { get; set; }
        public byte ShadowBlur { get; set; }
        public byte Unknown_45h { get; set; }
        public ushort Unknown_46h { get; set; }
        public uint Unknown_48h { get; set; } // 0x00000000
        public float VolumeIntensity { get; set; }
        public float VolumeSizeScale { get; set; }
        public byte VolumeOuterColorR { get; set; }
        public byte VolumeOuterColorG { get; set; }
        public byte VolumeOuterColorB { get; set; }
        public byte LightHash { get; set; }
        public float VolumeOuterIntensity { get; set; }
        public float CoronaSize { get; set; }
        public float VolumeOuterExponent { get; set; }
        public byte LightFadeDistance { get; set; }
        public byte ShadowFadeDistance { get; set; }
        public byte SpecularFadeDistance { get; set; }
        public byte VolumetricFadeDistance { get; set; }
        public float ShadowNearClip { get; set; }
        public float CoronaIntensity { get; set; }
        public float CoronaZBias { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Tangent { get; set; }
        public float ConeInnerAngle { get; set; }
        public float ConeOuterAngle { get; set; }
        public Vector3 Extent { get; set; }
        public MetaHash ProjectedTextureHash { get; set; }
        public uint Unknown_A4h { get; set; } // 0x00000000


        public Quaternion Orientation
        {
            get
            {
                Vector3 tx = new Vector3();
                Vector3 ty = new Vector3();

                switch (Type)
                {
                    case LightType.Point:
                        return Quaternion.Identity;
                    case LightType.Spot:
                    case LightType.Capsule:
                        tx = Vector3.Normalize(Tangent);
                        ty = Vector3.Normalize(Vector3.Cross(Direction, Tangent));
                        break;
                }

                Matrix m = new Matrix();
                m.Row1 = new Vector4(tx, 0);
                m.Row2 = new Vector4(ty, 0);
                m.Row3 = new Vector4(Direction, 0);
                return Quaternion.RotationMatrix(m);
            }
            set
            {
                Quaternion inv = Quaternion.Invert(Orientation);
                Quaternion delta = value * inv;
                Direction = Vector3.Normalize(delta.Multiply(Direction));
                Tangent = Vector3.Normalize(delta.Multiply(Tangent));
            }
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            YdrXml.SelfClosingTag(sb, indent, "Position " + FloatUtil.GetVector3XmlString(Position));
            YdrXml.SelfClosingTag(sb, indent,
                string.Format("Colour r=\"{0}\" g=\"{1}\" b=\"{2}\"", ColorR, ColorG, ColorB));
            YdrXml.ValueTag(sb, indent, "Flashiness", Flashiness.ToString());
            YdrXml.ValueTag(sb, indent, "Intensity", FloatUtil.ToString(Intensity));
            YdrXml.ValueTag(sb, indent, "Flags", Flags.ToString());
            YdrXml.ValueTag(sb, indent, "BoneId", BoneId.ToString());
            YdrXml.StringTag(sb, indent, "Type", Type.ToString());
            YdrXml.ValueTag(sb, indent, "GroupId", GroupId.ToString());
            YdrXml.ValueTag(sb, indent, "TimeFlags", TimeFlags.ToString());
            YdrXml.ValueTag(sb, indent, "Falloff", FloatUtil.ToString(Falloff));
            YdrXml.ValueTag(sb, indent, "FalloffExponent", FloatUtil.ToString(FalloffExponent));
            YdrXml.SelfClosingTag(sb, indent,
                "CullingPlaneNormal " + FloatUtil.GetVector3XmlString(CullingPlaneNormal));
            YdrXml.ValueTag(sb, indent, "CullingPlaneOffset", FloatUtil.ToString(CullingPlaneOffset));
            YdrXml.ValueTag(sb, indent, "Unknown45", Unknown_45h.ToString());
            YdrXml.ValueTag(sb, indent, "Unknown46", Unknown_46h.ToString());
            YdrXml.ValueTag(sb, indent, "VolumeIntensity", FloatUtil.ToString(VolumeIntensity));
            YdrXml.ValueTag(sb, indent, "VolumeSizeScale", FloatUtil.ToString(VolumeSizeScale));
            YdrXml.SelfClosingTag(sb, indent,
                string.Format("VolumeOuterColour r=\"{0}\" g=\"{1}\" b=\"{2}\"", VolumeOuterColorR, VolumeOuterColorG,
                    VolumeOuterColorB));
            YdrXml.ValueTag(sb, indent, "LightHash", LightHash.ToString());
            YdrXml.ValueTag(sb, indent, "VolumeOuterIntensity", FloatUtil.ToString(VolumeOuterIntensity));
            YdrXml.ValueTag(sb, indent, "CoronaSize", FloatUtil.ToString(CoronaSize));
            YdrXml.ValueTag(sb, indent, "VolumeOuterExponent", FloatUtil.ToString(VolumeOuterExponent));
            YdrXml.ValueTag(sb, indent, "LightFadeDistance", LightFadeDistance.ToString());
            YdrXml.ValueTag(sb, indent, "ShadowBlur", ShadowBlur.ToString());
            YdrXml.ValueTag(sb, indent, "ShadowFadeDistance", ShadowFadeDistance.ToString());
            YdrXml.ValueTag(sb, indent, "SpecularFadeDistance", SpecularFadeDistance.ToString());
            YdrXml.ValueTag(sb, indent, "VolumetricFadeDistance", VolumetricFadeDistance.ToString());
            YdrXml.ValueTag(sb, indent, "ShadowNearClip", FloatUtil.ToString(ShadowNearClip));
            YdrXml.ValueTag(sb, indent, "CoronaIntensity", FloatUtil.ToString(CoronaIntensity));
            YdrXml.ValueTag(sb, indent, "CoronaZBias", FloatUtil.ToString(CoronaZBias));
            YdrXml.SelfClosingTag(sb, indent, "Direction " + FloatUtil.GetVector3XmlString(Direction));
            YdrXml.SelfClosingTag(sb, indent, "Tangent " + FloatUtil.GetVector3XmlString(Tangent));
            YdrXml.ValueTag(sb, indent, "ConeInnerAngle", FloatUtil.ToString(ConeInnerAngle));
            YdrXml.ValueTag(sb, indent, "ConeOuterAngle", FloatUtil.ToString(ConeOuterAngle));
            YdrXml.SelfClosingTag(sb, indent, "Extent " + FloatUtil.GetVector3XmlString(Extent));
            YdrXml.StringTag(sb, indent, "ProjectedTextureHash", YdrXml.HashString(ProjectedTextureHash));
        }

        public void ReadXml(XmlNode node)
        {
            Position = Xml.GetChildVector3Attributes(node, "Position");
            ColorR = (byte)Xml.GetChildUIntAttribute(node, "Colour", "r");
            ColorG = (byte)Xml.GetChildUIntAttribute(node, "Colour", "g");
            ColorB = (byte)Xml.GetChildUIntAttribute(node, "Colour", "b");
            Flashiness = (byte)Xml.GetChildUIntAttribute(node, "Flashiness");
            Intensity = Xml.GetChildFloatAttribute(node, "Intensity");
            Flags = Xml.GetChildUIntAttribute(node, "Flags");
            BoneId = (ushort)Xml.GetChildUIntAttribute(node, "BoneId");
            Type = Xml.GetChildEnumInnerText<LightType>(node, "Type");
            GroupId = (byte)Xml.GetChildUIntAttribute(node, "GroupId");
            TimeFlags = Xml.GetChildUIntAttribute(node, "TimeFlags");
            Falloff = Xml.GetChildFloatAttribute(node, "Falloff");
            FalloffExponent = Xml.GetChildFloatAttribute(node, "FalloffExponent");
            CullingPlaneNormal = Xml.GetChildVector3Attributes(node, "CullingPlaneNormal");
            CullingPlaneOffset = Xml.GetChildFloatAttribute(node, "CullingPlaneOffset");
            Unknown_45h = (byte)Xml.GetChildUIntAttribute(node, "Unknown45");
            Unknown_46h = (ushort)Xml.GetChildUIntAttribute(node, "Unknown46");
            VolumeIntensity = Xml.GetChildFloatAttribute(node, "VolumeIntensity");
            VolumeSizeScale = Xml.GetChildFloatAttribute(node, "VolumeSizeScale");
            VolumeOuterColorR = (byte)Xml.GetChildUIntAttribute(node, "VolumeOuterColour", "r");
            VolumeOuterColorG = (byte)Xml.GetChildUIntAttribute(node, "VolumeOuterColour", "g");
            VolumeOuterColorB = (byte)Xml.GetChildUIntAttribute(node, "VolumeOuterColour", "b");
            LightHash = (byte)Xml.GetChildUIntAttribute(node, "LightHash");
            VolumeOuterIntensity = Xml.GetChildFloatAttribute(node, "VolumeOuterIntensity");
            CoronaSize = Xml.GetChildFloatAttribute(node, "CoronaSize");
            VolumeOuterExponent = Xml.GetChildFloatAttribute(node, "VolumeOuterExponent");
            LightFadeDistance = (byte)Xml.GetChildUIntAttribute(node, "LightFadeDistance");
            ShadowBlur = (byte)Xml.GetChildUIntAttribute(node, "ShadowBlur");
            ShadowFadeDistance = (byte)Xml.GetChildUIntAttribute(node, "ShadowFadeDistance");
            SpecularFadeDistance = (byte)Xml.GetChildUIntAttribute(node, "SpecularFadeDistance");
            VolumetricFadeDistance = (byte)Xml.GetChildUIntAttribute(node, "VolumetricFadeDistance");
            ShadowNearClip = Xml.GetChildFloatAttribute(node, "ShadowNearClip");
            CoronaIntensity = Xml.GetChildFloatAttribute(node, "CoronaIntensity");
            CoronaZBias = Xml.GetChildFloatAttribute(node, "CoronaZBias");
            Direction = Xml.GetChildVector3Attributes(node, "Direction");
            Tangent = Xml.GetChildVector3Attributes(node, "Tangent");
            ConeInnerAngle = Xml.GetChildFloatAttribute(node, "ConeInnerAngle");
            ConeOuterAngle = Xml.GetChildFloatAttribute(node, "ConeOuterAngle");
            Extent = Xml.GetChildVector3Attributes(node, "Extent");
            ProjectedTextureHash = XmlMeta.GetHash(Xml.GetChildInnerText(node, "ProjectedTextureHash"));
        }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            //read structure data
            Unknown_0h = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Position = reader.ReadVector3();
            Unknown_14h = reader.ReadUInt32();
            ColorR = reader.ReadByte();
            ColorG = reader.ReadByte();
            ColorB = reader.ReadByte();
            Flashiness = reader.ReadByte();
            Intensity = reader.ReadSingle();
            Flags = reader.ReadUInt32();
            BoneId = reader.ReadUInt16();
            Type = (LightType)reader.ReadByte();
            GroupId = reader.ReadByte();
            TimeFlags = reader.ReadUInt32();
            Falloff = reader.ReadSingle();
            FalloffExponent = reader.ReadSingle();
            CullingPlaneNormal = reader.ReadVector3();
            CullingPlaneOffset = reader.ReadSingle();
            ShadowBlur = reader.ReadByte();
            Unknown_45h = reader.ReadByte();
            Unknown_46h = reader.ReadUInt16();
            Unknown_48h = reader.ReadUInt32();
            VolumeIntensity = reader.ReadSingle();
            VolumeSizeScale = reader.ReadSingle();
            VolumeOuterColorR = reader.ReadByte();
            VolumeOuterColorG = reader.ReadByte();
            VolumeOuterColorB = reader.ReadByte();
            LightHash = reader.ReadByte();
            VolumeOuterIntensity = reader.ReadSingle();
            CoronaSize = reader.ReadSingle();
            VolumeOuterExponent = reader.ReadSingle();
            LightFadeDistance = reader.ReadByte();
            ShadowFadeDistance = reader.ReadByte();
            SpecularFadeDistance = reader.ReadByte();
            VolumetricFadeDistance = reader.ReadByte();
            ShadowNearClip = reader.ReadSingle();
            CoronaIntensity = reader.ReadSingle();
            CoronaZBias = reader.ReadSingle();
            Direction = reader.ReadVector3();
            Tangent = reader.ReadVector3();
            ConeInnerAngle = reader.ReadSingle();
            ConeOuterAngle = reader.ReadSingle();
            Extent = reader.ReadVector3();
            ProjectedTextureHash = new MetaHash(reader.ReadUInt32());
            Unknown_A4h = reader.ReadUInt32();
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            //write structure data
            writer.Write(Unknown_0h);
            writer.Write(Unknown_4h);
            writer.Write(Position);
            writer.Write(Unknown_14h);
            writer.Write(ColorR);
            writer.Write(ColorG);
            writer.Write(ColorB);
            writer.Write(Flashiness);
            writer.Write(Intensity);
            writer.Write(Flags);
            writer.Write(BoneId);
            writer.Write((byte)Type);
            writer.Write(GroupId);
            writer.Write(TimeFlags);
            writer.Write(Falloff);
            writer.Write(FalloffExponent);
            writer.Write(CullingPlaneNormal);
            writer.Write(CullingPlaneOffset);
            writer.Write(ShadowBlur);
            writer.Write(Unknown_45h);
            writer.Write(Unknown_46h);
            writer.Write(Unknown_48h);
            writer.Write(VolumeIntensity);
            writer.Write(VolumeSizeScale);
            writer.Write(VolumeOuterColorR);
            writer.Write(VolumeOuterColorG);
            writer.Write(VolumeOuterColorB);
            writer.Write(LightHash);
            writer.Write(VolumeOuterIntensity);
            writer.Write(CoronaSize);
            writer.Write(VolumeOuterExponent);
            writer.Write(LightFadeDistance);
            writer.Write(ShadowFadeDistance);
            writer.Write(SpecularFadeDistance);
            writer.Write(VolumetricFadeDistance);
            writer.Write(ShadowNearClip);
            writer.Write(CoronaIntensity);
            writer.Write(CoronaZBias);
            writer.Write(Direction);
            writer.Write(Tangent);
            writer.Write(ConeInnerAngle);
            writer.Write(ConeOuterAngle);
            writer.Write(Extent);
            writer.Write(ProjectedTextureHash.Hash);
            writer.Write(Unknown_A4h);
        }
    }


    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawableBase : ResourceFileBase
    {
        public override long BlockLength => 168;

        // structure data
        public ulong ShaderGroupPointer { get; set; }
        public ulong SkeletonPointer { get; set; }
        public Vector3 BoundingCenter { get; set; }
        public float BoundingSphereRadius { get; set; }
        public Vector3 BoundingBoxMin { get; set; }
        public uint Unknown_3Ch { get; set; } = 0x7f800001;
        public Vector3 BoundingBoxMax { get; set; }
        public uint Unknown_4Ch { get; set; } = 0x7f800001;
        public ulong DrawableModelsHighPointer { get; set; }
        public ulong DrawableModelsMediumPointer { get; set; }
        public ulong DrawableModelsLowPointer { get; set; }
        public ulong DrawableModelsVeryLowPointer { get; set; }
        public float LodDistHigh { get; set; }
        public float LodDistMed { get; set; }
        public float LodDistLow { get; set; }
        public float LodDistVlow { get; set; }
        public uint RenderMaskFlagsHigh { get; set; }
        public uint RenderMaskFlagsMed { get; set; }
        public uint RenderMaskFlagsLow { get; set; }
        public uint RenderMaskFlagsVlow { get; set; }
        public ulong JointsPointer { get; set; }
        public ushort Unknown_98h { get; set; } // 0x0000
        public ushort DrawableModelsBlocksSize { get; set; } // divided by 16
        public uint Unknown_9Ch { get; set; } // 0x00000000
        public ulong DrawableModelsPointer { get; set; }

        public byte FlagsHigh
        {
            get => (byte)(RenderMaskFlagsHigh & 0xFF);
            set => RenderMaskFlagsHigh = (RenderMaskFlagsHigh & 0xFFFFFF00) + (value & 0xFFu);
        }

        public byte FlagsMed
        {
            get => (byte)(RenderMaskFlagsMed & 0xFF);
            set => RenderMaskFlagsMed = (RenderMaskFlagsMed & 0xFFFFFF00) + (value & 0xFFu);
        }

        public byte FlagsLow
        {
            get => (byte)(RenderMaskFlagsLow & 0xFF);
            set => RenderMaskFlagsLow = (RenderMaskFlagsLow & 0xFFFFFF00) + (value & 0xFFu);
        }

        public byte FlagsVlow
        {
            get => (byte)(RenderMaskFlagsVlow & 0xFF);
            set => RenderMaskFlagsVlow = (RenderMaskFlagsVlow & 0xFFFFFF00) + (value & 0xFFu);
        }

        public byte RenderMaskHigh
        {
            get => (byte)((RenderMaskFlagsHigh >> 8) & 0xFF);
            set => RenderMaskFlagsHigh = (RenderMaskFlagsHigh & 0xFFFF00FF) + ((value & 0xFFu) << 8);
        }

        public byte RenderMaskMed
        {
            get => (byte)((RenderMaskFlagsMed >> 8) & 0xFF);
            set => RenderMaskFlagsMed = (RenderMaskFlagsMed & 0xFFFF00FF) + ((value & 0xFFu) << 8);
        }

        public byte RenderMaskLow
        {
            get => (byte)((RenderMaskFlagsLow >> 8) & 0xFF);
            set => RenderMaskFlagsLow = (RenderMaskFlagsLow & 0xFFFF00FF) + ((value & 0xFFu) << 8);
        }

        public byte RenderMaskVlow
        {
            get => (byte)((RenderMaskFlagsVlow >> 8) & 0xFF);
            set => RenderMaskFlagsVlow = (RenderMaskFlagsVlow & 0xFFFF00FF) + ((value & 0xFFu) << 8);
        }


        // reference data
        public ShaderGroup ShaderGroup { get; set; }
        public Skeleton Skeleton { get; set; }
        public Joints Joints { get; set; }
        public DrawableModelsBlock DrawableModels { get; set; }


        public DrawableModel[] AllModels { get; set; }
        public Dictionary<ulong, VertexDeclaration> VertexDecls { get; set; }

        public object Owner { get; set; }

        public long MemoryUsage
        {
            get
            {
                long val = 0;
                if (AllModels != null)
                    foreach (DrawableModel m in AllModels)
                        if (m != null)
                            val += m.MemoryUsage;

                if (ShaderGroup != null && ShaderGroup.TextureDictionary != null)
                    val += ShaderGroup.TextureDictionary.MemoryUsage;
                return val;
            }
        }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            ShaderGroupPointer = reader.ReadUInt64();
            SkeletonPointer = reader.ReadUInt64();
            BoundingCenter = reader.ReadVector3();
            BoundingSphereRadius = reader.ReadSingle();
            BoundingBoxMin = reader.ReadVector3();
            Unknown_3Ch = reader.ReadUInt32();
            BoundingBoxMax = reader.ReadVector3();
            Unknown_4Ch = reader.ReadUInt32();
            DrawableModelsHighPointer = reader.ReadUInt64();
            DrawableModelsMediumPointer = reader.ReadUInt64();
            DrawableModelsLowPointer = reader.ReadUInt64();
            DrawableModelsVeryLowPointer = reader.ReadUInt64();
            LodDistHigh = reader.ReadSingle();
            LodDistMed = reader.ReadSingle();
            LodDistLow = reader.ReadSingle();
            LodDistVlow = reader.ReadSingle();
            RenderMaskFlagsHigh = reader.ReadUInt32();
            RenderMaskFlagsMed = reader.ReadUInt32();
            RenderMaskFlagsLow = reader.ReadUInt32();
            RenderMaskFlagsVlow = reader.ReadUInt32();
            JointsPointer = reader.ReadUInt64();
            Unknown_98h = reader.ReadUInt16();
            DrawableModelsBlocksSize = reader.ReadUInt16();
            Unknown_9Ch = reader.ReadUInt32();
            DrawableModelsPointer = reader.ReadUInt64();

            // read reference data
            ShaderGroup = reader.ReadBlockAt<ShaderGroup>(ShaderGroupPointer);
            Skeleton = reader.ReadBlockAt<Skeleton>(SkeletonPointer);
            Joints = reader.ReadBlockAt<Joints>(JointsPointer);
            DrawableModels =
                reader.ReadBlockAt<DrawableModelsBlock>(
                    DrawableModelsPointer == 0 ? DrawableModelsHighPointer : DrawableModelsPointer, this);


            BuildAllModels();
            BuildVertexDecls();
            AssignGeometryShaders(ShaderGroup);


            ////just testing!!!

            //long pad(long o) => ((16 - (o % 16)) % 16);
            //long listlength(DrawableModel[] list)
            //{
            //    long l = 16;
            //    l += (list.Length) * 8;
            //    foreach (var m in list) l += pad(l) + m.BlockLength;
            //    return l;
            //}
            //var ptr = (long)DrawableModelsPointer;
            //if (DrawableModels?.High != null)
            //{
            //    if (ptr != (long)DrawableModelsHighPointer)
            //    { }//no hit
            //    ptr += listlength(DrawableModels?.High);
            //}
            //if (DrawableModels?.Med != null)
            //{
            //    ptr += pad(ptr);
            //    if (ptr != (long)DrawableModelsMediumPointer)
            //    { }//no hit
            //    ptr += listlength(DrawableModels?.Med);
            //}
            //if (DrawableModels?.Low != null)
            //{
            //    ptr += pad(ptr);
            //    if (ptr != (long)DrawableModelsLowPointer)
            //    { }//no hit
            //    ptr += listlength(DrawableModels?.Low);
            //}
            //if (DrawableModels?.VLow != null)
            //{
            //    ptr += pad(ptr);
            //    if (ptr != (long)DrawableModelsVeryLowPointer)
            //    { }//no hit
            //    ptr += listlength(DrawableModels?.VLow);
            //}


            //switch (Unknown_3Ch)
            //{
            //    case 0x7f800001:
            //    case 0: //only in yft's!
            //        break;
            //    default:
            //        break;
            //}
            //switch (Unknown_4Ch)
            //{
            //    case 0x7f800001:
            //    case 0: //only in yft's!
            //        break;
            //    default:
            //        break;
            //}
            //if ((DrawableModelsHigh?.data_items != null) != (Unknown_80h != 0))
            //{ }//no hit
            //if ((DrawableModelsMedium?.data_items != null) != (Unknown_84h != 0))
            //{ }//no hit
            //if ((DrawableModelsLow?.data_items != null) != (Unknown_88h != 0))
            //{ }//no hit
            //if ((DrawableModelsVeryLow?.data_items != null) != (Unknown_8Ch != 0))
            //{ }//no hit
            //if ((Unknown_80h & 0xFFFF0000) > 0)
            //{ }//no hit
            //if ((Unknown_84h & 0xFFFF0000) > 0)
            //{ }//no hit
            //if ((Unknown_88h & 0xFFFF0000) > 0)
            //{ }//no hit
            //if ((Unknown_8Ch & 0xFFFF0000) > 0)
            //{ }//no hit
            //BuildRenderMasks();

            //switch (FlagsHigh)
            //{
            //    case 2:
            //    case 1:
            //    case 13:
            //    case 4:
            //    case 12:
            //    case 5:
            //    case 3:
            //    case 8:
            //    case 9:
            //    case 15:
            //    case 130:
            //    case 11:
            //    case 10:
            //    case 7:
            //    case 131:
            //    case 129:
            //    case 75:
            //    case 69:
            //    case 6:
            //    case 64:
            //    case 14:
            //    case 77:
            //    case 73:
            //    case 76:
            //    case 71:
            //    case 79:
            //    case 65:
            //    case 0://some yft's have null HD models
            //    case 19:
            //    case 51:
            //        break;
            //    default:
            //        break;
            //}
            //switch (FlagsMed)
            //{
            //    case 0:
            //    case 1:
            //    case 9:
            //    case 8:
            //    case 13:
            //    case 3:
            //    case 2:
            //    case 5:
            //    case 11:
            //    case 15:
            //    case 10:
            //    case 12:
            //    case 4:
            //    case 7:
            //    case 51:
            //        break;
            //    default:
            //        break;
            //}
            //switch (FlagsLow)
            //{
            //    case 0:
            //    case 9:
            //    case 1:
            //    case 8:
            //    case 5:
            //    case 3:
            //    case 13:
            //    case 2:
            //    case 11:
            //    case 15:
            //    case 4:
            //    case 7:
            //    case 51:
            //        break;
            //    default:
            //        break;
            //}
            //switch (FlagsVlow)
            //{
            //    case 0:
            //    case 1:
            //    case 9:
            //    case 3:
            //    case 7:
            //    case 5:
            //    case 49:
            //    case 51:
            //    case 11:
            //        break;
            //    default:
            //        break;
            //}
            //switch (Unknown_98h)
            //{
            //    case 0:
            //        break;
            //    default:
            //        break;//no hit
            //}
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            ShaderGroupPointer = (ulong)(ShaderGroup != null ? ShaderGroup.FilePosition : 0);
            SkeletonPointer = (ulong)(Skeleton != null ? Skeleton.FilePosition : 0);
            DrawableModelsHighPointer = (ulong)(DrawableModels?.GetHighPointer() ?? 0);
            DrawableModelsMediumPointer = (ulong)(DrawableModels?.GetMedPointer() ?? 0);
            DrawableModelsLowPointer = (ulong)(DrawableModels?.GetLowPointer() ?? 0);
            DrawableModelsVeryLowPointer = (ulong)(DrawableModels?.GetVLowPointer() ?? 0);
            JointsPointer = (ulong)(Joints != null ? Joints.FilePosition : 0);
            DrawableModelsPointer = (ulong)(DrawableModels?.FilePosition ?? 0);
            DrawableModelsBlocksSize = (ushort)Math.Ceiling((DrawableModels?.BlockLength ?? 0) / 16.0);

            // write structure data
            writer.Write(ShaderGroupPointer);
            writer.Write(SkeletonPointer);
            writer.Write(BoundingCenter);
            writer.Write(BoundingSphereRadius);
            writer.Write(BoundingBoxMin);
            writer.Write(Unknown_3Ch);
            writer.Write(BoundingBoxMax);
            writer.Write(Unknown_4Ch);
            writer.Write(DrawableModelsHighPointer);
            writer.Write(DrawableModelsMediumPointer);
            writer.Write(DrawableModelsLowPointer);
            writer.Write(DrawableModelsVeryLowPointer);
            writer.Write(LodDistHigh);
            writer.Write(LodDistMed);
            writer.Write(LodDistLow);
            writer.Write(LodDistVlow);
            writer.Write(RenderMaskFlagsHigh);
            writer.Write(RenderMaskFlagsMed);
            writer.Write(RenderMaskFlagsLow);
            writer.Write(RenderMaskFlagsVlow);
            writer.Write(JointsPointer);
            writer.Write(Unknown_98h);
            writer.Write(DrawableModelsBlocksSize);
            writer.Write(Unknown_9Ch);
            writer.Write(DrawableModelsPointer);
        }

        public virtual void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            YdrXml.SelfClosingTag(sb, indent, "BoundingSphereCenter " + FloatUtil.GetVector3XmlString(BoundingCenter));
            YdrXml.ValueTag(sb, indent, "BoundingSphereRadius", FloatUtil.ToString(BoundingSphereRadius));
            YdrXml.SelfClosingTag(sb, indent, "BoundingBoxMin " + FloatUtil.GetVector3XmlString(BoundingBoxMin));
            YdrXml.SelfClosingTag(sb, indent, "BoundingBoxMax " + FloatUtil.GetVector3XmlString(BoundingBoxMax));
            YdrXml.ValueTag(sb, indent, "LodDistHigh", FloatUtil.ToString(LodDistHigh));
            YdrXml.ValueTag(sb, indent, "LodDistMed", FloatUtil.ToString(LodDistMed));
            YdrXml.ValueTag(sb, indent, "LodDistLow", FloatUtil.ToString(LodDistLow));
            YdrXml.ValueTag(sb, indent, "LodDistVlow", FloatUtil.ToString(LodDistVlow));
            YdrXml.ValueTag(sb, indent, "FlagsHigh", FlagsHigh.ToString());
            YdrXml.ValueTag(sb, indent, "FlagsMed", FlagsMed.ToString());
            YdrXml.ValueTag(sb, indent, "FlagsLow", FlagsLow.ToString());
            YdrXml.ValueTag(sb, indent, "FlagsVlow", FlagsVlow.ToString());
            if (ShaderGroup != null)
            {
                YdrXml.OpenTag(sb, indent, "ShaderGroup");
                ShaderGroup.WriteXml(sb, indent + 1, ddsfolder);
                YdrXml.CloseTag(sb, indent, "ShaderGroup");
            }

            if (Skeleton != null)
            {
                YdrXml.OpenTag(sb, indent, "Skeleton");
                Skeleton.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "Skeleton");
            }

            if (Joints != null)
            {
                YdrXml.OpenTag(sb, indent, "Joints");
                Joints.WriteXml(sb, indent + 1);
                YdrXml.CloseTag(sb, indent, "Joints");
            }

            if (DrawableModels?.High != null)
                YdrXml.WriteItemArray(sb, DrawableModels.High, indent, "DrawableModelsHigh");
            if (DrawableModels?.Med != null)
                YdrXml.WriteItemArray(sb, DrawableModels.Med, indent, "DrawableModelsMedium");
            if (DrawableModels?.Low != null) YdrXml.WriteItemArray(sb, DrawableModels.Low, indent, "DrawableModelsLow");
            if (DrawableModels?.VLow != null)
                YdrXml.WriteItemArray(sb, DrawableModels.VLow, indent, "DrawableModelsVeryLow");
            if (DrawableModels?.Extra != null) //is this right? duplicates..?
                YdrXml.WriteItemArray(sb, DrawableModels.Extra, indent, "DrawableModelsX");
        }

        public virtual void ReadXml(XmlNode node, string ddsfolder)
        {
            BoundingCenter = Xml.GetChildVector3Attributes(node, "BoundingSphereCenter");
            BoundingSphereRadius = Xml.GetChildFloatAttribute(node, "BoundingSphereRadius");
            BoundingBoxMin = Xml.GetChildVector3Attributes(node, "BoundingBoxMin");
            BoundingBoxMax = Xml.GetChildVector3Attributes(node, "BoundingBoxMax");
            LodDistHigh = Xml.GetChildFloatAttribute(node, "LodDistHigh");
            LodDistMed = Xml.GetChildFloatAttribute(node, "LodDistMed");
            LodDistLow = Xml.GetChildFloatAttribute(node, "LodDistLow");
            LodDistVlow = Xml.GetChildFloatAttribute(node, "LodDistVlow");
            FlagsHigh = (byte)Xml.GetChildUIntAttribute(node, "FlagsHigh");
            FlagsMed = (byte)Xml.GetChildUIntAttribute(node, "FlagsMed");
            FlagsLow = (byte)Xml.GetChildUIntAttribute(node, "FlagsLow");
            FlagsVlow = (byte)Xml.GetChildUIntAttribute(node, "FlagsVlow");
            XmlNode sgnode = node.SelectSingleNode("ShaderGroup");
            if (sgnode != null)
            {
                ShaderGroup = new ShaderGroup();
                ShaderGroup.ReadXml(sgnode, ddsfolder);
            }

            XmlNode sknode = node.SelectSingleNode("Skeleton");
            if (sknode != null)
            {
                Skeleton = new Skeleton();
                Skeleton.ReadXml(sknode);
            }

            XmlNode jnode = node.SelectSingleNode("Joints");
            if (jnode != null)
            {
                Joints = new Joints();
                Joints.ReadXml(jnode);
            }

            DrawableModels = new DrawableModelsBlock();
            DrawableModels.High = XmlMeta.ReadItemArray<DrawableModel>(node, "DrawableModelsHigh");
            DrawableModels.Med = XmlMeta.ReadItemArray<DrawableModel>(node, "DrawableModelsMedium");
            DrawableModels.Low = XmlMeta.ReadItemArray<DrawableModel>(node, "DrawableModelsLow");
            DrawableModels.VLow = XmlMeta.ReadItemArray<DrawableModel>(node, "DrawableModelsVeryLow");
            DrawableModels.Extra = XmlMeta.ReadItemArray<DrawableModel>(node, "DrawableModelsX");
            if (DrawableModels.BlockLength == 0) DrawableModels = null;

            BuildRenderMasks();
            BuildAllModels();
            BuildVertexDecls();

            FileVFT = 1079456120;
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>(base.GetReferences());
            if (ShaderGroup != null) list.Add(ShaderGroup);
            if (Skeleton != null) list.Add(Skeleton);
            if (Joints != null) list.Add(Joints);
            if (DrawableModels != null) list.Add(DrawableModels);
            return list.ToArray();
        }


        public void AssignGeometryShaders(ShaderGroup shaderGrp)
        {
            //if (shaderGrp != null)
            //{
            //    ShaderGroup = shaderGrp;
            //}

            //map the shaders to the geometries
            if (shaderGrp?.Shaders?.data_items != null)
            {
                ShaderFX[] shaders = shaderGrp.Shaders.data_items;
                foreach (DrawableModel model in AllModels)
                {
                    if (model?.Geometries == null) continue;

                    int geomcount = model.Geometries.Length;
                    for (int i = 0; i < geomcount; i++)
                    {
                        DrawableGeometry geom = model.Geometries[i];
                        ushort sid = geom.ShaderID;
                        geom.Shader = sid < shaders.Length ? shaders[sid] : null;
                    }
                }
            }
        }


        public void BuildAllModels()
        {
            List<DrawableModel> allModels = new List<DrawableModel>();
            if (DrawableModels?.High != null) allModels.AddRange(DrawableModels.High);
            if (DrawableModels?.Med != null) allModels.AddRange(DrawableModels.Med);
            if (DrawableModels?.Low != null) allModels.AddRange(DrawableModels.Low);
            if (DrawableModels?.VLow != null) allModels.AddRange(DrawableModels.VLow);
            if (DrawableModels?.Extra != null) allModels.AddRange(DrawableModels.Extra);
            AllModels = allModels.ToArray();
        }

        public void BuildVertexDecls()
        {
            Dictionary<ulong, VertexDeclaration> vds = new Dictionary<ulong, VertexDeclaration>();
            foreach (DrawableModel model in AllModels)
            {
                if (model.Geometries == null) continue;
                foreach (DrawableGeometry geom in model.Geometries)
                {
                    VertexDeclaration info = geom.VertexBuffer.Info;
                    if (info == null) continue;
                    ulong declid = info.GetDeclarationId();

                    if (!vds.ContainsKey(declid)) vds.Add(declid, info);
                    //else //debug test
                    //{
                    //    if ((VertexDecls[declid].Stride != info.Stride)||(VertexDecls[declid].Types != info.Types))
                    //    {
                    //    }
                    //}
                }
            }

            VertexDecls = new Dictionary<ulong, VertexDeclaration>(vds);
        }


        public void BuildRenderMasks()
        {
            byte hmask = BuildRenderMask(DrawableModels?.High);
            byte mmask = BuildRenderMask(DrawableModels?.Med);
            byte lmask = BuildRenderMask(DrawableModels?.Low);
            byte vmask = BuildRenderMask(DrawableModels?.VLow);

            ////just testing
            //if (hmask != RenderMaskHigh)
            //{ }//no hit
            //if (mmask != RenderMaskMed)
            //{ }//no hit
            //if (lmask != RenderMaskLow)
            //{ }//no hit
            //if (vmask != RenderMaskVlow)
            //{ }//no hit

            RenderMaskHigh = hmask;
            RenderMaskMed = mmask;
            RenderMaskLow = lmask;
            RenderMaskVlow = vmask;
        }

        private byte BuildRenderMask(DrawableModel[] models)
        {
            byte mask = 0;
            if (models != null)
                foreach (DrawableModel model in models)
                    mask = (byte)(mask | model.RenderMask);

            return mask;
        }


        public DrawableBase ShallowCopy()
        {
            DrawableBase r = null;
            if (this is FragDrawable fd)
            {
                FragDrawable f = new FragDrawable();
                f.FragMatrix = fd.FragMatrix;
                f.FragMatricesIndsCount = fd.FragMatricesIndsCount;
                f.FragMatricesCapacity = fd.FragMatricesCapacity;
                f.FragMatricesCount = fd.FragMatricesCount;
                f.Bound = fd.Bound;
                f.FragMatricesInds = fd.FragMatricesInds;
                f.FragMatrices = fd.FragMatrices;
                f.Name = fd.Name;
                f.OwnerFragment = fd.OwnerFragment;
                f.OwnerFragmentPhys = fd.OwnerFragmentPhys;
                r = f;
            }

            if (this is Drawable dd)
            {
                Drawable d = new Drawable();
                d.LightAttributes = dd.LightAttributes;
                d.Name = dd.Name;
                d.Bound = dd.Bound;
                r = d;
            }

            if (r != null)
            {
                r.BoundingCenter = BoundingCenter;
                r.BoundingSphereRadius = BoundingSphereRadius;
                r.BoundingBoxMin = BoundingBoxMin;
                r.BoundingBoxMax = BoundingBoxMax;
                r.LodDistHigh = LodDistHigh;
                r.LodDistMed = LodDistMed;
                r.LodDistLow = LodDistLow;
                r.LodDistVlow = LodDistVlow;
                r.RenderMaskFlagsHigh = RenderMaskFlagsHigh;
                r.RenderMaskFlagsMed = RenderMaskFlagsMed;
                r.RenderMaskFlagsLow = RenderMaskFlagsLow;
                r.RenderMaskFlagsVlow = RenderMaskFlagsVlow;
                r.Unknown_98h = Unknown_98h;
                r.DrawableModelsBlocksSize = DrawableModelsBlocksSize;
                r.ShaderGroup = ShaderGroup;
                r.Skeleton = Skeleton?.Clone();
                r.DrawableModels = new DrawableModelsBlock();
                r.DrawableModels.High = DrawableModels?.High;
                r.DrawableModels.Med = DrawableModels?.Med;
                r.DrawableModels.Low = DrawableModels?.Low;
                r.DrawableModels.VLow = DrawableModels?.VLow;
                r.DrawableModels.Extra = DrawableModels?.Extra;
                r.Joints = Joints;
                r.AllModels = AllModels;
                r.VertexDecls = VertexDecls;
                r.Owner = Owner;
            }

            return r;
        }


        public void EnsureGen9()
        {
            FileVFT = 1079456120;
            FileUnknown = 1;
            Unknown_3Ch = 0x7f800001;
            Unknown_4Ch = 0x7f800001;

            if (Skeleton != null)
            {
                Skeleton.VFT = 1080114336;
                Skeleton.Unknown_4h = 1;
                if (Skeleton.Bones != null)
                {
                    Skeleton.Bones.Unk0 = 0;
                    Skeleton.Bones.Unk1 = 0;
                    Skeleton.Bones.Unk2 = 0;
                }
            }

            if (Joints != null)
            {
                Joints.VFT = 1080130656;
                Joints.Unknown_4h = 1;
            }

            if (ShaderGroup != null)
            {
                ShaderGroup.VFT = 1080113136;
                ShaderGroup.Unknown_4h = 1;

                ShaderGroup.TextureDictionary?.EnsureGen9();

                ShaderFX[] shaders = ShaderGroup.Shaders?.data_items;
                if (shaders != null)
                    foreach (ShaderFX shader in shaders)
                        shader?.EnsureGen9();
            }

            if (AllModels != null)
                foreach (DrawableModel model in AllModels)
                {
                    if (model == null) continue;
                    model.VFT = 1080101528;
                    model.Unknown_4h = 1;
                    DrawableGeometry[] geoms = model.Geometries;
                    if (geoms == null) continue;
                    foreach (DrawableGeometry geom in geoms)
                    {
                        if (geom == null) continue;
                        geom.VFT = 1080133528;
                        geom.Unknown_4h = 1;
                        geom.VertexBuffer?.EnsureGen9();
                        geom.IndexBuffer?.EnsureGen9();
                    }
                }

            if (this is Drawable dwbl && dwbl.LightAttributes?.data_items != null)
                foreach (LightAttributes light in dwbl.LightAttributes.data_items)
                {
                    light.Unknown_0h = 0;
                    light.Unknown_4h = 0;
                }
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Drawable : DrawableBase
    {
        private string_r NameBlock; //only used when saving..
        public override long BlockLength => 208;

        // structure data
        public ulong NamePointer { get; set; }
        public ResourceSimpleList64<LightAttributes> LightAttributes { get; set; }
        public ulong UnkPointer { get; set; }
        public ulong BoundPointer { get; set; }

        // reference data
        public string Name { get; set; }
        public Bounds Bound { get; set; }

        public string ErrorMessage { get; set; }


#if DEBUG
        public ResourceAnalyzer Analyzer { get; set; }
#endif


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            NamePointer = reader.ReadUInt64();
            LightAttributes = reader.ReadBlock<ResourceSimpleList64<LightAttributes>>();
            UnkPointer = reader.ReadUInt64();
            BoundPointer = reader.ReadUInt64();

            try
            {
                // read reference data
                Name = reader.ReadStringAt(NamePointer);
                Bound = reader.ReadBlockAt<Bounds>(BoundPointer);
                if (Bound != null) Bound.Owner = this;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }

            if (UnkPointer != 0)
            {
            }


#if DEBUG
            Analyzer = new ResourceAnalyzer(reader);
#endif
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            NamePointer = (ulong)(NameBlock != null ? NameBlock.FilePosition : 0);
            BoundPointer = (ulong)(Bound != null ? Bound.FilePosition : 0);

            // write structure data
            writer.Write(NamePointer);
            writer.WriteBlock(LightAttributes);
            writer.Write(UnkPointer);
            writer.Write(BoundPointer);
        }

        public override void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            YdrXml.StringTag(sb, indent, "Name", YdrXml.XmlEscape(Name));
            base.WriteXml(sb, indent, ddsfolder);
            if (Bound != null) Bounds.WriteXmlNode(Bound, sb, indent);
            if (LightAttributes?.data_items != null)
                YdrXml.WriteItemArray(sb, LightAttributes.data_items, indent, "Lights");
        }

        public override void ReadXml(XmlNode node, string ddsfolder)
        {
            Name = Xml.GetChildInnerText(node, "Name");
            base.ReadXml(node, ddsfolder);
            XmlNode bnode = node.SelectSingleNode("Bounds");
            if (bnode != null) Bound = Bounds.ReadXmlNode(bnode, this);

            LightAttributes = new ResourceSimpleList64<LightAttributes>();
            LightAttributes.data_items = XmlMeta.ReadItemArray<LightAttributes>(node, "Lights");
        }

        public static void WriteXmlNode(Drawable d, StringBuilder sb, int indent, string ddsfolder,
            string name = "Drawable")
        {
            if (d == null) return;
            YdrXml.OpenTag(sb, indent, name);
            d.WriteXml(sb, indent + 1, ddsfolder);
            YdrXml.CloseTag(sb, indent, name);
        }

        public static Drawable ReadXmlNode(XmlNode node, string ddsfolder)
        {
            if (node == null) return null;
            Drawable d = new Drawable();
            d.ReadXml(node, ddsfolder);
            return d;
        }


        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>(base.GetReferences());
            if (Name != null)
            {
                NameBlock = (string_r)Name;
                list.Add(NameBlock);
            }

            if (Bound != null) list.Add(Bound);
            return list.ToArray();
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            return new[]
            {
                new Tuple<long, IResourceBlock>(0xB0, LightAttributes)
            };
        }


        public override string ToString()
        {
            return Name;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawablePtfx : DrawableBase
    {
        public override long BlockLength => 176;

        // structure data
        public ulong UnkPointer { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            UnkPointer = reader.ReadUInt64();

            if (UnkPointer != 0)
            {
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // write structure data
            writer.Write(UnkPointer);
        }

        public override void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            base.WriteXml(sb, indent, ddsfolder);
        }

        public override void ReadXml(XmlNode node, string ddsfolder)
        {
            base.ReadXml(node, ddsfolder);
        }

        public static void WriteXmlNode(DrawablePtfx d, StringBuilder sb, int indent, string ddsfolder,
            string name = "Drawable")
        {
            if (d == null) return;
            YdrXml.OpenTag(sb, indent, name);
            d.WriteXml(sb, indent + 1, ddsfolder);
            YdrXml.CloseTag(sb, indent, name);
        }

        public static DrawablePtfx ReadXmlNode(XmlNode node, string ddsfolder)
        {
            if (node == null) return null;
            DrawablePtfx d = new DrawablePtfx();
            d.ReadXml(node, ddsfolder);
            return d;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawablePtfxDictionary : ResourceFileBase
    {
        private ResourceSystemStructBlock<uint> HashesBlock; //only used for saving

        // structure data
        public ulong Unknown_10h; // 0x0000000000000000
        public ulong Unknown_18h = 1; // 0x0000000000000001
        public override long BlockLength => 64;
        public ulong HashesPointer { get; set; }
        public ushort HashesCount1 { get; set; }
        public ushort HashesCount2 { get; set; }
        public uint Unknown_2Ch { get; set; }
        public ulong DrawablesPointer { get; set; }
        public ushort DrawablesCount1 { get; set; }
        public ushort DrawablesCount2 { get; set; }
        public uint Unknown_3Ch { get; set; }

        // reference data
        //public ResourceSimpleArray<uint_r> Hashes { get; set; }
        public uint[] Hashes { get; set; }
        public ResourcePointerArray64<DrawablePtfx> Drawables { get; set; }


        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            Unknown_10h = reader.ReadUInt64();
            Unknown_18h = reader.ReadUInt64();
            HashesPointer = reader.ReadUInt64();
            HashesCount1 = reader.ReadUInt16();
            HashesCount2 = reader.ReadUInt16();
            Unknown_2Ch = reader.ReadUInt32();
            DrawablesPointer = reader.ReadUInt64();
            DrawablesCount1 = reader.ReadUInt16();
            DrawablesCount2 = reader.ReadUInt16();
            Unknown_3Ch = reader.ReadUInt32();

            // read reference data
            Hashes = reader.ReadUintsAt(HashesPointer, HashesCount1);
            Drawables = reader.ReadBlockAt<ResourcePointerArray64<DrawablePtfx>>(DrawablesPointer, DrawablesCount1);

            //if (Unknown_10h != 0)
            //{ }
            //if (Unknown_18h != 1)
            //{ }
            //if (Unknown_2Ch != 0)
            //{ }
            //if (Unknown_3Ch != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            HashesPointer = (ulong)(HashesBlock != null ? HashesBlock.FilePosition : 0);
            HashesCount1 = (ushort)(HashesBlock != null ? HashesBlock.ItemCount : 0);
            HashesCount2 = (ushort)(HashesBlock != null ? HashesBlock.ItemCount : 0);
            DrawablesPointer = (ulong)(Drawables != null ? Drawables.FilePosition : 0);
            DrawablesCount1 = (ushort)(Drawables != null ? Drawables.Count : 0);
            DrawablesCount2 = (ushort)(Drawables != null ? Drawables.Count : 0);

            // write structure data
            writer.Write(Unknown_10h);
            writer.Write(Unknown_18h);
            writer.Write(HashesPointer);
            writer.Write(HashesCount1);
            writer.Write(HashesCount2);
            writer.Write(Unknown_2Ch);
            writer.Write(DrawablesPointer);
            writer.Write(DrawablesCount1);
            writer.Write(DrawablesCount2);
            writer.Write(Unknown_3Ch);
        }

        public void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            if (Drawables?.data_items != null)
                for (int i = 0; i < Drawables.data_items.Length; i++)
                {
                    DrawablePtfx d = Drawables.data_items[i];
                    MetaHash h = (MetaHash)(i < (Hashes?.Length ?? 0) ? Hashes[i] : 0);
                    YddXml.OpenTag(sb, indent, "Item");
                    YddXml.StringTag(sb, indent + 1, "Name", YddXml.XmlEscape(h.ToCleanString()));
                    d.WriteXml(sb, indent + 1, ddsfolder);
                    YddXml.CloseTag(sb, indent, "Item");
                }
        }

        public void ReadXml(XmlNode node, string ddsfolder)
        {
            List<DrawablePtfx> drawables = new List<DrawablePtfx>();
            List<uint> drawablehashes = new List<uint>();

            XmlNodeList inodes = node.SelectNodes("Item");
            if (inodes != null)
                foreach (XmlNode inode in inodes)
                {
                    MetaHash h = XmlMeta.GetHash(Xml.GetChildInnerText(inode, "Name"));
                    DrawablePtfx d = new DrawablePtfx();
                    d.ReadXml(inode, ddsfolder);
                    drawables.Add(d);
                    drawablehashes.Add(h);
                }

            if (drawables.Count > 0)
            {
                Hashes = drawablehashes.ToArray();
                Drawables = new ResourcePointerArray64<DrawablePtfx>();
                Drawables.data_items = drawables.ToArray();
            }
        }

        public static void WriteXmlNode(DrawablePtfxDictionary d, StringBuilder sb, int indent, string ddsfolder,
            string name = "DrawableDictionary")
        {
            if (d == null) return;
            YddXml.OpenTag(sb, indent, name);
            d.WriteXml(sb, indent + 1, ddsfolder);
            YddXml.CloseTag(sb, indent, name);
        }

        public static DrawablePtfxDictionary ReadXmlNode(XmlNode node, string ddsfolder)
        {
            if (node == null) return null;
            DrawablePtfxDictionary d = new DrawablePtfxDictionary();
            d.ReadXml(node, ddsfolder);
            return d;
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>(base.GetReferences());
            if (Hashes != null)
            {
                HashesBlock = new ResourceSystemStructBlock<uint>(Hashes);
                list.Add(HashesBlock);
            }

            if (Drawables != null) list.Add(Drawables);
            return list.ToArray();
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DrawableDictionary : ResourceFileBase
    {
        private ResourceSystemStructBlock<uint> HashesBlock; //only used for saving

        // structure data
        public ulong Unknown_10h; // 0x0000000000000000
        public ulong Unknown_18h = 1; // 0x0000000000000001
        public uint Unknown_2Ch; // 0x00000000
        public uint Unknown_3Ch; // 0x00000000
        public override long BlockLength => 64;
        public ulong HashesPointer { get; set; }
        public ushort HashesCount1 { get; set; }
        public ushort HashesCount2 { get; set; }
        public ulong DrawablesPointer { get; set; }
        public ushort DrawablesCount1 { get; set; }
        public ushort DrawablesCount2 { get; set; }

        // reference data
        //public ResourceSimpleArray<uint_r> Hashes { get; set; }
        public uint[] Hashes { get; set; }
        public ResourcePointerArray64<Drawable> Drawables { get; set; }


        public long MemoryUsage
        {
            get
            {
                long val = 0;
                if (Drawables != null && Drawables.data_items != null)
                    foreach (Drawable drawable in Drawables.data_items)
                        val += drawable.MemoryUsage;

                return val;
            }
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            Unknown_10h = reader.ReadUInt64();
            Unknown_18h = reader.ReadUInt64();
            HashesPointer = reader.ReadUInt64();
            HashesCount1 = reader.ReadUInt16();
            HashesCount2 = reader.ReadUInt16();
            Unknown_2Ch = reader.ReadUInt32();
            DrawablesPointer = reader.ReadUInt64();
            DrawablesCount1 = reader.ReadUInt16();
            DrawablesCount2 = reader.ReadUInt16();
            Unknown_3Ch = reader.ReadUInt32();

            // read reference data
            Hashes = reader.ReadUintsAt(HashesPointer, HashesCount1);

            Drawables = reader.ReadBlockAt<ResourcePointerArray64<Drawable>>(
                DrawablesPointer, // offset
                DrawablesCount1
            );

            //if (Unknown_10h != 0)
            //{ }
            //if (Unknown_18h != 1)
            //{ }
            //if (Unknown_2Ch != 0)
            //{ }
            //if (Unknown_3Ch != 0)
            //{ }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            HashesPointer = (ulong)(HashesBlock != null ? HashesBlock.FilePosition : 0);
            HashesCount1 = (ushort)(HashesBlock != null ? HashesBlock.ItemCount : 0);
            HashesCount2 = (ushort)(HashesBlock != null ? HashesBlock.ItemCount : 0);
            DrawablesPointer = (ulong)(Drawables != null ? Drawables.FilePosition : 0);
            DrawablesCount1 = (ushort)(Drawables != null ? Drawables.Count : 0);
            DrawablesCount2 = (ushort)(Drawables != null ? Drawables.Count : 0);

            // write structure data
            writer.Write(Unknown_10h);
            writer.Write(Unknown_18h);
            writer.Write(HashesPointer);
            writer.Write(HashesCount1);
            writer.Write(HashesCount2);
            writer.Write(Unknown_2Ch);
            writer.Write(DrawablesPointer);
            writer.Write(DrawablesCount1);
            writer.Write(DrawablesCount2);
            writer.Write(Unknown_3Ch);
        }

        public void WriteXml(StringBuilder sb, int indent, string ddsfolder)
        {
            if (Drawables?.data_items != null)
                foreach (Drawable d in Drawables.data_items)
                {
                    YddXml.OpenTag(sb, indent, "Item");
                    d.WriteXml(sb, indent + 1, ddsfolder);
                    YddXml.CloseTag(sb, indent, "Item");
                }
        }

        public void ReadXml(XmlNode node, string ddsfolder)
        {
            List<Drawable> drawables = new List<Drawable>();
            List<uint> drawablehashes = new List<uint>();

            XmlNodeList inodes = node.SelectNodes("Item");
            if (inodes != null)
                foreach (XmlNode inode in inodes)
                {
                    Drawable d = new Drawable();
                    d.ReadXml(inode, ddsfolder);
                    drawables.Add(d);
                    drawablehashes.Add(XmlMeta.GetHash(d.Name));
                }

            Hashes = drawablehashes.ToArray();
            Drawables = new ResourcePointerArray64<Drawable>();
            Drawables.data_items = drawables.ToArray();
        }

        public static void WriteXmlNode(DrawableDictionary d, StringBuilder sb, int indent, string ddsfolder,
            string name = "DrawableDictionary")
        {
            if (d == null) return;
            YddXml.OpenTag(sb, indent, name);
            d.WriteXml(sb, indent + 1, ddsfolder);
            YddXml.CloseTag(sb, indent, name);
        }

        public static DrawableDictionary ReadXmlNode(XmlNode node, string ddsfolder)
        {
            if (node == null) return null;
            DrawableDictionary d = new DrawableDictionary();
            d.ReadXml(node, ddsfolder);
            return d;
        }

        public override IResourceBlock[] GetReferences()
        {
            List<IResourceBlock> list = new List<IResourceBlock>(base.GetReferences());
            if (Hashes != null)
            {
                HashesBlock = new ResourceSystemStructBlock<uint>(Hashes);
                list.Add(HashesBlock);
            }

            if (Drawables != null) list.Add(Drawables);
            return list.ToArray();
        }
    }
}