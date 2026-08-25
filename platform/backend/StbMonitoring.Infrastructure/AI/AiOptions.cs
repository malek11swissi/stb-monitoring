namespace StbMonitoring.Infrastructure.AI;
public sealed class AiOptions
{
    public const string Section="Ai";
    public string BaseUrl{get;set;}="http://localhost:5055";
    public int TimeoutSeconds{get;set;}=10;
    public int HistoryHours{get;set;}=24;
    public int MaxSamplesPerEndpoint{get;set;}=120;
}
