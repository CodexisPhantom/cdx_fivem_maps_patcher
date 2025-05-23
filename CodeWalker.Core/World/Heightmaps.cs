using System;
using System.Collections.Generic;
using CodeWalker.GameFiles;
using SharpDX;

namespace CodeWalker.World
{
    public class Heightmaps : BasePathData
    {
        public GameFileCache GameFileCache;

        public List<HeightmapFile> HeightmapFiles = new List<HeightmapFile>();
        public volatile bool Inited;

        public Vector4[] NodePositions;
        public EditorVertex[] TriangleVerts;


        public Vector4[] GetNodePositions()
        {
            return NodePositions;
        }

        public EditorVertex[] GetPathVertices()
        {
            return null;
        }

        public EditorVertex[] GetTriangleVertices()
        {
            return TriangleVerts;
        }


        public void Init(GameFileCache gameFileCache, Action<string> updateStatus)
        {
            Inited = false;

            GameFileCache = gameFileCache;


            HeightmapFiles.Clear();


            if (gameFileCache.EnableDlc)
            {
                LoadHeightmap("update\\update.rpf\\common\\data\\levels\\gta5\\heightmap.dat");
                LoadHeightmap("update\\update.rpf\\common\\data\\levels\\gta5\\heightmapheistisland.dat");
            }
            else
            {
                LoadHeightmap("common.rpf\\data\\levels\\gta5\\heightmap.dat");
            }


            BuildVertices();

            Inited = true;
        }

        private void LoadHeightmap(string filename)
        {
            HeightmapFile hmf = GameFileCache.RpfMan.GetFile<HeightmapFile>(filename);
            HeightmapFiles.Add(hmf);
        }


        public void BuildVertices()
        {
            List<EditorVertex> vlist = new List<EditorVertex>();
            List<Vector4> nlist = new List<Vector4>();

            foreach (HeightmapFile hmf in HeightmapFiles) BuildHeightmapVertices(hmf, vlist, nlist);

            if (vlist.Count > 0)
                TriangleVerts = vlist.ToArray();
            else
                TriangleVerts = null;
            if (nlist.Count > 0)
                NodePositions = nlist.ToArray();
            else
                NodePositions = null;
        }

        private void BuildHeightmapVertices(HeightmapFile hmf, List<EditorVertex> vl, List<Vector4> nl)
        {
            EditorVertex v1 = new EditorVertex();
            EditorVertex v2 = new EditorVertex();
            EditorVertex v3 = new EditorVertex();
            EditorVertex v4 = new EditorVertex();

            uint cgrn = (uint)new Color(0, 128, 0, 60).ToRgba();
            uint cyel = (uint)new Color(128, 128, 0, 200).ToRgba();

            ushort w = hmf.Width;
            ushort h = hmf.Height;
            byte[] hmin = hmf.MinHeights;
            byte[] hmax = hmf.MaxHeights;
            Vector3 min = hmf.BBMin;
            Vector3 max = hmf.BBMax;
            Vector3 siz = max - min;
            Vector3 step = siz / new Vector3(w - 1, h - 1, 255);

            v1.Colour = v2.Colour = v3.Colour = v4.Colour = cyel;
            for (int yi = 1; yi < h; yi++)
            {
                int yo = yi - 1;
                for (int xi = 1; xi < w; xi++)
                {
                    int xo = xi - 1;
                    int o1 = yo * w + xo;
                    int o2 = yo * w + xi;
                    int o3 = yi * w + xo;
                    int o4 = yi * w + xi;
                    v1.Position = min + step * new Vector3(xo, yo, hmin[o1]);
                    v2.Position = min + step * new Vector3(xi, yo, hmin[o2]);
                    v3.Position = min + step * new Vector3(xo, yi, hmin[o3]);
                    v4.Position = min + step * new Vector3(xi, yi, hmin[o4]);
                    vl.Add(v1);
                    vl.Add(v2);
                    vl.Add(v3);
                    vl.Add(v3);
                    vl.Add(v2);
                    vl.Add(v4);
                }
            }

            v1.Colour = v2.Colour = v3.Colour = v4.Colour = cgrn;
            for (int yi = 1; yi < h; yi++)
            {
                int yo = yi - 1;
                for (int xi = 1; xi < w; xi++)
                {
                    int xo = xi - 1;
                    int o1 = yo * w + xo;
                    int o2 = yo * w + xi;
                    int o3 = yi * w + xo;
                    int o4 = yi * w + xi;
                    v1.Position = min + step * new Vector3(xo, yo, hmax[o1]);
                    v2.Position = min + step * new Vector3(xi, yo, hmax[o2]);
                    v3.Position = min + step * new Vector3(xo, yi, hmax[o3]);
                    v4.Position = min + step * new Vector3(xi, yi, hmax[o4]);
                    vl.Add(v1);
                    vl.Add(v2);
                    vl.Add(v3);
                    vl.Add(v3);
                    vl.Add(v2);
                    vl.Add(v4);
                }
            }

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int o = y * w + x;
                nl.Add(new Vector4(min + step * new Vector3(x, y, hmin[o]), 10));
                nl.Add(new Vector4(min + step * new Vector3(x, y, hmax[o]), 10));
            }
        }
    }
}