# gymshinary-bot

`Паспорт проекта` и `файл использования генеративного ИИ` в папке [documents](./documents/)

## Инструкция по запуску и настройки бота

### 1)
Из корня репозитория:

```bash
cd src/Gymshinary.Bot
```

### 2)
Записать токен бота:

```bash
dotnet user-secrets set "BotConfiguration:BotToken" "ТВОЙ_ТОКЕН_ОТ_BOTFATHER"
```

### 3)
Проверить, что секрет сохранился:

```bash
dotnet user-secrets list
```

Должно показать примерно:

```text
BotConfiguration:BotToken = 123456789:ABC...
```

### 4)

Теперь нужно запустить:

```bash
dotnet run
```

Тогда код:

```csharp
builder.Services.Configure<BotConfiguration>(
    builder.Configuration.GetRequiredSection("BotConfiguration"));
```

сам подхватит значение из User Secrets, а здесь:

```csharp
botConfig.BotToken
```

окажется настоящий токен
