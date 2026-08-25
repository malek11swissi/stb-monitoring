from __future__ import annotations
from datetime import datetime, timezone
import math
import json
import os
from pathlib import Path
import numpy as np
import joblib
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score, precision_score, recall_score, roc_auc_score
from sklearn.model_selection import train_test_split
from ..model_schema import FEATURE_NAMES, HORIZONS


class SystemRiskPredictor:
    """
    Modèle initial entraîné sur des scénarios synthétiques reproductibles.
    Les données réelles de CheckResult sont utilisées à l'inférence. Cette version
    sera réentraînée sur l'historique STB dès que des pannes réelles seront étiquetées.
    """

    model_name = "system-risk-random-forest"
    model_version = "1.2.0-synthetic-validated"

    def __init__(self) -> None:
        self.model_source = "synthetic"
        self.thresholds = {horizon: 0.5 for horizon in HORIZONS}
        if not self._load_stb_models():
            self.models, self.evaluation = self._train_and_evaluate_synthetic_models()
            self.thresholds = {
                int(horizon): values["decisionThreshold"]
                for horizon, values in self.evaluation["horizons"].items()
            }

    def _load_stb_models(self) -> bool:
        """Charge les artefacts validés sans changer le code; False active le fallback démo."""
        default_dir = Path(__file__).resolve().parents[2] / "models"
        model_dir = Path(os.getenv("AI_MODEL_DIR", str(default_dir)))
        manifest_path = model_dir / "manifest.json"
        evaluation_path = model_dir / "evaluation.json"
        try:
            if not manifest_path.exists():
                return False
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            if manifest.get("status") != "validated" or manifest.get("featureNames") != list(FEATURE_NAMES):
                return False
            loaded = {}
            thresholds = {}
            for horizon in HORIZONS:
                artifact = joblib.load(model_dir / manifest["artifacts"][str(horizon)])
                loaded[horizon] = artifact["model"]
                thresholds[horizon] = float(artifact["decisionThreshold"])
            self.models = loaded
            self.thresholds = thresholds
            self.model_name = manifest.get("modelName", self.model_name)
            self.model_version = manifest["modelVersion"]
            self.model_source = "stb-real"
            self.evaluation = json.loads(evaluation_path.read_text(encoding="utf-8"))
            return True
        except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError):
            return False

    def evaluation_report(self) -> dict:
        """Retourne les métriques du jeu de test, jamais utilisé pour entraîner les modèles."""
        return self.evaluation

    def predict(self, payload: dict) -> dict:
        features, endpoint_risks, sample_count = self._extract_features(payload)
        vector = np.array([[features[name] for name in FEATURE_NAMES]], dtype=float)
        probabilities = {h: float(self.models[h].predict_proba(vector)[0, 1]) for h in (15, 30, 60)}
        probabilities[30] = max(probabilities[15], probabilities[30])
        probabilities[60] = max(probabilities[30], probabilities[60])
        score = int(round(probabilities[60] * 100))
        confidence = min(0.96, 0.42 + math.log1p(sample_count) / 12 + min(len(payload.get("endpoints", [])), 5) * 0.025)
        if sample_count < 5:
            confidence = min(confidence, 0.48)
        factors = self._explain(features)
        return {
            "available": True,
            "systemId": payload["systemId"],
            "modelName": self.model_name,
            "modelVersion": self.model_version,
            "modelSource": self.model_source,
            "sampleCount": sample_count,
            "risk15Minutes": round(probabilities[15], 4),
            "risk30Minutes": round(probabilities[30], 4),
            "risk60Minutes": round(probabilities[60], 4),
            "riskScore": score,
            "riskLevel": self._level(score),
            "confidence": round(confidence, 4),
            "estimatedTimeToDownMinutes": self._estimated_time(probabilities),
            "factors": factors,
            "mostRiskyEndpoints": sorted(endpoint_risks, key=lambda item: item["riskScore"], reverse=True)[:5],
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "message": ("Modèle entraîné sur l'historique STB validé ; validation humaine requise."
                        if self.model_source == "stb-real" else
                        "Modèle initial entraîné sur des scénarios synthétiques ; validation humaine requise."),
        }

    def _extract_features(self, payload: dict) -> tuple[dict, list[dict], int]:
        endpoints = payload.get("endpoints", [])
        endpoint_features = [self._endpoint_features(endpoint) for endpoint in endpoints]
        sample_count = sum(item["sample_count"] for item in endpoint_features)
        endpoint_count = max(1, len(endpoints))
        critical_count = sum(1 for endpoint in endpoints if endpoint.get("isCritical"))
        values = lambda name: [float(item[name]) for item in endpoint_features]
        criticality = {"Low": 0.2, "Medium": 0.45, "High": 0.72, "Critical": 1.0}.get(payload.get("criticality"), 0.5)
        features = {
            "criticality": criticality,
            "endpoint_count": min(len(endpoints) / 20, 1),
            "critical_endpoint_share": critical_count / endpoint_count,
            "mean_latency_ratio": self._mean(values("mean_latency_ratio")),
            "max_latency_ratio": max(values("max_latency_ratio"), default=0),
            "latency_trend": max(values("latency_trend"), default=0),
            "failure_rate": self._mean(values("failure_rate")),
            "timeout_rate": self._mean(values("timeout_rate")),
            "http_5xx_rate": self._mean(values("http_5xx_rate")),
            "degraded_rate": self._mean(values("degraded_rate")),
            "down_rate": self._mean(values("down_rate")),
            "status_instability": self._mean(values("status_instability")),
            "active_alert_density": min(float(payload.get("activeAlertCount", 0)) / endpoint_count / 3, 1),
            "critical_alert_count": min(float(payload.get("criticalAlertCount", 0)) / 3, 1),
            "open_incident_density": min(float(payload.get("openIncidentCount", 0)) / endpoint_count / 2, 1),
            "high_priority_count": min(float(payload.get("highPriorityIncidentCount", 0)) / 3, 1),
            "maintenance_active": 1.0 if payload.get("maintenanceActive") else 0.0,
        }
        risks = []
        for endpoint, item in zip(endpoints, endpoint_features):
            raw = 100 * min(1, 0.25 * item["max_latency_ratio"] + 0.25 * item["failure_rate"] + 0.15 * item["timeout_rate"] + 0.15 * item["down_rate"] + 0.1 * item["degraded_rate"] + 0.1 * item["latency_trend"])
            if endpoint.get("isCritical"):
                raw = min(100, raw * 1.08)
            score = int(round(raw))
            risks.append({"endpointId": endpoint.get("endpointId"), "name": endpoint.get("name", "Endpoint"), "riskScore": score, "riskLevel": self._level(score)})
        return features, risks, sample_count

    @staticmethod
    def _endpoint_features(endpoint: dict) -> dict:
        samples = endpoint.get("samples", [])
        if not samples:
            return {"sample_count": 0, "mean_latency_ratio": 0, "max_latency_ratio": 0, "latency_trend": 0, "failure_rate": 0, "timeout_rate": 0, "http_5xx_rate": 0, "degraded_rate": 0, "down_rate": 0, "status_instability": 0}
        durations = np.array([max(0, float(sample.get("durationMs", 0))) for sample in samples], dtype=float)
        down_threshold = max(1, float(endpoint.get("downThresholdMs", 3000)))
        statuses = [str(sample.get("status", "Unknown")).lower() for sample in samples]
        errors = [str(sample.get("errorType") or "").upper() for sample in samples]
        successes = [bool(sample.get("success")) for sample in samples]
        http = [sample.get("httpStatusCode") for sample in samples]
        recent = durations[-min(20, len(durations)):]
        slope = np.polyfit(np.arange(len(recent)), recent, 1)[0] / down_threshold if len(recent) >= 3 else 0
        changes = sum(1 for left, right in zip(statuses, statuses[1:]) if left != right)
        return {
            "sample_count": len(samples),
            "mean_latency_ratio": min(float(np.mean(recent)) / down_threshold, 2),
            "max_latency_ratio": min(float(np.max(recent)) / down_threshold, 2),
            "latency_trend": min(max(float(slope), 0), 1),
            "failure_rate": sum(not value for value in successes) / len(samples),
            "timeout_rate": sum("TIMEOUT" in value for value in errors) / len(samples),
            "http_5xx_rate": sum(code is not None and 500 <= int(code) <= 599 for code in http) / len(samples),
            "degraded_rate": statuses.count("degraded") / len(samples),
            "down_rate": statuses.count("down") / len(samples),
            "status_instability": changes / max(1, len(statuses) - 1),
        }

    def _train_and_evaluate_synthetic_models(self) -> tuple[dict[int, RandomForestClassifier], dict]:
        """Génère 6 000 cas reproductibles, réserve 20 % au test puis évalue chaque horizon."""
        rng = np.random.default_rng(20260825)
        count = 6000
        x = rng.beta(1.4, 3.2, size=(count, len(FEATURE_NAMES)))
        # endpoint_count et maintenance sont distribués différemment.
        x[:, 1] = rng.uniform(0.03, 1, count)
        x[:, 16] = rng.binomial(1, 0.08, count)
        index = {name: FEATURE_NAMES.index(name) for name in FEATURE_NAMES}
        # Signal métier synthétique : une panne future devient plus probable lorsque
        # latence, échecs, timeouts, états DOWN et alertes augmentent ensemble.
        latent = (
            0.06*x[:,index["criticality"]] + 0.04*x[:,index["critical_endpoint_share"]] +
            0.11*x[:,index["mean_latency_ratio"]] + 0.15*x[:,index["max_latency_ratio"]] +
            0.09*x[:,index["latency_trend"]] + 0.15*x[:,index["failure_rate"]] +
            0.09*x[:,index["timeout_rate"]] + 0.06*x[:,index["http_5xx_rate"]] +
            0.07*x[:,index["degraded_rate"]] + 0.12*x[:,index["down_rate"]] +
            0.04*x[:,index["status_instability"]] + 0.05*x[:,index["active_alert_density"]] +
            0.05*x[:,index["critical_alert_count"]] + 0.04*x[:,index["open_incident_density"]] +
            0.04*x[:,index["high_priority_count"]] - 0.05*x[:,index["maintenance_active"]]
        )
        labels = {}
        # Un petit bruit représente les causes futures invisibles au monitoring.
        # Les seuils par quantile produisent logiquement plus de pannes à 60 min.
        for horizon, failure_share in ((15, 0.12), (30, 0.24), (60, 0.38)):
            future_risk = latent + rng.normal(0, 0.018 + horizon / 6000, count)
            labels[horizon] = (future_risk >= np.quantile(future_risk, 1-failure_share)).astype(int)

        # Les mêmes indices sont réservés pour les trois horizons. Le modèle ne voit
        # jamais les 1 200 scénarios de test pendant son apprentissage.
        learning_indices, test_indices = train_test_split(
            np.arange(count), test_size=0.20, random_state=20260825, shuffle=True
        )
        train_indices, validation_indices = train_test_split(
            learning_indices, test_size=0.25, random_state=20260826, shuffle=True
        )
        models = {}
        metrics = {}
        for horizon in (15, 30, 60):
            y = labels[horizon]
            model = RandomForestClassifier(n_estimators=240, max_depth=12, min_samples_leaf=5, class_weight="balanced", random_state=2026+horizon, n_jobs=-1)
            model.fit(x[train_indices], y[train_indices])
            validation_probabilities = model.predict_proba(x[validation_indices])[:, 1]
            threshold = self._select_recall_threshold(y[validation_indices], validation_probabilities)

            # Une fois le seuil choisi sans toucher au test, on réentraîne sur les
            # 4 800 cas disponibles, puis on fait l'examen final sur les 1 200 cas.
            model.fit(x[learning_indices], y[learning_indices])
            models[horizon] = model
            probabilities = model.predict_proba(x[test_indices])[:, 1]
            predicted = (probabilities >= threshold).astype(int)
            true = y[test_indices]
            tn, fp, fn, tp = confusion_matrix(true, predicted, labels=[0, 1]).ravel()
            metrics[str(horizon)] = {
                "accuracy": round(float(accuracy_score(true, predicted)), 4),
                "precision": round(float(precision_score(true, predicted, zero_division=0)), 4),
                "recall": round(float(recall_score(true, predicted, zero_division=0)), 4),
                "f1Score": round(float(f1_score(true, predicted, zero_division=0)), 4),
                "rocAuc": round(float(roc_auc_score(true, probabilities)), 4),
                "decisionThreshold": round(float(threshold), 4),
                "actualFailures": int(np.sum(true)),
                "predictedFailures": int(np.sum(predicted)),
                "confusionMatrix": {
                    "trueNegative": int(tn), "falsePositive": int(fp),
                    "falseNegative": int(fn), "truePositive": int(tp),
                },
            }
        return models, {
            "modelName": self.model_name,
            "modelVersion": self.model_version,
            "datasetType": "synthetic",
            "totalScenarios": count,
            "trainingScenarios": len(learning_indices),
            "thresholdValidationScenarios": len(validation_indices),
            "testScenarios": len(test_indices),
            "split": "80% learning / 20% untouched test; threshold selected on a validation subset of learning data",
            "randomSeed": 20260825,
            "warning": "Ces métriques mesurent uniquement les scénarios synthétiques et ne prouvent pas la performance sur les SI réels de STB.",
            "horizons": metrics,
        }

    @staticmethod
    def _select_recall_threshold(true: np.ndarray, probabilities: np.ndarray) -> float:
        """Choisit sur validation le meilleur F2 : le rappel compte deux fois plus que la précision."""
        best_threshold, best_f2 = 0.5, -1.0
        for threshold in np.arange(0.15, 0.71, 0.01):
            predicted = (probabilities >= threshold).astype(int)
            precision = precision_score(true, predicted, zero_division=0)
            recall = recall_score(true, predicted, zero_division=0)
            f2 = (5 * precision * recall / (4 * precision + recall)) if precision + recall else 0
            if f2 > best_f2:
                best_threshold, best_f2 = float(threshold), float(f2)
        return best_threshold

    @staticmethod
    def _explain(f: dict) -> list[dict]:
        candidates = [
            ("DOWN_RATE", "Des contrôles récents sont déjà DOWN", f["down_rate"]*0.22),
            ("FAILURE_RATE", "Le taux d'échec récent est élevé", f["failure_rate"]*0.20),
            ("LATENCY", "La latence approche ou dépasse le seuil DOWN", min(f["max_latency_ratio"],1)*0.18),
            ("TREND", "La latence suit une tendance croissante", f["latency_trend"]*0.13),
            ("TIMEOUT", "Des timeouts ont été observés", f["timeout_rate"]*0.12),
            ("ALERTS", "Des alertes critiques sont actives", f["critical_alert_count"]*0.09),
            ("INCIDENTS", "Des incidents P1/P2 sont encore ouverts", f["high_priority_count"]*0.06),
        ]
        result = [{"code": code, "label": label, "contribution": round(value, 4)} for code, label, value in sorted(candidates, key=lambda item: item[2], reverse=True) if value >= 0.015]
        return result[:5] or [{"code": "STABLE", "label": "Aucun facteur de risque significatif détecté", "contribution": 0.0}]

    @staticmethod
    def _level(score: int) -> str:
        return "Critical" if score >= 80 else "High" if score >= 60 else "Medium" if score >= 35 else "Low"

    @staticmethod
    def _estimated_time(p: dict[int, float]) -> float | None:
        if p[15] >= 0.5: return 12.0
        if p[30] >= 0.5: return 25.0
        if p[60] >= 0.5: return 48.0
        return None

    @staticmethod
    def _mean(values: list[float]) -> float:
        return float(np.mean(values)) if values else 0.0


predictor = SystemRiskPredictor()
