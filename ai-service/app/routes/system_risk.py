from flask import Blueprint, jsonify, request
from ..services.system_risk_service import predictor

system_risk_bp = Blueprint("system_risk", __name__)


@system_risk_bp.get("/system-risk/evaluation")
def system_risk_evaluation():
    """Rapport scientifique du jeu de test synthétique réservé (20 %)."""
    return jsonify(predictor.evaluation_report()), 200


@system_risk_bp.post("/system-risk")
def predict_system_risk():
    """Reçoit l'historique préparé par .NET et retourne une prédiction explicable."""
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"message": "Un corps JSON est obligatoire."}), 400
    if not payload.get("systemId"):
        return jsonify({"message": "systemId est obligatoire."}), 400
    if not isinstance(payload.get("endpoints", []), list):
        return jsonify({"message": "endpoints doit être une liste."}), 400
    try:
        return jsonify(predictor.predict(payload)), 200
    except (TypeError, ValueError) as exc:
        return jsonify({"message": f"Données de prédiction invalides : {exc}"}), 400
