namespace StbMonitoring.Domain.Entities;
// États et types communs persistés sous forme de texte par Entity Framework Core.
public enum MonitoringStatus { Unknown, Up, Degraded, Down }
public enum SystemEnvironment { Production, Preproduction, Recette, Development }
public enum SystemCriticality { Low, Medium, High, Critical }
public enum CheckType { Http, ApiJson, Tls, Database }
public enum DatabaseEngine { MongoDb, Oracle, SqlServer, MySql }
public enum DatabaseRole { Primary, Secondary }
