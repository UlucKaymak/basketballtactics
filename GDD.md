# BasketballTactics
2D grid based sistem üzerine kurulmuş tactic genre'sında bir basketball oyunu.

* Tile-based grid (7x12)
* Turn-based gameplay
* Dice managed challanges (d6 and 2d6) (Şimdilik 2d6 kullanılmıyor.)

---

# Architecture & Implementation

## Managers
* **MatchSetter:** Oyunun başlangıcında Scriptable Object listelerinden oyuncuları spawn eder, takımları kurar ve topu ilk takıma teslim eder.
* **TurnManager:** Takımların (Red/Blue) sıralarını yönetir. Tur başlarında aksiyon haklarını sıfırlar ve stun durumlarını çözer.
    * **Reset Logic:** Gol atıldığında tüm oyuncular başlangıç pozisyonlarına döner ve top golü atan takıma verilir.
    * **Inspector Controls:** NaughtyAttributes kullanılarak "Skip Turn" butonu eklenmiştir.
* **GridManager:** 7x12 boyutunda, Unity Tilemap sistemi ile entegre grid yönetimi.

## Input & Control
* **InputController:** Fare tıklamalarıyla oyuncu seçimi, hareket, pas, şut ve combat kontrolü sağlar. UI tıklamalarında dünya seçimini korur.
* **Selection:** Oyuncu seçildiğinde hareket menzili **Yeşil**, tıklanan kare **Beyaz** highlightlanır.

---

# Game Mechanics

## Action System
Her oyuncu kendi turunda sadece **bir (1)** aksiyon alabilir. Oyuncu seçildiğinde bir **Aksiyon Paneli** açılır:
1. **Move:** Hareket menzili içindeki boş kareye gider.
2. **Pass:** Top varsa, takım arkadaşına fırlatır.
3. **Shoot:** Top varsa, potaya fırlatır.
4. **Wait:** Aksiyon almadan turu bitirir.
* **Auto-End Turn:** Takımdaki tüm oyuncular aksiyonlarını bitirdiğinde sıra otomatik olarak diğer takıma geçer.

## Ball, Passing & Shooting
* **Ball:** Sahada tek bir top objesi bulunur, oyuncuları takip eder.
* **Targeting:** Pas veya şut fırlatılmadan önce topun düşeceği hedef kare **Mavi** ile işaretlenir.
* **Movement:** Top, DOTween kullanılarak hedefine uçar; başarısız atışlarda yere seker (Bounce).
* **Dice Check:** 1d6 + Bonus >= Mesafe (CD) ise başarılıdır. 

## Combat (Steal & Stun)
* **Attack:** Elinde top olmayan bir oyuncu, hareket menzili içindeki bir rakibe tıklarsa saldırır.
* **Logic:** Saldıran (d6 + Defence Bonus) vs Savunan (d6 + Defence Bonus).
* **Outcome:** Kazanan taraf topu alır (eğer varsa) ve kaybeden tarafı 1 turluğuna **Stun** eder.

---

# PlayerUnit
Oyuncular scriptable object olarak üretilecek. 

## Player Class
Oyunda en temelde 3 farklı class var:
1. Offence
  * Shoot özelliği
2. Defence
  * Defence özelliği
3. Support
  * Büyü özelliği

### Player Stats
* Name, HP, Speed; Shooting, Passing, Defence bonusları.

### Görsel Durumlar
* **Team Sprites:** Oyuncunun takımına göre (Red/Blue) farklı sprite'lar otomatik yüklenir.
* **Tint Feedback:**
    * **Yeşil:** Topa sahip oyuncu.
    * **Kırmızı:** Topu olmayan aktif oyuncu.
    * **Gri:** Stunlanmış oyuncu.

---

# UI & HUD
* **UIManager:** Skor takibi, tur gösterimi ve aksiyon butonlarını yönetir. 
* **DiceResult:** DiceManager.cs'nin oluşturduğu Zar sonuçlarını ekranda görsel olarak gösterir. Her bir oyuncunun attığı zar oyuncunun yanında animasyon olarak gözükecek. Announcement olarak da result gözükecek. 
  * `Zar Sonucu + İlgili Bonus = Sonuç (Difficulty Class)`
  * `4 + 2 = 6 (5)

* **Announcement manager:** Announcement ve Mini-Announcement olarak iki farklı announcement olarak.
  *  Announcement: Oyunla alakalı announcementlar. 
  * Mini-Announcement: Maç ile alakalı spiker konuşmaları. (Bunu şimdilik es geçeceğiz.)

---

# Future Features

* **PlayerUnit Classes:** Classlara bağlı ek özellikler. Oyunculara özel ek özellikler.
* **Player Card:** Maç sonunda yeni oyuncu kazanma
* **Team Manager:** Takım oluşturma, takım yönetimi ve maç stratejileri
* **Farklı Sahalar:** Farklı konseptli saha tasarımları. 
* **Turnuva:** Takım ile maçlara katılma.

# Long-Shot Features
* **Spiker**
* **AI ile Maç**
* **Procedural Saha oluşturma**

