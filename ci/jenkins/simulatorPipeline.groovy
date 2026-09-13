def call(Map cfg) {
  pipeline {
    agent any
    parameters {
      string(name: 'REGISTRY', defaultValue: 'registry.local/stb-monitoring', description: 'Registre OCI')
      choice(name: 'DEPLOY_ENV', choices: ['none', 'dev', 'test', 'demo-prod'], description: 'Environnement cible')
      booleanParam(name: 'PUSH_IMAGE', defaultValue: false, description: 'Publier image')
    }
    environment {
      VERSION = "${env.BUILD_NUMBER}-${env.GIT_COMMIT?.take(8) ?: 'local'}"
      COMPONENT = "${cfg.name}"
      PROJECT = "${cfg.project}"
      IMAGE_NAME = "${cfg.image}"
    }
    options { timestamps(); disableConcurrentBuilds(); timeout(time: 30, unit: 'MINUTES') }
    stages {
      stage('Checkout') { steps { checkout scm } }
      stage('Secrets') { steps { sh 'docker run --rm -v "$PWD:/repo" zricethezav/gitleaks:v8.24.2 detect --source=/repo --no-banner --redact' } }
      stage('Build et tests') { steps { sh 'dotnet restore simulators/${PROJECT}/${PROJECT}.csproj && dotnet build simulators/${PROJECT}/${PROJECT}.csproj -c Release --no-restore' } }
      stage('Dépendances') { steps { sh 'dotnet list simulators/${PROJECT}/${PROJECT}.csproj package --vulnerable --include-transitive' } }
      stage('Image et scan') { steps { sh '''docker build -t ${REGISTRY}/${IMAGE_NAME}:${VERSION} simulators/${PROJECT}
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.58.2 image --exit-code 1 --severity CRITICAL,HIGH --ignore-unfixed ${REGISTRY}/${IMAGE_NAME}:${VERSION}''' } }
      stage('Push') { when { expression { params.PUSH_IMAGE } } steps { withCredentials([usernamePassword(credentialsId: 'stb-registry', usernameVariable: 'REGISTRY_USER', passwordVariable: 'REGISTRY_PASSWORD')]) { sh '''echo "$REGISTRY_PASSWORD" | docker login "${REGISTRY%%/*}" -u "$REGISTRY_USER" --password-stdin
docker push ${REGISTRY}/${IMAGE_NAME}:${VERSION}
docker logout "${REGISTRY%%/*}"''' } } }
      stage('Deploy') { when { expression { params.DEPLOY_ENV != 'none' } } steps { sh '''kubectl apply -k infrastructure/k8s/overlays/${DEPLOY_ENV}
kubectl -n stb-${DEPLOY_ENV} set image deployment/${COMPONENT}-simulator simulator=${REGISTRY}/${IMAGE_NAME}:${VERSION}
kubectl -n stb-${DEPLOY_ENV} rollout status deployment/${COMPONENT}-simulator --timeout=240s''' } }
    }
  }
}
return this
