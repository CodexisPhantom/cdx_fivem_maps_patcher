using cdx_fivem_maps_patcher.Classes;

namespace cdx_fivem_maps_patcher.Pages;

public class Translations : Page
{
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
                    ChangeLanguage("en");
                    break;
                case "2":
                    ChangeLanguage("fr");
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine(Messages.Get("invalid_entry"));
                    break;
            }
        }
    }
    
    private static void PrintMenu()
    {
        Console.WriteLine(Messages.Get("main_menu_title"));
        Console.WriteLine(Messages.Get("translations_change_language_en"));
        Console.WriteLine(Messages.Get("translations_change_language_fr"));
        Console.WriteLine(Messages.Get("translations_menu_return"));
    }
    
    private static void ChangeLanguage(string languageCode)
    {
        Messages.Lang = languageCode;
        Console.WriteLine(Messages.Get("language_changed", languageCode));
    }
}