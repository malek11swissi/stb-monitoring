"""Valide le schéma, les artefacts et les métriques avant utilisation par Flask."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import joblib

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from app.model_schema import FEATURE_NAMES, HORIZONS


# Seuils minimaux du prototype. Ils privilégient le rappel: rater une panne
# est plus coûteux qu'afficher un faux positif à faire confirmer humainement.
MINIMUMS = {"recall": 0.75, "f1Score": 0.60, "rocAuc": 0.80}


def validate(model_dir: Path) -> list[str]:
    errors: list[str] = []
    try:
        manifest = json.loads((model_dir / "manifest.json").read_text(encoding="utf-8"))
        evaluation = json.loads((model_dir / "evaluation.json").read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"Fichiers de métadonnées invalides: {exc}"]
    if manifest.get("status") != "validated":
        errors.append("Le manifest n'a pas le statut validated.")
    if manifest.get("featureNames") != list(FEATURE_NAMES):
        errors.append("Le schéma de features du manifest est incompatible.")
    for horizon in HORIZONS:
        key = str(horizon)
        try:
            artifact_path = model_dir / manifest["artifacts"][key]
            artifact = joblib.load(artifact_path)
            if artifact.get("featureNames") != list(FEATURE_NAMES):
                errors.append(f"Horizon {horizon}: schéma de l'artefact incompatible.")
            metrics = evaluation["horizons"][key]
            for metric, minimum in MINIMUMS.items():
                if float(metrics[metric]) < minimum:
                    errors.append(f"Horizon {horizon}: {metric}={metrics[metric]} < {minimum}.")
        except (OSError, KeyError, TypeError, ValueError) as exc:
            errors.append(f"Horizon {horizon}: artefact ou métriques invalides ({exc}).")
    return errors


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--models", default="models")
    args = parser.parse_args()
    validation_errors = validate(Path(args.models))
    if validation_errors:
        raise SystemExit("Validation refusée:\n- " + "\n- ".join(validation_errors))
    print("Modèle validé: schéma, artefacts et seuils métriques conformes.")
