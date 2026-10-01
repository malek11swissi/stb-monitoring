# STB Monitoring — démarrage DevOps et GitOps

Ce guide décrit l'ordre de démonstration local : Jenkins construit et publie,
le CD inscrit la version dans Git, Argo CD synchronise et Kubernetes exécute.

## 1. Usine logicielle locale

Dans PowerShell, à la racine du projet :

```powershell
docker context use desktop-linux
docker compose -f infrastructure/docker/docker-compose.yml --profile devops up -d --build jenkins nexus sonarqube
docker compose -f infrastructure/docker/docker-compose.yml --profile monitoring up -d prometheus grafana blackbox-exporter
docker compose -f infrastructure/docker/docker-compose.yml --profile devops --profile monitoring ps
```

Interfaces :

| Service | URL |
|---|---|
| Jenkins | http://localhost:8088 |
| Nexus | http://localhost:8083 |
| SonarQube | http://localhost:9000 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

## 2. Jobs Jenkins

Créer cinq jobs **Pipeline script from SCM**. Dépôt :
`https://github.com/malek11swissi/stb-monitoring.git`, branche `*/main`.

| Job | Script Path |
|---|---|
| `stb-monitoring-pipeline` | `ci/jenkins/Jenkinsfile` |
| `stb-monitoring-ci` | `ci/jenkins/Jenkinsfile.ci` |
| `stb-monitoring-cd` | `ci/jenkins/Jenkinsfile.cd` |
| `stb-monitoring-observability` | `ci/jenkins/Jenkinsfile.observability` |
| `stb-monitoring-performance` | `ci/jenkins/Jenkinsfile.performance` |

Credentials globales requises : `nexus-credentials`, `sonarqube-token` et
`git-credentials`. La configuration SonarQube Jenkins s'appelle
`stb-sonarqube`.

Premier test de l'orchestrateur :

```text
TARGET_ENV        = none
PUBLISH_IMAGES    = true
RUN_OBSERVABILITY = false
```

Noter le tag produit, par exemple `15-a1b2c3d4`, et vérifier les trois images
dans Nexus.

## 3. Cluster Kubernetes Docker Desktop

Dans Docker Desktop, ouvrir **Kubernetes**, créer/activer le cluster et attendre
son état `Running`. Puis :

```powershell
kubectl config get-contexts
kubectl config use-context docker-desktop
kubectl get nodes
```

Le nœud doit être `Ready`.

## 4. Secrets de développement

Créer d'abord le namespace :

```powershell
kubectl apply -f infrastructure/k8s/overlays/dev/namespace.yaml
```

Remplacer les valeurs d'exemple avant d'exécuter :

```powershell
kubectl create secret generic stb-database-env -n stb-dev `
  --from-literal=POSTGRES_DB=stb_monitoring `
  --from-literal=POSTGRES_USER=stb_admin `
  --from-literal=POSTGRES_PASSWORD='CHANGE_ME'

kubectl create secret generic stb-secrets -n stb-dev `
  --from-literal=postgres-connection='Host=stb-postgres;Port=5432;Database=stb_monitoring;Username=stb_admin;Password=CHANGE_ME' `
  --from-literal=jwt-key='CHANGE_ME_MINIMUM_32_RANDOM_CHARACTERS' `
  --from-literal=smtp-username='CHANGE_ME' `
  --from-literal=smtp-sender='CHANGE_ME' `
  --from-literal=smtp-password='CHANGE_ME'
```

Ne jamais enregistrer les vraies valeurs dans Git.

## 5. Promotion de la première version

Lancer `stb-monitoring-cd` avec :

```text
IMAGE_TAG    = tag créé par la CI
ENVIRONMENT  = dev
PUSH_GITOPS  = true
```

Le job pousse un commit `[gitops]` modifiant l'overlay `dev`.

## 6. Installation Argo CD

```powershell
kubectl create namespace argocd
kubectl apply -n argocd --server-side --force-conflicts `
  -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl wait --for=condition=Available deployment/argocd-server -n argocd --timeout=300s
```

Afficher le mot de passe initial dans PowerShell :

```powershell
$encodedPassword = kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encodedPassword))
```

Garder ce terminal ouvert :

```powershell
kubectl port-forward svc/argocd-server -n argocd 8089:443
```

Ouvrir `https://localhost:8089`, accepter le certificat local et utiliser
`admin` avec le mot de passe affiché.

Si le dépôt GitHub est privé, ajouter ses credentials dans **Settings >
Repositories** avant de créer les applications.

## 7. Applications Argo CD

```powershell
kubectl apply -f infrastructure/gitops/applications/stb-monitoring-dev.yaml
kubectl apply -f infrastructure/gitops/applications/stb-monitoring-prod.yaml
kubectl get applications -n argocd
```

`stb-monitoring-dev` se synchronise automatiquement. `stb-monitoring-prod`
demande volontairement un clic manuel sur **Sync**.

Dans Argo CD, le développement doit finir en `Synced` et `Healthy`.

## 8. Vérifications Kubernetes

```powershell
kubectl get all -n stb-dev
kubectl get pods -n stb-dev -w
kubectl logs -n stb-dev deployment/stb-api
kubectl describe pod -n stb-dev
```

Si un Pod affiche `ImagePullBackOff`, consulter ses événements avec
`kubectl describe pod`. Une erreur HTTPS contre le Nexus HTTP signifie que le
registre doit être exposé en HTTPS avant de poursuivre ; ne pas modifier le
Docker Engine au hasard.

## 9. Ouvrir l'application et Swagger

Application complète (le Nginx frontend transmet `/api` au Service Kubernetes
`api`) :

```powershell
kubectl port-forward -n stb-dev service/stb-frontend 4201:8080
```

Ouvrir http://localhost:4201.

Swagger dans un second terminal :

```powershell
kubectl port-forward -n stb-dev service/stb-api 5042:8080
```

Ouvrir http://localhost:5042/swagger.

## 10. Démonstration d'une nouvelle version

1. Pousser une modification applicative dans GitHub.
2. Lancer `stb-monitoring-pipeline` avec `TARGET_ENV=dev`.
3. La CI teste, analyse avec SonarQube et publie dans Nexus.
4. Confirmer **Promouvoir** dans Jenkins.
5. Le CD pousse le nouveau tag dans Git.
6. Argo CD passe brièvement à `OutOfSync`, puis `Synced`.
7. Kubernetes effectue le Rolling Update.
8. Vérifier les Pods et recharger http://localhost:4201.

Le job d'observabilité ne doit être activé dans l'orchestrateur qu'après avoir
fourni au conteneur Jenkins un kubeconfig autorisé à lire le cluster.
