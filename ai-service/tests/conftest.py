"""Prépare une fois des artefacts isolés: importer Flask ne lance jamais l'entraînement."""
from __future__ import annotations

import os
from pathlib import Path


def pytest_sessionstart(session):
    model_dir = Path(str(session.config.rootpath)) / ".pytest-models"
    os.environ["AI_MODEL_DIR"] = str(model_dir)
    os.environ["AI_SYNTHETIC_TREES"] = "40"
    from app.services.system_risk_service import SystemRiskPredictor, predictor

    trainer = SystemRiskPredictor(model_dir)
    trainer.publish_synthetic_models("test-synthetic")
    predictor.model_dir = model_dir
    assert predictor.reload()


def pytest_sessionfinish(session, exitstatus):
    model_dir = Path(str(session.config.rootpath)) / ".pytest-models"
    for path in model_dir.glob("*") if model_dir.exists() else []:
        path.unlink()
    if model_dir.exists():
        model_dir.rmdir()
