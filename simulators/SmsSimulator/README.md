# Simulateur SMS STB

Microservice pédagogique exposant des API métier de suivi SMS et utilisant une base MySQL indépendante. STB Sentinel contrôle ces API par HTTP/HTTPS, JSON, latence et TLS.

Contrôle conseillé : `GET /api/v1/messages/SMS-STB-001/statut-livraison`, code `200`, propriété `serviceSmsAccessible=true`.

## Laboratoire haute disponibilité

`docker-compose.sms-ha.yml` déploie trois instances MySQL 8, un InnoDB Cluster en mode Primary unique, MySQL Router et l'API SMS. L'API se connecte uniquement au port écriture du Router (`6446`) : après la panne du Primary, le cluster élit un nouveau Primary et Router y dirige les nouvelles connexions.

```powershell
docker compose -f simulators/docker-compose.sms-ha.yml up -d --build
docker compose -f simulators/docker-compose.sms-ha.yml ps
docker logs stb-sms-cluster-init
```

API HA : `http://localhost:5303`. Diagnostic : `GET /api/v1/database/status`.

Le service `sms-cluster-init` doit terminer avec `Exited (0)` : cela signifie que l'initialisation est réussie. Les trois volumes MySQL conservent les données.
