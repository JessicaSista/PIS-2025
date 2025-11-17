window.themeController = {
    /**
     * Alterna la clase 'dark-mode' en el body del documento.
     * @param {string} mode - El tema actual ('Light' o 'Dark').
     */
    setThemeClass: function (mode) {
        const isDark = mode === "Dark";
        const body = document.body;

        if (isDark) {
            body.classList.add('dark-mode');
        } else {
            body.classList.remove('dark-mode');
        }

        // Desactiva temporalmente las transiciones CSS para evitar un efecto de transición lento 
        // cuando se aplica la clase por primera vez, asegurando un cambio instantáneo.
        body.style.transition = 'none';
        setTimeout(() => {
            body.style.transition = '';
        }, 50);
    }
};
