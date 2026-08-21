/** Client HTTP des alertes, incidents, SLA, notifications, maintenance et rapports. */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AlertItem { id:string;alertNumber:string;systemId:string;systemName:string;endpointId:string;endpointName:string;type:string;title:string;description:string;severity:string;status:string;occurrenceCount:number;firstDetectedAt:string;lastDetectedAt:string;acknowledgedAt?:string;resolvedAt?:string;incidentId?:string;lastError?:string }
export interface AlertRule { id:string;name:string;description:string;eventType:string;severity:string;consecutiveFailures:number;deduplicationMinutes:number;autoResolve:boolean;notifyInApp:boolean;isActive:boolean;systemId?:string;endpointId?:string }
export interface Incident { id:string;incidentNumber:string;alertId?:string;systemId?:string;endpointId?:string;systemName?:string;title:string;description:string;category:string;priority:string;status:string;slaStatus:string;assignedToUserId?:string;assignedToName?:string;responseDueAt?:string;resolutionDueAt?:string;resolvedAt?:string;resolutionSummary?:string;rootCause?:string;correctiveAction?:string;preventiveAction?:string;resolutionEvidence?:string;cancellationReason?:string;cancelledAt?:string;isArchived:boolean;archivedAt?:string;archivedByUserId?:string;reopenCount:number;createdAt:string;updatedAt:string }
export interface IncidentDetail { incident:Incident;comments:{id:string;userName:string;content:string;commentType:string;isInternal:boolean;createdAt:string}[];history:{id:string;action:string;oldValue?:string;newValue?:string;details?:string;createdAt:string}[] }
export interface NotificationItem { id:string;type:string;title:string;message:string;severity:string;entityType?:string;entityId?:string;actionUrl?:string;isRead:boolean;createdAt:string }
export interface OperationsSummary { openAlerts:number;criticalAlerts:number;openIncidents:number;unassignedIncidents:number;slaBreached:number;unreadNotifications:number }
export interface AlertDetail { alert:AlertItem;occurrences:{id:string;checkResultId:string;status:string;errorType?:string;errorMessage?:string;detectedAt:string}[] }
export interface SlaPolicy { id:string;name:string;description:string;priority:string;responseTimeMinutes:number;resolutionTimeMinutes:number;warningPercentage:number;isActive:boolean }
export interface ReportingDashboard {from:string;to:string;generatedAt:string;personal:boolean;kpis:{systems:number;availability:number;openAlerts:number;openIncidents:number;unassignedIncidents:number;slaBreached:number;slaAtRisk:number;meanDetectionMinutes:number;meanAcknowledgementMinutes:number;meanResolutionMinutes:number;meanResponseMinutes:number;slaComplianceRate:number};incidentsByStatus:{key:string;count:number}[];incidentsByPriority:{key:string;count:number}[];incidentsByCategory:{key:string;count:number}[];slaByStatus:{key:string;count:number}[];alertsBySeverity:{key:string;count:number}[];systemHealth:{id:string;name:string;status:string;checks:number;availability:number;averageResponseMs:number}[];trends:{date:string;incidents:number;alerts:number}[]}
export interface MaintenanceWindow {id:string;title:string;description?:string;systemId?:string;endpointId?:string;startsAt:string;endsAt:string;suppressAlerts:boolean;isCancelled:boolean;createdByUserId:string;createdAt:string}
export interface SavedView {id:string;name:string;scope:string;filtersJson:string}
export interface IncidentAttachment {id:string;fileName:string;contentType:string;size:number;isResolutionProof:boolean;createdAt:string}

