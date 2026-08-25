"""Transforme l'export historique STB en fenêtres de features et labels 15/30/60 min."""
from __future__ import annotations
import argparse
import csv
from datetime import datetime, timedelta, timezone
from pathlib import Path
import numpy as np

CRITICALITY = {"low": .2, "medium": .45, "high": .72, "critical": 1.0}
FEATURES = (
    "criticality", "endpoint_count", "critical_endpoint_share", "mean_latency_ratio",
    "max_latency_ratio", "latency_trend", "failure_rate", "timeout_rate", "http_5xx_rate",
    "degraded_rate", "down_rate", "status_instability", "active_alert_density",
    "critical_alert_count", "open_incident_density", "high_priority_count", "maintenance_active",
)
REQUIRED = {
    "timestamp", "system_id", "criticality", "endpoint_id", "is_critical", "duration_ms",
    "down_threshold_ms", "status", "success", "http_status_code", "error_type",
    "active_alert_count", "critical_alert_count", "open_incident_count",
    "high_priority_incident_count", "maintenance_active",
}


def boolean(value: str) -> bool:
    return value.strip().lower() in {"1", "true", "yes", "oui"}


def number(value: str, default: float = 0) -> float:
    try: return float(value)
    except (TypeError, ValueError): return default


def instant(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00")).astimezone(timezone.utc)


def prepare(source: Path, destination: Path, window_hours: int = 24) -> int:
    with source.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        missing = REQUIRED - set(reader.fieldnames or [])
        if missing: raise ValueError(f"Colonnes obligatoires absentes: {', '.join(sorted(missing))}")
        rows = [dict(row, _time=instant(row["timestamp"])) for row in reader]
    rows.sort(key=lambda row: (row["system_id"], row["_time"]))
    systems = sorted({row["system_id"] for row in rows})
    output = []
    for system_id in systems:
        system_rows = [row for row in rows if row["system_id"] == system_id]
        times = sorted({row["_time"] for row in system_rows})
        for current in times:
            # Sans 60 minutes d'observation future, le label serait faussement égal à zéro.
            if current + timedelta(minutes=60) > times[-1]: continue
            history = [row for row in system_rows if current-timedelta(hours=window_hours) <= row["_time"] <= current]
            # Il faut au moins cinq contrôles pour produire un exemple exploitable.
            if len(history) < 5: continue
            endpoints = {row["endpoint_id"] for row in history}
            endpoint_count = max(1, len(endpoints))
            durations = np.array([max(0, number(row["duration_ms"])) for row in history])
            ratios = np.array([duration/max(1, number(row["down_threshold_ms"], 3000)) for duration, row in zip(durations, history)])
            statuses = [row["status"].strip().lower() for row in history]
            recent = ratios[-min(20, len(ratios)):]
            slope = np.polyfit(np.arange(len(recent)), recent, 1)[0] if len(recent) >= 3 else 0
            latest = history[-1]
            changes = sum(a != b for a, b in zip(statuses, statuses[1:]))
            feature = {
                "criticality": CRITICALITY.get(latest["criticality"].strip().lower(), .5),
                "endpoint_count": min(endpoint_count/20, 1),
                "critical_endpoint_share": len({r["endpoint_id"] for r in history if boolean(r["is_critical"])})/endpoint_count,
                "mean_latency_ratio": min(float(np.mean(recent)), 2),
                "max_latency_ratio": min(float(np.max(recent)), 2),
                "latency_trend": min(max(float(slope), 0), 1),
                "failure_rate": sum(not boolean(r["success"]) for r in history)/len(history),
                "timeout_rate": sum("TIMEOUT" in r["error_type"].upper() for r in history)/len(history),
                "http_5xx_rate": sum(500 <= number(r["http_status_code"], -1) <= 599 for r in history)/len(history),
                "degraded_rate": statuses.count("degraded")/len(history),
                "down_rate": statuses.count("down")/len(history),
                "status_instability": changes/max(1, len(statuses)-1),
                "active_alert_density": min(number(latest["active_alert_count"])/endpoint_count/3, 1),
                "critical_alert_count": min(number(latest["critical_alert_count"])/3, 1),
                "open_incident_density": min(number(latest["open_incident_count"])/endpoint_count/2, 1),
                "high_priority_count": min(number(latest["high_priority_incident_count"])/3, 1),
                "maintenance_active": 1 if boolean(latest["maintenance_active"]) else 0,
            }
            result = {"timestamp": current.isoformat(), "system_id": system_id, **feature}
            for horizon in (15, 30, 60):
                future = [row for row in system_rows if current < row["_time"] <= current+timedelta(minutes=horizon)]
                result[f"failure_{horizon}m"] = int(any(row["status"].strip().lower() == "down" for row in future))
            output.append(result)
    if not output: raise ValueError("Aucune fenêtre produite. Vérifiez qu'il existe au moins 5 contrôles par SI.")
    destination.parent.mkdir(parents=True, exist_ok=True)
    fields = ["timestamp", "system_id", *FEATURES, "failure_15m", "failure_30m", "failure_60m"]
    with destination.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader(); writer.writerows(output)
    return len(output)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default="data/raw/stb_check_history.csv")
    parser.add_argument("--output", default="data/processed/system_risk_features.csv")
    parser.add_argument("--window-hours", type=int, default=24)
    args = parser.parse_args()
    count = prepare(Path(args.input), Path(args.output), args.window_hours)
    print(f"Dataset préparé: {count} fenêtres -> {args.output}")
