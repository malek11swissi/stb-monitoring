"""Entraîne, évalue chronologiquement et publie les modèles STB validés."""
from __future__ import annotations
import argparse
import csv
import json
from datetime import datetime, timezone
from pathlib import Path
import joblib
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score, precision_score, recall_score, roc_auc_score

FEATURES = (
    "criticality", "endpoint_count", "critical_endpoint_share", "mean_latency_ratio",
    "max_latency_ratio", "latency_trend", "failure_rate", "timeout_rate", "http_5xx_rate",
    "degraded_rate", "down_rate", "status_instability", "active_alert_density",
    "critical_alert_count", "open_incident_density", "high_priority_count", "maintenance_active",
)


def select_threshold(true, probabilities):
    best = (.5, -1.)
    for threshold in np.arange(.15, .71, .01):
        predicted = probabilities >= threshold
        p = precision_score(true, predicted, zero_division=0); r = recall_score(true, predicted, zero_division=0)
        f2 = 5*p*r/(4*p+r) if p+r else 0
        if f2 > best[1]: best = (float(threshold), float(f2))
    return best[0]


def train(dataset: Path, model_dir: Path, version: str):
    with dataset.open(encoding="utf-8", newline="") as stream:
        rows = sorted(csv.DictReader(stream), key=lambda row: row["timestamp"])
    if len(rows) < 200: raise ValueError("Au moins 200 fenêtres historiques sont nécessaires.")
    x = np.array([[float(row[name]) for name in FEATURES] for row in rows])
    train_end = int(len(rows)*.70); validation_end = int(len(rows)*.85)
    if train_end == 0 or validation_end == train_end or validation_end == len(rows): raise ValueError("Dataset trop petit.")
    model_dir.mkdir(parents=True, exist_ok=True)
    metrics, artifacts = {}, {}
    for horizon in (15, 30, 60):
        y = np.array([int(row[f"failure_{horizon}m"]) for row in rows])
        if len(np.unique(y[:train_end])) < 2 or len(np.unique(y[validation_end:])) < 2:
            raise ValueError(f"Horizon {horizon}: entraînement et test doivent contenir panne=0 et panne=1.")
        model = RandomForestClassifier(n_estimators=300, max_depth=12, min_samples_leaf=5, class_weight="balanced", random_state=2026+horizon, n_jobs=-1)
        model.fit(x[:train_end], y[:train_end])
        threshold = select_threshold(y[train_end:validation_end], model.predict_proba(x[train_end:validation_end])[:, 1])
        model.fit(x[:validation_end], y[:validation_end])
        probability = model.predict_proba(x[validation_end:])[:, 1]; true = y[validation_end:]
        predicted = (probability >= threshold).astype(int); tn, fp, fn, tp = confusion_matrix(true, predicted, labels=[0, 1]).ravel()
        metrics[str(horizon)] = {
            "accuracy": round(float(accuracy_score(true,predicted)),4), "precision": round(float(precision_score(true,predicted,zero_division=0)),4),
            "recall": round(float(recall_score(true,predicted,zero_division=0)),4), "f1Score": round(float(f1_score(true,predicted,zero_division=0)),4),
            "rocAuc": round(float(roc_auc_score(true,probability)),4), "decisionThreshold": round(threshold,4),
            "confusionMatrix": {"trueNegative":int(tn),"falsePositive":int(fp),"falseNegative":int(fn),"truePositive":int(tp)},
        }
        filename = f"system_risk_{horizon}m.joblib"; artifacts[str(horizon)] = filename
        joblib.dump({"model":model,"decisionThreshold":threshold,"featureNames":list(FEATURES)}, model_dir/filename)
    evaluation = {"modelName":"system-risk-random-forest","modelVersion":version,"datasetType":"stb-real","totalScenarios":len(rows),
                  "trainingScenarios":train_end,"validationScenarios":validation_end-train_end,"testScenarios":len(rows)-validation_end,
                  "split":"chronological 70/15/15","horizons":metrics,
                  "warning":"Performance mesurée sur l'historique fourni; surveiller la dérive et valider humainement."}
    manifest = {"status":"validated","modelName":"system-risk-random-forest","modelVersion":version,"modelSource":"stb-real",
                "trainedAt":datetime.now(timezone.utc).isoformat(),"featureNames":list(FEATURES),"artifacts":artifacts}
    (model_dir/"evaluation.json").write_text(json.dumps(evaluation,indent=2),encoding="utf-8")
    (model_dir/"manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    return evaluation


if __name__ == "__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("--dataset",default="data/processed/system_risk_features.csv")
    parser.add_argument("--models",default="models"); parser.add_argument("--version",default=datetime.now().strftime("stb-%Y.%m.%d")); args=parser.parse_args()
    report=train(Path(args.dataset),Path(args.models),args.version); print(json.dumps(report,indent=2))

