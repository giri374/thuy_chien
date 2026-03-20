# WeaponSetupScene Setup Guide - Bỏ Scroll View

## Tóm tắt thay đổi
- **Trước**: Instantiate weapon buttons động từ prefab trong scroll view
- **Sau**: Drag-drop weapon buttons vào inspector (không cần scroll view)

## Các bước setup

### 1. Mở WeaponSetupScene.unity

### 2. Loại bỏ Scroll View
- Trong Hierarchy, tìm và xóa GameObject chứa ScrollRect component
- Giữ lại Canvas và các UI elements khác

### 3. Tạo hoặc sắp xếp Weapon Buttons trong Scene
Bạn có 2 lựa chọn:

**Tùy chọn A: Drag prefab vào scene**
- Drag `weaponButtonPrefab` từ Assets vào Canvas
- Repeat cho mỗi loại weapon (trừ NormalShot)
- Sắp xếp vị trí các button theo ý thích

**Tùy chọn B: Tạo button từ UI menu**
- Right-click Canvas → UI → Button - TextMeshPro
- Thêm component `WeaponButton` vào mỗi button
- Setup references thủ công (`weaponIconImage`, `weaponNameText`, v.v.)
- Repeat cho mỗi loại weapon

### 4. Cấu hình WeaponButton References

Cho **mỗi** WeaponButton trong scene:
- Drag Image component vào field `weaponIconImage`
- Drag Text component (tên) vào field `weaponNameText`
- Drag Text component (giá) vào field `goldCostText`  
- Drag Text component (mô tả) vào field `descriptionText`
- Drag Button component vào field `selectButton`

### 5. Assign Weapon Buttons vào WeaponSetupUIManager

**QUAN TRỌNG**: Số lượng buttons phải khớp với số lượng weapons trong WeaponListData (trừ NormalShot)

1. Select Canvas object (hoặc object chứa WeaponSetupUIManager)
2. Trong Inspector, tìm `WeaponSetupUIManager` component
3. Kiểm tra field `Weapon Buttons` (array)
4. Set **Size** = số lượng weapon types (trừ NormalShot)
   - Ví dụ: Nếu có 5 weapons tổng cộng, NormalShot là 1, thì Size = 4
5. Drag-drop các WeaponButton objects từ Hierarchy vào array elements
   - **Thứ tự không quan trọng** nhưng hãy tổ chức gọn gàng

### 6. Kiểm tra khác

Đảm bảo các field khác trong WeaponSetupUIManager vẫn được assign:
- `playerTitleText` - Text hiển thị "Player X - Weapon Setup"
- `currentGoldText` - Text hiển thị "Gold: XXX"
- `confirmButton` - Button "Confirm"
- `cancelButton` - Button "Cancel"
- `resetButton` - Button "Reset"

### 7. Test Scene

1. Play scene
2. Kiểm tra:
   - ✓ Weapon buttons hiển thị đúng thông tin
   - ✓ Click button để select/deselect weapons
   - ✓ Gold updates khi chọn weapon
   - ✓ Buttons disable khi không đủ gold
   - ✓ Confirm button chuyển sang scene tiếp theo
   - ✓ Reset button clear all selections

## Troubleshooting

### "No weapon buttons assigned in inspector"
- Kiểm tra `Weapon Buttons` array không rỗng
- Verify tất cả elements trong array không phải null

### Buttons không respond khi click
- Verify `selectButton` được assign đúng
- Check `Button` component có `Interactable` = true (nếu có đủ gold)

### Gold không update
- Check `currentGoldText` được assign
- Verify GameManager instance tồn tại

### Weapons không hiển thị thông tin
- Check `WeaponButton` component có tất cả references
- Verify WeaponListData được load vào GameManager

## Lưu ý quan trọng

1. **Số lượng buttons phải đúng**: Phải có button cho mỗi weapon type (trừ NormalShot)
2. **RectTransform**: Mỗi button cần RectTransform được setup đúng để hiển thị
3. **Canvas Scale**: Nếu dùng CanvasScaler, verify settings để UI scale đúng
4. **Event System**: Kiểm tra scene có EventSystem (tự động khi tạo Canvas)

## Notes về Code Changes

- `CreateWeaponButtons()`: Thay vì instantiate, giờ setup các button có sẵn từ array
- `RefreshWeaponButtons()`: Iterate qua array thay vì children của container
- `weaponButtonPrefab` và `weaponButtonContainer` đã được bỏ

Không cần thay đổi logic game, chỉ là phương thức setup UI khác.
