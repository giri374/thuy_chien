# Fix Layout Buttons - Hướng Dẫn

## 🐛 Vấn Đề
Các weapon buttons được instantiate cùng 1 vị trí (chồng lên nhau) và nằm lệch khỏi Scroll View.

## ✅ Giải Pháp

Đã cập nhật `CreateWeaponButtons()` trong `WeaponSetupUIManager.cs` để tự động:

1. **Thêm VerticalLayoutGroup** vào Container (Content) nếu chưa có
2. **Set RectTransform** của button để anchor đúng
3. **Thêm LayoutElement** cho mỗi button để set preferred height
4. **Force rebuild layout** để cập nhật ngay lập tức

### Code Changes:
```csharp
// Thêm VerticalLayoutGroup nếu chưa có
var layoutGroup = weaponButtonContainer.GetComponent<VerticalLayoutGroup>();
if (layoutGroup == null)
{
    layoutGroup = weaponButtonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
}

// Set RectTransform cho button
var buttonRect = buttonObj.GetComponent<RectTransform>();
if (buttonRect != null)
{
    buttonRect.anchorMin = new Vector2(0, 1);
    buttonRect.anchorMax = new Vector2(1, 1);
    buttonRect.pivot = new Vector2(0.5f, 1);
    buttonRect.offsetMin = Vector2.zero;
    buttonRect.offsetMax = Vector2.zero;
}

// Thêm LayoutElement
var layoutElement = buttonObj.GetComponent<LayoutElement>();
if (layoutElement == null)
{
    layoutElement = buttonObj.AddComponent<LayoutElement>();
}
layoutElement.preferredHeight = 100;

// Force rebuild
LayoutRebuilder.ForceRebuildLayoutImmediate(weatherButtonContainer.GetComponent<RectTransform>());
```

## 📋 Setup Canvas (Manual Check)

**Nếu vẫn không work, kiểm tra:**

### 1. Content (Weapon Button Container) Settings:
```
RectTransform
├── Width: Preferred (hoặc set cụ thể)
├── Height: Preferred
├── Anchors: Top-Left
└── Pivot: Top-Left (0, 1)

Layout Group
├── Component: VerticalLayoutGroup
├── Child Force Expand Height: OFF
├── Child Force Expand Width: ON
├── Child Control Size Height: OFF
├── Child Control Size Width: ON
├── Spacing: 10 (tùy chỉnh)
```

### 2. WeaponButton Prefab:
```
WeaponButton (Button)
├── RectTransform
│   └── Preferred Size: 300x100 (hoặc tự động)
├── Button component
├── LayoutElement (tự động thêm)
└── [Child elements]
```

### 3. ScrollView Settings:
```
ScrollView
├── Content Layout Group: VerticalLayoutGroup
├── Viewport
│   └── Mask component: ON
└── Scroll Rect
    ├── Horizontal: OFF
    ├── Vertical: ON
    └── Content: [Kéo Content vào]
```

## 🎯 Testing

**Sau cập nhật code:**
1. Clear Assets (Ctrl+Shift+K) hoặc reload scene
2. PlayMode
3. Xác nhận:
   - ✅ Buttons sắp xếp dọc (vertical)
   - ✅ Buttons cách đều nhau
   - ✅ Buttons full width container
   - ✅ Scroll View hoạt động
   - ✅ Buttons không chồng lên

## 💡 Nếu còn lỗi

**Check points:**
- Canvas Scaler có được setup không?
- Weapon Button prefab có RectTransform không?
- weaponButtonContainer (Content) đã được gán không?
- ScrollView có Mask component không?

---

**Tham khảo**: Unity's Layout System documentation
- VerticalLayoutGroup: Sắp xếp elements dọc
- LayoutElement: Kiểm soát kích thước preferred
- LayoutRebuilder: Force rebuild layout ngay lập tức
