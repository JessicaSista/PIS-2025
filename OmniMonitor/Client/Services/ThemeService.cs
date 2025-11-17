using Blazored.LocalStorage;
using MudBlazor;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace OmniMonitor.Client.Services
{
    public enum ThemeMode { Light, Dark }
    
    public class ThemeService
    {
        private const string ThemeKey = "currentThemeMode";
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _jsRuntime;


        // Evento específico para el cambio de Tema
        public event Action? OnThemeChange;
        // Propiedades de estado
        public bool IsDarkMode => CurrentMode == ThemeMode.Dark;
        public MudTheme CurrentTheme { get; }
        public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;



        public ThemeService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
        {
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;

            // Inicializamos CurrentTheme definiendo AMBAS paletas
            CurrentTheme = new MudTheme()
            {
                // Paleta para el Modo Claro
                PaletteLight = new PaletteLight()
                {
                    AppbarBackground = "#BBDEFB",
                    AppbarText = "#0D47A1",
                    DrawerBackground = "#BBDEFB",
                    DrawerText = "#212121",
                    TextPrimary = "#212121",
                    TextSecondary = "rgba(0, 0, 0, 0.6)",
                    Primary = "#1E88E5",
                    Secondary = "#424242",
                    Error = "#8F1515",
                },
                // Paleta para el Modo Oscuro
                PaletteDark = new PaletteDark()
                {
                    AppbarBackground = "#1A1818",
                    AppbarText = "#FFFFFF",
                    DrawerBackground = "#1A1818",
                    DrawerText = "#B7BBB2",
                    TextPrimary = "#FFFFFF",
                    TextSecondary = "91A2B7",
                    Primary = "#4A81E9",
                    Secondary = "#C2C2C3",
                    Error = "#8F1515",
                }
            };
        }

        public async Task InitializeThemeAsync()
        {
            try
            {
                var storedMode = await _localStorage.GetItemAsync<ThemeMode?>(ThemeKey);
                CurrentMode = storedMode.GetValueOrDefault(ThemeMode.Dark);
            }
            catch (Exception ex)
            {
                CurrentMode = ThemeMode.Dark;
            }
            await _jsRuntime.InvokeVoidAsync("themeController.setThemeClass", CurrentMode.ToString());

            NotifyStateChanged();
        }


        public async Task ToggleThemeAsync()
        {
            CurrentMode = IsDarkMode ? ThemeMode.Light : ThemeMode.Dark;

            await _localStorage.SetItemAsync(ThemeKey, CurrentMode);

            await _jsRuntime.InvokeVoidAsync("themeController.setThemeClass", CurrentMode.ToString());

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnThemeChange?.Invoke();


    }
}
