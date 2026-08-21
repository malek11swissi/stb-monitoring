namespace StbMonitoring.Domain.Entities;
/// <summary>Jeu de filtres JSON réutilisable par son propriétaire.</summary>
public sealed class SavedView
{
    private SavedView(){}public SavedView(Guid userId,string name,string scope,string filters){Id=Guid.NewGuid();UserId=userId;Update(name,scope,filters);}
    public Guid Id{get;private set;}public Guid UserId{get;private set;}public string Name{get;private set;}="";public string Scope{get;private set;}="";public string FiltersJson{get;private set;}="{}";public DateTime CreatedAt{get;private set;}=DateTime.UtcNow;
    public void Update(string name,string scope,string filters){if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(scope))throw new ArgumentException("Nom et périmètre obligatoires.");Name=name.Trim();Scope=scope.Trim();FiltersJson=filters;}
}
