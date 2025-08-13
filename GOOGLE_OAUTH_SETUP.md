# Настройка Google OAuth для ChatGPT+

## 🎯 Что это дает

Вместо хранения паролей в открытом виде, бот будет использовать безопасную авторизацию через Google OAuth для доступа к ChatGPT+.

## 📋 Предварительные требования

1. **Google аккаунт** с доступом к ChatGPT+
2. **Google Cloud Console** проект
3. **OAuth 2.0** приложение

## 🔧 Пошаговая настройка

### Шаг 1: Создание проекта в Google Cloud Console

1. Перейдите на [Google Cloud Console](https://console.cloud.google.com/)
2. Создайте новый проект или выберите существующий
3. Включите **Google+ API** и **OAuth 2.0**

### Шаг 2: Создание OAuth 2.0 приложения

1. В меню слева выберите **"APIs & Services"** → **"Credentials"**
2. Нажмите **"Create Credentials"** → **"OAuth 2.0 Client IDs"**
3. Выберите тип приложения: **"Desktop application"**
4. Введите название: `TelegramGPTBot`
5. Нажмите **"Create"**

### Шаг 3: Получение Client ID и Client Secret

После создания вы получите:
- **Client ID** - скопируйте его
- **Client Secret** - скопируйте его

### Шаг 4: Настройка redirect URI

В настройках OAuth клиента добавьте:
```
http://localhost:8080/callback
```

### Шаг 5: Получение Authorization Code

1. Откройте браузер и перейдите по URL:
```
https://accounts.google.com/o/oauth2/v2/auth?
client_id=ВАШ_CLIENT_ID&
redirect_uri=http://localhost:8080/callback&
scope=https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile openid&
response_type=code&
access_type=offline&
prompt=consent
```

2. Войдите в Google аккаунт
3. Разрешите доступ приложению
4. Скопируйте **authorization code** из URL (параметр `code=`)

### Шаг 6: Обмен кода на токены

Используйте утилиту `GoogleOAuthHelper` или выполните POST запрос:

```bash
curl -X POST https://oauth2.googleapis.com/token \
  -d "client_id=ВАШ_CLIENT_ID" \
  -d "client_secret=ВАШ_CLIENT_SECRET" \
  -d "code=АВТОРИЗАЦИОННЫЙ_КОД" \
  -d "grant_type=authorization_code" \
  -d "redirect_uri=http://localhost:8080/callback"
```

### Шаг 7: Настройка конфигурации

Обновите `appsettings.json`:

```json
"GoogleOAuth": {
  "ClientId": "ВАШ_CLIENT_ID",
  "ClientSecret": "ВАШ_CLIENT_SECRET", 
  "RefreshToken": "ВАШ_REFRESH_TOKEN",
  "RedirectUri": "http://localhost:8080/callback"
}
```

## 🔐 Безопасность

✅ **Преимущества OAuth:**
- Нет паролей в открытом виде
- Автоматическое обновление токенов
- Отзыв доступа в любой момент
- Соответствие стандартам безопасности

⚠️ **Важно:**
- Храните `ClientSecret` в безопасности
- `RefreshToken` дает долгосрочный доступ
- Используйте переменные окружения в продакшене

## 🚀 Использование

После настройки переключитесь на Google OAuth:

```
/switch GoogleOAuth
```

## 🔍 Проверка работы

1. Запустите бота: `dotnet run`
2. Проверьте доступные провайдеры: `/providers`
3. Переключитесь: `/switch GoogleOAuth`
4. Отправьте тестовое сообщение

## 🆘 Устранение неполадок

### Ошибка "invalid_client"
- Проверьте правильность Client ID и Client Secret
- Убедитесь, что OAuth приложение активно

### Ошибка "invalid_grant" 
- Refresh token истек или недействителен
- Получите новый authorization code

### Ошибка "access_denied"
- Проверьте настройки OAuth приложения
- Убедитесь, что API включены

## 📚 Дополнительные ресурсы

- [Google OAuth 2.0 документация](https://developers.google.com/identity/protocols/oauth2)
- [Google Cloud Console](https://console.cloud.google.com/)
- [OAuth 2.0 Playground](https://developers.google.com/oauthplayground/)
