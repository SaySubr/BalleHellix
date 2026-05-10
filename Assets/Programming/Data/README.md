# 💾 Система сохранений (JSON)

## 📋 Что сделано

### 1. **GameSaveData.cs**
- Структура данных для JSON
- Хранит: уровни, монеты, скины, прогресс

### 2. **DataController.cs**
- Singleton (не уничтожается)
- Сохранение/загрузка JSON
- Методы для работы с уровнями, монетами, скинами

### 3. **Интеграция**
- `GameLauncher` → сохраняет при победе
- `IslandSpawner` → загружает статус уровней
- Автоматическая загрузка при старте

---

## 🔧 Настройка

### 1. Проверь что есть на сцене

```
MainMenu.unity:
☑ DataController (Singleton, загружается автоматически)

GameScene.unity:
☑ DataController (Singleton, загружается автоматически)
```

### 2. Проверь работу

```
1. Запусти MainMenu
2. В консоли: "💾 Загружено сохранение..." или "📄 Сохранение не найдено"
3. Кликни на уровень 1 → пройди его
4. Победа → "⭐ Уровень 1: 3 звёзд!" + "🔓 Открыт уровень 2!"
5. Вернись в меню → уровень 2 открыт (цветом)!
```

---

## 📁 Где хранится

```
Windows: C:\Users\[User]\AppData\LocalLow\[CompanyName]\[GameName]\Saves\savegame.json
Mac: ~/Library/Application Support/[CompanyName]/[GameName]/Saves/savegame.json
```

Путь можно посмотреть в консоли при сохранении!

---

## 🎮 API для использования

### Уровни:

```csharp
// Проверка: открыт ли уровень
DataController.Instance.IsLevelUnlocked(levelNumber);

// Завершить уровень
DataController.Instance.CompleteLevel(levelNumber, stars, score);

// Получить звёзды
DataController.Instance.GetLevelStars(levelNumber);

// Получить рекорд
DataController.Instance.GetLevelBestScore(levelNumber);
```

### Монеты:

```csharp
// Добавить
DataController.Instance.AddCoins(100);

// Потратить
if (DataController.Instance.SpendCoins(50)) {
    // Куплено!
}

// Получить количество
int coins = DataController.Instance.Coins;
```

### Скины:

```csharp
// Купить
DataController.Instance.PurchaseSkin(skinId);

// Выбрать
DataController.Instance.SelectSkin(skinId);

// Проверить
if (DataController.Instance.IsSkinPurchased(skinId)) { ... }

// Получить выбранный
int selected = DataController.Instance.GetSelectedSkin();
```

---

## 🗑️ Сброс сохранений

```csharp
// В консоли (через компонент)
DataController.Instance.DeleteSave();

// Или вручную удали файл:
// %APPDATA%\..\LocalLow\[Company]\[Game]\Saves\savegame.json
```

---

## 📄 Пример JSON

```json
{
  "highestUnlockedLevel": 5,
  "coins": 1500,
  "purchasedSkins": [1, 3, 5],
  "selectedSkinId": 3,
  "lastSaveDate": "2026-04-03 15:30:00",
  "totalPlayTime": 3600,
  "levels": [
    {
      "levelNumber": 1,
      "isUnlocked": true,
      "starsEarned": 3,
      "bestScore": 500,
      "completedDate": "2026-04-03 14:00:00"
    },
    {
      "levelNumber": 2,
      "isUnlocked": true,
      "starsEarned": 2,
      "bestScore": 350,
      "completedDate": "2026-04-03 14:30:00"
    }
  ]
}
```

---

## 🐛 Проблемы

### Сохранение не работает:
- Проверь что DataController есть на сцене
- Смотри консоль на ошибки

### Уровни не открываются:
- Проверь что `CompleteLevel()` вызывается при победе
- Проверь логи: "🔓 Открыт уровень X!"

### JSON битый:
- Удали файл сохранения
- Запусти снова → создастся новый

---

**Готово!** 🎉
