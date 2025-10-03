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
        // Define el idioma por defecto si no hay nada guardado.
        const string DefaultCultureCode = "es";

        // 1. Lee la cultura guardada en el Local Storage
        var savedCulture = await _localStorage.GetItemAsStringAsync("culture");
        string cultureCodeToUse = DefaultCultureCode;

        // 2. Si no hay nada guardado o es inválido, usa el valor por defecto.
        if (string.IsNullOrEmpty(savedCulture) || CultureInfo.GetCultureInfo(savedCulture) == null)
        {
        }
        else
        {
            cultureCodeToUse = savedCulture;
        }

        // 3. Si usamos el valor por defecto, lo guardamos para la próxima sesión.
        if (string.IsNullOrEmpty(savedCulture) || CultureInfo.GetCultureInfo(savedCulture) == null)
        {
            await _localStorage.SetItemAsStringAsync("culture", cultureCodeToUse);
        }

        // 4. Establece la cultura globalmente (aplica el idioma)
        var cultureInfo = new CultureInfo(cultureCodeToUse);
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
}
