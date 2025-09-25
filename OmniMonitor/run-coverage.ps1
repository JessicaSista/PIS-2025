# Script simplificado para ejecutar pruebas con cobertura de código
Write-Host "Ejecutando pruebas con cobertura..." -ForegroundColor Green

# Limpiar reportes anteriores
if (Test-Path "./CoverageReports") {
    Remove-Item "./CoverageReports" -Recurse -Force
}

# Ejecutar pruebas con cobertura
dotnet test ./Tests/QA.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --results-directory "./CoverageReports" `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -eq 0) {
    # Buscar archivo de cobertura
    $coverageFile = Get-ChildItem -Path "./CoverageReports" -Filter "*.xml" -Recurse | Select-Object -First 1
    
    if ($coverageFile) {
        # Mostrar resumen de cobertura
        $coverageXml = [xml](Get-Content $coverageFile.FullName)
        $lineRate = [math]::Round([decimal]$coverageXml.coverage.'line-rate' * 100, 2)
        $branchRate = [math]::Round([decimal]$coverageXml.coverage.'branch-rate' * 100, 2)
        $linesCovered = $coverageXml.coverage.'lines-covered'
        $linesValid = $coverageXml.coverage.'lines-valid'
        $branchesCovered = $coverageXml.coverage.'branches-covered'
        $branchesValid = $coverageXml.coverage.'branches-valid'
        
        Write-Host "`nCOBERTURA DE CODIGO:" -ForegroundColor Cyan
        Write-Host "=================================================" -ForegroundColor Gray
        Write-Host "Lineas:  $lineRate% ($linesCovered/$linesValid)" -ForegroundColor White
        Write-Host "Ramas:   $branchRate% ($branchesCovered/$branchesValid)" -ForegroundColor White
        Write-Host "=================================================" -ForegroundColor Gray
        
        # Evaluacion de calidad
        if ($lineRate -ge 80) {
            Write-Host "Excelente cobertura!" -ForegroundColor Green
        } elseif ($lineRate -ge 60) {
            Write-Host "Cobertura mejorable" -ForegroundColor Yellow
        } else {
            Write-Host "Cobertura insuficiente" -ForegroundColor Red
        }
    } else {
        Write-Host "No se encontro archivo de cobertura" -ForegroundColor Red
    }
    
    # Limpiar carpeta de reportes al finalizar
    if (Test-Path "./CoverageReports") {
        Remove-Item "./CoverageReports" -Recurse -Force
    }
} else {
    Write-Host "Las pruebas fallaron" -ForegroundColor Red

    # Limpiar carpeta de reportes al finalizar
    if (Test-Path "./CoverageReports") {
        Remove-Item "./CoverageReports" -Recurse -Force
    }

    exit 1
}