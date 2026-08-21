# Guide du code — STB Sentinel

Ce document explique où se trouve chaque responsabilité. Les dossiers `bin`, `obj`, `node_modules` et les fichiers EF Core `*.Designer.cs` sont générés automatiquement et ne doivent pas être documentés ou modifiés manuellement.

## Lecture rapide de l'architecture

1. Angular affiche les pages et appelle l'API HTTP.
2. Les contrôleurs API vérifient l'identité et délèguent les cas d'utilisation.
3. La couche Application applique les règles métier et orchestre les traitements.
4. Domain contient les entités, états et transitions autorisées.
5. Infrastructure parle à PostgreSQL, aux SI surveillés, à Brevo et à Twilio.
6. Les workers exécutent périodiquement supervision, SLA, escalades et nettoyage.

## Frontend Angular

### Démarrage et structure

| Fichier | Utilité |
|---|---|
| `src/main.ts` | Démarre l'application Angular standalone. |
| `src/index.html` | Document HTML hôte dans lequel Angular est monté. |
| `src/styles.css` | Charte graphique globale, états, formulaires, tableaux et responsive. |
| `src/app/app.config.ts` | Configure routeur, HTTP et intercepteur JWT. |
| `src/app/app.routes.ts` | Associe URL, composant, guard et permission requise. |
| `src/app/app.component.ts` | Logique de la coquille principale et navigation selon le rôle. |
| `src/app/app.component.html` | Sidebar, en-tête, identité connectée et zone `router-outlet`. |
| `src/app/app.component.css` | Mise en page et adaptation responsive de la coquille. |
| `src/app/app.component.spec.ts` | Vérifie que le composant racine peut être créé. |

### Noyau frontend

| Fichier | Utilité |
|---|---|
| `core/auth.service.ts` | Connexion, déconnexion, JWT, utilisateur courant et matrice de permissions. |
| `core/auth.interceptor.ts` | Ajoute `Authorization: Bearer` aux requêtes HTTP. |
| `core/auth.guard.ts` | Bloque les routes non authentifiées ou interdites au rôle. |
| `core/monitoring.service.ts` | Modèles et appels API des SI, endpoints et résultats de contrôle. |
| `core/operations.service.ts` | Appels API des alertes, incidents, SLA, notifications, maintenances et rapports. |

### Pages fonctionnelles

| Fichier | Utilité |
|---|---|
| `features/login.component.ts` | Écran moderne de connexion et gestion du retour après login. |
| `features/forbidden.component.ts` | Explique qu'une page est interdite au rôle courant. |
| `features/dashboard.component.ts` | Affiche santé, disponibilité, MTTD, MTTA, MTTR, SLA et exports. |
| `features/users.component.ts` | CRUD fonctionnel des utilisateurs et rôles fixes. |
| `features/profile.component.ts` | Profil, mot de passe et préférences e-mail/SMS. |
| `features/systems.component.ts` | Catalogue, filtres, création, modification et archivage des SI. |
| `features/system-detail.component.ts` | Endpoints, contrôles manuels, historique et preuve TLS d'un SI. |
| `features/alerts.component.ts` | Liste des alertes et administration des règles standards. |
| `features/alert-detail.component.ts` | Occurrences d'une alerte et création/ouverture de l'incident. |
| `features/incidents.component.ts` | Liste, filtres avancés, vues sauvegardées et création d'incident. |
| `features/incident-detail.component.ts` | Affectation, transitions, commentaires, preuves et résolution. |
| `features/sla.component.ts` | Paramétrage des délais de réponse/résolution par priorité. |
| `features/notifications.component.ts` | Flux personnel lu/non lu et navigation vers l'objet concerné. |
| `features/calendar.component.ts` | Calendrier jour/semaine/mois et CRUD des maintenances. |
| `features/audit.component.ts` | Consultation et filtrage des actions sensibles. |

## Backend — Domain

### Entités persistées

| Fichier | Utilité métier |
|---|---|
| `Entities/User.cs` | Compte, rôle fixe, activation et profil. |
| `Entities/AuditLog.cs` | Trace immuable d'une action et de son succès/échec. |
| `Entities/MonitoredSystem.cs` | SI STB, environnement, criticité et état agrégé. |
| `Entities/MonitoringEndpoint.cs` | Configuration HTTP/API/TLS et dernier état du contrôle. |
| `Entities/CheckResult.cs` | Preuve historisée d'une exécution technique. |
| `Entities/AlertRule.cs` | Seuil d'échecs, sévérité, déduplication et résolution automatique. |
| `Entities/Alert.cs` | Anomalie confirmée, compteur, acquittement et clôture. |
| `Entities/AlertOccurrence.cs` | Lien entre une répétition d'alerte et son résultat technique. |
| `Entities/Incident.cs` | Cycle de traitement, affectation, SLA et informations de résolution. |
| `Entities/IncidentComment.cs` | Commentaire fonctionnel ou technique sur un incident. |
| `Entities/IncidentHistory.cs` | Chronologie avant/après de chaque changement d'incident. |
| `Entities/IncidentAttachment.cs` | Pièce jointe ou preuve de résolution, limitée à 10 Mo. |
| `Entities/IncidentEscalation.cs` | Niveau et prochaine échéance d'une escalade persistante. |
| `Entities/SlaPolicy.cs` | Délais de réponse et résolution associés à une priorité. |
| `Entities/Notification.cs` | Notification interne et état lu/non lu. |
| `Entities/UserNotificationPreference.cs` | Canaux choisis, téléphone et délai d'escalade. |
| `Entities/SavedView.cs` | Filtres avancés sauvegardés en JSON. |
| `Entities/MaintenanceWindow.cs` | Période planifiée pendant laquelle les alertes peuvent être suspendues. |
| `Entities/MonitoringStatus.cs` | Énumérations de supervision, environnement, criticité et type de contrôle. |

