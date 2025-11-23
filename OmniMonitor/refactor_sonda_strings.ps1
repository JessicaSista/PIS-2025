# Sonda Services String Refactoring Script
# Replaces hardcoded API error messages with Language constants

Write-Host "Starting Sonda Services String Refactoring..." -ForegroundColor Green

$basePath = "c:\Users\ivanr\OneDrive\Documents\PIS\OmniMonitor\Server\Services"

# Define all files to process
$files = @(
    "$basePath\SondaUMService.cs",
    "$basePath\SondaIMService.cs",
    "$basePath\SondaEMService.cs",
    "$basePath\SondaAMService.cs"
)

# Define replacements: [original -> replacement]
$replacements = @(
    @{
        Find = 'throw new Exception(Language.ApiUnauthorized);'
        Replace = 'throw new Exception(Language.ApiUnauthorized);'
    },
    @{
        Find = 'throw new Exception("No tienes permisos para acceder a este recurso (403 Forbidden).");'
        Replace = 'throw new Exception(Language.ApiForbidden);'
    },
    @{
        Find = 'throw new Exception("La respuesta de la API está vacía.");'
        Replace = 'throw new Exception(Language.ApiResponseEmpty);'
    },
    @{
        Find = '"La respuesta de la API no es JSON válido. Respuesta: " + responseBody'
        Replace = 'string.Format(Language.ApiResponseInvalidJson, responseBody)'
    },
    @{
        Find = 'throw new Exception("No se encontraron eventos (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "eventos"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron noticias (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "noticias"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron devices (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "devices"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron sources (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "sources"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron sensors (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "sensors"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron assets (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "assets"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron bundles (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "bundles"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron linked assets (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "linked assets"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron stocksParameters  (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "stocksParameters"));'
    },
    @{
        Find = 'throw new Exception("No se encontro historia para el asset (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "historia del asset"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron event task instances (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "event task instances"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron EventTaskActions (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "EventTaskActions"));'
    },
    @{
        Find = 'throw new Exception("No se encontraron stocks para el taskInstanceId proporcionado (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "stocks"));'
    },
    @{
        Find = 'throw new Exception("No se encontro informacion (404 NotFound).");'
        Replace = 'throw new Exception(string.Format(Language.ApiNotFound, "información"));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''bundleId'' debe ser mayor que cero.", nameof(bundleId));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "bundleId"), nameof(bundleId));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''stockId'' debe ser mayor que cero.", nameof(stockId));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "stockId"), nameof(stockId));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''taskInstanceId'' debe ser mayor que cero.", nameof(taskInstanceId));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "taskInstanceId"), nameof(taskInstanceId));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''page'' debe ser mayor que cero.", nameof(page));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "page"), nameof(page));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''pageSize'' debe ser mayor que cero.", nameof(pageSize));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterMustBePositive, "pageSize"), nameof(pageSize));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''page'' es requerido.", nameof(page));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterRequired, "page"), nameof(page));'
    },
    @{
        Find = 'throw new ArgumentException("El parámetro ''pageSize'' es requerido.", nameof(pageSize));'
        Replace = 'throw new ArgumentException(string.Format(Language.ParameterRequired, "pageSize"), nameof(pageSize));'
    },
    @{
        Find = '"AssetNotFound"'
        Replace = 'Language.AssetNotFound'
    },
    @{
        Find = 'throw new Exception("Error al deserializar la respuesta de la API: JSON inválido.");'
        Replace = 'throw new Exception(Language.DeserializationError);'
    }
)

$totalReplacements = 0

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "`nProcessing: $file" -ForegroundColor Cyan
        $content = Get-Content -Path $file -Raw -Encoding UTF8
        $fileChanged = $false
        
        foreach ($replacement in $replacements) {
            $originalContent = $content
            $content = $content.Replace($replacement.Find, $replacement.Replace)
            
            if ($originalContent -ne $content) {
                $count = ([regex]::Matches($originalContent, [regex]::Escape($replacement.Find))).Count
                Write-Host "  ✓ Replaced $count occurrence(s): $($replacement.Find.Substring(0, [Math]::Min(60, $replacement.Find.Length)))..." -ForegroundColor Yellow
                $totalReplacements += $count
                $fileChanged = $true
            }
        }
        
        if ($fileChanged) {
            Set-Content -Path $file -Value $content -Encoding UTF8 -NoNewline
            Write-Host "  ✓ File updated successfully" -ForegroundColor Green
        } else {
            Write-Host "  - No changes needed" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ✗ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "Refactoring Complete!" -ForegroundColor Green
Write-Host "Total replacements made: $totalReplacements" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Green
