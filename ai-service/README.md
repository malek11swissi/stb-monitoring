# STB Sentinel AI - Prédiction du risque SI

Ce microservice reçoit de l'API .NET l'historique récent des endpoints d'un SI. Il calcule les features temporelles, puis estime la probabilité de panne dans 15, 30 et 60 minutes.

## Fichiers

- `run.py` : démarre Flask sur le port 5055.
- `app/routes/system_risk.py` : contrat HTTP et validation.
- `app/services/system_risk_service.py` : features, modèle Random Forest et explications.
- `tests/test_system_risk.py` : vérifie qu'un scénario dégradé est plus risqué qu'un scénario stable.
- `Dockerfile` : image indépendante du microservice.

## Limite scientifique

La version 1.0 est entraînée sur des scénarios synthétiques reproductibles. Elle sert au laboratoire et à la démonstration PFE. Avant une utilisation réelle STB, le modèle doit être réentraîné et évalué sur un historique réel étiqueté.

## Exécution locale sous Windows

```powershell
cd ai-service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python run.py
```

Le service répond sur `http://localhost:5055`. La plateforme .NET consomme ensuite `POST /api/v1/predictions/system-risk` via la configuration `Ai:BaseUrl`.

## Évaluation scientifique reproductible

Au démarrage, 4 800 scénarios synthétiques servent à l'apprentissage et 1 200 autres,
jamais vus par le modèle, servent exclusivement au test. Dans le groupe d'apprentissage,
1 200 cas de validation choisissent le seuil qui privilégie le rappel des pannes (F2),
avant un réentraînement final sur les 4 800 cas. Le rapport Accuracy, Precision, Recall,
F1, ROC-AUC, seuils et matrices de confusion est consultable ici :

```http
GET http://localhost:5055/api/v1/predictions/system-risk/evaluation
```

Ces métriques valident le comportement académique sur les scénarios synthétiques. Elles
ne doivent pas être présentées comme une validation sur les futurs SI réels de STB.

## Remplacer automatiquement la démonstration par l'historique STB

STB n'a pas à modifier le code. Le contrat d'import est illustré dans
`data/raw/stb_check_history.example.csv`. L'export réel porte le nom
`data/raw/stb_check_history.csv` et contient une ligne par contrôle avec son contexte
alertes/incidents/maintenance.

1. Préparer les fenêtres temporelles et les labels futurs :

```powershell
python training/prepare_dataset.py
```

2. Entraîner, valider chronologiquement et publier les artefacts :

```powershell
python training/train.py --version stb-2026.01
```

3. Redémarrer Flask. Il détecte `models/manifest.json`, vérifie le contrat des 17
features et charge les trois fichiers `.joblib`. En absence d'artefacts validés, il
revient automatiquement au modèle synthétique de démonstration.

`GET /health` et chaque prédiction indiquent `modelSource` : `stb-real` ou `synthetic`.
Les données brutes, datasets préparés et modèles générés sont ignorés par Git afin de
ne pas publier de données STB sensibles. En Docker, `models/` est monté en lecture seule.

## Contrat côté plateforme

Après connexion avec un rôle Admin, Superviseur ou Manager IT :

```http
GET /api/ai/systems/{systemId}/risk
```

La plateforme collecte automatiquement les contrôles des dernières 24 heures, les alertes actives, incidents ouverts et maintenances. Aucun identifiant ou feature ne doit être saisi manuellement.
