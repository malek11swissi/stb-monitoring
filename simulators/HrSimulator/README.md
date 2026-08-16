# SI RH Simulator

Microservice RH indépendant utilisant une vraie base SQL Server. Les API exposées sont métier ; STB Sentinel exécute lui-même les contrôles HTTP, HTTPS, JSON et TLS.

Endpoint recommandé : `GET /api/v1/employes/STB-TECH-001/situation-professionnelle` avec validation JSON `donneesRhAccessibles = true`.

Démarrage : `docker compose -f simulators/docker-compose.hr.yml up -d --build`.

Panne réelle : `docker compose -f simulators/docker-compose.hr.yml stop hr-sqlserver`.
