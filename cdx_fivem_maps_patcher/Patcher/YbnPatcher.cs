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
        
        Dictionary<string, List<string>> selectedYbns = PromptUserForYbnSelection(duplicates);
        
        if (selectedYbns.Count == 0)
        {
            Console.WriteLine(Messages.Get("no_ybns_selected_for_patching"));
            return;
        }

        foreach (KeyValuePair<string, List<string>> entry in selectedYbns) 
            PatchYbn(entry.Key, entry.Value);
    }

    private Dictionary<string, List<string>> PromptUserForYbnSelection(Dictionary<string, List<string>> duplicates)
    {
        Dictionary<string, List<string>> selectedYbns = new(StringComparer.OrdinalIgnoreCase);
        
        Console.WriteLine(Messages.Get("found_duplicate_ybn_files_header"));
        Console.WriteLine(Messages.Get("select_ybns_to_patch_prompt"));

        List<KeyValuePair<string, List<string>>> duplicatesList = duplicates.ToList();
        
        for (int i = 0; i < duplicatesList.Count; i++)
        {
            KeyValuePair<string, List<string>> kvp = duplicatesList[i];
            Console.WriteLine(Messages.Get("ybn_selection_item_format", i + 1, kvp.Key, kvp.Value.Count));
            
            foreach (string filePath in kvp.Value)
            {
                Console.WriteLine(Messages.Get("ybn_selection_file_path_prefix") + filePath);
            }
            Console.WriteLine();
        }

        Console.WriteLine(Messages.Get("ybn_selection_options_header"));
        Console.WriteLine(Messages.Get("ybn_selection_option_numbers"));
        Console.WriteLine(Messages.Get("ybn_selection_option_all"));
        Console.WriteLine(Messages.Get("ybn_selection_option_none"));
        Console.Write(Messages.Get("ybn_selection_input_prompt"));

        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) || input.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return selectedYbns;
        }

        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return duplicates;
        }

        string[] selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> validIndices = [];

        foreach (string selection in selections)
        {
            if (int.TryParse(selection.Trim(), out int index) && 
                index >= 1 && index <= duplicatesList.Count)
            {
                validIndices.Add(index - 1);
            }
            else
            {
                Console.WriteLine(Messages.Get("ybn_selection_invalid_warning", selection));
            }
        }

        foreach (KeyValuePair<string, List<string>> kvp in validIndices.Select(index => duplicatesList[index]))
        {
            selectedYbns[kvp.Key] = kvp.Value;
        }

        if (selectedYbns.Count <= 0) return selectedYbns;
        
        Console.WriteLine(Messages.Get("ybn_selection_selected_count_message", selectedYbns.Count));
        foreach (string ybnName in selectedYbns.Keys)
        {
            Console.WriteLine(Messages.Get("ybn_selection_selected_item_prefix") + ybnName);
        }

        return selectedYbns;
    }

    private static Dictionary<string, List<string>> FindDuplicateYbnFiles(string directoryPath)
    {
        Dictionary<string, List<string>> nameToFiles = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(Messages.Get("directory_not_found_error", directoryPath));

            string[] ybnFiles = Directory.GetFiles(directoryPath, "*.ybn", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "cdx_fivem_maps_patcher" +
                                        Path.DirectorySeparatorChar))
                .ToArray();
            if (ybnFiles.Length == 0) return nameToFiles;

            foreach (string filePath in ybnFiles)
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
                    Console.WriteLine(Messages.Get("file_access_error", filePath, ex.Message));
                }

            return nameToFiles
                .Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Messages.Get("duplicate_search_error", ex.Message));
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void PatchYbn(string name, List<string> files)
    {
        Dictionary<uint, RpfFileEntry> ybnDict = GameFileCache.YbnDict;

        uint ybnHash =
            (from entry in GameFileCache.YbnDict
                where entry.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                select entry.Key).FirstOrDefault();

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

        Console.WriteLine(Messages.Get("patching_ybn_message", name));

        // TODO: Implement YBN merging logic here
        // This is where the actual merging/patching logic will go in the future

        Backups.SaveYbn(ServerPath, mainYbn);
    }
}