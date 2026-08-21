import fs from "node:fs/promises";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outDir = "C:/Users/malek/Documents/stb-monitoring/outputs/stb-sentinel-jira-gantt";
await fs.mkdir(outDir, { recursive: true });

const navy = "#123B5D", teal = "#087E8B", cyan = "#2CB5C0", canvas = "#F5F8FA";
const ink = "#18252E", muted = "#61727E", success = "#198754", warning = "#E89A19", danger = "#D64545", purple = "#7257A8";
const paleBlue = "#E8F0F5", paleTeal = "#E7F5F3", paleOrange = "#FFF4DF", paleRed = "#FDECEC";

const d = (value) => new Date(`${value}T00:00:00`);
const addDays = (date, days) => { const x = new Date(date); x.setDate(x.getDate() + days); return x; };
const colName = (n) => { let s=""; while(n>0){ n--; s=String.fromCharCode(65+n%26)+s; n=Math.floor(n/26); } return s; };

const sprints = [
  ["Sprint 0","Cadrage et initialisation","Architecture, dépôts, conventions, environnement local","2026-08-24","2026-09-04",85,"En cours",""],
  ["Sprint 1","Identité et accès","JWT, utilisateurs, rôles simplifiés, profil et audit","2026-09-07","2026-09-18",80,"En cours","Sprint 0"],
  ["Sprint 2","Catalogue des SI","Environnements, SI, endpoints, filtres et validation","2026-09-21","2026-10-02",85,"En cours","Sprint 1"],
  ["Sprint 3","Moteur de monitoring","HTTP, API JSON, TLS, worker, verrou distribué et preuves","2026-10-05","2026-10-16",90,"En cours","Sprint 2"],
  ["Sprint 4","Simulateurs","Core Banking/Oracle, RNE/MongoDB, SMS/MySQL, RH/SQL Server","2026-10-19","2026-10-30",75,"En cours","Sprint 3"],
  ["Sprint 5","Alertes et incidents","Règles, corrélation, incidents, SLA, affectation et résolution","2026-11-02","2026-11-13",90,"En cours","Sprint 3"],
  ["Sprint 6","Notifications","Préférences, Brevo, Twilio et escalade différée","2026-11-16","2026-11-27",70,"En cours","Sprint 5"],
  ["Sprint 7","Dashboard et rapports","KPI, filtres, vues sauvegardées, PDF, XLSX et CSV","2026-11-30","2026-12-11",75,"En cours","Sprint 5"],
  ["Sprint 8","IA explicable","Dérive, score de risque, similarité et recommandations","2026-12-14","2026-12-25",0,"À faire","Sprint 7"],
  ["Sprint 9","DevOps et observabilité","Docker plateforme, Jenkins, Kubernetes, Prometheus et Grafana","2026-12-28","2027-01-08",20,"À faire","Sprint 4"],
  ["Sprint 10","Stabilisation et soutenance","Tests E2E, charge, documentation, recette et démonstration","2027-01-11","2027-01-22",55,"À faire","Sprint 6, Sprint 7, Sprint 9"]
];

const epics = [
  ["EP-01","Identité, accès et profils","Must","Partiel",80,"Sécuriser l’accès, le profil et les privilèges par acteur"],
  ["EP-02","Catalogue des systèmes d’information","Must","Implémenté",90,"Configurer SI, environnements et endpoints sans recompilation"],
  ["EP-03","Moteur de monitoring","Must","Implémenté",90,"Exécuter et historiser HTTP, API JSON, HTTPS et TLS"],
  ["EP-04","Alertes et règles","Must","Implémenté",90,"Transformer les anomalies confirmées en alertes corrélées"],
  ["EP-05","Incidents et SLA","Must","Implémenté",90,"Affecter, traiter, documenter, résoudre et clôturer"],
  ["EP-06","Notifications et escalades","Must","Partiel",70,"Informer technicien, superviseur et manager IT"],
  ["EP-07","Maintenance et calendrier","Must","Implémenté",90,"Planifier les maintenances et suspendre les alertes"],
  ["EP-08","Dashboard et rapports","Must","Partiel",75,"Fournir KPI, filtres, vues et exports"],
  ["EP-09","Simulateurs des quatre SI","Must","Partiel",75,"Tester sans accès aux SI réels"],
  ["EP-10","DevOps et observabilité","Must","Partiel",20,"Industrialiser build, déploiement et supervision technique"],
  ["EP-11","IA et prédiction","Should","Non implémenté",0,"Détecter les dérives et expliquer le risque"],
  ["EP-12","Remédiation contrôlée","Could","Non implémenté",0,"Exécuter des actions autorisées en laboratoire"],
  ["EP-13","Profil enrichi et gamification","Should","Non implémenté",0,"Photo, compétences, disponibilité et badges qualité"]
];

