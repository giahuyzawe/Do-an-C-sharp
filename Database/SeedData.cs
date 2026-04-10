using FoodStreetGuide.Models;

namespace FoodStreetGuide.Database;

public static class SeedData
{
    public static List<POI> GetTestPOIs()
    {
        return new List<POI>
        {
            new POI
            {
                NameVi = "Phở Hòa Pasteur",
                NameEn = "Pho Hoa Pasteur",
                DescriptionVi = "Quán phở nổi tiếng tại TP.HCM với hương vị truyền thống",
                DescriptionEn = "Famous pho restaurant in Ho Chi Minh City with traditional flavor",
                Latitude = 10.7714983,
                Longitude = 106.694,
                Radius = 100, // 100 meters
                Priority = 1,
                Image = "pho_hoa.jpg",
                AudioVi = "",
                AudioEn = "",
                MapUrl = "",
                Address = "260C Pasteur, Phường Võ Thị Sáu, Quận 3, TP.HCM",
                OpeningHours = "06:00 - 22:00"
            },
            new POI
            {
                NameVi = "Bánh Mì Huỳnh Hoa",
                NameEn = "Banh Mi Huynh Hoa",
                DescriptionVi = "Bánh mì kẹp thịt đặc sản Sài Gòn",
                DescriptionEn = "Saigon speciality meat sandwich",
                Latitude = 10.762622,
                Longitude = 106.660172,
                Radius = 150, // 150 meters
                Priority = 2,
                Image = "banh_mi.jpg",
                AudioVi = "",
                AudioEn = "",
                MapUrl = "",
                Address = "26 Lê Thị Riêng, Phường Bến Thành, Quận 1, TP.HCM",
                OpeningHours = "07:00 - 20:00"
            },
            new POI
            {
                NameVi = "Cơm Tấm Cali",
                NameEn = "Com Tam Cali",
                DescriptionVi = "Cơm tấm sườn bì chả truyền thống",
                DescriptionEn = "Traditional broken rice with ribs, skin, egg",
                Latitude = 10.775,
                Longitude = 106.700,
                Radius = 120, // 120 meters
                Priority = 1,
                Image = "com_tam.jpg",
                AudioVi = "",
                AudioEn = "",
                MapUrl = "",
                Address = "107 Đặng Văn Ngữ, Phường 14, Quận Phú Nhuận, TP.HCM",
                OpeningHours = "24/7"
            }
        };
    }
}
