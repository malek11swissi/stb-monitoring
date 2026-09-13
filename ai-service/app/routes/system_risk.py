from flask import Blueprint, jsonify, request
from ..services.system_risk_service import predictor
from ..services.incident_recommendation_service import incident_recommender
from ..services.technician_assignment_service import technician_assignment

system_risk_bp = Blueprint("system_risk", __name__)


@system_risk_bp.post("/technician-assignment")
def recommend_technician_assignment():
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict) or not isinstance(payload.get("incident"), dict):
        return jsonify({"message": "incident est obligatoire."}), 400
    if not isinstance(payload.get("technicians"), list):
        return jsonify({"message": "technicians doit être une liste."}), 400
    return jsonify(technician_assignment.recommend(payload)), 200


@system_risk_bp.post("/incident-resolution")
def recommend_incident_resolution():
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict) or not isinstance(payload.get("incident"), dict):
        return jsonify({"message": "incident est obligatoire."}), 400
    if not isinstance(payload.get("resolvedIncidents", []), list):
        return jsonify({"message": "resolvedIncidents doit être une liste."}), 400
    return jsonify(incident_recommender.recommend(payload)), 200


@system_risk_bp.get("/system-risk/evaluation")
def system_risk_evaluation():
    """Rapport scientifique du jeu de test synthétique réservé (20 %)."""
    return jsonify(predictor.evaluation_report()), 200 if predictor.is_available else 503


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
    except RuntimeError as exc:
        return jsonify({"available": False, "message": str(exc)}), 503
    except (TypeError, ValueError) as exc:
        return jsonify({"message": f"Données de prédiction invalides : {exc}"}), 400
