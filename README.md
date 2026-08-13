# stb-monitoring
Plateforme proactive de supervision et de gestion des incidents des écosystèmes bancaires.


> Projet de fin d’études réalisé dans un contexte académique.  
> Les systèmes, données et scénarios utilisés sont entièrement simulés et ne représentent pas les systèmes réels de la STB.

## Objectif

STB Monitoring permet de surveiller la disponibilité et les performances de plusieurs systèmes afin de détecter les anomalies avant qu’elles ne provoquent des réclamations clients.

La plateforme assure notamment :

- la supervision HTTP et HTTPS ;
- le contrôle des API ;
- la vérification des certificats TLS ;
- le contrôle optionnel des bases de données ;
- la surveillance des conteneurs ;
- la détection des états `UP`, `DEGRADED` et `DOWN` ;
- la gestion des alertes et des incidents ;
- l’envoi de notifications ;
- le calcul des SLA et KPI ;
- la génération de rapports mensuels et annuels ;
- la simulation d’anomalies ;
- l’observabilité avec Prometheus et Grafana.

## Architecture

La solution comprend :

- une application web Angular avec Tailwind CSS ;
- une plateforme centrale .NET 8 organisée en monolithe modulaire ;
- un worker .NET 8 pour l’exécution des contrôles planifiés ;
- une base PostgreSQL centrale ;
- RabbitMQ pour certains traitements asynchrones ;
- quatre microservices simulant des systèmes externes ;
- Docker et Kubernetes pour le déploiement ;
- Jenkins et GitHub pour l’intégration et le déploiement continus.

## Systèmes simulés

| Microservice | Responsabilité simulée | Base de données |
|---|---|---|
| Core Banking Simulator | Consultation de comptes bancaires fictifs | Oracle |
| RNE Simulator | Consultation d’entreprises fictives | MongoDB |
| SMS Orange Simulator | Envoi et suivi de SMS fictifs | MySQL |
| RH Simulator | Consultation d’employés fictifs | SQL Server |

Chaque simulateur fournit :

- une API REST .NET 8 ;
- un endpoint `/health` ;
- un endpoint `/metrics` ;
- un mécanisme de simulation des anomalies ;
- une base de données indépendante.

## Technologies

### Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- xUnit
- Moq
- FluentAssertions

### Frontend

- Angular
- TypeScript
- Tailwind CSS
- RxJS

### Bases des simulateurs

- Oracle
- MongoDB
- MySQL
- SQL Server

### DevOps et observabilité

- GitHub
- Jenkins
- Docker
- Kubernetes
- Prometheus
- Grafana
- SonarQube
