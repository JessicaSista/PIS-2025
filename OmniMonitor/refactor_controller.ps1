# PowerShell script to refactor Dataset controllers
# Adds imports, regions, and Language constants

param(
    [string]$ControllerPath
)

$content = Get-Content $ControllerPath -Raw

# Step 1: Add using statement if not present
if ($content -notmatch 'using OmniMonitor\.Server\.Resources;') {
    $content = $content -replace '(using OmniMonitor\.Shared\.Dtos;)', "`$1`r`nusing OmniMonitor.Server.Resources;"
}

# Step 2: Add #region directives
# Find the class opening
$classPattern = '(public class \w+Controller : ControllerBase\s*\{)'
if ($content -match $classPattern) {
    # Add Fields region after class opening brace
    $content = $content -replace '(public class \w+Controller : ControllerBase\s*\{)', "`$1`r`n        #region Fields`r`n"
    
    # Add Constructors region before first constructor
    $content = $content -replace '(\s+)(public \w+Controller\([^)]+\))', "`$1#endregion`r`n`r`n        #region Constructors`r`n`r`n`$1`$2"
    
    # Add Methods region after constructor closes
    $content = $content -replace '(\}\s*\r?\n\s*\[Authorize)', "        }`r`n`r`n        #endregion`r`n`r`n        #region Methods`r`n`r`n        [Authorize"
}

# Step 3: Replace hardcoded strings with Language constants
$replacements = @{
    '"Usuario no encontrado\."' = 'Language.UserNotFound'
    '"ContentType inválido o no soportado"' = 'Language.ContentTypeInvalid'
    '"Módulo no definido"' = 'Language.ModuleNotDefined'
    '"Entidad no definida para el módulo seleccionado"' = 'Language.EntityNotDefined'
}

foreach ($key in $replacements.Keys) {
    $value = $replacements[$key]
    $content = $content -replace $key, $value
}

# Step 4: Replace string interpolation error messages
$content = $content -replace '\$"Error interno al crear el dataset: \{ex\.Message\}"', 'string.Format(Language.DatasetCreateError, ex.Message)'
$content = $content -replace '\$"Error interno al obtener los datasets: \{ex\.Message\}"', 'string.Format(Language.DatasetGetError, ex.Message)'
$content = $content -replace '\$"Error interno al obtener el dataset: \{ex\.Message\}"', 'string.Format(Language.DatasetGetByIdError, ex.Message)'
$content = $content -replace '\$"Error interno al actualizar el dataset: \{ex\.Message\}"', 'string.Format(Language.DatasetUpdateError, ex.Message)'
$content = $content -replace '\$"Error interno al eliminar el dataset: \{ex\.Message\}"', 'string.Format(Language.DatasetDeleteError, ex.Message)'
$content = $content -replace '"El filtro no encontró ninguna noticia\. El dataset no puede crearse sin resultados\."', 'string.Format(Language.FilterNoResults, "noticia")'
$content = $content -replace '"El filtro no encontró ningún evento\. El dataset no puede crearse sin resultados\."', 'string.Format(Language.FilterNoResults, "evento")'
$content = $content -replace '"El filtro no encontró ninguna noticia\. El dataset no puede actualizarse sin resultados\."', 'string.Format(Language.FilterNoResultsUpdate, "noticia")'
$content = $content -replace '"El filtro no encontró ningún evento\. El dataset no puede actualizarse sin resultados\."', 'string.Format(Language.FilterNoResultsUpdate, "evento")'

# Step 5: Add #endregion at end of class before closing brace
$content = $content -replace '(\s+)\}\s*\}\s*$', "`r`n        #endregion`r`n    }`r`n}`r`n"

# Write back to file
Set-Content -Path $ControllerPath -Value $content -NoNewline
Write-Host "Refactoring complete for $ControllerPath"
