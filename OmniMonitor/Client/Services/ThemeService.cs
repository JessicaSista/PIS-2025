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
            --superficie: #FFFFFF;
            --Boton-Usuario: rgba(30, 30, 30, 0.8);
            --text-primary: #212121;
            --text-secondary: rgba(0, 0, 0, 0.6);
            --text-disabled: rgba(0, 0, 0, 0.38);
            --theme-toggle: rgba(30, 30, 30, 0.8);



            //Usar estas variables
            //Para llamar las variables en CSS: var(--nombre-variable) ejemplo: color: var(--textColor1);


            --fondo: #E3F2FD;
            --card-color-1: #BBDEFB;
            --textField-color-1: #E3F2FD;
            --textColor-1: #E1E6E7;
            --textColor-2: #647984;
            --textColor-3: #C2C6C0;
            --textColor-4: #949FD0;
            --textColor-5: #F47F7F;
            --textColor-6: #91A2B7;
            --WindowColor-1: #BBDEFB;
            --PrimaryButton: #4A81E9;
            --SecondaryButton: #C2C2C3;
            --ErrorButton: #8F1515;
            
            --appbar-background: #BBDEFB;
            --appbar-text: #0D47A1;
            --drawer-background: #BBDEFB;
            --drawer-text: #212121;
            
            --fondo-drawer: #BBDEFB;

            --card: #f2f9ff;
            --card-text-primary: #212121;
            --card-text-secondary: #546E7A;
            --action-button: #028DFF;
            --action-button-text: #FFFFFF;
        ";

        private readonly string _darkModeVars = @"
            --hover: rgba(255, 255, 255, 0.08);
            --seleccion: rgba(255, 255, 255, 0.15);
            --borde-drawer: rgba(255, 255, 255, 0.12);
            --scrollbar-fondo: #2D2D2D;
            --scrollbar-pulgar: #555555;
            --scrollbar-pulgar-hover: #777777;
            --superficie: rgba(30, 30, 30, 0.8);
            --Boton-Usuario: #9AA5CE;
            --text-primary: #C4C6CB;
            --text-secondary: rgba(255, 255, 255, 0.7);
            --text-disabled: rgba(255, 255, 255, 0.38);
            --theme-toggle: #C4C6CB;
            

            //Usar estas variables
            //Para llamar las variables en CSS: var(--nombre-variable) ejemplo: color: var(--textColor1);

            --fondo: linear-gradient(180deg, #0F1522 0%, #20262F 50%);
            --card-color-1: #030A16;
            --textField-color-1: #020817;
            --textColor-1: #E1E6E7;
            --textColor-2: #647984;
            --textColor-3: #C2C6C0;
            --textColor-4: #949FD0;
            --textColor-5: #F47F7F;
            --textColor-6: #91A2B7;
            --WindowColor-1: #0F172A;
            --PrimaryButton: #4A81E9;
            --SecondaryButton: #C2C2C3;
            --ErrorButton: #8F1515;

            --appbar-background: #1A1818;
            --appbar-text: #FFFFFF;
            --drawer-background: #1A1818;
            --drawer-text: #B7BBB2;

            --card: #030A16;
            --card-text-primary: #BCC3C5;
            --card-text-secondary: #69818D;
            --action-button: #FFFFFF;
            --action-button-text: #000000;
            
            --fondo-drawer: #1A1818;
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
                    AppbarBackground = "#BBDEFB",
                    AppbarText = "#0D47A1",
                    DrawerBackground = "#BBDEFB",
                    DrawerText = "#212121",
                    TextPrimary = "#212121",
                    TextSecondary = "rgba(0, 0, 0, 0.6)",
                    Primary = "#1E88E5",
                    Secondary = "#424242",
                    Error = "#8F1515",

                    
                    //Surface = "#FFFFFF",
                    //ActionDefault = "#6200EE"
                    //Background = "#E3F2FD",
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

                    //Surface = "#1E1E1E",
                    //ActionDefault = "#BB86FC"
                    //Background = "#121212",
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