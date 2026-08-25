param(
    [string]$ApiUrl = "http://localhost:5041",
    [string]$Username = "admin",
    [string]$Password = "ChangeMe123!"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$compose = Join-Path $root "simulators/docker-compose.rne.yml"
function Title([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }
function Call([string]$method,[string]$url,$body=$null) {
    $args = @{ Method=$method; Uri=$url; Headers=$headers; ContentType="application/json" }
    if ($null -ne $body) { $args.Body = ($body | ConvertTo-Json -Depth 6) }
    Invoke-RestMethod @args
}

Title "1/6 - Démarrage du RNE et de MongoDB"
docker compose -f $compose up -d --build
$login = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/auth/login" -ContentType "application/json" -Body (@{usernameOrEmail=$Username;password=$Password}|ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.token)" }
$systems = Call Get "$ApiUrl/api/systems?includeArchived=false"
$rne = $systems | Where-Object { $_.code -match "RNE" -or $_.name -match "RNE" } | Select-Object -First 1
if (-not $rne) { throw "Ajoutez d'abord le SI RNE et ses endpoints dans STB Sentinel." }
$detail = Call Get "$ApiUrl/api/systems/$($rne.id)"
$business = $detail.endpoints | Where-Object { $_.checkType -ne "Tls" -and $_.isActive } | Select-Object -First 1
if (-not $business) { throw "Aucun endpoint métier RNE actif trouvé." }

Title "2/6 - Vérification initiale UP"
Call Post "$ApiUrl/api/systems/endpoints/$($business.id)/execute" @{} | Format-Table endpointName,status,durationMs

try {
    Title "3/6 - Arrêt contrôlé de MongoDB"
    docker stop stb-rne-mongodb
    Start-Sleep -Seconds 5
    Title "4/6 - Deux contrôles pour confirmer DOWN et créer une alerte"
    1..2 | ForEach-Object { Call Post "$ApiUrl/api/systems/endpoints/$($business.id)/execute" @{} | Format-Table endpointName,status,errorType; Start-Sleep -Seconds 2 }
    $alerts = Call Get "$ApiUrl/api/alerts"
    $alerts | Where-Object endpointId -eq $business.id | Select-Object -First 3 | Format-Table alertNumber,severity,status,occurrenceCount
}
finally {
    Title "5/6 - Restauration de MongoDB"
    docker start stb-rne-mongodb
    Start-Sleep -Seconds 15
}

Title "6/6 - Nouveau contrôle et retour UP"
Call Post "$ApiUrl/api/systems/endpoints/$($business.id)/execute" @{} | Format-Table endpointName,status,durationMs
Write-Host "Démonstration terminée : UP -> DB DOWN -> alerte -> restauration -> UP." -ForegroundColor Green
