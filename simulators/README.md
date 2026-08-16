# Simulateurs des SI supervisés par STB Sentinel

Chaque SI est un microservice indépendant avec sa propre base. Les API exposées sont des API métier ; les contrôles HTTP, HTTPS, JSON, latence et TLS sont exécutés par STB Sentinel.

| SI | Base | Port | Endpoint métier conseillé | Propriété JSON attendue |
|---|---|---:|---|---|
| Core Banking | Oracle | 5101 | `/api/v1/comptes/TN5901000000000012345678/position` | `coreBankingAccessible=true` |
| RNE | MongoDB | 5102 | `/api/v1/entreprises/1234567A/situation-juridique` | `registreAccessible=true` |
| SMS | MySQL | 5103 | `/api/v1/messages/SMS-STB-001/statut-livraison` | `serviceSmsAccessible=true` |
| RH | SQL Server | 5104 | `/api/v1/employes/STB-TECH-001/situation-professionnelle` | `donneesRhAccessibles=true` |

## Démarrage

```powershell
docker compose -f simulators/docker-compose.core-banking.yml up -d --build
docker compose -f simulators/docker-compose.rne.yml up -d --build
docker compose -f simulators/docker-compose.sms.yml up -d --build
docker compose -f simulators/docker-compose.hr.yml up -d --build
```

Le premier téléchargement d'Oracle peut être long. Si STB Sentinel s'exécute dans Docker, remplacez `localhost` par `host.docker.internal` dans les URLs des endpoints.

Oracle est publié sur le port hôte `1522` pour éviter les conflits avec une installation Oracle locale utilisant déjà `1521`. Dans le réseau Docker, le Core Banking continue à joindre Oracle sur `core-oracle:1521`.

## Test d'une panne réelle de base

Arrêter uniquement la base concernée :

```powershell
docker stop stb-core-oracle
docker stop stb-rne-mongodb
docker stop stb-sms-mysql
docker stop stb-hr-sqlserver
```

L'API métier correspondante répond alors `503` et le contrôle STB Sentinel devient `DOWN`. Restaurer avec `docker start <nom-du-conteneur>`.

Les endpoints `/api/lab/scenario` permettent aussi de simuler temporairement `Http500`, `Unavailable`, `HighLatency`, `Timeout`, `DatabaseDown` ou `InvalidResponse`. Ils sont protégés par l'en-tête `X-Lab-Key`.
