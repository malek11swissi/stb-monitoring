"""Recherche NLP explicable de résolutions dans l'historique des incidents."""
import json
from pathlib import Path
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity


class IncidentRecommendationService:
    def __init__(self):
        path = Path(__file__).resolve().parents[2] / "data" / "resolution_knowledge_base.json"
        self.knowledge_base = json.loads(path.read_text(encoding="utf-8")) if path.exists() else []

    def recommend(self, payload: dict) -> dict:
        current = payload["incident"]
        historical = [dict(item, sourceType="HistoricalIncident") for item in payload.get("resolvedIncidents", []) if item.get("correctiveAction")]
        candidates = [*historical, *self.knowledge_base]
        if len(candidates) < 2:
            return {"available": True, "modelName": "tfidf-cosine-retrieval", "candidateCount": len(candidates),
                    "recommendations": [], "message": "Pas assez d'incidents résolus documentés (minimum 2)."}
        documents = [self._signature(current), *[self._signature(item) for item in candidates]]
        matrix = TfidfVectorizer(ngram_range=(1, 2), min_df=1, strip_accents="unicode", lowercase=True).fit_transform(documents)
        similarities = cosine_similarity(matrix[0:1], matrix[1:]).flatten()
        ranked = []
        for item, similarity in zip(candidates, similarities):
            bonus = .08 if item.get("systemId") == current.get("systemId") else 0
            bonus += .06 if item.get("category") == current.get("category") else 0
            bonus += .03 if item.get("priority") == current.get("priority") else 0
            score = min(1., float(similarity) + bonus)
            if score >= .05:
                reasons = []
                if item.get("systemId") == current.get("systemId"): reasons.append("Même système d'information")
                if item.get("category") == current.get("category"): reasons.append("Même catégorie d'incident")
                if item.get("priority") == current.get("priority"): reasons.append("Même niveau de priorité")
                reasons.append("Description technique similaire")
                ranked.append({"sourceIncidentId": item["id"], "incidentNumber": item["incidentNumber"],
                               "title": item["title"], "similarity": round(score, 4),
                               "sourceType": item.get("sourceType", "HistoricalIncident"),
                               "rootCause": item.get("rootCause"), "correctiveAction": item["correctiveAction"],
                               "preventiveAction": item.get("preventiveAction"), "resolutionSummary": item.get("resolutionSummary"),
                               "matchReasons": reasons})
        ranked.sort(key=lambda item: item["similarity"], reverse=True)
        return {"available": True, "modelName": "tfidf-cosine-retrieval", "modelVersion": "1.0.0",
                "candidateCount": len(candidates), "recommendations": ranked[:3],
                "message": "Suggestions issues d'incidents historiques similaires; validation du technicien obligatoire."}

    @staticmethod
    def _signature(item: dict) -> str:
        return " ".join(str(item.get(name) or "") for name in ("title", "description", "category", "systemName"))


incident_recommender = IncidentRecommendationService()
