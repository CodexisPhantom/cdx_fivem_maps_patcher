using cdx_fivem_maps_patcher.Classes;
using cdx_fivem_maps_patcher.Pages;
using cdx_fivem_maps_patcher.Patcher;
using CodeWalker.GameFiles;

const double cacheTime = 60.0;
const long cacheSize = 2L * 1024 * 1024 * 1024; // 2GB Cache
const bool isGen9 = false;
const bool enableMods = false;
const string dlc = "";
const string excludeFolders = "";

string gtaPath = PromptPath(Messages.Get("prompt_gta_path"));
string serverPath = PromptPath(Messages.Get("prompt_server_path"));

GTA5Keys.LoadFromPath(gtaPath);
GameFileCache gameFileCache = new(cacheSize, cacheTime, gtaPath, isGen9, dlc, enableMods, excludeFolders);
gameFileCache.EnableDlc = true;
gameFileCache.Init(
    message => Console.WriteLine($"[GameFileCache] {message}"),
    error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
);

Console.Clear();
Backups backups = new(serverPath);
Patcher patcher = new YmapPatcher(gameFileCache, serverPath);

while (true)
{
    PrintMainMenu();
    string? input;
    do
    {
        input = Console.ReadLine();
    } while (input == null);

    Console.Clear();
    switch (input)
    {
        case "1":
            backups.Show();
            break;
        case "2":
            patcher.Show();
            break;
        case "3":
            Console.WriteLine(Messages.Get("goodbye"));
            return;
        default:
            Console.WriteLine(Messages.Get("invalid_entry"));
            break;
    }
}

void PrintMainMenu()
{
    Console.WriteLine(Messages.Get("main_menu_title"));
    Console.WriteLine(Messages.Get("main_menu_backups"));
    Console.WriteLine(Messages.Get("main_menu_patch"));
    Console.WriteLine(Messages.Get("main_menu_quit"));
}

string PromptPath(string message)
{
    string? path = null;
    while (string.IsNullOrEmpty(path))
    {
        Console.Write(message);
        path = Console.ReadLine();
        if (Directory.Exists(path)) continue;
        Console.WriteLine("Chemin invalide. Veuillez réessayer.");
        path = null;
    }

    Console.WriteLine($"Chemin utilisé : {path}");
    return path;
}