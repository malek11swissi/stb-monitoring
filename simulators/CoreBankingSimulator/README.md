# Simulateur Core Banking STB

Microservice pédagogique exposant des API métier bancaires et utilisant une base Oracle indépendante. Les données sont fictives. STB Sentinel contrôle les API par HTTP/HTTPS, JSON, latence et TLS.

Contrôle conseillé : `GET /api/v1/comptes/TN5901000000000012345678/position`, code `200`, propriété `coreBankingAccessible=true`.

## Démonstration primaire / secours Oracle

Le fichier `docker-compose.core-banking-ha.yml` démarre deux instances Oracle
et un simulateur sur `http://localhost:5301`. Le simulateur essaie la primaire,
puis utilise la secondaire si la connexion primaire échoue.

```powershell
docker compose -f simulators/docker-compose.core-banking-ha.yml up -d --build
docker compose -f simulators/docker-compose.core-banking-ha.yml ps
```

Disponibilité et rôle réellement utilisé :
`GET http://localhost:5301/api/v1/core/disponibilite`.

Dans STB Monitoring, déclarer le groupe `core-oracle-lab` :

- primaire : `localhost:1531` ;
- secours : `localhost:1532`.

Pour tester la bascule :

```powershell
docker compose -f simulators/docker-compose.core-banking-ha.yml stop core-oracle-primary
docker compose -f simulators/docker-compose.core-banking-ha.yml start core-oracle-primary
```

Ce laboratoire démontre une bascule de connexion. Il ne configure pas Oracle
Data Guard : les deux instances reçoivent le même jeu de données initial, mais
les écritures ultérieures ne sont pas répliquées automatiquement. Ne pas
présenter ce mécanisme comme une réplication Oracle de production.
