param(
    [ValidateSet('dev', 'test', 'demo-prod')]
    [string]$Environment = 'dev',
    [string]$SecretsFile = 'infrastructure/k8s/secrets.yaml'
)

$ErrorActionPreference = 'Stop'
$namespace = "stb-$Environment"
$overlay = "infrastructure/k8s/overlays/$Environment"

if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    throw 'kubectl est introuvable. Installez kubectl ou activez Kubernetes dans Docker Desktop.'
}

if (-not (Test-Path -LiteralPath $SecretsFile)) {
    throw "Secrets absents. Copiez infrastructure/k8s/secrets.example.yaml vers $SecretsFile et remplacez CHANGE_ME."
}

if (Select-String -LiteralPath $SecretsFile -Pattern 'CHANGE_ME' -Quiet) {
    throw 'Le fichier de secrets contient encore CHANGE_ME. Le déploiement est bloqué par sécurité.'
}

kubectl apply -f "$overlay/namespace.yaml"
kubectl -n $namespace apply -f $SecretsFile
kubectl apply -k $overlay

kubectl -n $namespace rollout status deployment/stb-api --timeout=240s
kubectl -n $namespace rollout status deployment/stb-frontend --timeout=180s
kubectl -n $namespace rollout status deployment/stb-ai-service --timeout=240s

kubectl -n $namespace get pods,services,ingress