### Énumérations et constantes

| Fichier | Utilité |
|---|---|
| `Enums/AlertEventType.cs` | Catégorise DOWN, DEGRADED, timeout, validation et erreurs TLS. |
| `Enums/AlertSeverity.cs` | Définit Info, Warning, Minor, Major et Critical. |
| `Enums/AlertStatus.cs` | Cycle Open, Acknowledged, Resolved et Closed. |
| `Enums/IncidentCategory.cs` | Classe la cause fonctionnelle de l'incident. |
| `Enums/IncidentPriority.cs` | Définit P1 à P4 et pilote le SLA. |
| `Enums/IncidentStatus.cs` | États autorisés du cycle d'incident. |
| `Enums/SlaStatus.cs` | États OnTrack, AtRisk, Breached et Met. |
| `Constants/RoleNames.cs` | Rôles fixes et normalisation de leur écriture. |
| `Constants/PermissionNames.cs` | Permissions utilisées par les politiques API et le frontend. |

## Backend — Application

### Contrats

| Fichier | Utilité |
|---|---|
| `Contracts/IdentityContracts.cs` | Requêtes/réponses de login, profil et utilisateurs. |
| `Contracts/MonitoringContracts.cs` | Requêtes/réponses des SI, endpoints et contrôles. |
| `Contracts/OperationsContracts.cs` | Requêtes/réponses des alertes, incidents, SLA et notifications. |

### Interfaces

| Fichier | Utilité |
|---|---|
| `Interfaces/IIdentityServices.cs` | Cas d'utilisation d'authentification et comptes. |
| `Interfaces/IIdentityStore.cs` | Opérations de persistance nécessaires à l'identité. |
| `Interfaces/ISecurityServices.cs` | Abstractions de hachage des mots de passe et création JWT. |
| `Interfaces/IMonitoringServices.cs` | Supervision, stockage et exécuteurs de contrôles. |
| `Interfaces/IOperationsServices.cs` | Alertes, incidents, SLA, notifications et stockage associé. |
| `Interfaces/INotificationChannel.cs` | Contrat commun d'envoi e-mail et SMS. |
| `Interfaces/IIncidentNotificationDispatcher.cs` | Déclenchement métier des notifications d'incident critique. |

### Services

| Fichier | Utilité |
|---|---|
| `Services/IdentityServices.cs` | Login, profil, mot de passe, comptes et audit. |
| `Services/MonitoringService.cs` | CRUD des SI/endpoints et chaîne complète d'un contrôle. |
| `Services/OperationsService.cs` | Déduplication, alertes, incidents, corrélation, SLA et notifications. |
| `Services/OperationsService.IncidentUpdates.cs` | Modification d'un incident avec recalcul du SLA et historique. |

## Backend — Infrastructure

| Fichier | Utilité |
|---|---|
| `Persistence/MonitoringDbContext.cs` | Mapping des 18 tables, relations, index et suppressions EF Core. |
| `Persistence/IdentityStore.cs` | Requêtes PostgreSQL des utilisateurs et audits. |
| `Persistence/MonitoringStore.cs` | Requêtes PostgreSQL des SI, endpoints et résultats. |
| `Persistence/OperationsStore.cs` | Requêtes des règles, alertes, incidents, SLA et notifications. |
| `Persistence/DatabaseSeeder.cs` | Crée les comptes, SI, endpoints, règles et SLA initiaux. |
| `Authentication/PasswordService.cs` | Hache et vérifie les mots de passe avec BCrypt. |
| `Monitoring/HttpCheckExecutors.cs` | Exécute HTTP, API JSON et validation complète TLS. |
| `Notifications/ApiNotificationChannel.cs` | Appelle Brevo/Twilio ou simule les envois. |
| `Notifications/IncidentNotificationDispatcher.cs` | Notifie le technicien P1 et initialise l'escalade. |
| `Persistence/Migrations/*.cs` | Évolution générée du schéma PostgreSQL ; ne pas éditer manuellement. |

## Backend — API

### Démarrage et composants transversaux

