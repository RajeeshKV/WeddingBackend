namespace WeddingInvite.Application.Interfaces;

public interface ICacheService
{
    string GetWeddingPublicConfigKey(Guid weddingId);
    string GetWeddingGalleryKey(Guid weddingId, string queryHash);
    string GetWeddingEventsKey(Guid weddingId);
    string GetWeddingSettingsKey(Guid weddingId);
    string GetAdminDashboardKey(Guid weddingId, string queryHash);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    void RemoveWedding(string weddingId);
}
