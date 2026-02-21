# BasketballTactics
2D grid based sistem üzerine kurulmuş tactic genre'sında bir basketball oyunu.

* Tile based grid (7x12) - **Implemented (GridManager.cs)**
* Turn based gameplay - **Implemented (TurnManager.cs)**
* Dice managed challanges (d6 and 2d6) - **Implemented**

---

# Architecture & Implementation

## Managers
* **MatchSetter:** Oyunun başlangıcında Scriptable Object listelerinden oyuncuları spawn eder, takımları kurar ve topu ilk takıma teslim eder. **Implemented (MatchSetter.cs)**
* **TurnManager:** Takımların (Red/Blue) sıralarını yönetir. Tur başlarında aksiyon haklarını sıfırlar ve stun durumlarını çözer. **Implemented (TurnManager.cs)**
* **GridManager:** 7x12 boyutunda, Unity Tilemap sistemi ile entegre grid yönetimi. **Implemented (GridManager.cs)**

## Input & Control
* **InputController:** Fare tıklamalarıyla oyuncu seçimi, hareket, pas, şut ve combat kontrolü sağlar. UI tıklamalarında dünya seçimini korur. **Implemented (InputController.cs)**
* **Selection:** Oyuncu seçildiğinde hareket menzili **Yeşil**, tıklanan kare **Beyaz** highlightlanır.

---

# Game Mechanics

## Action System
Her oyuncu kendi turunda sadece **bir (1)** aksiyon alabilir. Oyuncu seçildiğinde bir **Aksiyon Paneli** açılır:
1. **Move:** Hareket menzili içindeki boş kareye gider.
2. **Pass:** Top varsa, takım arkadaşına fırlatır.
3. **Shoot:** Top varsa, potaya fırlatır.
4. **Wait:** Aksiyon almadan turu bitirir.

## Ball, Passing & Shooting
* **Ball:** Sahada tek bir top objesi bulunur, oyuncuları takip eder. **Implemented (Ball.cs)**
* **Targeting:** Pas veya şut fırlatılmadan önce topun düşeceği hedef kare **Mavi** ile işaretlenir.
* **Movement:** Top, DOTween kullanılarak hedefine uçar; başarısız atışlarda yere seker (Bounce).
* **Challenge Difficulty (CD):** Mesafe (Manhattan) = CD.
* **Dice Check:** 1d6 + Bonus >= CD ise başarılıdır. 

## Combat (Steal & Stun)
* **Attack:** Elinde top olmayan bir oyuncu, hareket menzili içindeki bir rakibe tıklarsa saldırır.
* **Logic:** Saldıran (d6 + Defence Bonus) vs Savunan (d6 + Defence Bonus).
* **Outcome:** Kazanan taraf topu alır (eğer varsa) ve kaybeden tarafı 1 turluğuna **Stun** eder.
* **Stun:** Stunlanan oyuncu grileşir ve o tur aksiyon alamaz.

---

# PlayerUnit
Oyuncular scriptable object olarak üretilecek. **Implemented (PlayerUnitData.cs & PlayerUnit.cs)**

### Player Stats
* Name - Oyuncu adı
* Speed - Hareket edebildiği tile sınırı
* Shooting - Shoot bonus
* Passing - Pass bonus
* Defence - Defence bonus

### Görsel Durumlar
* **Yeşil:** Topa sahip oyuncu.
* **Kırmızı:** Topu olmayan aktif oyuncu.
* **Gri:** Stunlanmış oyuncu.

---

# UI & HUD
* **UIManager:** Skor takibi, tur gösterimi ve aksiyon butonlarını yönetir. **Implemented (UIManager.cs)**
* **DiceResult:** Zar sonuçlarını ekranda görsel olarak gösterir.
