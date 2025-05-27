using cdx_fivem_maps_patcher.Classes;
using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Patcher;

public abstract class Patcher(GameFileCache gameFileCache, string serverPath) : Page
{
    internal readonly GameFileCache GameFileCache = gameFileCache;
    internal readonly RpfManager RpfManager = new();
    internal readonly string ServerPath = serverPath;

    public void Show()
    {
        while (true)
        {
            PrintMenu();
            string? input = Console.ReadLine();
            Console.Clear();
            switch (input)
            {
                case "1":
                    Patch();
                    break;
                case "2":
                    return;
                default:
                    Console.WriteLine(Messages.Get("invalid_entry"));
                    break;
            }
        }
    }

    protected abstract void Patch();

    internal static YmapFile OpenYmapFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string name = new FileInfo(path).Name;
        RpfFileEntry fileEntry = CreateFileEntry(name, path, ref data);
        YmapFile? ymap = RpfFile.GetFile<YmapFile>(fileEntry, data);
        ymap.FilePath = path;
        return ymap;
    }

    internal static YbnFile OpenYbnFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string name = new FileInfo(path).Name;
        RpfFileEntry fileEntry = CreateFileEntry(name, path, ref data);
        YbnFile? ybn = RpfFile.GetFile<YbnFile>(fileEntry, data);
        ybn.FilePath = path;
        return ybn;
    }

    private static void PrintMenu()
    {
        Console.WriteLine(Messages.Get("main_menu_title"));
        Console.WriteLine(Messages.Get("patch_menu"));
        Console.WriteLine(Messages.Get("patch_menu_return"));
    }

    private static RpfFileEntry CreateFileEntry(string name, string path, ref byte[] data)
    {
        RpfFileEntry e;
        uint rsc7 = data.Length > 4 ? BitConverter.ToUInt32(data, 0) : 0;
        if (rsc7 == 0x37435352)
        {
            e = RpfFile.CreateResourceFileEntry(ref data, 0);
            data = ResourceBuilder.Decompress(data);
        }
        else
        {
            RpfBinaryFileEntry be = new()
            {
                FileSize = (uint)data.Length
            };
            be.FileUncompressedSize = be.FileSize;
            e = be;
        }

        e.Name = name;
        e.NameLower = name.ToLowerInvariant();
        e.NameHash = JenkHash.GenHash(e.NameLower);
        e.ShortNameHash = JenkHash.GenHash(Path.GetFileNameWithoutExtension(e.NameLower));
        e.Path = path;
        return e;
    }
}