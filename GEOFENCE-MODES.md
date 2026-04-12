# 🎯 GEOFENCE MODES - 3 Chế Độ Tracking

## 📋 TỔNG QUAN

App hiện tại có **3 chế độ tracking** khác nhau, bạn có thể chuyển đổi trong **Settings → Chế Độ Geofence**

---

## 🔒 MODE 1: DÍNH (STICKY)

### Mô tả
Phải **ra khỏi POI hiện tại** mới trigger được POI khác

### Logic
```
Đang ở POI A → Trigger A
Vẫn trong radius A → Không trigger B (dù B gần hơn)
Ra khỏi radius A → Giờ có thể trigger B
```

### Khi nào dùng?
- ✅ Không muốn bị spam nhiều quán liên tục
- ✅ Đứng giữa 2 quán, chỉ muốn nghe 1 quán
- ❌ Có thể bỏ lỡ quán gần hơn nếu chưa ra khỏi quán cũ

### Log ví dụ
```
[Geofence] Mode: sticky
[Geofence] Inside 2 POIs, nearest: Bánh Mì Huỳnh Hoa (45m)
[Geofence] *** TRIGGERED (sticky): Bánh Mì Huỳnh Hoa (45m)

// Di chuyển gần Phở Lệ hơn nhưng vẫn trong radius Bánh Mì
[Geofence] Inside 2 POIs, nearest: Phở Lệ (30m)
[Geofence] SKIP Phở Lệ (sticky mode: already triggered, still inside Bánh Mì)
```

---

## 🎯 MODE 2: GẦN NHẤT (NEAREST)

### Mô tả
**Luôn trigger POI gần nhất** bất kể đã trigger POI nào trước đó

### Logic
```
Đang ở POI A → Trigger A
Di chuyển gần B hơn → Ngay lập tức trigger B
Di chuyển gần C hơn → Ngay lập tức trigger C
```

### Khi nào dùng?
- ✅ Muốn luôn biết quán gần nhất
- ✅ Đi bộ/di chuyển liên tục
- ❌ Có thể bị spam nếu đứng giữa nhiều quán
- ❌ Cooldown vẫn áp dụng (5 phút/quán)

### Log ví dụ
```
[Geofence] Mode: nearest
[Geofence] Inside 2 POIs, nearest: Bánh Mì Huỳnh Hoa (45m)
[Geofence] *** TRIGGERED (nearest): Bánh Mì Huỳnh Hoa (45m)

// Di chuyển gần Phở Lệ hơn
[Geofence] Inside 2 POIs, nearest: Phở Lệ (30m)
[Geofence] *** TRIGGERED (nearest): Phở Lệ (30m)
```

---

## 🧠 MODE 3: THÔNG MINH (SMART) - DEFAULT ⭐

### Mô tả
**Luôn ưu tiên POI gần nhất**, mỗi POI có **cooldown riêng biệt** (5 phút)

### Logic
```
POI A đã trigger cách đây 2 phút → Không trigger A (cooldown)
POI B chưa từng trigger → Trigger B (gần nhất trong các POI chưa trigger)
POI C đã trigger cách đây 10 phút → Có thể trigger C lại
```

### Khi nào dùng?
- ✅ Cân bằng giữa không spam và không bỏ lỡ quán
- ✅ Mỗi quán chỉ thông báo 1 lần/5 phút
- ✅ Tự động chuyển sang quán gần hơn
- ⭐ Khuyến nghị dùng mode này

### Log ví dụ
```
[Geofence] Mode: smart

// Lần 1: Chưa trigger quán nào
[Geofence] Inside 3 POIs, nearest: Bánh Mì Huỳnh Hoa (45m)
[Geofence] *** TRIGGERED (smart): Bánh Mì Huỳnh Hoa (45m)

// Lần 2: Bánh Mì đang cooldown, chuyển sang quán gần nhất tiếp theo
[Geofence] Inside 3 POIs, nearest: Phở Lệ (30m)
[Geofence] *** TRIGGERED (smart): Phở Lệ (30m)

// Lần 3: Cả 2 đều cooldown, chuyển sang quán thứ 3
[Geofence] Inside 3 POIs, nearest: Bún Bò Huế (50m)
[Geofence] *** TRIGGERED (smart): Bún Bò Huế (50m)

// Lần 4: Tất cả đều trong cooldown
[Geofence] Inside 3 POIs
[Geofence] SKIP Bánh Mì Huỳnh Hoa (smart mode: cooldown active)
[Geofence] SKIP Phở Lệ (smart mode: cooldown active)
[Geofence] SKIP Bún Bò Huế (smart mode: cooldown active)
```