const stories = [
  ["US-001","EP-01","Configurer l’architecture et le dépôt","Story",5,"Sprint 0","Highest","Terminé","Architecture en couches et solution .NET/Angular"],
  ["US-002","EP-10","Préparer Docker Compose PostgreSQL","Story",3,"Sprint 0","High","Terminé","Base locale reproductible"],
  ["US-003","EP-10","Créer un pipeline Jenkins minimal","Story",5,"Sprint 0","High","À faire","Build et tests automatiques"],
  ["US-101","EP-01","Se connecter avec JWT","Story",5,"Sprint 1","Highest","Terminé","Compte inactif refusé"],
  ["US-102","EP-01","Gérer les utilisateurs et rôles","Story",8,"Sprint 1","Highest","Terminé","Admin gère les comptes"],
  ["US-103","EP-01","Tracer les actions sensibles","Story",5,"Sprint 1","High","Terminé","Audit consultable et nettoyé"],
  ["US-104","EP-13","Ajouter photo, compétences et badges","Story",8,"Sprint 1","Medium","À faire","Profil enrichi du technicien"],
  ["US-201","EP-02","Créer un SI et son environnement","Story",8,"Sprint 2","Highest","Terminé","Dev, Test, Préprod et Production"],
  ["US-202","EP-02","Configurer endpoint, fréquence et seuils","Story",8,"Sprint 2","Highest","Terminé","HTTP, API JSON ou TLS"],
  ["US-203","EP-02","Gérer authentification et secrets endpoint","Story",8,"Sprint 2","High","À faire","API Key, Basic, OAuth2 et références de secrets"],
  ["US-301","EP-03","Lancer un contrôle manuel","Story",5,"Sprint 3","High","Terminé","Action réservée au superviseur"],
  ["US-302","EP-03","Planifier les contrôles avec verrou distribué","Story",8,"Sprint 3","Highest","Terminé","Aucune double exécution multi-instance"],
  ["US-303","EP-03","Contrôler HTTP et API JSON","Story",8,"Sprint 3","Highest","Terminé","Code, contenu, timeout et latence"],
  ["US-304","EP-03","Valider toute la chaîne TLS","Story",8,"Sprint 3","Highest","Terminé","Expiration, hôte, confiance et handshake"],
  ["US-305","EP-03","Télécharger une preuve technique","Story",5,"Sprint 3","Medium","En cours","Formaliser le téléchargement du résultat"],
  ["US-401","EP-09","Simuler Core Banking avec Oracle","Story",8,"Sprint 4","High","Terminé","Service et base indépendants"],
  ["US-402","EP-09","Simuler RNE avec MongoDB","Story",8,"Sprint 4","High","Terminé","Endpoint métier dépendant de MongoDB"],
  ["US-403","EP-09","Simuler SMS avec MySQL","Story",8,"Sprint 4","High","Terminé","Livraison SMS et panne base"],
  ["US-404","EP-09","Simuler RH avec SQL Server","Story",8,"Sprint 4","High","Terminé","Situation professionnelle et panne base"],
  ["US-405","EP-09","Automatiser les scénarios de panne E2E","Story",8,"Sprint 4","High","À faire","500, latence, timeout, DB down et TLS"],
  ["US-501","EP-04","Créer et gérer les règles d’alerte","Story",8,"Sprint 5","Highest","Terminé","Règles standards et personnalisables"],
  ["US-502","EP-04","Dédupliquer et corréler les alertes","Story",8,"Sprint 5","Highest","Terminé","Occurrences sans duplication"],
  ["US-503","EP-05","Créer et affecter un incident","Story",8,"Sprint 5","Highest","Terminé","Affectation au technicien actif"],
  ["US-504","EP-05","Documenter et résoudre un incident","Story",8,"Sprint 5","Highest","Terminé","Cause, actions et preuve"],
  ["US-505","EP-05","Calculer SLA, MTTD, MTTA et MTTR","Story",8,"Sprint 5","High","Terminé","Indicateurs frontend et backend"],
  ["US-601","EP-06","Créer les préférences de notification","Story",5,"Sprint 6","High","Terminé","Interne, email, SMS et délai"],
  ["US-602","EP-06","Envoyer les emails critiques avec Brevo","Story",5,"Sprint 6","Highest","En cours","Code présent, configuration réelle à tester"],
  ["US-603","EP-06","Envoyer les SMS critiques avec Twilio","Story",5,"Sprint 6","Highest","En cours","Code présent, configuration réelle à tester"],
  ["US-604","EP-06","Escalader technicien, superviseur, manager","Story",8,"Sprint 6","Highest","Terminé","Escalade persistée et différée"],
  ["US-605","EP-06","Journaliser toutes les tentatives d’envoi","Story",5,"Sprint 6","High","À faire","Statut, fournisseur, erreur et identifiant externe"],
  ["US-701","EP-08","Afficher dashboard santé et SLA","Story",8,"Sprint 7","Highest","Terminé","KPI globaux et personnels"],
  ["US-702","EP-08","Ajouter filtres et vues sauvegardées","Story",5,"Sprint 7","High","Terminé","Filtres incidents persistants"],
  ["US-703","EP-08","Exporter CSV, PDF et Excel","Story",8,"Sprint 7","High","Terminé","Exports mensuels et annuels"],
  ["US-704","EP-08","Planifier et historiser les rapports","Story",8,"Sprint 7","Medium","À faire","Worker et historique des publications"],
  ["US-705","EP-07","Gérer les maintenances dans un calendrier","Story",8,"Sprint 7","High","Terminé","Jour, semaine, mois et CRUD"],
  ["US-801","EP-11","Détecter une dérive de latence","Story",8,"Sprint 8","Medium","À faire","Baseline glissante explicable"],
  ["US-802","EP-11","Calculer un score de risque explicable","Story",8,"Sprint 8","Medium","À faire","Facteurs contributifs visibles"],
  ["US-803","EP-11","Recommander des incidents similaires","Story",8,"Sprint 8","Low","À faire","Validation humaine obligatoire"],
  ["US-901","EP-10","Dockeriser API et frontend","Story",8,"Sprint 9","Highest","À faire","Images versionnées"],
  ["US-902","EP-10","Créer pipeline Jenkins et SonarQube","Story",13,"Sprint 9","Highest","À faire","Build, tests, analyse et image"],
  ["US-903","EP-10","Déployer sur Kubernetes","Story",13,"Sprint 9","Highest","À faire","Deployment, Service, Ingress, Secret et PVC"],
  ["US-904","EP-10","Instrumenter Prometheus et Grafana","Story",8,"Sprint 9","High","À faire","Métriques API, workers et simulateurs"],
  ["US-1001","EP-10","Automatiser le cycle E2E complet","Story",13,"Sprint 10","Highest","À faire","Détection à clôture"],
  ["US-1002","EP-10","Valider tests d’intégration PostgreSQL","Story",5,"Sprint 10","Highest","En cours","Tests présents, base locale requise"],
  ["US-1003","EP-10","Tester charge, sécurité et régression","Story",8,"Sprint 10","High","À faire","Recette sans anomalie bloquante"],
  ["US-1004","EP-10","Finaliser rapport et démonstration","Story",8,"Sprint 10","High","En cours","Rapport rédigé, scénario à répéter"]
];