| Fichier | Utilité |
|---|---|
| `Program.cs` | Configure DI, JWT, autorisations, CORS, EF Core, workers et routes. |
| `Security/JwtTokenService.cs` | Produit un JWT signé contenant identité et rôle. |
| `Middleware/ApiExceptionHandler.cs` | Transforme les exceptions métier en réponses HTTP cohérentes. |
| `Reporting/ReportFileBuilder.cs` | Construit les fichiers PDF, Excel `.xlsx` et CSV. |

### Contrôleurs REST

| Fichier | Utilité |
|---|---|
| `Controllers/AuthController.cs` | Login, refresh, profil et changement de mot de passe. |
| `Controllers/UsersController.cs` | Administration des utilisateurs. |
| `Controllers/SystemsController.cs` | CRUD SI/endpoints, contrôles et téléchargement des preuves. |
| `Controllers/AlertRulesController.cs` | CRUD et activation des règles d'alerte. |
| `Controllers/AlertsController.cs` | Liste, détail, acquittement, résolution et clôture des alertes. |
| `Controllers/IncidentsController.cs` | Cycle incident, affectation, commentaires et pièces jointes. |
| `Controllers/OperationsController.cs` | Résumé, SLA et notifications internes. |
| `Controllers/MaintenanceWindowsController.cs` | CRUD, calendrier et annulation des maintenances. |
| `Controllers/UserPreferencesController.cs` | Préférences notification et vues sauvegardées. |
| `Controllers/NotificationChannelsController.cs` | État et test des canaux Brevo/Twilio. |
| `Controllers/ReportingController.cs` | KPI, rapports mensuels/annuels et exports. |
| `Controllers/AuditController.cs` | Consultation sécurisée des audits. |

### Workers

| Fichier | Utilité |
|---|---|
| `Workers/MonitoringWorker.cs` | Lance les contrôles planifiés avec verrou PostgreSQL distribué. |
| `Workers/SlaWorker.cs` | Recalcule les SLA des incidents ouverts. |
| `Workers/IncidentEscalationWorker.cs` | Notifie successivement superviseur puis manager IT. |
| `Workers/AuditCleanupWorker.cs` | Supprime les audits dépassant la durée de rétention. |

## Simulateurs de SI

| Fichier | Utilité |
|---|---|
| `CoreBankingSimulator/Program.cs` | Simule le Core Banking et sa dépendance Oracle. |
| `RneSimulator/Program.cs` | Expose les API métier simulées du RNE. |
| `RneSimulator/Data/MongoSettings.cs` | Configuration de connexion MongoDB. |
| `RneSimulator/Data/CompanyRepository.cs` | Accès aux entreprises fictives MongoDB. |
| `RneSimulator/Models/Company.cs` | Document MongoDB d'une entreprise. |
| `SmsSimulator/Program.cs` | Simule l'envoi/état SMS et sa dépendance MySQL. |
| `HrSimulator/Program.cs` | Simule le SI RH et ses routes métier. |
| `HrSimulator/Data/HrDbContext.cs` | Mapping SQL Server du simulateur RH. |
| `HrSimulator/Models/Employee.cs` | Employé fictif du SI RH. |
| `HrSimulator/Models/Department.cs` | Département fictif du SI RH. |
| `simulators/docker-compose.*.yml` | Démarre séparément chaque SI et sa base dédiée. |
| `*/Dockerfile` | Construit l'image du microservice simulateur. |
| `*/appsettings.json` | Paramètres locaux et chaînes de connexion du simulateur. |

## Tests

| Fichier | Utilité |
|---|---|
| `SprintOneTests.cs` | Règles utilisateurs, rôles et identité. |
| `SprintTwoTests.cs` | Catalogue des SI et endpoints. |
| `SprintThreeTests.cs` | États de monitoring et calculs de supervision. |
| `UserTests.cs` | Comportements unitaires de l'entité User. |
| `MaintenanceWindowTests.cs` | Validation des périodes de maintenance. |
| `IncidentLifecycleTests.cs` | Transitions autorisées et résolution d'incident. |
| `ArchitectureTests.cs` | Vérifie les dépendances entre couches. |
| `AuthorizationTests.cs` | Vérifie les droits HTTP selon le rôle. |
| `PostgreSqlIntegrationTests.cs` | Vérifie migrations, persistance et cycle complet avec PostgreSQL. |

## Déploiement local

| Fichier | Utilité |
|---|---|
| `StbMonitoring.sln` | Regroupe backend, simulateurs et projets de tests .NET. |
| `global.json` | Fixe la version du SDK .NET utilisée. |
| `NuGet.Config` | Configure les sources et le cache des packages NuGet. |
| `package.json` / `package-lock.json` | Scripts et versions exactes des dépendances frontend. |
| `infrastructure/docker/docker-compose.yml` | Démarre PostgreSQL et les dépendances locales de la plateforme. |
| `README.md` | Commandes principales d'installation, démarrage et test. |
