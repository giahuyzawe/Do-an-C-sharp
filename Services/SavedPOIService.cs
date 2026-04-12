using System.Collections.ObjectModel;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface ISavedPOIService
{
    ObservableCollection<POI> SavedPOIs { get; }
    event EventHandler? SavedPOIsChanged;
    bool IsSaved(POI poi);
    void ToggleSave(POI poi);
    void Save(POI poi);
    void Unsave(POI poi);
}

public class SavedPOIService : ISavedPOIService
{
    public ObservableCollection<POI> SavedPOIs { get; } = new();
    public event EventHandler? SavedPOIsChanged;

    public bool IsSaved(POI poi)
    {
        return SavedPOIs.Any(p => p.Id == poi.Id);
    }

    public void ToggleSave(POI poi)
    {
        if (IsSaved(poi))
        {
            Unsave(poi);
        }
        else
        {
            Save(poi);
        }
    }

    public void Save(POI poi)
    {
        if (!IsSaved(poi))
        {
            SavedPOIs.Add(poi);
            System.Diagnostics.Debug.WriteLine($"[SavedPOIService] Saved POI: {poi.NameVi}");
            SavedPOIsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Unsave(POI poi)
    {
        var existing = SavedPOIs.FirstOrDefault(p => p.Id == poi.Id);
        if (existing != null)
        {
            SavedPOIs.Remove(existing);
            System.Diagnostics.Debug.WriteLine($"[SavedPOIService] Unsaved POI: {poi.NameVi}");
            SavedPOIsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