const wb = Workbook.create();
wb.comments.setSelf({ displayName: "Malek Jendoubi" });
const dashboard = wb.worksheets.add("Pilotage");
const pb = wb.worksheets.add("Product Backlog");
const jira = wb.worksheets.add("Import Jira");
const gantt = wb.worksheets.add("Gantt");
const guide = wb.worksheets.add("Guide Jira");

for (const sh of [dashboard,pb,jira,gantt,guide]) sh.showGridLines = false;

function title(sheet, range, text, subtitle) {
  sheet.getRange(range).merge(); sheet.getRange(range).values = [[text]];
  sheet.getRange(range).format = { fill: navy, font: { bold: true, color: "#FFFFFF", size: 20 }, verticalAlignment: "center" };
  sheet.getRange(range).format.rowHeight = 38;
  if (subtitle) { const r = sheet.getRange("A2:H2"); r.merge(); r.values=[[subtitle]]; r.format={fill:paleBlue,font:{color:muted,italic:true},wrapText:true}; r.format.rowHeight=30; }
}

title(dashboard,"A1:H1","STB Sentinel — Pilotage Jira et Gantt","Planning prévisionnel au 21 août 2026 · sprints de deux semaines · dates ajustables");
dashboard.getRange("A4:B7").values=[["INDICATEUR","VALEUR"],["Epics",null],["User Stories",null],["Avancement pondéré",null]];
dashboard.getRange("B5").formulas=[["=COUNTA('Product Backlog'!A5:A40)"]];
dashboard.getRange("B6").formulas=[["=COUNTA('Import Jira'!A5:A100)"]];
dashboard.getRange("B7").formulas=[["=SUMPRODUCT('Product Backlog'!E5:E17,'Product Backlog'!G5:G17)/SUM('Product Backlog'!G5:G17)"]];
dashboard.getRange("B7").format.numberFormat="0%";
dashboard.getRange("A4:B4").format={fill:teal,font:{bold:true,color:"#FFFFFF"}};
dashboard.getRange("A5:B7").format={fill:"#FFFFFF",borders:{preset:"inside",style:"thin",color:"#DDE6EA"}};
dashboard.getRange("D4:H4").merge(); dashboard.getRange("D4:H4").values=[["Décisions de planification"]]; dashboard.getRange("D4:H4").format={fill:teal,font:{bold:true,color:"#FFFFFF"}};
dashboard.getRange("D5:H9").merge(); dashboard.getRange("D5:H9").values=[["1. Utiliser un projet Scrum géré par l’entreprise.\n2. Hiérarchie : Epic → Story → Sous-tâche.\n3. Conserver les états actuels dans un champ personnalisé.\n4. Prioriser d’abord E2E, notifications réelles et DevOps.\n5. Utiliser Timeline pour le Gantt d’un seul projet ; Plans pour plusieurs projets."]];
dashboard.getRange("D5:H9").format={fill:paleTeal,font:{color:ink},wrapText:true,verticalAlignment:"top"};
dashboard.getRange("A11:H11").merge(); dashboard.getRange("A11:H11").values=[["Ordre conseillé des prochains travaux"]]; dashboard.getRange("A11:H11").format={fill:navy,font:{bold:true,color:"#FFFFFF"}};
dashboard.getRange("A12:H16").values=[
  ["1","Tests","Démarrer PostgreSQL et valider tous les tests d’intégration","Highest","Sprint 10","Bloquant pour la recette","",""],
  ["2","Notifications","Configurer Brevo/Twilio et journaliser les envois","Highest","Sprint 6","Validation réelle requise","",""],
  ["3","E2E","Automatiser détection → alerte → incident → clôture","Highest","Sprint 10","Critère majeur de soutenance","",""],
  ["4","DevOps","Dockeriser plateforme puis Jenkins/Kubernetes","High","Sprint 9","Industrialisation","",""],
  ["5","Profil","Photo, compétences, disponibilité et badges qualité","Medium","Après MVP","Valeur utilisateur","",""]
];
dashboard.getRange("A12:F16").format={fill:"#FFFFFF",borders:{preset:"inside",style:"thin",color:"#E0E8EC"},wrapText:true};
dashboard.getRange("A:H").format.font={name:"Aptos",color:ink};
dashboard.getRange("A:A").format.columnWidth=10; dashboard.getRange("B:B").format.columnWidth=18; dashboard.getRange("C:C").format.columnWidth=48;
dashboard.getRange("D:F").format.columnWidth=18; dashboard.getRange("G:H").format.columnWidth=12;

