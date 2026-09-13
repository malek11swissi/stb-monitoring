/* Pipeline globale : elle orchestre les jobs spécialisés et arrête la chaîne au premier échec. */
pipeline {
  agent any
  options { timestamps(); disableConcurrentBuilds(); buildDiscarder(logRotator(numToKeepStr: '20')) }
  parameters {
    booleanParam(name: 'DEPLOY', defaultValue: false, description: 'Lancer le CD après une CI réussie')
    booleanParam(name: 'RUN_PERFORMANCE', defaultValue: false, description: 'Lancer le test k6 après le déploiement')
    booleanParam(name: 'CHECK_MONITORING', defaultValue: false, description: 'Valider Prometheus et Grafana')
    string(name: 'IMAGE_TAG', defaultValue: '', description: 'Version à déployer; vide = version produite par la CI')
  }
  stages {
    stage('Intégration continue') {
      steps {
        build job: 'stb-monitoring-ci', wait: true, propagate: true,
          parameters: [booleanParam(name: 'PUBLISH_IMAGES', value: params.DEPLOY)]
      }
    }
    stage('Qualité SonarQube') {
      steps { build job: 'stb-monitoring-sonar', wait: true, propagate: true }
    }
    stage('Déploiement') {
      when { expression { params.DEPLOY } }
      steps {
        script { if (!params.IMAGE_TAG?.trim()) { error('IMAGE_TAG est obligatoire lorsque DEPLOY=true.') } }
        build job: 'stb-monitoring-cd', wait: true, propagate: true,
          parameters: [string(name: 'IMAGE_TAG', value: params.IMAGE_TAG)]
      }
    }
    stage('Performance') {
      when { expression { params.DEPLOY && params.RUN_PERFORMANCE } }
      steps { build job: 'stb-monitoring-performance', wait: true, propagate: true }
    }
    stage('Observabilité') {
      when { expression { params.DEPLOY && params.CHECK_MONITORING } }
      steps { build job: 'stb-monitoring-observability', wait: true, propagate: true }
    }
  }
  post {
    success { echo 'Chaîne STB Monitoring validée.' }
    failure { echo 'Chaîne interrompue : ouvrir le job en échec pour consulter les logs.' }
  }
}
