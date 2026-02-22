# Basketball Tactics - Mimari ve Tasarım Dersleri

Bu dosya, projenin geliştirilmesi sırasında uygulanan yazılım prensiplerini ve oyun mekaniklerinin teknik arka planını belgeler.

---

## Ders 1: Veri ve Mantık Ayrımı (Data vs. Logic)
*Ayrıştırma (Decoupling)*

Oyun geliştirmede en önemli prensiplerden biri, **veriyi (Data)** onu işleyen **mantıktan (Logic)** ayırmaktır.
- **Oyun İçi Örnek:** `PlayerUnitData` (ScriptableObject) ve `PlayerUnit` (MonoBehaviour).
- **Ders:** `PlayerUnitData.cs` içinde oyuncunun `speed`, `shootingBonus` gibi statik değerleri tutulur. `PlayerUnit.cs` ise bu verileri kullanarak `GetReachableTiles()` metodunda menzil hesaplaması yapar. Bu sayede aynı veri varlığını (Asset) birden fazla oyuncu objesine bağlayarak bellek tasarrufu ve kolay yönetim sağlarız.

---

## Ders 2: Kalıtım ve Polimorfizm (Inheritance & Polymorphism)
*Genişletilebilirlik (Extensibility)*

Ortak davranışları tek merkezde toplayıp, özel durumları alt sınıflarda yönetmek kodun sürdürülebilirliğini sağlar.
- **Oyun İçi Örnek:** `PlayerUnit` (Temel) -> `OffensivePlayerUnit` (Alt Sınıf).
- **Ders:** `PlayerUnit.cs` içinde `UpdateVisuals()` metodu `virtual` olarak tanımlanmıştır. `OffensivePlayerUnit.cs` bu metodu `override` ederek takım rengini kırmızıya büker. `InputController` gibi sınıflar sahadaki nesnenin tipini bilmeden sadece `UpdateVisuals()` çağırır; C# çalışma anında doğru sınıfın metodunu çalıştırır.

---

## Ders 3: Singleton Pattern (Tekil Yönetim)
*Merkezi Erişim (Global Access)*

Oyun genelinde tek bir örneği olması gereken yönetici sınıflar için kullanılır.
- **Oyun İçi Örnek:** `TurnManager.Instance`, `ScoreManager.Instance`, `UIManager.Instance`.
- **Ders:** `static` bir referans üzerinden bu yöneticilere her yerden (örneğin bir oyuncu içinden) `ScoreManager.Instance.AddScore(...)` şeklinde kolayca erişilir. Bu, nesneler arası karmaşık referans bağları kurma zorunluluğunu ortadan kaldırır.

---

## Ders 4: Event-Driven Architecture (Olay Güdümlü Mimari)
*Gevşek Bağlılık (Loose Coupling)*

Sınıfların birbirini doğrudan tanıması yerine, "olaylar" (events) üzerinden haberleşmesidir.
- **Oyun İçi Örnek:** `ScoreManager.OnGoalScored` ve `TurnManager.OnTurnChanged`.
- **Ders:** Basket atıldığında `ScoreManager` bir olay tetikler. `TurnManager` bu olayı dinleyerek sahayı resetler, `UIManager` ise gol yazısını ekrana basar. `ScoreManager`, bu diğer sınıfların varlığından haberdar olmak zorunda değildir; sadece "bir gol oldu" haberini yayar.

---

## Ders 5: State Machine (Durum Makinesi)
*Akış Kontrolü (Control Flow)*

Oyunun o anki "ruh halini" (State) kontrol ederek kullanıcı girişlerini ve animasyonları senkronize etmek.
- **Oyun İçi Örnek:** `StateManager.cs`.
- **Ders:** 
    - `GameState.Idle`: Giriş bekleniyor.
    - `GameState.Busy`: Animasyon veya hesaplama sürüyor (örneğin `MoveTo` çalışırken), girişler bloklanır.
    - `GameState.Resolution`: Zar sonuçları ekranda, oyuncunun onay (tıklama) vermesi bekleniyor.

---

## Ders 6: Izgara Sistemi ve Navigasyon (Grid & BFS)
*Matematiksel Dünya (Mathematical World)*

Sıra tabanlı oyunlarda hareket menzilini belirlemek için ızgara yapısı ve arama algoritmaları kullanılır.
- **Oyun İçi Örnek:** `GridManager.cs` ve `PlayerUnit.GetReachableTiles()`.
- **Ders:** `GetReachableTiles()` içinde **BFS (Breadth-First Search)** algoritması kullanılır. `GridManager.UpdateOccupantPositions` ise aynı kareye giren iki oyuncunun üst üste binmesini engeller, onları hafifçe yana kaydırarak (Tweening ile) düzenler.

---

## Ders 7: Girdi ve Etkileşim (Input & Raycasting)
*Kullanıcı Deneyimi (UX)*

Fare tıklamalarını oyun dünyasındaki anlamlı eylemlere dönüştürmek.
- **Oyun İçi Örnek:** `InputController.cs`.
- **Ders:** 
    - `EventSystem.current.IsPointerOverGameObject()`: Eğer tıklama bir UI butonu üzerindeyse, arkadaki oyuncuyu seçmeyi engeller.
    - `ActionMode`: (Move, Pass, Shoot) enum yapısı sayesinde aynı tıklama seçili moda göre farklı metodları (örneğin `Attack` veya `Pass`) tetikler.

---

## Ders 8: Görsel Geri Bildirim ve Tweening (DOTween)
*Cila ve Hissiyat (Juice & Feel)*

Kodun sadece çalışması yetmez, oyuncuya "yaşıyormuş" hissi vermesi gerekir.
- **Oyun İçi Örnek:** `transform.DOJump`, `transform.DOScale`.
- **Ders:** 
    - Oyuncu bir kareye ışınlanmak yerine `DOJump` ile zıplayarak gider. 
    - Bir butona tıklandığında `DOPunchScale` ile hafifçe titrer.
    - Zar atılırken `calculationText` rastgele sayılar arasında hızlıca dönerek heyecan yaratır.

---

## Ders 9: Coroutines ve Asenkron Akış
*Zaman Yönetimi (Time Management)*

Zamanla gerçekleşen olayları (animasyonlar, beklemeler) yönetmek için kullanılır.
- **Oyun İçi Örnek:** `UIManager.AnimateDiceRoll` ve `TurnManager.EndTurnRoutine`.
- **Ders:** Zar atma animasyonu `yield return new WaitForSeconds(...)` kullanarak kodun o satırda durup animasyon bitene kadar beklemesini sağlar. Bu, karmaşık animasyon dizilerinin ("Önce zar dönsün, sonra sonuç çıksın, sonra tıkla, sonra hareket et") tek bir akışta yazılabilmesine olanak tanır.
