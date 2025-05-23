using System.ComponentModel;
using System.Threading;
using CodeWalker.GameFiles;
using SharpDX;

namespace CodeWalker.World
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Weapon
    {
        public YmapEntityDef RenderEntity = new YmapEntityDef(); //placeholder entity object for rendering
        public string Name { get; set; } = string.Empty;
        public MetaHash NameHash { get; set; } = 0; //base weapon name hash
        public MetaHash ModelHash { get; set; } = 0; //weapon model name hash, can be _hi

        public YdrFile Ydr { get; set; }
        public Drawable Drawable { get; set; }

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;


        public void Init(string name, GameFileCache gfc, bool hidef = true)
        {
            Name = name;
            string modelnamel = name.ToLowerInvariant();
            MetaHash modelhash = JenkHash.GenHash(modelnamel);
            MetaHash modelhashhi = JenkHash.GenHash(modelnamel + "_hi");
            MetaHash ydrhash = hidef ? modelhashhi : modelhash;

            NameHash = modelhash;
            ModelHash = ydrhash;

            MetaHash useHash = ModelHash;
            Ydr = gfc.GetYdr(ModelHash);
            if (Ydr == null)
            {
                useHash = NameHash;
                Ydr = gfc.GetYdr(NameHash);
            }

            while (Ydr != null && !Ydr.Loaded)
            {
                Thread.Sleep(1); //kinda hacky
                Ydr = gfc.GetYdr(useHash);
            }

            if (Ydr != null) Drawable = Ydr.Drawable?.ShallowCopy() as Drawable;


            UpdateEntity();
        }


        public void UpdateEntity()
        {
            RenderEntity.SetPosition(Position);
            RenderEntity.SetOrientation(Rotation);
        }
    }
}