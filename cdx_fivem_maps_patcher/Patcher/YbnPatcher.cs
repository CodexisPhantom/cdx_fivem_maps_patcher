using cdx_fivem_maps_patcher.Classes;
using cdx_fivem_maps_patcher.Pages;
using CodeWalker.GameFiles;

namespace cdx_fivem_maps_patcher.Patcher;

public class YbnPatcher(GameFileCache gameFileCache, string serverPath) : Patcher(gameFileCache, serverPath)
{
    protected override void Patch()
    {
        Dictionary<string, List<string>> duplicates = FindDuplicateYbnFiles(ServerPath);
        if (duplicates.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_duplicates_found"));
            return;
        }
        Console.WriteLine(Messages.Get("duplicates_found"));
        foreach (KeyValuePair<string, List<string>> entry in duplicates) PatchYbn(entry.Key, entry.Value);
    }
    
    private static Dictionary<string, List<string>> FindDuplicateYbnFiles(string directoryPath)
    {
        Dictionary<string, List<string>> nameToFiles = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory {directoryPath} does not exist.");

            string[] ymapFiles = Directory.GetFiles(directoryPath, "*.ybn", SearchOption.AllDirectories).Where(f => !f.Contains(Path.DirectorySeparatorChar + "cdx_fivem_maps_patcher" + Path.DirectorySeparatorChar)).ToArray();
            if (ymapFiles.Length == 0) return nameToFiles;

            foreach (string filePath in ymapFiles)
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    if (!nameToFiles.TryGetValue(fileName, out List<string>? value))
                    {
                        value = [];
                        nameToFiles[fileName] = value;
                    }

                    value.Add(filePath);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Erreur d'accès au fichier {filePath} : {ex.Message}");
                }

            return nameToFiles
                .Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void PatchYbn(string name, List<string> files)
    {
        Console.WriteLine($"Patching {name}...");
        Dictionary<uint, RpfFileEntry> ybnDict = GameFileCache.YbnDict;

        uint ybnHash = (from entry in GameFileCache.YbnDict where entry.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase) select entry.Key).FirstOrDefault();
        if (ybnHash == 0) return;

        RpfFileEntry ybnEntry = ybnDict[ybnHash];
        YbnFile? mainYbn = RpfManager.GetFile<YbnFile>(ybnEntry);

        List<YbnFile> ybnFiles = [];
        foreach (string filePath in files)
            try
            {
                YbnFile ybn = OpenYbnFile(filePath);
                ybnFiles.Add(ybn);
                File.Move(filePath, filePath + ".backup", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error patching {filePath}: {ex.Message}");
            }

        if (ybnFiles.Count == 0) return;

        
        
        Backups.SaveYbn(ServerPath, mainYbn);
    }
}