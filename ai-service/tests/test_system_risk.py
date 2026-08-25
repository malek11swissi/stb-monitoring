from app import create_app


def payload(durations, statuses=None, errors=None):
    statuses = statuses or ["Up"] * len(durations)
    errors = errors or [None] * len(durations)
    return {
        "systemId": "11111111-1111-1111-1111-111111111111", "systemCode": "RNE", "systemName": "RNE",
        "environment": "Production", "criticality": "Critical", "currentStatus": statuses[-1],
        "activeAlertCount": 0, "criticalAlertCount": 0, "openIncidentCount": 0,
        "highPriorityIncidentCount": 0, "maintenanceActive": False,
        "endpoints": [{"endpointId": "22222222-2222-2222-2222-222222222222", "name": "Consultation entreprises",
            "isCritical": True, "currentStatus": statuses[-1], "degradedThresholdMs": 1000, "downThresholdMs": 3000,
            "samples": [{"timestamp": "2026-08-25T10:00:00Z", "durationMs": duration, "status": status,
                "success": status != "Down", "httpStatusCode": 200 if status != "Down" else None, "errorType": error}
                for duration, status, error in zip(durations, statuses, errors)]}]
    }


def test_stable_system_has_lower_risk_than_failing_system():
    app = create_app(testing=True)
    with app.test_client() as client:
        stable = client.post("/api/v1/predictions/system-risk", json=payload([180, 190, 185, 200, 195, 205, 200, 210])).get_json()
        failing = client.post("/api/v1/predictions/system-risk", json=payload(
            [300, 600, 1000, 1500, 2400, 3000, 3200, 3500],
            ["Up", "Up", "Degraded", "Degraded", "Degraded", "Down", "Down", "Down"],
            [None, None, None, None, "TIMEOUT", "TIMEOUT", "TIMEOUT", "NETWORK"])).get_json()
    assert stable["risk60Minutes"] < failing["risk60Minutes"]
    assert failing["riskScore"] >= stable["riskScore"]
    assert failing["factors"][0]["code"] in {"DOWN_RATE", "FAILURE_RATE", "LATENCY", "TIMEOUT"}


def test_invalid_payload_returns_400():
    app = create_app(testing=True)
    with app.test_client() as client:
        response = client.post("/api/v1/predictions/system-risk", json={})
    assert response.status_code == 400


def test_evaluation_uses_an_independent_holdout_and_exposes_metrics():
    app = create_app(testing=True)
    with app.test_client() as client:
        response = client.get("/api/v1/predictions/system-risk/evaluation")
        report = response.get_json()
    assert response.status_code == 200
    assert report["trainingScenarios"] == 4800
    assert report["thresholdValidationScenarios"] == 1200
    assert report["testScenarios"] == 1200
    assert report["datasetType"] == "synthetic"
    for horizon in ("15", "30", "60"):
        metrics = report["horizons"][horizon]
        assert 0 <= metrics["accuracy"] <= 1
        assert 0 <= metrics["precision"] <= 1
        assert 0 <= metrics["recall"] <= 1
        assert 0 <= metrics["f1Score"] <= 1
        assert 0 <= metrics["rocAuc"] <= 1
        assert 0.15 <= metrics["decisionThreshold"] <= 0.70
        assert sum(metrics["confusionMatrix"].values()) == 1200


def test_incident_resolution_recommends_the_most_similar_history():
    app = create_app(testing=True)
    body = {"incident":{"id":"1","title":"Timeout base MongoDB RNE","description":"connexion lente puis timeout","category":"Database","priority":"P1Critical","systemId":"rne","systemName":"RNE"},
            "resolvedIncidents":[
                {"id":"2","incidentNumber":"INC-002","title":"Timeout MongoDB RNE","description":"connexion base impossible timeout","category":"Database","priority":"P1Critical","systemId":"rne","systemName":"RNE","rootCause":"Pool saturé","correctiveAction":"Redémarrer le pool MongoDB","resolutionSummary":"Accès restauré"},
                {"id":"3","incidentNumber":"INC-003","title":"Certificat SMS expiré","description":"erreur TLS","category":"Certificate","priority":"P3Medium","systemId":"sms","systemName":"SMS","rootCause":"Expiration","correctiveAction":"Renouveler le certificat"}]}
    with app.test_client() as client:
        response=client.post("/api/v1/predictions/incident-resolution",json=body)
        result=response.get_json()
    assert response.status_code==200
    assert result["modelName"]=="tfidf-cosine-retrieval"
    assert result["recommendations"][0]["incidentNumber"]=="INC-002"
    assert result["recommendations"][0]["correctiveAction"]=="Redémarrer le pool MongoDB"
