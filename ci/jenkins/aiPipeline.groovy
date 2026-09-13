def call() {
  pipeline {
    agent any
    parameters {
      string(name: 'REGISTRY', defaultValue: 'registry.local/stb-monitoring', description: 'Registre OCI')
      choice(name: 'DEPLOY_ENV', choices: ['none', 'dev', 'test', 'demo-prod'], description: 'Environnement cible')
      booleanParam(name: 'PUSH_IMAGE', defaultValue: false, description: 'Publier image')
    }
    environment { VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT?.take(8) ?: 'local'}" }
    options { timestamps(); disableConcurrentBuilds(); timeout(time: 30, unit: 'MINUTES') }
    stages {
      stage('Checkout') { steps { checkout scm } }
      stage('Secrets') { steps { sh 'docker run --rm -v "$PWD:/repo" zricethezav/gitleaks:v8.24.2 detect --source=/repo --no-banner --redact' } }
      stage('Tests') { steps { dir('ai-service') { sh 'python -m pip install -r requirements.txt && python -m pytest -q' } } }
      stage('Dépendances') { steps { dir('ai-service') { sh 'python -m pip install pip-audit && python -m pip_audit -r requirements.txt' } } }
      stage('Image et scan') { steps { sh '''docker build -t ${REGISTRY}/ai-service:${VERSION} ai-service
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.58.2 image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed ${REGISTRY}/ai-service:${VERSION}''' } }
      stage('Push') { when { expression { params.PUSH_IMAGE } } steps { withCredentials([usernamePassword(credentialsId: 'stb-registry', usernameVariable: 'REGISTRY_USER', passwordVariable: 'REGISTRY_PASSWORD')]) { sh '''echo "$REGISTRY_PASSWORD" | docker login "${REGISTRY%%/*}" -u "$REGISTRY_USER" --password-stdin
docker push ${REGISTRY}/ai-service:${VERSION}
docker logout "${REGISTRY%%/*}"''' } } }
      stage('Deploy') { when { expression { params.DEPLOY_ENV != 'none' } } steps { sh '''kubectl apply -k infrastructure/k8s/overlays/${DEPLOY_ENV}
kubectl -n stb-${DEPLOY_ENV} set image deployment/stb-ai-service ai-service=${REGISTRY}/ai-service:${VERSION}
kubectl -n stb-${DEPLOY_ENV} rollout status deployment/stb-ai-service --timeout=180s''' } }
    }
  }
}
return this
