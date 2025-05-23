using System;
using System.Collections.Generic;
using System.Xml;
using CodeWalker.GameFiles;
using SharpDX;

namespace CodeWalker.World
{
    public class Water
    {
        public List<WaterCalmingQuad> CalmingQuads = new List<WaterCalmingQuad>();
        public GameFileCache GameFileCache;
        public volatile bool Inited;
        public List<WaterQuad> WaterQuads = new List<WaterQuad>();
        public List<WaterWaveQuad> WaveQuads = new List<WaterWaveQuad>();

        public void Init(GameFileCache gameFileCache, Action<string> updateStatus)
        {
            GameFileCache = gameFileCache;


            WaterQuads.Clear();
            CalmingQuads.Clear();
            WaveQuads.Clear();

            LoadWaterXml("common.rpf\\data\\levels\\gta5\\water.xml");

            if (GameFileCache.EnableDlc)
                LoadWaterXml("update\\update.rpf\\common\\data\\levels\\gta5\\water_heistisland.xml");


            Inited = true;
        }

        private void LoadWaterXml(string filename)
        {
            RpfManager rpfman = GameFileCache.RpfMan;
            XmlDocument waterxml = rpfman.GetFileXml(filename);

            XmlElement waterdata = waterxml.DocumentElement;

            XmlNodeList waterquads = waterdata.SelectNodes("WaterQuads/Item");
            for (int i = 0; i < waterquads.Count; i++)
            {
                WaterQuad waterquad = new WaterQuad();
                waterquad.Init(waterquads[i], i);
                WaterQuads.Add(waterquad);
            }

            XmlNodeList calmingquads = waterdata.SelectNodes("CalmingQuads/Item");
            for (int i = 0; i < calmingquads.Count; i++)
            {
                WaterCalmingQuad calmingquad = new WaterCalmingQuad();
                calmingquad.Init(calmingquads[i], i);
                CalmingQuads.Add(calmingquad);
            }

            XmlNodeList wavequads = waterdata.SelectNodes("WaveQuads/Item");
            for (int i = 0; i < wavequads.Count; i++)
            {
                WaterWaveQuad wavequad = new WaterWaveQuad();
                wavequad.Init(wavequads[i], i);
                WaveQuads.Add(wavequad);
            }
        }


        public List<T> GetVisibleQuads<T>(Camera camera, IEnumerable<T> allQuads) where T : BaseWaterQuad
        {
            List<T> quads = new List<T>();

            if (!Inited) return quads;

            Frustum vf = camera.ViewFrustum;
            foreach (T quad in allQuads)
            {
                Vector3 camrel = quad.BSCenter - camera.Position;
                if (vf.ContainsSphereNoClipNoOpt(ref camrel, quad.BSRadius)) quads.Add(quad);
            }

            return quads;
        }
    }

    public abstract class BaseWaterQuad
    {
        public int xmlNodeIndex { get; set; }
        public float minX { get; set; }
        public float maxX { get; set; }
        public float minY { get; set; }
        public float maxY { get; set; }
        public float? z { get; set; }

        public Vector3 BSCenter { get; private set; }
        public float BSRadius { get; private set; }

        public abstract void Init(XmlNode node, int index);

        public void CalcBS()
        {
            BSCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, z ?? 0);
            BSRadius = new Vector2(maxX - minX, maxY - minY).Length() * 0.5f;
        }

        public override string ToString()
        {
            return string.Format("[{0}] X=({1} : {2}), Y=({3} : {4})", xmlNodeIndex, minX, maxX, minY, maxY);
        }
    }

    public class WaterQuad : BaseWaterQuad
    {
        public int Type { get; set; }
        public bool IsInvisible { get; set; }
        public bool HasLimitedDepth { get; set; }
        public float a1 { get; set; }
        public float a2 { get; set; }
        public float a3 { get; set; }
        public float a4 { get; set; }
        public bool NoStencil { get; set; }


        public override void Init(XmlNode node, int index)
        {
            xmlNodeIndex = index;
            minX = Xml.GetChildFloatAttribute(node, "minX");
            maxX = Xml.GetChildFloatAttribute(node, "maxX");
            minY = Xml.GetChildFloatAttribute(node, "minY");
            maxY = Xml.GetChildFloatAttribute(node, "maxY");
            Type = Xml.GetChildIntAttribute(node, "Type");
            IsInvisible = Xml.GetChildBoolAttribute(node, "IsInvisible");
            HasLimitedDepth = Xml.GetChildBoolAttribute(node, "HasLimitedDepth");
            z = Xml.GetChildFloatAttribute(node, "z");
            a1 = Xml.GetChildFloatAttribute(node, "a1");
            a2 = Xml.GetChildFloatAttribute(node, "a2");
            a3 = Xml.GetChildFloatAttribute(node, "a3");
            a4 = Xml.GetChildFloatAttribute(node, "a4");
            NoStencil = Xml.GetChildBoolAttribute(node, "NoStencil");

            /*
            <minX value="-1592" />
            <maxX value="-1304" />
            <minY value="-1744" />
            <maxY value="-1624" />
            <Type value="0" />
            <IsInvisible value="false" />
            <HasLimitedDepth value="false" />
            <z value="0.0" />
            <a1 value="26" />
            <a2 value="26" />
            <a3 value="26" />
            <a4 value="26" />
            <NoStencil value="false" />
             */

            CalcBS();
        }
    }

    public class WaterCalmingQuad : BaseWaterQuad
    {
        public float fDampening { get; set; }

        public override void Init(XmlNode node, int index)
        {
            xmlNodeIndex = index;
            minX = Xml.GetChildFloatAttribute(node, "minX");
            maxX = Xml.GetChildFloatAttribute(node, "maxX");
            minY = Xml.GetChildFloatAttribute(node, "minY");
            maxY = Xml.GetChildFloatAttribute(node, "maxY");
            fDampening = Xml.GetChildFloatAttribute(node, "fDampening");

            /*
            <minX value="1752" />
            <maxX value="2076" />
            <minY value="216" />
            <maxY value="800" />
            <fDampening value="0.05" />
             */

            CalcBS();
        }
    }

    public class WaterWaveQuad : BaseWaterQuad
    {
        public float Amplitude { get; set; }
        public float XDirection { get; set; }
        public float YDirection { get; set; }
        public Quaternion WaveOrientation { get; set; }


        public override void Init(XmlNode node, int index)
        {
            xmlNodeIndex = index;
            minX = Xml.GetChildFloatAttribute(node, "minX");
            maxX = Xml.GetChildFloatAttribute(node, "maxX");
            minY = Xml.GetChildFloatAttribute(node, "minY");
            maxY = Xml.GetChildFloatAttribute(node, "maxY");
            Amplitude = Xml.GetChildFloatAttribute(node, "Amplitude");
            XDirection = Xml.GetChildFloatAttribute(node, "XDirection");
            YDirection = Xml.GetChildFloatAttribute(node, "YDirection");

            float angl = (float)Math.Atan2(YDirection, XDirection);
            WaveOrientation = Quaternion.RotationYawPitchRoll(0.0f, 0.0f, angl);

            /*
            <minX value="1664" />
            <maxX value="1988" />
            <minY value="-120" />
            <maxY value="132" />
            <Amplitude value="0.1" />
            <XDirection value="-0.603208" />
            <YDirection value="-0.797584" />
             */

            CalcBS();
        }
    }
}