title(pb,"A1:G1","Product Backlog STB Sentinel","Epics ordonnés par valeur métier, état réel et avancement estimé");
pb.getRange("A4:G4").values=[["Epic","Intitulé","MoSCoW","État réel","Avancement","Objectif métier","Poids"]];
pb.getRange("A5:G17").values=epics.map((x,i)=>[...x,i<10?3:i===10?2:1]);
pb.getRange("E5:E17").format.numberFormat="0%";
pb.getRange("E5:E17").values=epics.map(x=>[x[4]/100]);
pb.getRange("A4:G4").format={fill:navy,font:{bold:true,color:"#FFFFFF"}};
pb.getRange("A5:G17").format={wrapText:true,borders:{preset:"inside",style:"thin",color:"#E4EBEF"}};
pb.getRange("D5:D17").conditionalFormats.add("containsText",{text:"Implémenté",format:{fill:"#DFF3E7",font:{color:success,bold:true}}});
pb.getRange("D5:D17").conditionalFormats.add("containsText",{text:"Partiel",format:{fill:paleOrange,font:{color:"#9A5B00",bold:true}}});
pb.getRange("D5:D17").conditionalFormats.add("containsText",{text:"Non implémenté",format:{fill:paleRed,font:{color:danger,bold:true}}});
pb.getRange("E5:E17").conditionalFormats.add("dataBar",{color:teal,gradient:true});
pb.getRange("A:A").format.columnWidth=12; pb.getRange("B:B").format.columnWidth=34; pb.getRange("C:D").format.columnWidth=18; pb.getRange("E:E").format.columnWidth=16; pb.getRange("F:F").format.columnWidth=55; pb.getRange("G:G").format.columnWidth=10;
pb.freezePanes.freezeRows(4); pb.tables.add("A4:G17",true,"ProductBacklogTable").style="TableStyleMedium2";

