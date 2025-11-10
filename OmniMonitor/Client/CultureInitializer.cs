using System.Globalization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

public class CultureInitializer
{
    private readonly ILocalStorageService _localStorage;

    public CultureInitializer(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task InitializeCultureAsync()
    {
        // Define the default language if nothing is saved.
        const string DefaultCultureCode = "es";

        // 1. Read the saved culture from Local Storage
        var savedCulture = await _localStorage.GetItemAsStringAsync("culture");
        string cultureCodeToUse = DefaultCultureCode;
        if (string.IsNullOrEmpty(savedCulture) || CultureInfo.GetCultureInfo(savedCulture) == null)
        {
        }
        else
        {
            cultureCodeToUse = savedCulture;
        }

        // 3. If we use the default value, save it for the next session.
        if (string.IsNullOrEmpty(savedCulture) || CultureInfo.GetCultureInfo(savedCulture) == null)
        {
            await _localStorage.SetItemAsStringAsync("culture", cultureCodeToUse);
        }

        // 4. Set the culture globally (apply the language)
        var cultureInfo = new CultureInfo(cultureCodeToUse);
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
}
