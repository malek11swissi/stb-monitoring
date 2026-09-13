def call() {
  pipeline {
    agent any
    parameters {
      string(name: 'REGISTRY', defaultValue: 'registry.local/stb-monitoring', description: 'Registre OCI sans slash final')
      choice(name: 'DEPLOY_ENV', choices: ['none', 'dev', 'test', 'demo-prod'], description: 'Environnement cible')
      booleanParam(name: 'PUSH_IMAGES', defaultValue: false, description: 'Publier les images')
    }
    environment { VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT?.take(8) ?: 'local'}" }
    options { timestamps(); disableConcurrentBuilds(); timeout(time: 45, unit: 'MINUTES'); buildDiscarder(logRotator(numToKeepStr: '20')) }
    stages {
      stage('Checkout') { steps { checkout scm } }
      stage('Secrets') { steps { sh 'docker run --rm -v "$PWD:/repo" zricethezav/gitleaks:v8.24.2 detect --source=/repo --no-banner --redact' } }
      stage('Tests backend') {
        steps {
          sh '''dotnet restore StbMonitoring.sln
docker compose -f infrastructure/docker/docker-compose.yml up -d --wait postgres
dotnet test StbMonitoring.sln -c Release --no-restore --logger "trx"'''
        }
        post {
          always {
            archiveArtifacts allowEmptyArchive: true, artifacts: '**/*.trx'
            sh 'docker compose -f infrastructure/docker/docker-compose.yml stop postgres || true'
          }
        }
      }
      stage('Dépendances backend') { steps { sh 'dotnet list StbMonitoring.sln package --vulnerable --include-transitive' } }
      stage('Frontend') { steps { dir('platform/frontend') { sh 'npm ci && npm audit --audit-level=high && npm run build' } } }
      stage('Images') { steps { sh '''docker build -f platform/backend/Dockerfile -t ${REGISTRY}/api:${VERSION} .
docker build -t ${REGISTRY}/frontend:${VERSION} platform/frontend''' } }
      stage('Scan images') { steps { sh '''docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.58.2 image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed ${REGISTRY}/api:${VERSION}
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.58.2 image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed ${REGISTRY}/frontend:${VERSION}''' } }
      stage('Push') { when { expression { params.PUSH_IMAGES } } steps { withCredentials([usernamePassword(credentialsId: 'stb-registry', usernameVariable: 'REGISTRY_USER', passwordVariable: 'REGISTRY_PASSWORD')]) { sh '''echo "$REGISTRY_PASSWORD" | docker login "${REGISTRY%%/*}" -u "$REGISTRY_USER" --password-stdin
docker push ${REGISTRY}/api:${VERSION}
docker push ${REGISTRY}/frontend:${VERSION}
docker logout "${REGISTRY%%/*}"''' } } }
      stage('Deploy') { when { expression { params.DEPLOY_ENV != 'none' } } steps { sh '''kubectl apply -k infrastructure/k8s/overlays/${DEPLOY_ENV}
kubectl -n stb-${DEPLOY_ENV} set image deployment/stb-api api=${REGISTRY}/api:${VERSION}
kubectl -n stb-${DEPLOY_ENV} set image deployment/stb-frontend frontend=${REGISTRY}/frontend:${VERSION}
kubectl -n stb-${DEPLOY_ENV} rollout status deployment/stb-api --timeout=180s
kubectl -n stb-${DEPLOY_ENV} rollout status deployment/stb-frontend --timeout=180s''' } }
    }
    post { always { archiveArtifacts allowEmptyArchive: true, artifacts: 'platform/frontend/dist/**' } }
  }
}
return this
