/** Client HTTP et modèles frontend du catalogue SI, des endpoints et contrôles. */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export type MonitoringStatus = 'Unknown'|'Up'|'Degraded'|'Down';
export interface MonitoredSystem { id:string;code:string;name:string;description:string;environment:string;criticality:string;owner?:string;monitoringEnabled:boolean;isArchived:boolean;status:MonitoringStatus;lastCheckedAt?:string;endpointCount:number;upCount:number;degradedCount:number;downCount:number }
export interface Endpoint { id:string;systemId:string;name:string;url:string;checkType:string;httpMethod:string;expectedStatusCode:number;timeoutSeconds:number;intervalSeconds:number;degradedThresholdMs:number;downThresholdMs:number;isCritical:boolean;isActive:boolean;expectedJsonProperty?:string;expectedJsonValue?:string;status:MonitoringStatus;lastCheckedAt?:string;nextCheckAt:string;lastDurationMs?:number;lastError?:string }
export interface CheckResult { id:string;systemId:string;endpointId:string;endpointName:string;status:MonitoringStatus;success:boolean;startedAt:string;completedAt:string;durationMs:number;httpStatusCode?:number;errorType?:string;errorMessage?:string;triggeredManually:boolean;metadata?:string }
export interface SystemDetail { system:MonitoredSystem;endpoints:Endpoint[];recentResults:CheckResult[] }
export interface SystemDraft { code:string;name:string;description:string;environment:string;criticality:string;owner:string }
export interface EndpointDraft { name:string;url:string;checkType:string;httpMethod:string;expectedStatusCode:number;timeoutSeconds:number;intervalSeconds:number;degradedThresholdMs:number;downThresholdMs:number;isCritical:boolean;expectedJsonProperty:string;expectedJsonValue:string }
export interface PagedResult<T>{items:T[];total:number;page:number;pageSize:number;totalPages:number}
export interface SystemRiskPrediction{available:boolean;systemId:string;modelName:string;modelVersion:string;modelSource:string;sampleCount:number;risk15Minutes:number;risk30Minutes:number;risk60Minutes:number;riskScore:number;riskLevel:string;confidence:number;estimatedTimeToDownMinutes?:number;factors:{code:string;label:string;contribution:number}[];mostRiskyEndpoints:{endpointId:string;name:string;riskScore:number;riskLevel:string}[];generatedAt:string;message?:string}

@Injectable({providedIn:'root'}) export class MonitoringService {
  private readonly api='http://localhost:5041/api/systems';
  constructor(private http:HttpClient){}
  systems(includeArchived=false){return this.http.get<MonitoredSystem[]>(`${this.api}?includeArchived=${includeArchived}`)}
  systemsPaged(filters:Record<string,string|number|boolean>){const q=new URLSearchParams();Object.entries(filters).forEach(([k,v])=>{if(v!==''&&v!==undefined)q.set(k,String(v))});return this.http.get<PagedResult<MonitoredSystem>>(`${this.api}/paged?${q}`)}
  detail(id:string){return this.http.get<SystemDetail>(`${this.api}/${id}`)}
  create(value:SystemDraft){return this.http.post<MonitoredSystem>(this.api,value)}
  update(id:string,value:SystemDraft){return this.http.put<MonitoredSystem>(`${this.api}/${id}`,value)}
  monitoring(id:string,enabled:boolean){return this.http.patch<void>(`${this.api}/${id}/monitoring?enabled=${enabled}`,{})}
  archive(id:string,value:boolean){return this.http.patch<void>(`${this.api}/${id}/archived?value=${value}`,{})}
  delete(id:string){return this.http.delete<void>(`${this.api}/${id}`)}
  createEndpoint(systemId:string,value:EndpointDraft){return this.http.post<Endpoint>(`${this.api}/${systemId}/endpoints`,value)}
  updateEndpoint(id:string,value:EndpointDraft){return this.http.put<Endpoint>(`${this.api}/endpoints/${id}`,value)}
  endpointActive(id:string,value:boolean){return this.http.patch<void>(`${this.api}/endpoints/${id}/active?value=${value}`,{})}
  deleteEndpoint(id:string){return this.http.delete<void>(`${this.api}/endpoints/${id}`)}
  execute(id:string){return this.http.post<CheckResult>(`${this.api}/endpoints/${id}/execute`,{})}
  checks(filters:{systemId?:string;endpointId?:string;status?:string;from?:string;to?:string;take?:number}){const q=new URLSearchParams();Object.entries(filters).forEach(([k,v])=>{if(v!==undefined&&v!=='')q.set(k,String(v))});return this.http.get<CheckResult[]>(`${this.api}/checks?${q}`)}
  checksPaged(filters:Record<string,string|number>){const q=new URLSearchParams();Object.entries(filters).forEach(([k,v])=>{if(v!=='')q.set(k,String(v))});return this.http.get<PagedResult<CheckResult>>(`${this.api}/checks/paged?${q}`)}
  evidence(id:string){return this.http.get(`${this.api}/checks/${id}/evidence`,{responseType:'blob'})}
  systemRisk(id:string){return this.http.get<SystemRiskPrediction>(`http://localhost:5041/api/ai/systems/${id}/risk`)}
}