title(jira,"A1:J1","Backlog prêt pour Jira","Copier cette feuille en CSV UTF-8 puis mapper les colonnes dans l’assistant d’import Jira");
jira.getRange("A4:J4").values=[["External ID","Epic Link","Summary","Issue Type","Story Points","Sprint","Priority","Status","Description / Acceptance","Labels"]];
jira.getRange(`A5:J${stories.length+4}`).values=stories.map(x=>[x[0],x[1],x[2],x[3],x[4],x[5],x[6],x[7],x[8],`stb-sentinel,${x[5].toLowerCase().replace(" ","-")}`]);
jira.getRange("A4:J4").format={fill:navy,font:{bold:true,color:"#FFFFFF"},wrapText:true};
jira.getRange(`A5:J${stories.length+4}`).format={wrapText:true,borders:{preset:"inside",style:"thin",color:"#E4EBEF"},verticalAlignment:"top"};
jira.getRange(`H5:H${stories.length+4}`).conditionalFormats.add("containsText",{text:"Terminé",format:{fill:"#DFF3E7",font:{color:success,bold:true}}});
jira.getRange(`H5:H${stories.length+4}`).conditionalFormats.add("containsText",{text:"En cours",format:{fill:paleOrange,font:{color:"#9A5B00",bold:true}}});
jira.getRange(`H5:H${stories.length+4}`).conditionalFormats.add("containsText",{text:"À faire",format:{fill:paleRed,font:{color:danger,bold:true}}});
jira.getRange(`G5:G${stories.length+4}`).dataValidation={rule:{type:"list",values:["Highest","High","Medium","Low","Lowest"]}};
jira.getRange(`H5:H${stories.length+4}`).dataValidation={rule:{type:"list",values:["À faire","En cours","Terminé","Bloqué"]}};
jira.getRange("A:A").format.columnWidth=14; jira.getRange("B:B").format.columnWidth=12; jira.getRange("C:C").format.columnWidth=42; jira.getRange("D:D").format.columnWidth=13; jira.getRange("E:E").format.columnWidth=12; jira.getRange("F:H").format.columnWidth=16; jira.getRange("I:I").format.columnWidth=55; jira.getRange("J:J").format.columnWidth=24;
jira.freezePanes.freezeRows(4); jira.tables.add(`A4:J${stories.length+4}`,true,"JiraImportTable").style="TableStyleMedium2";

