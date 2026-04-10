using FoodStreetGuide.Admin.Models;

namespace FoodStreetGuide.Admin.Data;

public class AppDbContext
{
    private static readonly List<POI> _pois = new()
    {
        new POI { Id = 1, NameVi = "Phở Hòa", NameEn = "Pho Hoa", DescriptionVi = "Phở bò truyền thống", Latitude = 10.762622, Longitude = 106.660172, Radius = 100, Priority = 1 },
        new POI { Id = 2, NameVi = "Bánh Mì Huỳnh Hoa", NameEn = "Banh Mi Huynh Hoa", DescriptionVi = "Bánh mì thịt nguội", Latitude = 10.770765, Longitude = 106.672288, Radius = 100, Priority = 2 },
        new POI { Id = 3, NameVi = "Cơm Tấm Sườn Nướng", NameEn = "Broken Rice", DescriptionVi = "Cơm tấm sườn bì chả", Latitude = 10.755432, Longitude = 106.682345, Radius = 100, Priority = 1 }
    };
    
    public List<POI> POIs => _pois;
    
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
}
