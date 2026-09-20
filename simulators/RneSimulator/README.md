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
# Démonstration MongoDB haute disponibilité (isolée)

Le fichier `simulators/docker-compose.rne-ha.yml` démarre un RNE de démonstration
sur `http://localhost:5202`, avec une base Mongo principale, une base de secours
et un arbiter qui vote mais ne contient aucune donnée métier. Les volumes de
`docker-compose.rne.yml` ne sont ni réutilisés ni modifiés : les anciennes
données RNE ne sont **pas** copiées automatiquement vers ce laboratoire.

Depuis la racine du dépôt :

```powershell
docker compose -f simulators/docker-compose.rne-ha.yml up -d --build
docker compose -f simulators/docker-compose.rne-ha.yml ps
```

Attendre que `mongo-init` se termine avec succès, puis ouvrir
`http://localhost:5202/api/v1/registre/disponibilite`. Pour démontrer une panne
du primaire initial, arrêter uniquement `rne-mongo-1` :

```powershell
docker compose -f simulators/docker-compose.rne-ha.yml stop rne-mongo-1
docker compose -f simulators/docker-compose.rne-ha.yml start rne-mongo-1
```

Ne pas utiliser `down -v` : cela supprimerait les données du laboratoire.
Après le retour de l'ancienne instance, MongoDB peut la garder comme secondaire ;
le rôle actuel se lit dans le contrôle et ne se déduit pas du nom du conteneur.

Dans STB Monitoring, ajouter deux endpoints `Database` au SI RNE, groupe `rs0` :
`localhost:27018` et `localhost:27019` si l'API de monitoring tourne directement
sur Windows, ou `host.docker.internal:27018` et `:27019` si elle tourne dans
Docker Desktop. Les rôles observés par MongoDB peuvent changer. Le contrôle
Mongo vérifie le rôle et l'appartenance au replica set ; le **retard de
réplication est estimé par l'écart d'`optime` du secondaire, sans garantie de
zéro perte. La commande `replSetGetStatus` nécessite un compte autorisé :
configurer `Monitoring:Mongo:Username` et `Monitoring:Mongo:Password` dans
`appsettings.Local.json` (non suivi par Git) avec les identifiants du labo.
Sans ces droits, l'interface affiche « écart non vérifié » ; le contrôle de
disponibilité et le rôle observé restent utilisables. Les autres moteurs (Oracle, SQL Server,
MySQL) n'ont pour l'instant qu'un contrôle TCP, explicitement signalé comme tel.

Avant d'enregistrer ces endpoints dans STB Monitoring, sauvegarder PostgreSQL
puis redémarrer l'API avec le nouveau build. Au démarrage, l'application ajoute
les six colonnes facultatives manquantes à `monitoring_endpoints` sans supprimer
les SI, endpoints ou résultats existants. Sur une base créée avec `EnsureCreated`,
ne pas lancer `dotnet ef database update` : l'historique des migrations EF est
absent et les anciennes migrations seraient rejouées.
Une alerte est créée uniquement si une règle d'alerte correspondante est active.

Cette installation sur une seule machine démontre la panne d'un conteneur,
pas la panne du PC ou de Docker Desktop. Les mots de passe par défaut sont
réservés au laboratoire local ; ils ne doivent pas servir en production.