@Injectable({ providedIn:'root' })
export class OperationsService {
  private readonly api='http://localhost:5041/api';
  constructor(private http:HttpClient) {}
  alerts(){return this.http.get<AlertItem[]>(`${this.api}/alerts`)}
  alert(id:string){return this.http.get<AlertDetail>(`${this.api}/alerts/${id}`)}
  ack(id:string){return this.http.post<void>(`${this.api}/alerts/${id}/acknowledge`,{})}
  resolveAlert(id:string){return this.http.post<void>(`${this.api}/alerts/${id}/resolve`,{})}
  closeAlert(id:string){return this.http.post<void>(`${this.api}/alerts/${id}/close`,{})}
  rules(){return this.http.get<AlertRule[]>(`${this.api}/alert-rules`)}
  saveRule(rule:any,id?:string){return id?this.http.put(`${this.api}/alert-rules/${id}`,rule):this.http.post(`${this.api}/alert-rules`,rule)}
  ruleActive(id:string,value:boolean){return this.http.patch(`${this.api}/alert-rules/${id}/active?value=${value}`,{})}
  deleteRule(id:string){return this.http.delete(`${this.api}/alert-rules/${id}`)}
  incidents(archived=false){return this.http.get<Incident[]>(`${this.api}/incidents`,{params:{archived}})}
  incident(id:string){return this.http.get<IncidentDetail>(`${this.api}/incidents/${id}`)}
  createIncident(value:any){return this.http.post<Incident>(`${this.api}/incidents`,value)}
  updateIncident(id:string,value:any){return this.http.put<Incident>(`${this.api}/incidents/${id}`,value)}
  assign(id:string,userId:string){return this.http.post(`${this.api}/incidents/${id}/assign`,{userId})}
  action(id:string,action:string,body:any={}){return this.http.post(`${this.api}/incidents/${id}/${action}`,body)}
  deleteIncident(id:string,reason:string){return this.http.delete(`${this.api}/incidents/${id}`,{body:{reason}})}
  comment(id:string,content:string){return this.http.post(`${this.api}/incidents/${id}/comments`,{content,commentType:'Comment',isInternal:true})}
  attachments(id:string){return this.http.get<IncidentAttachment[]>(`${this.api}/incidents/${id}/attachments`)}
  uploadAttachment(id:string,file:File,proof:boolean){const form=new FormData();form.append('file',file);form.append('isResolutionProof',String(proof));return this.http.post<IncidentAttachment>(`${this.api}/incidents/${id}/attachments`,form)}
  downloadAttachment(id:string){return this.http.get(`${this.api}/incidents/attachments/${id}/download`,{responseType:'blob'})}
  savedViews(scope:string){return this.http.get<SavedView[]>(`${this.api}/user-preferences/views`,{params:{scope}})}
  createSavedView(value:any){return this.http.post<SavedView>(`${this.api}/user-preferences/views`,value)}
  deleteSavedView(id:string){return this.http.delete(`${this.api}/user-preferences/views/${id}`)}
  slas(){return this.http.get<SlaPolicy[]>(`${this.api}/sla-policies`)}
  saveSla(value:any,id?:string){return id?this.http.put<SlaPolicy>(`${this.api}/sla-policies/${id}`,value):this.http.post<SlaPolicy>(`${this.api}/sla-policies`,value)}
  slaActive(id:string,value:boolean){return this.http.patch(`${this.api}/sla-policies/${id}/active?value=${value}`,{})}
  notifications(){return this.http.get<NotificationItem[]>(`${this.api}/notifications`)}
  read(id:string){return this.http.patch(`${this.api}/notifications/${id}/read`,{})}
  readAll(){return this.http.post(`${this.api}/notifications/read-all`,{})}
  summary(){return this.http.get<OperationsSummary>(`${this.api}/operations/summary`)}
  reporting(from:string,to:string){return this.http.get<ReportingDashboard>(`${this.api}/reporting/dashboard`,{params:{from,to}})}
  exportIncidents(from:string,to:string){return this.http.get(`${this.api}/reporting/incidents.csv`,{params:{from,to},responseType:'blob'})}
  exportReport(format:'pdf'|'xlsx',period:'custom'|'monthly'|'annual',from:string,to:string){return this.http.get(`${this.api}/reporting/incidents.${format}`,{params:{period,from,to},responseType:'blob'})}
  maintenances(from:string,to:string){return this.http.get<MaintenanceWindow[]>(`${this.api}/maintenance-windows`,{params:{from,to}})}
  createMaintenance(value:any){return this.http.post<MaintenanceWindow>(`${this.api}/maintenance-windows`,value)}
  updateMaintenance(id:string,value:any){return this.http.put<MaintenanceWindow>(`${this.api}/maintenance-windows/${id}`,value)}
  cancelMaintenance(id:string){return this.http.post(`${this.api}/maintenance-windows/${id}/cancel`,{})}
  deleteMaintenance(id:string){return this.http.delete(`${this.api}/maintenance-windows/${id}`)}
}
