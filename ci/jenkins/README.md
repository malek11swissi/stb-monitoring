# Pipelines Jenkins — STB Monitoring

La chaîne conserve cinq jobs. Les noms ci-dessous sont utilisés par le pipeline
orchestrateur et doivent donc être repris exactement dans Jenkins.

| Job Jenkins | Script Path | Rôle |
|---|---|---|
| `stb-monitoring-pipeline` | `ci/jenkins/Jenkinsfile` | Orchestration générale |
| `stb-monitoring-ci` | `ci/jenkins/Jenkinsfile.ci` | Build, tests, SonarQube, images et Nexus |
| `stb-monitoring-cd` | `ci/jenkins/Jenkinsfile.cd` | Promotion d'un tag dans GitOps |
| `stb-monitoring-observability` | `ci/jenkins/Jenkinsfile.observability` | Contrôle Kubernetes et monitoring |
| `stb-monitoring-performance` | `ci/jenkins/Jenkinsfile.performance` | Test k6 manuel |

## Credentials Jenkins

- `nexus-credentials` : identifiant et mot de passe du registre Nexus.
- `sonarqube-token` : token d'analyse du projet SonarQube.
- `git-credentials` : identifiant GitHub et Personal Access Token autorisé à
  pousser le commit GitOps.

L'installation SonarQube déclarée dans **Manage Jenkins > System** doit porter
exactement le nom `stb-sonarqube`. Son webhook doit cibler :

`http://stb-monitoring-jenkins:8080/sonarqube-webhook/`

## Flux de livraison

1. L'orchestrateur calcule un tag unique, par exemple `15-a1b2c3d4`.
2. La CI valide .NET, Angular et Python, puis attend le Quality Gate.
3. La CI construit et publie les trois images avec ce même tag.
4. Après validation humaine, le CD inscrit le tag dans l'overlay `dev` ou
   `prod` et pousse un commit `[gitops]`.
5. Argo CD, une fois installé, synchronise ce commit vers Kubernetes.

Pour tester uniquement la CI avant l'installation de Kubernetes, lancer
`stb-monitoring-pipeline` avec `TARGET_ENV=none` et
`RUN_OBSERVABILITY=false`.