title(gantt,"A1:AB1","Diagramme de Gantt — STB Sentinel","Modifier les dates de début/fin : les barres hebdomadaires sont calculées automatiquement");
gantt.getRange("A4:M4").values=[["Sprint","Gestion","Objectif","État","Progression","Dépendance","Début","Fin","Durée (j)","SP prévus","SP réalisés","Risque","Responsable"]];
const sprintRows=sprints.map((x,i)=>[x[0],x[1],x[2],x[6],x[5]/100,x[7],d(x[3]),d(x[4]),null,[18,26,29,31,37,40,36,37,24,42,39][i],null,i===8||i===9?"Élevé":i===6||i===10?"Moyen":"Faible",i===0?"Équipe":i===9?"DevOps":"Malek Jendoubi"]);
gantt.getRange("A5:M15").values=sprintRows;
gantt.getRange("I5").formulas=[["=H5-G5+1"]]; gantt.getRange("I5:I15").fillDown();
gantt.getRange("K5").formulas=[["=ROUND(J5*E5,0)"]]; gantt.getRange("K5:K15").fillDown();
gantt.getRange("E5:E15").format.numberFormat="0%"; gantt.getRange("G5:H15").format.numberFormat="dd/mm/yyyy";
gantt.getRange("A4:M4").format={fill:navy,font:{bold:true,color:"#FFFFFF"},wrapText:true}; gantt.getRange("A5:M15").format={wrapText:true,borders:{preset:"inside",style:"thin",color:"#E4EBEF"}};
gantt.getRange("D5:D15").dataValidation={rule:{type:"list",values:["À faire","En cours","Terminé","Bloqué"]}};
gantt.getRange("L5:L15").dataValidation={rule:{type:"list",values:["Faible","Moyen","Élevé"]}};
gantt.getRange("E5:E15").conditionalFormats.add("dataBar",{color:teal,gradient:true});
gantt.getRange("L5:L15").conditionalFormats.add("containsText",{text:"Élevé",format:{fill:paleRed,font:{color:danger,bold:true}}});
gantt.getRange("L5:L15").conditionalFormats.add("containsText",{text:"Moyen",format:{fill:paleOrange,font:{color:"#9A5B00",bold:true}}});
const weekStart=d("2026-08-24");
for(let c=0;c<23;c++){
  const col=colName(14+c); const dt=addDays(weekStart,c*7);
  gantt.getRange(`${col}4`).values=[[dt]]; gantt.getRange(`${col}4`).format.numberFormat="dd-mmm";
  gantt.getRange(`${col}5`).formulas=[[`=IF(AND(${col}$4<=$H5,${col}$4+6>=$G5),1,"")`]]; gantt.getRange(`${col}5:${col}15`).fillDown();
  gantt.getRange(`${col}5:${col}15`).format.numberFormat=';;;';
  gantt.getRange(`${col}5:${col}15`).conditionalFormats.add("cellIs",{operator:"equal",formula:1,format:{fill:c%2===0?teal:cyan}});
  gantt.getRange(`${col}:${col}`).format.columnWidth=11;
}
gantt.getRange("A:A").format.columnWidth=12; gantt.getRange("B:B").format.columnWidth=28; gantt.getRange("C:C").format.columnWidth=48; gantt.getRange("D:F").format.columnWidth=16; gantt.getRange("G:H").format.columnWidth=13; gantt.getRange("I:M").format.columnWidth=13;
gantt.freezePanes.freezeRows(4); gantt.freezePanes.freezeColumns(3);
gantt.tables.add("A4:M15",true,"SprintPlanTable").style="TableStyleMedium2";

