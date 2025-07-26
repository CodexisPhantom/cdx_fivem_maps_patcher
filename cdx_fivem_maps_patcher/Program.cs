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

string gtaPath = "E:\\Games\\Rockstar\\Grand Theft Auto V Legacy"; //PromptPath(Messages.Get("prompt_gta_path"));
string serverPath = "E:\\Projects\\Starling\\Fivem\\Server\\resources\\[streamings]"; //PromptPath(Messages.Get("prompt_server_path"));

GTA5Keys.LoadFromPath(gtaPath);
GameFileCache gameFileCache = new(cacheSize, cacheTime, gtaPath, isGen9, dlc, enableMods, excludeFolders)
    {
        EnableDlc = true
    };
gameFileCache.Init(
    message => Console.WriteLine($"[GameFileCache] {message}"),
    error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
);

Console.Clear();
Backups backups = new(serverPath);
Patcher ymapPatcher = new YmapPatcher(gameFileCache, serverPath);
Patcher ybnPatcher = new YbnPatcher(gameFileCache, serverPath);
Patcher ydrPatcher = new YdrPatcher(gameFileCache, serverPath);
Translations translations = new();

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
            ymapPatcher.Show();
            break;
        case "3":
            ybnPatcher.Show();
            break;
        case "4":
            ydrPatcher.Show();
            break;
        case "5":
            translations.Show();
            break;
        case "6":
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
    Console.WriteLine(Messages.Get("main_menu_patch_ymap"));
    Console.WriteLine(Messages.Get("main_menu_patch_ybn"));
    Console.WriteLine(Messages.Get("main_menu_patch_ydr"));
    Console.WriteLine(Messages.Get("main_menu_translations"));
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
        Console.WriteLine(Messages.Get("invalid_path"));
        path = null;
    }

    Console.WriteLine(Messages.Get("path_used", path));
    return path;
}