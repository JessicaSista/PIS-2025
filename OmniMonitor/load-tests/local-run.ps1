Param(
  [string]$Script = 'smoke',
  [string]$BaseUrl = 'http://localhost:5000',
  [string]$User = 'admin',
  [string]$Pass = 'Secret123'
)

Write-Host "Ejecutando k6 script: $Script"

$env:BASE_URL = $BaseUrl
$env:LOGIN_USER = $User
$env:LOGIN_PASS = $Pass

k6 run "$(Resolve-Path "$PSScriptRoot\$Script.js")"
