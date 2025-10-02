using Blazored.LocalStorage;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace OmniMonitor.Client.Services
{
    public enum ThemeMode { Light, Dark }
    
    public class ThemeService
    {
        private const string ThemeKey = "currentThemeMode";
        private readonly ILocalStorageService _localStorage;

        private readonly string _lightModeVars = @"
            --hover: rgba(0, 0, 0, 0.04);
            --seleccion: rgba(0, 0, 0, 0.08);
            --borde-drawer: rgba(0, 0, 0, 0.12);
            --scrollbar-fondo: #F5F5F5;
            --scrollbar-pulgar: #BDBDBD;
            --scrollbar-pulgar-hover: #9E9E9E;
            --fondo-drawer: #BBDEFB;
            --superficie: #FFFFFF;
            --Boton-Usuario: rgba(30, 30, 30, 0.8);
            --text-primary: #212121;
            --text-secondary: rgba(0, 0, 0, 0.6);
            --text-disabled: rgba(0, 0, 0, 0.38);
            --appbar-background: #BBDEFB;
            --appbar-text: #0D47A1;
            --theme-toggle: rgba(30, 30, 30, 0.8);

            --fondo: #E3F2FD;
            --card-color-1: #BBDEFB;
            --textField-color-1: #E3F2FD;
        ";

        private readonly string _darkModeVars = @"
            --hover: rgba(255, 255, 255, 0.08);
            --seleccion: rgba(255, 255, 255, 0.15);
            --borde-drawer: rgba(255, 255, 255, 0.12);
            --scrollbar-fondo: #2D2D2D;
            --scrollbar-pulgar: #555555;
            --scrollbar-pulgar-hover: #777777;
            --fondo-drawer: #1E1E1E;
            --superficie: rgba(30, 30, 30, 0.8);
            --Boton-Usuario: #9AA5CE;
            --text-primary: #C4C6CB;
            --text-secondary: rgba(255, 255, 255, 0.7);
            --text-disabled: rgba(255, 255, 255, 0.38);
            --appbar-background: #1E1E1E;
            --appbar-text: #C4C6CB;
            --theme-toggle: #C4C6CB;

            --fondo: linear-gradient(180deg, #0F1522 0%, #20262F 50%);
            --card-color-1: #030A16;
            --textField-color-1: #020817;
        ";

        public string CurrentThemeVariables => (CurrentMode==ThemeMode.Dark) ? _darkModeVars : _lightModeVars;

        // Evento específico para el cambio de Tema
        public event Action? OnThemeChange;

        

        // Propiedades de estado
        public bool IsDarkMode => CurrentMode == ThemeMode.Dark;
        public MudTheme CurrentTheme { get; }
        public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;



        public ThemeService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            
            // 1. Inicializamos CurrentTheme UNA SOLA VEZ, definiendo AMBAS paletas.
            CurrentTheme = new MudTheme()
            {
                // Paleta para el Modo Claro
                PaletteLight = new PaletteLight()
                {
                    Primary = "#1E88E5",
                    Secondary = "#424242",
                    Background = "#E3F2FD",
                    AppbarBackground = "#BBDEFB",
                    AppbarText = "#0D47A1",
                    DrawerBackground = "#BBDEFB",
                    DrawerText = "#212121",
                    Surface = "#FFFFFF",
                    TextPrimary = "#212121",
                    TextSecondary = "rgba(0, 0, 0, 0.6)",
                    ActionDefault = "#6200EE"
                },
                // Paleta para el Modo Oscuro
                PaletteDark = new PaletteDark()
                {
                    Primary = "#1E88E5",
                    Secondary = "#424242",
                    Background = "#121212",
                    AppbarBackground = "#1E1E1E",
                    AppbarText = "#FFFFFF",
                    DrawerBackground = "#1E1E1E",
                    DrawerText = "#FFFFFF",
                    Surface = "#1E1E1E",
                    TextPrimary = "#FFFFFF",
                    TextSecondary = "rgba(255, 255, 255, 0.7)",
                    ActionDefault = "#BB86FC"
                }
            };
        }

        public async Task InitializeThemeAsync()
        {
            try
            {
                var storedMode = await _localStorage.GetItemAsync<ThemeMode?>(ThemeKey);
                CurrentMode = storedMode.GetValueOrDefault(ThemeMode.Light);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el tema desde LocalStorage: {ex.Message}");
                CurrentMode = ThemeMode.Light;
            }

            NotifyStateChanged();
        }


        public async Task ToggleThemeAsync()
        {
            CurrentMode = IsDarkMode ? ThemeMode.Light : ThemeMode.Dark;

            await _localStorage.SetItemAsync(ThemeKey, CurrentMode);

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnThemeChange?.Invoke();


    }
}