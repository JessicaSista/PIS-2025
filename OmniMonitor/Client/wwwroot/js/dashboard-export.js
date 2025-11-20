window.dashboardExport = {
    downloadDashboard: async function (format, filename) {
        try {
            // Importar html2canvas dinámicamente
            if (!window.html2canvas) {
                await this.loadScript('https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js');
            }

            // Buscar el contenedor del dashboard
            const dashboardElement = document.querySelector('.dashboard');
            if (!dashboardElement) {
                throw new Error('No se encontró el elemento dashboard');
            }

            // Detectar tema actual
            const isDarkTheme = document.body.classList.contains('dark-theme');
            const backgroundColor = isDarkTheme ? '#0F1522' : '#ffffff';

            // Configurar opciones para html2canvas
            const options = {
                useCORS: true,
                scale: 3, // Alta resolución para mejor calidad
                backgroundColor: backgroundColor, // Fondo según el tema actual
                removeContainer: false,
                logging: false,
                allowTaint: false,
                foreignObjectRendering: false,
                imageTimeout: 15000,
                ignoreElements: function(element) {
                    // Ignorar elementos de scroll y otros elementos de UI innecesarios
                    return element.classList.contains('mud-overlay') || 
                           element.classList.contains('mud-popover') ||
                           element.classList.contains('mud-menu') ||
                           element.tagName === 'SCRIPT';
                },
                onclone: function(clonedDocument, element) {
                    // Aplicar el mismo fondo que tiene el dashboard original
                    const originalBody = document.body;
                    const clonedBody = clonedDocument.body;
                    
                    // Copiar estilos de fondo del body original
                    const bodyStyles = window.getComputedStyle(originalBody);
                    clonedBody.style.background = bodyStyles.background;
                    clonedBody.style.backgroundColor = bodyStyles.backgroundColor;
                    clonedBody.style.backgroundImage = bodyStyles.backgroundImage;
                    clonedBody.style.backgroundSize = bodyStyles.backgroundSize;
                    clonedBody.style.backgroundRepeat = bodyStyles.backgroundRepeat;
                    clonedBody.style.backgroundAttachment = bodyStyles.backgroundAttachment;
                    
                    // Limpiar estilos del dashboard clonado
                    const clonedElement = clonedDocument.querySelector('.dashboard');
                    if (clonedElement) {
                        clonedElement.style.padding = '10px';
                        clonedElement.style.margin = '0';
                        clonedElement.style.border = 'none';
                        clonedElement.style.boxShadow = 'none';
                        clonedElement.style.backgroundColor = 'transparent'; // Transparente para mostrar el fondo del body
                        
                        // Ocultar botón de "Agregar nueva gráfica"
                        const addButtons = clonedElement.querySelectorAll('.add-card-widget');
                        addButtons.forEach(btn => {
                            btn.style.display = 'none';
                        });
                        
                        // Optimizar tarjetas para la captura
                        const cards = clonedElement.querySelectorAll('.mud-grid-item');
                        cards.forEach(card => {
                            card.style.border = 'none';
                            card.style.boxShadow = 'none';
                        });
                    }
                }
            };

            // Capturar el elemento como canvas
            const canvas = await html2canvas(dashboardElement, options);

            if (format === 'pdf') {
                await this.downloadAsPDF(canvas, filename);
            } else if (format === 'jpg') {
                this.downloadAsJPG(canvas, filename);
            }

        } catch (error) {
            console.error('Error al descargar dashboard:', error);
            throw error;
        }
    },

    downloadAsJPG: function (canvas, filename) {
        // Convertir canvas a blob JPG con alta calidad
        canvas.toBlob(function(blob) {
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename + '.jpg';
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }, 'image/jpeg', 0.95); // Alta calidad
    },

    downloadAsPDF: async function (canvas, filename) {
        try {
            // Importar jsPDF dinámicamente
            if (!window.jsPDF) {
                await this.loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
            }

            const { jsPDF } = window.jspdf;
            
            // Obtener dimensiones del canvas
            const imgData = canvas.toDataURL('image/jpeg', 0.95);
            const imgWidth = canvas.width;
            const imgHeight = canvas.height;

            // Determinar orientación basada en las proporciones
            const isLandscape = imgWidth > imgHeight;
            
            // Configurar dimensiones del PDF
            const pdfWidth = isLandscape ? 297 : 210; // A4 en mm
            const pdfHeight = isLandscape ? 210 : 297;
            
            // Calcular escala manteniendo proporción y minimizando bordes
            const mmPerPx = 0.264583; // conversión px a mm
            const maxWidth = pdfWidth - 10; // margen mínimo de 5mm por lado
            const maxHeight = pdfHeight - 10; // margen mínimo de 5mm arriba/abajo
            
            const scaleX = maxWidth / (imgWidth * mmPerPx);
            const scaleY = maxHeight / (imgHeight * mmPerPx);
            const scale = Math.min(scaleX, scaleY);
            
            const finalWidth = (imgWidth * mmPerPx) * scale;
            const finalHeight = (imgHeight * mmPerPx) * scale;

            // Crear PDF
            const orientation = isLandscape ? 'landscape' : 'portrait';
            const pdf = new jsPDF(orientation, 'mm', 'a4');
            
            // Centrar imagen en el PDF
            const x = (pdfWidth - finalWidth) / 2;
            const y = (pdfHeight - finalHeight) / 2;
            
            pdf.addImage(imgData, 'JPEG', x, y, finalWidth, finalHeight);
            pdf.save(filename + '.pdf');

        } catch (error) {
            console.error('Error al generar PDF:', error);
            throw error;
        }
    },

    loadScript: function (src) {
        return new Promise((resolve, reject) => {
            // Verificar si el script ya está cargado
            if (document.querySelector(`script[src="${src}"]`)) {
                resolve();
                return;
            }
            
            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }
};