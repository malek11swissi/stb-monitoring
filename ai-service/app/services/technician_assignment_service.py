"""Classement explicable des techniciens; l'affectation reste décidée par le superviseur."""
from __future__ import annotations

import re


class TechnicianAssignmentService:
    model_name = "explainable-technician-ranking"
    model_version = "1.0.0"

    def recommend(self, payload: dict) -> dict:
        incident = payload["incident"]
        technicians = [item for item in payload.get("technicians", []) if item.get("available", True)]
        if not technicians:
            return {"available": True, "modelName": self.model_name, "modelVersion": self.model_version,
                    "recommendations": [], "message": "Aucun technicien actif n'est disponible."}

        incident_terms = self._terms(" ".join(str(incident.get(key) or "") for key in ("title", "description", "category", "systemName")))
        urgent = incident.get("priority") in {"P1Critical", "P2High"}
        ranked = []
        for technician in technicians:
            skills = technician.get("skills") or []
            skill_terms = self._terms(" ".join(str(skill) for skill in skills))
            skill_score = len(incident_terms & skill_terms) / max(1, min(5, len(incident_terms)))
            similar_score = min(float(technician.get("similarResolvedCount", 0)) / 5, 1)
            system_score = min(float(technician.get("systemResolvedCount", 0)) / 5, 1)
            workload = max(0, int(technician.get("activeIncidentCount", 0)))
            capacity_score = max(0, 1 - workload / (3 if urgent else 5))
            experience_score = min(float(technician.get("totalResolvedCount", 0)) / 12, 1)
            score = (0.30 * skill_score + 0.25 * similar_score + 0.20 * system_score
                     + 0.15 * capacity_score + 0.10 * experience_score)
            reasons = []
            matched = sorted(incident_terms & skill_terms)
            if matched:
                reasons.append("Compétences correspondantes : " + ", ".join(matched[:4]))
            if technician.get("similarResolvedCount", 0):
                reasons.append(f"{technician['similarResolvedCount']} incident(s) similaire(s) résolu(s)")
            if technician.get("systemResolvedCount", 0):
                reasons.append(f"{technician['systemResolvedCount']} incident(s) résolu(s) sur ce SI")
            reasons.append(f"Charge actuelle : {workload} incident(s) actif(s)")
            ranked.append({
                "technicianId": technician["technicianId"], "firstName": technician["firstName"],
                "lastName": technician["lastName"], "avatarPath": technician.get("avatarPath"),
                "score": round(score, 4), "confidence": "High" if score >= .65 else "Medium" if score >= .35 else "Low",
                "skills": skills, "activeIncidentCount": workload,
                "similarResolvedCount": int(technician.get("similarResolvedCount", 0)),
                "systemResolvedCount": int(technician.get("systemResolvedCount", 0)), "reasons": reasons,
            })
        ranked.sort(key=lambda item: (-item["score"], item["activeIncidentCount"], item["lastName"]))
        return {"available": True, "modelName": self.model_name, "modelVersion": self.model_version,
                "recommendations": ranked[:5],
                "message": "Classement d'aide à la décision; le superviseur confirme toujours l'affectation."}

    @staticmethod
    def _terms(text: str) -> set[str]:
        aliases = {"database": "base", "certificate": "tls", "certificat": "tls", "mongodb": "mongo"}
        words = {aliases.get(word, word) for word in re.findall(r"[a-z0-9+#.]+", text.lower())}
        return words - {"incident", "erreur", "service", "systeme", "système", "avec", "dans", "pour", "une", "des", "the"}


technician_assignment = TechnicianAssignmentService()
