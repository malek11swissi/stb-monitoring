namespace StbMonitoring.Domain.Entities;
/// <summary>Canaux et délai d'escalade choisis par un utilisateur.</summary>
public sealed class UserNotificationPreference
{
    private UserNotificationPreference(){} public UserNotificationPreference(Guid userId){UserId=userId;}
    public Guid UserId{get;private set;}public bool InAppEnabled{get;private set;}=true;public bool EmailEnabled{get;private set;}=true;public bool SmsEnabled{get;private set;}public string? PhoneNumber{get;private set;}public bool CriticalOnly{get;private set;}=true;public int EscalationDelayMinutes{get;private set;}=15;
    public void Update(bool inApp,bool email,bool sms,string? phone,bool criticalOnly,int delay){if(delay is <1 or >1440)throw new ArgumentOutOfRangeException(nameof(delay));InAppEnabled=inApp;EmailEnabled=email;SmsEnabled=sms;PhoneNumber=phone?.Trim();CriticalOnly=criticalOnly;EscalationDelayMinutes=delay;}
}
