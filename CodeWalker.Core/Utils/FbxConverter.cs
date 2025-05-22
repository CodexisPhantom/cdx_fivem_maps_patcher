using CodeWalker.GameFiles;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeWalker
{
    public class FbxConverter
    {
        public bool InvertTexcoordV { get; set; } = true;


        public YdrFile ConvertToYdr(string name, byte[] fbxdata)
        {
            FbxDocument fdoc = FbxIO.Read(fbxdata);
            if (fdoc == null)
                return null;

            Drawable dwbl = TryConvertDrawable(fdoc, name);


            YdrFile ydr = new YdrFile();
            ydr.Drawable = dwbl;
            ydr.Name = name;

            return ydr;
        }



        private Drawable TryConvertDrawable(FbxDocument fdoc, string name)
        {

            List<FbxNode> rootnodes = fdoc.GetSceneNodes();

            List<List<FbxModel>> mlists = new List<List<FbxModel>>();
            List<FbxModel> mlistall = new List<FbxModel>();

            foreach (FbxNode node in rootnodes)
            {
                if (node.Name == "Model")
                {
                    List<FbxModel> mlist = TryConvertModels(node); //flatten any models structure with depth >2
                    if (mlist != null)
                    {
                        mlists.Add(mlist);
                        mlistall.AddRange(mlist);
                    }
                }
            }

            List<DrawableModel> mlHigh = new List<DrawableModel>();
            List<DrawableModel> mlMed = new List<DrawableModel>();
            List<DrawableModel> mlLow = new List<DrawableModel>();
            List<DrawableModel> mlVlow = new List<DrawableModel>();
            List<DrawableModel> mlUnks = new List<DrawableModel>();
            List<DrawableModel> mlAll = new List<DrawableModel>();
            foreach (FbxModel m in mlistall)
            {
                string mnl = m.Name.ToLowerInvariant();
                if (mnl.EndsWith("_vlow"))
                {
                    mlVlow.Add(m.Model);
                }
                else if (mnl.EndsWith("_low"))
                {
                    mlLow.Add(m.Model);
                }
                else if (mnl.EndsWith("_med"))
                {
                    mlMed.Add(m.Model);
                }
                else if (mnl.EndsWith("_high"))
                {
                    mlHigh.Add(m.Model);
                }
                else
                {
                    mlUnks.Add(m.Model);
                }
            }
            if (mlHigh.Count == 0)//mlUnks could be embedded collisions... ignore for now
            {
                mlHigh.AddRange(mlUnks);
            }
            mlAll.AddRange(mlHigh);
            mlAll.AddRange(mlMed);
            mlAll.AddRange(mlLow);
            mlAll.AddRange(mlVlow);



            List<Vector3> allVerts = new List<Vector3>();
            Vector3 bbMin = new Vector3(float.MaxValue);
            Vector3 bbMax = new Vector3(float.MinValue);
            Vector3 bsCen = Vector3.Zero;
            float bsRad = 0.0f;
            foreach (FbxModel m in mlistall)
            {
                if (m?.Model?.Geometries == null) continue;
                foreach (DrawableGeometry g in m.Model.Geometries)
                {
                    byte[] vb = g.VertexData.VertexBytes;
                    int vs = g.VertexData.VertexStride;
                    int vc = g.VertexData.VertexCount;
                    for (int i = 0; i < vc; i++)
                    {
                        Vector3 vp = MetaTypes.ConvertData<Vector3>(vb, i * vs);//position offset should always be 0!
                        allVerts.Add(vp);
                        bbMin = Vector3.Min(bbMin, vp);
                        bbMax = Vector3.Max(bbMax, vp);
                        //bsCen += vp;
                    }
                }
            }
            if (allVerts.Count > 0)
            {
                //bsCen = bsCen / allVerts.Count;
                bsCen = (bbMin + bbMax) * 0.5f;
                foreach (Vector3 vp in allVerts)
                {
                    bsRad = Math.Max(bsRad, (vp - bsCen).Length());
                }
            }



            ShaderGroup sgrp = new ShaderGroup();
            List<ShaderFX> slist = new List<ShaderFX>();
            List<ushort> smapp = new List<ushort>();
            foreach (DrawableModel m in mlAll)
            {
                if (m?.Geometries == null) continue;
                smapp.Clear();
                foreach (DrawableGeometry g in m.Geometries)
                {
                    smapp.Add((ushort)slist.Count);
                    slist.Add(g.Shader);
                }
                m.ShaderMapping = smapp.ToArray();//TODO: re-use shaders!!
            }
            sgrp.Shaders = new ResourcePointerArray64<ShaderFX>();
            sgrp.Shaders.data_items = slist.ToArray();
            sgrp.ShadersCount1 = (ushort)slist.Count;
            sgrp.ShadersCount2 = (ushort)slist.Count;
            sgrp.VFT = 1080113376;//is this needed?
            sgrp.Unknown_4h = 1;


            Drawable d = new Drawable();
            d.Name = name + ".#dr";
            d.ShaderGroup = sgrp;
            d.BoundingCenter = bsCen;
            d.BoundingSphereRadius = bsRad;
            d.BoundingBoxMin = bbMin;
            d.BoundingBoxMax = bbMax;
            d.LodDistHigh = 9998;//lod dist defaults
            d.LodDistMed = 9998;
            d.LodDistLow = 9998;
            d.LodDistVlow = 9998;
            d.FileVFT = 1079446584;
            d.FileUnknown = 1;
            d.DrawableModels = new DrawableModelsBlock();
            if (mlHigh.Count > 0)
            {
                d.DrawableModels.High = mlHigh.ToArray();
                d.FlagsHigh = 1;//what flags should be used??
            }
            if (mlMed.Count > 0)
            {
                d.DrawableModels.Med = mlMed.ToArray();
                d.LodDistHigh = bsRad * 2.0f; //when med models present, generate a high lod dist..
                d.FlagsMed = 1;
            }
            if (mlLow.Count > 0)
            {
                d.DrawableModels.Low = mlLow.ToArray();
                d.LodDistMed = bsRad * 8.0f; //when low models present, generate a med lod dist..
                d.FlagsLow = 1;
            }
            if (mlVlow.Count > 0)
            {
                d.DrawableModels.VLow = mlVlow.ToArray();
                d.LodDistLow = bsRad * 32.0f; //when vlow models present, generate a low lod dist..
                d.FlagsVlow = 1;
            }

            d.BuildRenderMasks();

            d.LightAttributes = new ResourceSimpleList64<LightAttributes>();
            //todo: light attributes?


            return d;
        }



        private List<FbxModel> TryConvertModels(FbxNode mnode)
        {
            List<FbxModel> result = new List<FbxModel>();

            FbxModel nodemodel = TryConvertModel(mnode);
            if (nodemodel != null)
            {
                result.Add(nodemodel);
            }

            foreach (FbxNode cnode in mnode.Connections)
            {
                if (cnode.Name == "Model")
                {
                    List<FbxModel> mlist = TryConvertModels(cnode);
                    if (mlist != null)
                    {
                        result.AddRange(mlist);
                    }
                }
            }

            return result;
        }

        private FbxModel TryConvertModel(FbxNode mnode)
        {

            FbxNode geonode = null;
            List<FbxNode> matnodes = new List<FbxNode>();
            foreach (FbxNode cnode in mnode.Connections)
            {
                if (cnode == null) continue;
                switch (cnode.Name)
                {
                    case "Geometry":
                        geonode = cnode;
                        break;
                    case "Material":
                        matnodes.Add(cnode);
                        break;
                }
            }

            if (geonode == null) return null;
            if (matnodes.Count == 0)
                return null; //need atleast one material...

            int[] fnEdges = geonode["Edges"]?.Value as int[];//do we need this? maybe for collision/navmesh
            double[] fnVerts = geonode["Vertices"]?.Value as double[];
            int[] fnIndices = geonode["PolygonVertexIndex"]?.Value as int[];

            if ((fnVerts == null) || (fnIndices == null))
            { return null; } //no mesh data.. abort!

            List<FbxNode> fnNormals = new List<FbxNode>();
            List<FbxNode> fnBinormals = new List<FbxNode>();
            List<FbxNode> fnTangents = new List<FbxNode>();
            List<FbxNode> fnTexcoords = new List<FbxNode>();
            List<FbxNode> fnColours = new List<FbxNode>();
            List<FbxNode> fnMaterials = new List<FbxNode>();

            foreach (FbxNode cnode in geonode.Nodes)
            {
                if (cnode == null) continue;
                switch (cnode.Name)
                {
                    case "LayerElementNormal": fnNormals.Add(cnode); break;
                    case "LayerElementBinormal": fnBinormals.Add(cnode); break;
                    case "LayerElementTangent": fnTangents.Add(cnode); break;
                    case "LayerElementUV": fnTexcoords.Add(cnode); break;
                    case "LayerElementColor": fnColours.Add(cnode); break;
                    case "LayerElementMaterial": fnMaterials.Add(cnode); break;
                    case "LayerElementSmoothing": break;//ignore currently
                    case "Layer": break;//ignore- merge all layers data instead
                }
            }

            int nNormals = fnNormals.Count;
            int nBinormals = fnBinormals.Count;
            int nTangents = fnTangents.Count;
            int nTexcoords = fnTexcoords.Count;
            int nColours = fnColours.Count;
            int nMaterials = fnMaterials.Count;

            List<FbxPolygon> fPolys = new List<FbxPolygon>();
            List<FbxVertex> fPolyVerts = new List<FbxVertex>();
            List<FbxPolygon>[] fPolysByMat = new List<FbxPolygon>[matnodes.Count];

            foreach (int fnIndex in fnIndices) //build the polygons.
            {
                FbxVertex pVert = new FbxVertex();
                pVert.Position = GetVector3FromDoubleArray(fnVerts, (fnIndex < 0) ? (-fnIndex-1) : fnIndex);
                pVert.Normals = nNormals > 0 ? new Vector3[nNormals] : null;
                pVert.Binormals = nBinormals > 0 ? new Vector3[nBinormals] : null;
                pVert.Tangents = nTangents > 0 ? new Vector3[nTangents] : null;
                pVert.Texcoords = nTexcoords > 0 ? new Vector2[nTexcoords] : null;
                pVert.Colours = nColours > 0 ? new Vector4[nColours] : null;
                fPolyVerts.Add(pVert);
                if (fnIndex < 0) //yeah because negative index means end of polygon...
                {
                    FbxPolygon fPoly = new FbxPolygon();
                    fPoly.Vertices = fPolyVerts.ToArray();
                    fPoly.Materials = nMaterials > 0 ? new FbxNode[nMaterials] : null;
                    fPolyVerts.Clear();
                    fPolys.Add(fPoly);
                    if (fPoly.Vertices.Length > 3)
                    { } //more than 3 vertices in this poly! will need to split it into triangles!! but do it later since all poly verts are needed for next steps
                }
            }

            for (int i = 0; i < nNormals; i++)
            {
                FbxNode fnNorms = fnNormals[i];
                double[] arNorms = fnNorms["Normals"]?.Value as double[];
                int[] aiNorms = fnNorms["NormalIndex"]?.Value as int[];
                if (!IsByPolygonVertexMapType(fnNorms))
                { continue; }
                bool indexed = IsIndexToDirectRefType(fnNorms);
                if (indexed && (aiNorms == null))
                { continue; } //need the index array if it's IndexToDirect!
                int j = 0;
                foreach (FbxPolygon fPoly in fPolys)
                {
                    foreach (FbxVertex fVert in fPoly.Vertices)
                    {
                        int ai = indexed ? aiNorms[j] : j;
                        fVert.Normals[i] = GetVector3FromDoubleArray(arNorms, ai);
                        j++;
                    }
                }
            }
            for (int i = 0; i < nBinormals; i++)
            {
                FbxNode fnBinorms = fnBinormals[i];
                double[] arBinorms = fnBinorms["Binormals"]?.Value as double[];
                int[] aiBinorms = fnBinorms["BinormalIndex"]?.Value as int[];
                if (!IsByPolygonVertexMapType(fnBinorms))
                { continue; }
                bool indexed = IsIndexToDirectRefType(fnBinorms);
                if (indexed && (aiBinorms == null))
                { continue; } //need the index array if it's IndexToDirect!
                int j = 0;
                foreach (FbxPolygon fPoly in fPolys)
                {
                    foreach (FbxVertex fVert in fPoly.Vertices)
                    {
                        int ai = indexed ? aiBinorms[j] : j;
                        fVert.Binormals[i] = GetVector3FromDoubleArray(arBinorms, ai);
                        j++;
                    }
                }
            }
            for (int i = 0; i < nTangents; i++)
            {
                FbxNode fnTangs = fnTangents[i];
                double[] arTangs = fnTangs["Tangents"]?.Value as double[];
                int[] aiTangs = fnTangs["TangentIndex"]?.Value as int[];
                if (!IsByPolygonVertexMapType(fnTangs))
                { continue; }
                bool indexed = IsIndexToDirectRefType(fnTangs);
                if (indexed && (aiTangs == null))
                { continue; } //need the index array if it's IndexToDirect!
                int j = 0;
                foreach (FbxPolygon fPoly in fPolys)
                {
                    foreach (FbxVertex fVert in fPoly.Vertices)
                    {
                        int ai = indexed ? aiTangs[j] : j;
                        fVert.Tangents[i] = GetVector3FromDoubleArray(arTangs, ai);
                        j++;
                    }
                }
            }
            for (int i = 0; i < nTexcoords; i++)
            {
                FbxNode fnTexcs = fnTexcoords[i];
                double[] arTexcs = fnTexcs["UV"]?.Value as double[];
                int[] aiTexcs = fnTexcs["UVIndex"]?.Value as int[];
                if (!IsByPolygonVertexMapType(fnTexcs))
                { continue; }
                bool indexed = IsIndexToDirectRefType(fnTexcs);
                if (indexed && (aiTexcs == null))
                { continue; } //need the index array if it's IndexToDirect!
                int j = 0;
                foreach (FbxPolygon fPoly in fPolys)
                {
                    foreach (FbxVertex fVert in fPoly.Vertices)
                    {
                        int ai = indexed ? aiTexcs[j] : j;
                        Vector2 tc = GetVector2FromDoubleArray(arTexcs, ai);
                        fVert.Texcoords[i] = InvertTexcoordV ? new Vector2(tc.X, -tc.Y) : tc;//whyyyy
                        j++;
                    }
                }
            }
            for (int i = 0; i < nColours; i++)
            {
                FbxNode fnCols = fnColours[i];
                double[] arCols = fnCols["Colors"]?.Value as double[];
                int[] aiCols = fnCols["ColorIndex"]?.Value as int[];
                if (!IsByPolygonVertexMapType(fnCols))
                { continue; }
                bool indexed = IsIndexToDirectRefType(fnCols);
                if (indexed && (aiCols == null))
                { continue; } //need the index array if it's IndexToDirect!
                int j = 0;
                foreach (FbxPolygon fPoly in fPolys)
                {
                    foreach (FbxVertex fVert in fPoly.Vertices)
                    {
                        int ai = indexed ? aiCols[j] : j;
                        fVert.Colours[i] = GetVector4FromDoubleArray(arCols, ai);
                        j++;
                    }
                }
            }
            for (int i = 0; i < nMaterials; i++)
            {
                FbxNode fnMats = fnMaterials[i];
                int[] arMats = fnMats["Materials"]?.Value as int[];
                string mapType = fnMats["MappingInformationType"]?.Value as string;
                string refType = fnMats["ReferenceInformationType"]?.Value as string;
                bool allSame = false;
                switch (mapType)
                {
                    case "ByPolygon": break;
                    case "AllSame": allSame = true; break;
                    default:
                        continue;
                }
                switch (refType)
                {
                    case "IndexToDirect": break;
                    default:
                        continue;
                }
                for (int j = 0; j < fPolys.Count; j++)
                {
                    FbxPolygon fPoly = fPolys[j];
                    int iMat = allSame ? arMats[0] : arMats[j];
                    fPoly.Materials[i] = matnodes[iMat];

                    //group all the polygons by material...
                    List<FbxPolygon> matPolys = fPolysByMat[iMat];
                    if (matPolys == null)
                    {
                        matPolys = new List<FbxPolygon>();
                        fPolysByMat[iMat] = matPolys;
                    }
                    matPolys.Add(fPoly);
                }
            }




            DrawableModel dModel = new DrawableModel();
            
            List<DrawableGeometry> dGeoms = new List<DrawableGeometry>();
            List<AABB_s> dGeomAABBs = new List<AABB_s>();
            AABB_s dModelAABB = new AABB_s();
            for (int i = 0; i < fPolysByMat.Length; i++)
            {
                AABB_s dGeomAABB;
                DrawableGeometry dGeom = TryConvertGeometry(fPolysByMat[i], matnodes[i], out dGeomAABB);
                if (dGeom != null)
                {
                    dGeoms.Add(dGeom);
                    dGeomAABBs.Add(dGeomAABB);
                }
            }
            if (dGeomAABBs.Count > 1)//need to include whole model AABB first, if more than one geometry..
            {
                List<AABB_s> dGeomAABBs2 = new List<AABB_s>();
                dModelAABB.Min = new Vector4(float.MaxValue);
                dModelAABB.Max = new Vector4(float.MinValue);
                foreach (AABB_s aabb in dGeomAABBs)
                {
                    dModelAABB.Min = Vector4.Min(dModelAABB.Min, aabb.Min);
                    dModelAABB.Max = Vector4.Max(dModelAABB.Max, aabb.Max);
                }
                dGeomAABBs2.Add(dModelAABB);
                dGeomAABBs2.AddRange(dGeomAABBs);
                dGeomAABBs = dGeomAABBs2;
            }


            dModel.VFT = 1080101496;//is this needed?
            dModel.Unknown_4h = 1;
            dModel.RenderMaskFlags = 0x00FF; //GIMS "Mask"
            dModel.Geometries = dGeoms.ToArray();
            dModel.GeometriesCount1 = (ushort)dGeoms.Count;
            dModel.GeometriesCount2 = (ushort)dGeoms.Count;
            dModel.GeometriesCount3 = (ushort)dGeoms.Count;
            dModel.BoundsData = dGeomAABBs.ToArray();
            //shader mappings array will be added when adding models to drawable.



            FbxModel fModel = new FbxModel();
            fModel.Name = (mnode.Properties.Count > 1) ? (mnode.Properties[1] as string)?.Replace("Model::", "") : null;
            fModel.Node = mnode;
            fModel.Model = dModel;

            return fModel;
        }

        private DrawableGeometry TryConvertGeometry(List<FbxPolygon> fPolys, FbxNode matNode, out AABB_s aabb)
        {
            aabb = new AABB_s();

            if (matNode == null) return null;
            if (fPolys == null) return null;
            if (fPolys.Count == 0) return null;


            ShaderFX dShader = TryConvertMaterial(matNode);
            VertexDeclaration dVertDecl = GetVertexDeclaration(dShader);

            Dictionary<FbxVertex, ushort> vDict = new Dictionary<FbxVertex, ushort>();
            List<FbxVertex> vList = new List<FbxVertex>();
            List<ushort> iList = new List<ushort>();

            foreach (FbxPolygon fPoly in fPolys)
            {
                if (fPoly.Vertices == null) continue;
                if (vList.Count >= 65535)
                    break;//too many vertices in this geometry!!

                ushort i0 = 0;//first generated index
                ushort iP = 0;//previous generated index
                ushort iN = 0;//current index
                for (int v = 0; v < fPoly.Vertices.Length; v++)
                {
                    FbxVertex vert = fPoly.Vertices[v];
                    vert.GenVertexBytes(dVertDecl);

                    if (!vDict.TryGetValue(vert, out iN))
                    {
                        iN = (ushort)vList.Count;
                        vDict[vert] = iN;
                        vList.Add(vert);
                    }
                    else
                    { }//found identical vertex, use its index
                    if (v == 0) i0 = iN;
                    if (v < 3) //first triangle
                    {
                        iList.Add(iN);
                    }
                    else //for each extra vertex, make triangle from v0, vN-1, vN - assumes convex polygon!!
                    {
                        iList.Add(i0);
                        iList.Add(iP);
                        iList.Add(iN);
                    }
                    iP = iN;
                }
            }


            ushort vStride = dVertDecl.Stride;
            byte[] vBytes = new byte[vList.Count * vStride];
            for (int i = 0; i < vList.Count; i++)
            {
                byte[] v = vList[i].Bytes;
                int o = i * vStride;
                for (int j = 0; j < vStride; j++)
                {
                    vBytes[o + j] = v[j];
                }
            }

            if (vList.Count > 0)
            {
                aabb.Min = new Vector4(float.MaxValue);
                aabb.Max = new Vector4(float.MinValue);
                foreach (FbxVertex vert in vList)
                {
                    Vector4 v = new Vector4(vert.Position, vert.Position.X);
                    aabb.Min = Vector4.Min(aabb.Min, v);
                    aabb.Max = Vector4.Max(aabb.Max, v);
                }
            }


            VertexData vData = new VertexData();
            vData.Info = dVertDecl;
            vData.VertexType = (VertexType)dVertDecl.Flags;
            vData.VertexStride = dVertDecl.Stride;
            vData.VertexCount = vList.Count;
            vData.VertexBytes = vBytes;

            VertexBuffer vBuff = new VertexBuffer();
            vBuff.Data1 = vData;
            vBuff.Data2 = vData;
            vBuff.Info = dVertDecl;
            vBuff.VertexCount = (uint)vList.Count;
            vBuff.VertexStride = vStride;
            vBuff.VFT = 1080153064;//is this needed?
            vBuff.Unknown_4h = 1;

            IndexBuffer iBuff = new IndexBuffer();
            iBuff.IndicesCount = (uint)iList.Count;
            iBuff.Indices = iList.ToArray();
            iBuff.VFT = 1080111576;//is this needed?
            iBuff.Unknown_4h = 1;


            DrawableGeometry dGeom = new DrawableGeometry();
            dGeom.Shader = dShader;
            dGeom.VertexData = vData;
            dGeom.VertexBuffer = vBuff;
            dGeom.IndexBuffer = iBuff;
            dGeom.VFT = 1080133736;//is this needed?
            dGeom.Unknown_4h = 1;
            dGeom.IndicesCount = (uint)iList.Count;
            dGeom.TrianglesCount = (uint)iList.Count / 3;
            dGeom.VerticesCount = (ushort)vList.Count;
            dGeom.Unknown_62h = 3; //indices per triangle..?
            dGeom.VertexStride = vStride;
            dGeom.BoneIdsCount = 0;//todo: bones


            return dGeom;
        }

        private ShaderFX TryConvertMaterial(FbxNode matNode)
        {
            ShaderFX shader = new ShaderFX();

            string spsName = "default";
            List<FbxNode> texConns = new List<FbxNode>();
            List<string> texNames = new List<string>();

            #region 3dsmax/GIMS properties
            //var floatValueNames = new List<string>();
            //var floatValues = new List<Vector4>();
            //var texValueNames = new List<string>();
            //var texValues = new List<FbxNode>();
            //var matProps = matNode["Properties70"];
            //foreach (var matProp in matProps.Nodes)//currently broken due to GIMS not doing things right
            //{
            //    if (matProp == null) continue;
            //    if (matProp.Name != "P") continue;
            //    var propStr = GetStringFromObjectList(matProp.Properties, 4);
            //    var propId = matProp.Value as string;
            //    if (propId == null) continue;
            //    if (propId == "3dsMax|params|SPSName") spsName = propStr?.ToLowerInvariant() ?? "default";
            //    if (propId.StartsWith("3dsMax|params|FloatValueNames|FloatValueNames")) floatValueNames.Add(propStr);
            //    if (propId.StartsWith("3dsMax|params|FloatValues|FloatValues")) floatValues.Add(GetVector4FromObjectList(matProp.Properties, 4));
            //    if (propId.StartsWith("3dsMax|params|TexValueNames|TexValueNames")) texValueNames.Add(propStr);
            //    if (propId.StartsWith("3dsMax|params|TexValues|TexValues")) texValues.Add(matProp);
            //}
            #endregion

            foreach (FbxNode conn in matNode.Connections)
            {
                if (conn.Name == "Texture")
                {
                    texConns.Add(conn);
                    string texName = GetStringFromObjectList(conn.Properties, 1)?.Replace("Texture::", "");
                    string ftexName = conn["FileName"]?.Value as string;
                    if (ftexName != null)
                    {
                        try
                        {
                            texName = Path.GetFileNameWithoutExtension(ftexName);
                        }
                        catch
                        { }
                    }
                    texNames.Add(texName);
                }
            }

            if (texNames.Count > 1)
            {
                spsName = "normal";
            }

            string spsFileName = spsName + ".sps";

            shader.Name = JenkHash.GenHash(spsName);
            shader.FileName = JenkHash.GenHash(spsFileName);

            shader.ParametersList = new ShaderParametersBlock();
            ShaderParametersBlock paramsBlock = shader.ParametersList;
            List<ShaderParamNames> pNames = new List<ShaderParamNames>();
            List<ShaderParameter> pVals = new List<ShaderParameter>();


            shader.Unknown_Ch = 0;
            shader.RenderBucket = 0;
            shader.Unknown_12h = 32768;//shrugs
            shader.Unknown_1Ch = 0;
            shader.Unknown_24h = 0;
            shader.Unknown_26h = 0;
            shader.Unknown_28h = 0;


            switch (spsName)
            {
                default:
                case "default":
                    //shader.RenderBucket = 3;
                    //shader.ParameterSize = 208;
                    //shader.ParameterDataSize = 272;
                    AddShaderParam(pNames, pVals, ShaderParamNames.DiffuseSampler, GetTextureBaseParam(texNames, 0));//assume first texture is diffuse...
                    AddShaderParam(pNames, pVals, ShaderParamNames.matMaterialColorScale, new Vector4(1, 0, 0, 1));
                    AddShaderParam(pNames, pVals, ShaderParamNames.HardAlphaBlend, new Vector4(0, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.useTessellation, new Vector4(0, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.wetnessMultiplier, new Vector4(1, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.globalAnimUV1, new Vector4(0, 1, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.globalAnimUV0, new Vector4(1, 0, 0, 0));
                    break;
                case "normal":
                    //shader.RenderBucket = 0;
                    //shader.ParameterSize = 320;
                    //shader.ParameterDataSize = 400;
                    AddShaderParam(pNames, pVals, ShaderParamNames.DiffuseSampler, GetTextureBaseParam(texNames, 0));//assume first texture is diffuse...
                    AddShaderParam(pNames, pVals, ShaderParamNames.BumpSampler, GetTextureBaseParam(texNames, 1));//assume 2nd texture is normalmap..
                    AddShaderParam(pNames, pVals, ShaderParamNames.HardAlphaBlend, new Vector4(1, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.useTessellation, new Vector4(0, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.wetnessMultiplier, new Vector4(1, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.bumpiness, new Vector4(1, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.specularIntensityMult, new Vector4(0.5f, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.specularFalloffMult, new Vector4(20, 0, 0, 0));//too metallic?
                    AddShaderParam(pNames, pVals, ShaderParamNames.specularFresnel, new Vector4(0.9f, 0, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.globalAnimUV1, new Vector4(0, 1, 0, 0));
                    AddShaderParam(pNames, pVals, ShaderParamNames.globalAnimUV0, new Vector4(1, 0, 0, 0));
                    break;
            }

            for (int i = 0; i < pVals.Count; i++)
            {
                ShaderParameter pVal = pVals[i];
                if (pVal.DataType == 1)
                {
                    pVal.Unknown_1h = (byte)(160 + ((pVals.Count - 1) - i));//seriously wtf is this and why
                }
            }

            MetaName[] nameHashes = new MetaName[pNames.Count];
            for (int i = 0; i < pNames.Count; i++)
            {
                nameHashes[i] = (MetaName)pNames[i];
            }

            paramsBlock.Hashes = nameHashes;
            paramsBlock.Parameters = pVals.ToArray();
            paramsBlock.Count = pVals.Count;

            shader.ParameterSize = paramsBlock.ParametersSize;
            shader.ParameterDataSize = (ushort)(paramsBlock.BlockLength + 36);//but why +36?
            shader.ParameterCount = (byte)pVals.Count;
            shader.TextureParametersCount = paramsBlock.TextureParamsCount;
            shader.RenderBucketMask = (1u << shader.RenderBucket) | 0xFF00;


            return shader;
        }

        private TextureBase GetTextureBaseParam(List<string> texNames, int index)
        {
            string name = "givemechecker";
            if (texNames.Count > index)
            {
                string nameval = texNames[index];
                if (nameval != null)
                {
                    name = texNames[index];
                }
            }
            TextureBase texParam = new TextureBase();
            texParam.Unknown_4h = 1;
            texParam.Unknown_30h = 1;// 131073;//wtf is this? 2x shorts, 0x00020001
            texParam.Unknown_32h = 2;
            texParam.Name = name;
            texParam.NameHash = JenkHash.GenHash(name.ToLowerInvariant());
            return texParam;
        }
        private void AddShaderParam(List<ShaderParamNames> paramNames, List<ShaderParameter> paramValues, ShaderParamNames paramName, object paramValue)
        {
            ShaderParameter p = new ShaderParameter();
            p.Data = paramValue;
            if (paramValue is TextureBase)
            {
                p.DataType = 0;
                p.Unknown_1h = (byte)((paramNames.Count > 0) ? paramNames.Count + 1 : 0);//seriously wtf is this?
            }
            else if (paramValue is Vector4)
            {
                p.DataType = 1;
            }
            else
            { }

            paramNames.Add(paramName);
            paramValues.Add(p);
        }

        private VertexDeclaration GetVertexDeclaration(ShaderFX shader)
        {
            VertexDeclaration d = new VertexDeclaration();
            d.Types = VertexDeclarationTypes.GTAV1;
            d.Unknown_6h = 0;

            switch (shader.Name)
            {
                default:
                case 3839837909: //default
                    d.Flags = 89;
                    d.Stride = 36;
                    d.Count = 4;
                    break;
                case 1330140418: //normal
                    d.Flags = 16473;
                    d.Stride = 52;
                    d.Count = 5;
                    break;
            }

            return d;
        }


        private bool IsByPolygonVertexMapType(FbxNode node)
        {
            string mapType = node["MappingInformationType"]?.Value as string;
            if (mapType != "ByPolygonVertex")
            { return false; } //any other types?
            return true;
        }
        private bool IsIndexToDirectRefType(FbxNode node)
        {
            string refType = node["ReferenceInformationType"]?.Value as string;
            bool indexed = false;
            switch (refType)
            {
                case "Direct": break;
                case "IndexToDirect": indexed = true; break;
                default:
                    break;//shouldn't be possible
            }
            return indexed;
        }

        private Vector2 GetVector2FromDoubleArray(double[] arr, int i)
        {
            int aIndX = i * 2;
            int aIndY = aIndX + 1;
            double pX = aIndX < arr.Length ? arr[aIndX] : 0;
            double pY = aIndY < arr.Length ? arr[aIndY] : 0;
            return new Vector2((float)pX, (float)pY);
        }
        private Vector3 GetVector3FromDoubleArray(double[] arr, int i)
        {
            int aIndX = i * 3;
            int aIndY = aIndX + 1;
            int aIndZ = aIndX + 2;
            double pX = aIndX < arr.Length ? arr[aIndX] : 0;
            double pY = aIndY < arr.Length ? arr[aIndY] : 0;
            double pZ = aIndZ < arr.Length ? arr[aIndZ] : 0;
            return new Vector3((float)pX, (float)pY, (float)pZ);
        }
        private Vector4 GetVector4FromDoubleArray(double[] arr, int i)
        {
            int aIndX = i * 4;
            int aIndY = aIndX + 1;
            int aIndZ = aIndX + 2;
            int aIndW = aIndX + 3;
            double pX = aIndX < arr.Length ? arr[aIndX] : 0;
            double pY = aIndY < arr.Length ? arr[aIndY] : 0;
            double pZ = aIndZ < arr.Length ? arr[aIndZ] : 0;
            double pW = aIndW < arr.Length ? arr[aIndW] : 0;
            return new Vector4((float)pX, (float)pY, (float)pZ, (float)pW);
        }
        private Vector4 GetVector4FromObjectList(List<object> list, int i)
        {
            int aIndX = i;
            int aIndY = aIndX + 1;
            int aIndZ = aIndX + 2;
            int aIndW = aIndX + 3;
            object pX = aIndX < list.Count ? list[aIndX] : 0;
            object pY = aIndY < list.Count ? list[aIndY] : 0;
            object pZ = aIndZ < list.Count ? list[aIndZ] : 0;
            object pW = aIndW < list.Count ? list[aIndW] : 0;
            Vector4 r = Vector4.Zero;
            if (pX is double) r.X = (float)(double)pX;
            if (pY is double) r.Y = (float)(double)pY;
            if (pZ is double) r.Z = (float)(double)pZ;
            if (pW is double) r.W = (float)(double)pW;
            return r;
        }
        private string GetStringFromObjectList(List<object> list, int i)
        {
            return (list.Count > i) ? list[i] as string : string.Empty;
        }

    }


    public class FbxModel
    {
        public string Name { get; set; }
        public FbxNode Node { get; set; }
        public DrawableModel Model { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class FbxPolygon
    {
        public FbxVertex[] Vertices { get; set; }
        public FbxNode[] Materials { get; set; }
    }

    public class FbxVertex : IEquatable<FbxVertex>
    {
        public Vector3 Position { get; set; }
        public Vector3[] Normals { get; set; }
        public Vector3[] Binormals { get; set; }
        public Vector3[] Tangents { get; set; }
        public Vector2[] Texcoords { get; set; }
        public Vector4[] Colours { get; set; }
        public byte[] Bytes { get; set; }

        public void GenVertexBytes(VertexDeclaration decl)
        {
            Bytes = new byte[decl.Stride];

            switch ((VertexType)decl.Flags)
            {
                default:
                case VertexType.Default://PNCT
                    WriteBytes(Position, 0);
                    WriteBytes((Normals?.Length > 0) ? Normals[0] : Vector3.UnitZ, 12);
                    WriteBytes(GetColourInt((Colours?.Length > 0) ? Colours[0] : Vector4.One), 24);
                    WriteBytes((Texcoords?.Length > 0) ? Texcoords[0] : Vector2.Zero, 28);
                    break;
                case VertexType.DefaultEx://PNCTX
                    WriteBytes(Position, 0);
                    WriteBytes((Normals?.Length > 0) ? Normals[0] : Vector3.UnitZ, 12);
                    WriteBytes(GetColourInt((Colours?.Length > 0) ? Colours[0] : Vector4.One), 24);
                    WriteBytes((Texcoords?.Length > 0) ? Texcoords[0] : Vector2.Zero, 28);
                    WriteBytes(new Vector4((Tangents?.Length > 0) ? Tangents[0] : Vector3.UnitX, 0), 36);
                    //WriteBytes(new Vector4((Binormals?.Length > 0) ? Binormals[0] : Vector3.UnitX, 0), 36);
                    break;
            }


        }
        private void WriteBytes<T>(T val, int offset) where T : struct
        {
            byte[] data = MetaTypes.ConvertToBytes(val);
            for (int i = 0; i < data.Length; i++)
            {
                Bytes[offset + i] = data[i];
            }
        }
        private int GetColourInt(Vector4 c)
        {
            Color v = new Color(c);
            return v.ToRgba();
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as FbxVertex);
        }
        public bool Equals(FbxVertex other)
        {
            return (other != null)
                && ((Bytes == null) ? (other.Bytes == null) : Bytes.SequenceEqual(other.Bytes));
        }
        public override int GetHashCode()
        {
            int hashCode = -907793594;
            if (Bytes != null) hashCode = hashCode * -1521134295 + ((IStructuralEquatable)Bytes).GetHashCode(EqualityComparer<byte>.Default);
            return hashCode;
        }
    }

}
