"""Contrat stable entre la préparation des données, l'entraînement et Flask."""

FEATURE_NAMES = (
    "criticality", "endpoint_count", "critical_endpoint_share", "mean_latency_ratio",
    "max_latency_ratio", "latency_trend", "failure_rate", "timeout_rate", "http_5xx_rate",
    "degraded_rate", "down_rate", "status_instability", "active_alert_density",
    "critical_alert_count", "open_incident_density", "high_priority_count", "maintenance_active",
)

HORIZONS = (15, 30, 60)

