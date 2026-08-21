param([string]$Password = "ChangeMe_Lab_TLS_123!")
$ErrorActionPreference = "Stop"
$certificateDirectory = Join-Path $PSScriptRoot "certificates"
New-Item -ItemType Directory -Force -Path $certificateDirectory | Out-Null
$certificatePath = Join-Path $certificateDirectory "stb-lab.pfx"
dotnet dev-certs https --clean
dotnet dev-certs https --trust
dotnet dev-certs https -ep $certificatePath -p $Password
Write-Host "Certificat créé : $certificatePath"
Write-Host "Ports HTTPS : Core 7101, RNE 7102, SMS 7103, RH 7104"
