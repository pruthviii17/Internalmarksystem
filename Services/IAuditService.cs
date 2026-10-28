namespace InternalMarksSystem.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string? entityName = null, string? entityId = null, string? details = null);
    }
}
