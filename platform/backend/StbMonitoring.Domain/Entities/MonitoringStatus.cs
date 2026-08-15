namespace StbMonitoring.Domain.Entities;
public enum MonitoringStatus { Unknown, Up, Degraded, Down }
public enum SystemEnvironment { Production, Preproduction, Recette, Development }
public enum SystemCriticality { Low, Medium, High, Critical }
public enum CheckType { Http, ApiJson, Tls }
