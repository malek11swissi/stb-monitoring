# Simulateur SMS STB

Microservice pédagogique exposant des API métier de suivi SMS et utilisant une base MySQL indépendante. STB Sentinel contrôle ces API par HTTP/HTTPS, JSON, latence et TLS.

Contrôle conseillé : `GET /api/v1/messages/SMS-STB-001/statut-livraison`, code `200`, propriété `serviceSmsAccessible=true`.
