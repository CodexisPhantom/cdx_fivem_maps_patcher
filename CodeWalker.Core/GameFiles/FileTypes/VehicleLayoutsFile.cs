using System.ComponentModel;

namespace CodeWalker.GameFiles
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VehicleLayoutsFile : GameFile, PackedFile
    {
        public string Xml { get; set; }

        public VehicleLayoutsFile() : base(null, GameFileType.VehicleLayouts)
        { }
        public VehicleLayoutsFile(RpfFileEntry entry) : base(entry, GameFileType.VehicleLayouts)
        {
        }

        public void Load(byte[] data, RpfFileEntry entry)
        {
            RpfFileEntry = entry;
            Name = entry.Name;
            FilePath = Name;


            //always XML .meta
            Xml = TextUtil.GetUTF8Text(data);

            //TODO: parse CVehicleMetadataMgr XML


            Loaded = true;
        }
    }
}
