"""Entraîne explicitement le modèle de démonstration et sauvegarde ses artefacts."""
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from app.services.system_risk_service import SystemRiskPredictor


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--models", default="models")
    parser.add_argument("--version", default=datetime.now().strftime("demo-%Y.%m.%d"))
    args = parser.parse_args()
    trainer = SystemRiskPredictor(Path(args.models))
    report = trainer.publish_synthetic_models(args.version)
    print(json.dumps(report, indent=2))
