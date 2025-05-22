using SharpDX;
using System.Collections.Generic;

namespace CodeWalker
{
    public class TriangleBVH : TriangleBVHNode
    {

        public TriangleBVH(TriangleBVHItem[] tris, int depth = 8)
        {
            if (tris == null) return;
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < tris.Length; i++)
            {
                TriangleBVHItem tri = tris[i];
                tri.UpdateBox();
                min = Vector3.Min(min, tri.Box.Minimum);
                max = Vector3.Max(max, tri.Box.Maximum);
            }
            Box = new BoundingBox(min, max);

            Build(tris, depth);

        }
    }

    public class TriangleBVHNode
    {
        public TriangleBVHItem[] Triangles { get; set; }
        public TriangleBVHNode Node1 { get; set; }
        public TriangleBVHNode Node2 { get; set; }
        public BoundingBox Box { get; set; }

        public void Build(TriangleBVHItem[] tris, int depth)
        {
            if (tris.Length <= 10) depth = 0;
            if (depth <= 0)
            {
                Triangles = tris;
                Node1 = null;
                Node2 = null;
            }
            else
            {
                Vector3 min = Box.Minimum;
                Vector3 max = Box.Maximum;
                Vector3 cen = Box.Center;
                Vector3 siz = Box.Size;
                BoundingBox b1, b2;
                if ((siz.X >= siz.Y) && (siz.X >= siz.Z))
                {
                    b1 = new BoundingBox(min, new Vector3(cen.X, max.Y, max.Z));
                    b2 = new BoundingBox(new Vector3(cen.X, min.Y, min.Z), max);
                }
                else if (siz.Y >= siz.Z)
                {
                    b1 = new BoundingBox(min, new Vector3(max.X, cen.Y, max.Z));
                    b2 = new BoundingBox(new Vector3(min.X, cen.Y, min.Z), max);
                }
                else
                {
                    b1 = new BoundingBox(min, new Vector3(max.X, max.Y, cen.Z));
                    b2 = new BoundingBox(new Vector3(min.X, min.Y, cen.Z), max);
                }
                List<TriangleBVHItem> l1 = new List<TriangleBVHItem>();
                List<TriangleBVHItem> l2 = new List<TriangleBVHItem>();
                for (int i = 0; i < tris.Length; i++)
                {
                    TriangleBVHItem tri = tris[i];
                    if (tri.Box.Contains(b1) != ContainmentType.Disjoint)// (tri.Box.Intersects(b1))
                    {
                        l1.Add(tri);
                    }
                    if (tri.Box.Contains(b2) != ContainmentType.Disjoint)// (tri.Box.Intersects(b2))
                    {
                        l2.Add(tri);
                    }
                }
                if (l1.Count > 0)
                {
                    Node1 = new TriangleBVHNode();
                    Node1.Box = b1;
                    Node1.Build(l1.ToArray(), depth - 1);
                }
                if (l2.Count > 0)
                {
                    Node2 = new TriangleBVHNode();
                    Node2.Box = b2;
                    Node2.Build(l2.ToArray(), depth - 1);
                }
            }
        }


        public TriangleBVHItem RayIntersect(ref Ray ray, ref float hitdist)
        {
            if (ray.Intersects(Box) == false) return null;

            TriangleBVHItem hit = null;
            if (Triangles != null)
            {
                for (int i = 0; i < Triangles.Length; i++)
                {
                    TriangleBVHItem tri = Triangles[i];
                    Vector3 v1 = tri.Corner1;
                    Vector3 v2 = tri.Corner2;
                    Vector3 v3 = tri.Corner3;
                    if (ray.Intersects(ref v1, ref v2, ref v3, out float d) && (d < hitdist) && (d > 0))
                    {
                        hitdist = d;
                        hit = tri;
                    }
                }
            }
            if (Node1 != null)
            {
                float hd = hitdist;
                TriangleBVHItem h = Node1.RayIntersect(ref ray, ref hd);
                if ((h != null) && (hd < hitdist))
                {
                    hitdist = hd;
                    hit = h;
                }
            }
            if (Node2 != null)
            {
                float hd = hitdist;
                TriangleBVHItem h = Node2.RayIntersect(ref ray, ref hd);
                if ((h != null) && (hd < hitdist))
                {
                    hitdist = hd;
                    hit = h;
                }
            }

            return hit;
        }

    }

    public abstract class TriangleBVHItem
    {
        public Vector3 Corner1 { get; set; }
        public Vector3 Corner2 { get; set; }
        public Vector3 Corner3 { get; set; }
        public BoundingBox Box { get; set; }

        public Vector3 Center
        {
            get
            {
                return (Corner1 + Corner2 + Corner3) * 0.3333333f;
            }
            set
            {
                Vector3 delta = value - Center;
                Corner1 += delta;
                Corner2 += delta;
                Corner3 += delta;
            }
        }
        public Quaternion Orientation 
        {
            get
            {
                return _Orientation;
            }
            set
            {
                Quaternion inv = Quaternion.Invert(_Orientation);
                Quaternion delta = value * inv;
                Vector3 cen = Center;
                Corner1 = cen + delta.Multiply(Corner1 - cen);
                Corner2 = cen + delta.Multiply(Corner2 - cen);
                Corner3 = cen + delta.Multiply(Corner3 - cen);
                _Orientation = value;
            }
        }
        private Quaternion _Orientation = Quaternion.Identity;
        public Vector3 Scale
        {
            get
            {
                return _Scale;
            }
            set
            {
                Quaternion inv = Quaternion.Invert(_Orientation);
                Vector3 delta = value / _Scale;
                Vector3 cen = Center;
                Corner1 = cen + _Orientation.Multiply(inv.Multiply(Corner1 - cen) * delta);
                Corner2 = cen + _Orientation.Multiply(inv.Multiply(Corner2 - cen) * delta);
                Corner3 = cen + _Orientation.Multiply(inv.Multiply(Corner3 - cen) * delta);
                _Scale = value;
            }
        }
        private Vector3 _Scale = Vector3.One;

        public void UpdateBox()
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            min = Vector3.Min(min, Corner1);
            min = Vector3.Min(min, Corner2);
            min = Vector3.Min(min, Corner3);
            max = Vector3.Max(max, Corner1);
            max = Vector3.Max(max, Corner2);
            max = Vector3.Max(max, Corner3);
            Box = new BoundingBox(min, max);
        }


    }

}
