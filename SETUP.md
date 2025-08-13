# Настройка API ключей

## Безопасность

⚠️ **ВАЖНО**: Никогда не публикуйте ваши API ключи в открытом доступе!

## Настройка

1. **Скопируйте файл-пример**:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. **Отредактируйте `appsettings.json`** и замените следующие значения:
   ```json
   {
     "TelegramBot": {
       "BotToken": "ВАШ_ТЕЛЕГРАМ_БОТ_ТОКЕН"
     },
     "OpenRouter": {
       "ApiKey": "ВАШ_OPENROUTER_API_КЛЮЧ",
       "Model": "mistralai/mistral-7b-instruct",
       "HttpReferer": "https://yourapp.com"
     }
   }
   ```

## Получение API ключей

### Telegram Bot Token
1. Найдите [@BotFather](https://t.me/BotFather) в Telegram
2. Отправьте команду `/newbot`
3. Следуйте инструкциям для создания бота
4. Скопируйте полученный токен

### OpenRouter API Key
1. Зарегистрируйтесь на [openrouter.ai](https://openrouter.ai)
2. Перейдите в раздел API Keys
3. Создайте новый API ключ
4. Скопируйте полученный ключ

### ChatGPT+ (опционально)
1. У вас должна быть активная подписка ChatGPT+
2. Используйте ваш email и пароль от аккаунта OpenAI
3. **Внимание**: Пароль хранится в открытом виде в конфигурации

## Переменные окружения (альтернативный способ)

Вместо файла `appsettings.json` вы можете использовать переменные окружения:

```bash
# Windows PowerShell
$env:TelegramBot__BotToken="ВАШ_ТОКЕН"
$env:OpenRouter__ApiKey="ВАШ_КЛЮЧ"

# Windows Command Prompt
set TelegramBot__BotToken=ВАШ_ТОКЕН
set OpenRouter__ApiKey=ВАШ_КЛЮЧ

# Linux/macOS
export TelegramBot__BotToken="ВАШ_ТОКЕН"
export OpenRouter__ApiKey="ВАШ_КЛЮЧ"
```

## Проверка настройки

После настройки запустите бота:
```bash
dotnet run
```

Если все настроено правильно, вы увидите сообщение:
```
Бот [имя_бота] в работе
Доступные провайдеры: OpenRouter, ChatGPTPlus
Текущий провайдер: OpenRouter
```

## Управление провайдерами

Бот поддерживает команды для управления провайдерами:

- `/providers` - показать доступные провайдеры
- `/current` - показать текущий провайдер
- `/switch <провайдер>` - переключиться на другой провайдер
- `/help` - показать справку по командам

### Примеры использования:
```
/switch ChatGPTPlus    # Переключиться на ChatGPT+
/switch OpenRouter     # Переключиться на OpenRouter
/current               # Показать текущий провайдер
```

## Безопасность

⚠️ **ВАЖНО**: 
- Файл `appsettings.json` исключен из Git
- Пароль ChatGPT+ хранится в открытом виде
- Используйте переменные окружения для продакшена

Если возникнут ошибки, проверьте:
- Правильность API ключей
- Наличие файла `appsettings.json`
- Права доступа к файлу
- Доступность выбранного провайдера
