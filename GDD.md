# BasketballTactics
2D grid based sistem üzerine kurulmuş tactic genre'sında bir basketball oyunu.

* Tile based grid (7x12) - **Implemented (GridManager.cs)**
* Turn based gameplay
* Dice managed challanges (d6 and 2d6)

---

# Architecture & Implementation

## Grid System
* **GridManager:** 7x12 boyutunda, Unity Tilemap sistemi ile entegre. **Implemented (GridManager.cs)**
* **Coordinate System:** Tilemap cell coordinates (x, y) kullanılır.
* **Bounds:** 0-6 x, 0-11 y aralığındadır.

## Input & Control
* **InputController:** Fare tıklamalarıyla oyuncu seçimi, hareket ve pas kontrolü sağlar. **Implemented (InputController.cs)**
* **Movement Logic:** Manhattan mesafesi (x_diff + y_diff) kullanılarak hız sınırına göre hareket kontrolü yapılır.

---

# Ball & Passing
* **Ball:** Sahada tek bir top objesi bulunur, oyuncuları takip eder. **Implemented (Ball.cs)**
* **Pass Action:** Elinde top olan oyuncu, başka bir oyuncuya tıkladığında pas atar.
* **Challenge Difficulty (CD):** Pas mesafesi (Manhattan) = CD.
* **Dice Check:** 1d6 + Passing Bonus >= CD ise pas başarılıdır. **Implemented (PlayerUnit.cs)**

---

# PlayerUnit
Oyuncular scriptable object olarak üretilecek. **Implemented (PlayerUnitData.cs & PlayerUnit.cs)**

### Scriptable object içeriği
* Name - Oyuncu adı
* Speed - Hareket edebildiği tile sınırı
* Shooting - Shoot bonus
* Passing - Pass bonus
* Defence - Defence bonus


## PlayerUnit Aksiyonları
1. **Move:**
    * Bir tile'dan başka bir tile'a (x, y) koordinatlarıyla ilerler. **Implemented (MoveTo coroutine)**
    * Manhattan mesafesi ile hız (speed) kontrolü yapılır.
2. **Tur boyunca sabit Durma:**
     * Karakter olduğu yerde durur. ve Turunu bitirir
3. **Pass: (Top varsa)**
    * Başka bir oyuncuya pas atılır.
    * d6 ile hesaplanır.
    * Oyuncunun bulunduğu tile 0 alınarak her grid ile +1 eklenerek mesafe ve Challange Difficulty hesaplanır.
4. **Shoot: (Top varsa)**
    * Potaya top atılır.
    * Oyuncunun bulunduğu tile 0 alınarak her grid ile +1 eklenerek mesafe ve Challange Difficulty hesaplanır.
5. **Combat**
    * (Oyuncunun elinde top var ise saldıramaz.)
    * Turu gelen oyuncu rakip oyuncuyla aynı tile'a yürüdüğü zaman saldırır.
    * İki taraf da d6 atar. yüksek atan taraf diğer tarafı 1 turluğuna stunlar ve topu alır.