title(guide,"A1:H1","Guide de création dans Jira","Procédure recommandée pour un projet Scrum STB Sentinel");
guide.getRange("A1:H1").unmerge(); guide.getRange("A1:D1").merge(); guide.getRange("A1:D1").values=[["Guide de création dans Jira"]]; guide.getRange("A1:D1").format={fill:navy,font:{bold:true,color:"#FFFFFF",size:20},verticalAlignment:"center"}; guide.getRange("A1:D1").format.rowHeight=38;
guide.getRange("A2:H2").unmerge(); guide.getRange("A2:D2").merge(); guide.getRange("A2:D2").values=[["Procédure recommandée pour un projet Scrum STB Sentinel"]]; guide.getRange("A2:D2").format={fill:paleBlue,font:{color:muted,italic:true},wrapText:true};
const guideRows=[
 ["1","Créer le projet","Projects → Create project → Software development → Scrum → Company-managed","Choisir la clé STBS"],
 ["2","Configurer les types","Conserver Epic, Story, Task, Bug et Sub-task","Epic → Story/Task → Sub-task"],
 ["3","Créer les champs","Story Points, Start date, Due date, État d’implémentation, MoSCoW et Risque","Les dates alimentent Timeline"],
 ["4","Créer les Epics","Reprendre les lignes EP-01 à EP-13 de Product Backlog","Un Epic représente une grande gestion"],
 ["5","Importer les Stories","Exporter la feuille Import Jira en CSV UTF-8 puis External system import → CSV","Mapper Summary, Issue Type, Epic Link, Sprint, Priority et Story Points"],
 ["6","Créer les sprints","Backlog → Create sprint, de Sprint 0 à Sprint 10","Durée proposée : deux semaines"],
 ["7","Planifier","Glisser les Stories dans leur sprint et contrôler la capacité","20 à 30 SP au départ, à recalibrer"],
 ["8","Ajouter les dépendances","Utiliser le lien Blocks / Is blocked by","Ex. Sprint 6 dépend du Sprint 5"],
 ["9","Créer le Gantt Jira","Ouvrir Timeline dans le projet ; renseigner Start date et Due date","La Timeline est la vue Gantt du projet"],
 ["10","Planning avancé","Avec Jira Premium : Plans → Create plan → choisir projet/board/filter","Utile pour plusieurs projets, équipes et capacité"],
 ["11","Workflow","À faire → En cours → Revue → Test → Terminé ; ajouter Bloqué","La Definition of Done doit être respectée"],
 ["12","Pilotage","Suivre burndown, vélocité, CFD, anomalies et blocages","Faire planning, daily, review et rétrospective"]
];
guide.getRange("A4:D4").values=[["Étape","Action","Chemin / opération Jira","Conseil STB Sentinel"]];
guide.getRange("A5:D16").values=guideRows;
guide.getRange("A4:D4").format={fill:navy,font:{bold:true,color:"#FFFFFF"}}; guide.getRange("A5:D16").format={wrapText:true,verticalAlignment:"top",borders:{preset:"inside",style:"thin",color:"#E2E9ED"}};
guide.getRange("A:A").format.columnWidth=9; guide.getRange("B:B").format.columnWidth=24; guide.getRange("C:C").format.columnWidth=68; guide.getRange("D:D").format.columnWidth=48;
guide.getRange("A18:D18").merge(); guide.getRange("A18:D18").values=[["Sources officielles Atlassian"]]; guide.getRange("A18:D18").format={fill:teal,font:{bold:true,color:"#FFFFFF"}};
guide.getRange("A19:D21").values=[
 ["Timeline","https://support.atlassian.com/jira-software-cloud/docs/what-is-the-timeline-and-how-do-i-use-it/","Gantt d’un seul projet",null],
 ["Plans Premium","https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-roadmaps/","Planification multi-projets et capacité",null],
 ["Import CSV","https://support.atlassian.com/jira-cloud-administration/docs/import-and-export-your-data-to-and-from-jira-cloud/","Importer le backlog",null]
];
guide.getRange("A19:D21").format={wrapText:true,fill:paleBlue};

for(const sh of [dashboard,pb,jira,gantt,guide]){
  const used=sh.getUsedRange(); used.format.font={name:"Aptos",color:ink,size:10};
  sh.getRange("A1:AZ1").format.font={name:"Aptos",color:"#FFFFFF",bold:true,size:20};
  sh.getRange("A2:AZ2").format.font={name:"Aptos",color:muted,italic:true,size:10};
}

wb.comments.addThread({cell:gantt.getRange("G5")},"Les dates sont prévisionnelles. Modifier Début et Fin pour adapter le planning à la date réelle de soutenance.");
wb.comments.addThread({cell:jira.getRange("B4")},"Lors de l’import CSV, mapper cette colonne vers Epic Link ou Parent selon le type de projet Jira.");

const inspect1=await wb.inspect({kind:"table",sheetId:"Gantt",range:"A4:Z15",include:"values,formulas",tableMaxRows:15,tableMaxCols:26,maxChars:6000});
console.log(inspect1.ndjson);
const errors=await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:100},summary:"final formula error scan"});
console.log(errors.ndjson);
for(const [name,range,file] of [["Pilotage","A1:H16","pilotage.png"],["Product Backlog","A1:G17","product-backlog.png"],["Import Jira","A1:J28","import-jira.png"],["Gantt","A1:AJ15","gantt.png"],["Guide Jira","A1:D21","guide-jira.png"]]){
  const preview=await wb.render({sheetName:name,range,scale:1,format:"png"});
  await fs.writeFile(`${outDir}/${file}`,new Uint8Array(await preview.arrayBuffer()));
}
const xlsx=await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(`${outDir}/Planification_Jira_Gantt_STB_Sentinel.xlsx`);
console.log(`${outDir}/Planification_Jira_Gantt_STB_Sentinel.xlsx`);