---

## 🔄 SO SÁNH 3 MODE

| Tình huống | STICKY | NEAREST | SMART |
|------------|--------|---------|-------|
| Đang ở A, B gần hơn | Không trigger B | Trigger B | Trigger B |
| Đứng giữa A và B | Chỉ trigger 1 | Trigger liên tục | Trigger gần nhất |
| A vừa trigger 1 phút | Không trigger A | Không trigger A | Không trigger A |
| A trigger cách 10 phút | Trigger A nếu gần | Trigger A nếu gần | Trigger A |
| B chưa từng trigger | Phải ra khỏi A | Trigger B | Trigger B |
| Spam protection | ✅ Tốt | ❌ Có thể spam | ✅ Cân bằng |
| Không bỏ lỡ quán | ❌ Có thể bỏ lỡ | ✅ Không bỏ lỡ | ✅ Không bỏ lỡ |

---

## 🎮 HƯỚNG DẪN TEST

### Test Scenario: 3 quán gần nhau
- **A: Bánh Mì Huỳnh Hoa** (10.7701, 106.6923) - Radius 50m
- **B: Chợ Bến Thành** (10.7720, 106.6983) - Radius 300m  
- **C: Bún Bò Huế** (10.7760, 106.6955) - Radius 100m

### Test 1: Di chuyển từ A → B → C (Sticky Mode)
```
Tại A (10.7701, 106.6923) → Trigger A ✅
Di chuyển (10.7710, 106.6950) → Vẫn trong radius A → Không trigger ❌
Tại B (10.7720, 106.6983) → Vẫn trong radius A → Không trigger ❌
Ra khỏi A, tại B → Trigger B ✅
```

### Test 2: Di chuyển từ A → B → C (Nearest Mode)
```
Tại A (10.7701, 106.6923) → Trigger A ✅
Di chuyển (10.7710, 106.6950) → Gần B hơn → Trigger B ✅
Tại B (10.7720, 106.6983) → Trigger B ✅
Tại C (10.7760, 106.6955) → Gần C hơn → Trigger C ✅
```

### Test 3: Di chuyển từ A → B → C (Smart Mode)
```
Tại A (10.7701, 106.6923) → Trigger A ✅
Di chuyển (10.7710, 106.6950) → A cooldown, B gần → Trigger B ✅
Tại B (10.7720, 106.6983) → A,B cooldown → Trigger C (gần nhất còn lại) ✅
```

---

## ⚙️ CÁCH ĐỔI MODE

### Trong App:
1. Mở app
2. Vào tab **Settings** (Cài đặt)
3. Tìm phần **"CHẾ ĐỘ GEOFENCE"**
4. Chọn **Picker**: Dính (Sticky) / Gần nhất / Thông minh
5. Mô tả chi tiết hiện bên dưới

### Mặc định: **THÔNG MINH (Smart)**

---

## 📝 CODE REFERENCE

```csharp
// Constants
public static class GeofenceModes
{
    public const string Sticky = "sticky";      // Must exit before triggering new POI
    public const string Nearest = "nearest";    // Always trigger nearest
    public const string Smart = "smart";        // Cooldown per POI + nearest priority
}

// Settings
string mode = _settingsService.GetGeofenceMode(); // "sticky", "nearest", or "smart"
_settingsService.SetGeofenceMode(GeofenceModes.Smart);
```

---

## 🎯 KHUYẾN NGHỊ

| Use Case | Mode Khuyến nghị |
|----------|------------------|
| Đi bộ tham quan | **Smart** hoặc **Nearest** |
| Đứng 1 chỗ lâu | **Sticky** |
| Không muốn bị spam | **Smart** hoặc **Sticky** |
| Muốn biết tất cả quán gần | **Nearest** |
| Lần đầu dùng app | **Smart** (mặc định) |
