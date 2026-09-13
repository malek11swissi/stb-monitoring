# Guide DevOps local — STB Monitoring

## Architecture Jenkins

| Job | Jenkinsfile | Déclenchement |
|---|---|---|
| `stb-monitoring-pipeline` | `Jenkinsfile` | Manuel, orchestration globale |
| `stb-monitoring-ci` | `ci/jenkins/Jenkinsfile.ci` | Push ou Pull Request |
| `stb-monitoring-sonar` | `ci/jenkins/Jenkinsfile.sonar` | Appelé par la pipeline globale |
| `stb-monitoring-cd` | `ci/jenkins/Jenkinsfile.cd` | Manuel, version Nexus obligatoire |
| `stb-monitoring-performance` | `ci/jenkins/Jenkinsfile.performance` | Manuel et optionnel |
| `stb-monitoring-observability` | `ci/jenkins/Jenkinsfile.observability` | Après déploiement |

Les sous-pipelines sont des stages internes, pas des jobs supplémentaires.

## Démarrer les interfaces

Docker Desktop doit être démarré avant les commandes suivantes.

```powershell
# Usine logicielle : Jenkins, SonarQube et Nexus
docker compose -f infrastructure/docker/docker-compose.yml --profile devops up -d --build

# Prometheus, Blackbox Exporter et Grafana
docker compose -f infrastructure/docker/docker-compose.yml --profile monitoring up -d

# Application principale
docker compose -f infrastructure/docker/docker-compose.yml up -d --build
```

| Interface | URL | Compte initial |
|---|---|---|
| Jenkins | http://localhost:8088 | Mot de passe initial du conteneur |
| SonarQube | http://localhost:9000 | `admin` / `admin` |
| Nexus | http://localhost:8081 | `admin` / mot de passe du conteneur |
| Prometheus | http://localhost:9090 | Aucun |
| Grafana | http://localhost:3000 | `admin` / `admin` |
| Frontend | http://localhost:4200 | Comptes STB Monitoring |
| API | http://localhost:5041/health | Aucun pour `/health` |
| IA | http://localhost:5055/health | Aucun pour `/health` |

```powershell
# Mot de passe initial Jenkins
docker exec stb-monitoring-jenkins cat /var/jenkins_home/secrets/initialAdminPassword

# Mot de passe initial Nexus
docker exec stb-monitoring-nexus cat /nexus-data/admin.password
```

## Première configuration de Jenkins

1. Ouvrir `http://localhost:8088` et coller le mot de passe initial.
2. Choisir **Install suggested plugins**. Les plugins Pipeline, Git, GitHub,
   Credentials Binding, SonarQube et Docker Pipeline sont déjà demandés par
   l'image Jenkins du projet.
3. Créer le compte administrateur local.
4. Ouvrir **Administrer Jenkins > Credentials > System > Global credentials**.
5. Créer les credentials suivants :

| ID exact | Type Jenkins | Contenu |
|---|---|---|
| `github-credentials` | Username/password ou PAT | Accès au dépôt privé |
| `sonarqube-token` | Secret text | Token créé dans SonarQube |
| `nexus-credentials` | Username with password | Compte autorisé sur le registre Nexus |
| `postgres-credentials` | Username with password | `stb_admin` et mot de passe PostgreSQL |
| `gmail-smtp-credentials` | Username with password | Adresse Gmail et mot de passe d'application |
| `jwt-secret` | Secret text | Clé JWT de 32 caractères minimum |

Le credential `ai-service-secret` sera ajouté uniquement lorsque
l'authentification interne du service IA sera implémentée.

## Configurer SonarQube

1. Ouvrir `http://localhost:9000` et changer le mot de passe initial.
2. Ouvrir **My Account > Security** et générer un token nommé `jenkins`.
3. Enregistrer ce token dans Jenkins avec l'ID `sonarqube-token`.
4. Dans Jenkins, ouvrir **Administrer Jenkins > System > SonarQube servers**.
5. Ajouter un serveur nommé exactement `stb-sonarqube` avec l'URL
   `http://sonarqube:9000` et sélectionner le token.
6. Dans SonarQube, configurer le webhook :
   `http://jenkins:8080/sonarqube-webhook/`.

## Configurer Nexus

1. Ouvrir `http://localhost:8081` et se connecter comme `admin`.
2. Créer un repository **docker (hosted)** nommé `stb-docker-hosted`.
3. Lui affecter le connecteur HTTP `8082`.
4. Créer un utilisateur Jenkins autorisé à lire et écrire dans ce repository.
5. Enregistrer ce compte dans Jenkins avec l'ID `nexus-credentials`.

## Créer un job Pipeline from SCM

Répéter les étapes pour chacun des six jobs :

1. Tableau de bord Jenkins > **Nouveau Item**.
2. Saisir le nom exact du job.
3. Choisir **Pipeline**, puis **OK**.
4. Dans **Pipeline**, sélectionner **Pipeline script from SCM**.
5. SCM : **Git**.
6. Repository URL : URL du dépôt GitHub STB Monitoring.
7. Credentials : `github-credentials` si le dépôt est privé.
8. Branche : `*/main`, ou la branche réelle du projet.
9. Renseigner le **Script Path** selon le tableau au début du guide.
10. Enregistrer puis utiliser **Build Now**.

Pour `stb-monitoring-ci`, activer ensuite le déclenchement GitHub ou
**Poll SCM**. Le CD reste manuel.

## Ordre du premier test

1. Lancer `stb-monitoring-ci` avec `PUBLISH_IMAGES=false`.
2. Corriger les éventuels tests ou builds.
3. Configurer SonarQube et lancer `stb-monitoring-sonar`.
4. Configurer le repository Docker Nexus.
5. Relancer la CI avec `PUBLISH_IMAGES=true`.
6. Copier le tag produit par la CI.
7. Lancer `stb-monitoring-cd` avec ce tag.
8. Lancer `stb-monitoring-observability`.
9. Lancer `stb-monitoring-performance` uniquement quand l'API est stable.
10. Lancer enfin `stb-monitoring-pipeline` pour démontrer l'orchestration.

## Kubernetes

Kubernetes constitue la phase suivante. Il ne doit être branché au CD qu'après
validation complète de Docker Compose, Nexus et des smoke tests.
