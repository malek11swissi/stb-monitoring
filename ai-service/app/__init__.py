from flask import Flask, jsonify
from .routes.system_risk import system_risk_bp
from .services.system_risk_service import predictor


def create_app(testing: bool = False) -> Flask:
    """Fabrique l'API Flask. Le modèle est chargé une fois au démarrage."""
    app = Flask(__name__)
    app.config["TESTING"] = testing
    app.register_blueprint(system_risk_bp, url_prefix="/api/v1/predictions")

    @app.get("/health")
    def health():
        return jsonify({"status": "UP" if predictor.is_available else "DEGRADED",
                        "service": "stb-sentinel-ai", "modelLoaded": predictor.is_available,
                        "model": predictor.model_name, "modelVersion": predictor.model_version,
                        "modelSource": predictor.model_source,
                        "message": None if predictor.is_available else predictor.load_error})

    return app
