# RNE Simulator

Simulateur indépendant du Registre National des Entreprises tunisien. Toutes les données sont fictives.

## Démarrage avec la vraie base MongoDB

Depuis la racine du dépôt :

`docker compose -f simulators/docker-compose.rne.yml up -d --build`

Le conteneur `stb-rne-mongodb` héberge la base `rne_simulator` et la collection `companies`. Les trois entreprises fictives sont insérées automatiquement au premier démarrage.

Pour tester une vraie panne de base :

`docker compose -f simulators/docker-compose.rne.yml stop rne-mongodb`

Pour restaurer MongoDB :

`docker compose -f simulators/docker-compose.rne.yml start rne-mongodb`

Endpoint métier recommandé pour STB Sentinel :

`GET http://localhost:5102/api/v1/entreprises/0000012A/situation-juridique`

Validation JSON recommandée : propriété `registreAccessible`, valeur `true`.

Endpoint de disponibilité détaillée :

`GET http://localhost:5102/api/v1/registre/disponibilite`

Scénarios disponibles : `Normal`, `Http500`, `Unavailable`, `HighLatency`, `Timeout`, `DatabaseDown`, `InvalidResponse`, `EmptyResponse`.

Pour activer un scénario, envoyer `X-Lab-Key: change-this-lab-key` à `POST /api/lab/scenario`.
