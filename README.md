# Notes API

[🇬🇧 Read in English](README-EN.md)

Полнофункциональное REST API для управления заметками, задачами и пользователями, построенное на **.NET 10**, **PostgreSQL** и **JWT-аутентификации**.  
Разработано с централизованной обработкой исключений, автоматическим аудитом и настройками, готовыми к production.

## 🚀 Возможности

- **Управление пользователями** — регистрация, вход, обновление профиля, удаление аккаунта
- **Заметки** — CRUD с архивированием и цветовыми метками
- **Задачи** — CRUD с приоритетами (Низкий / Средний / Высокий), сроками, фильтрацией просроченных
- **Аутентификация** — JWT access + refresh токены (ASP.NET Core JWT Bearer)
- **Централизованная обработка ошибок** — `GlobalExceptionHandler` + типизированные доменные исключения, ответы в формате RFC 7807 `ProblemDetails`
- **Безопасность** — хеширование паролей BCrypt, GUID-ключи, секреты через переменные окружения
- **Автоматический аудит** — поля `CreatedAt` и `UpdatedAt` управляются перехватчиками EF Core
- **Валидация** — FluentValidation для всех входных DTO
- **Swagger UI** — интерактивная документация API по адресу `/swagger`
- **CORS** — настраиваемая поддержка кросс-доменных запросов
- **Структурированное логирование** — консольные логи с разной детализацией для Development / Production

## 📋 Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- (Опционально) Visual Studio 2022+, Rider или VS Code

## ⚙️ Инструкция по запуску

### 1. Клонировать репозиторий

```bash
git clone https://github.com/Obj-Proger/notes-api.git
cd notes-api
```

### 2. Настроить переменные окружения

Скопируйте файл-пример и отредактируйте под свои значения:

```bash
cp NotesApi/.env.example NotesApi/.env
```

Откройте `NotesApi/.env` и заполните:

```env
# База данных
DB_HOST=localhost
DB_PORT=5432
DB_NAME=notes_db
DB_USER=postgres
DB_PASSWORD=ваш_надёжный_пароль

# JWT
JWT_SECRET=ваш_супер_секретный_ключ_минимум_32_символа!
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_EXPIRATION_DAYS=7

# Порты HTTP и HTTPS
APP_HTTP_PORT=5001
APP_HTTPS_PORT=7001

# Окружение
APP_ENV=Development
```

### 3. Убедиться, что PostgreSQL запущен

```bash
# Windows (от администратора)
net start postgresql-x64-16

# Linux / macOS
sudo systemctl start postgresql
```

### 4. Восстановить пакеты и собрать проект

```bash
dotnet restore
dotnet build
```

### 5. Запустить приложение

```bash
dotnet run --project NotesApi
```

Миграции применяются автоматически при старте. Swagger UI доступен по адресу `http://localhost:5001/swagger`.

> **Ручное применение миграций** (если нужно):
> ```bash
> dotnet ef database update --project NotesApi
> ```

## 📚 Документация API

Полная интерактивная документация доступна через Swagger UI при запущенном приложении.

### 🔐 Аутентификация

#### Регистрация
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "ivan_ivanov",
  "email": "ivan@example.com",
  "password": "SecurePass123!",
  "firstName": "Иван",
  "lastName": "Иванов"
}
```

#### Вход
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "ivan@example.com",
  "password": "SecurePass123!"
}
```

Ответ содержит `accessToken`, `refreshToken` и срок действия. Используйте `Bearer <accessToken>` в заголовке `Authorization` для всех защищённых эндпоинтов.

### 👤 Пользователи `[Authorize]`

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/users` | Список всех пользователей |
| `GET` | `/api/users/{id}` | Профиль пользователя по ID |
| `PUT` | `/api/users/{id}` | Обновить свой профиль |
| `DELETE` | `/api/users/{id}` | Удалить свой аккаунт |

### 📝 Заметки `[Authorize]`

| Метод | Путь | Описание |
|---|---|---|
| `POST` | `/api/notes` | Создать заметку |
| `GET` | `/api/notes/{id}` | Получить заметку по ID |
| `GET` | `/api/notes/user/all` | Все активные заметки |
| `GET` | `/api/notes/user/archived` | Архивированные заметки |
| `PUT` | `/api/notes/{id}` | Обновить заметку (частичное обновление) |
| `DELETE` | `/api/notes/{id}` | Удалить заметку |

### ✅ Задачи `[Authorize]`

| Метод | Путь | Описание |
|---|---|---|
| `POST` | `/api/tasks` | Создать задачу |
| `GET` | `/api/tasks/{id}` | Получить задачу по ID |
| `GET` | `/api/tasks/user/all` | Все задачи (фильтр: `?completed=true/false`) |
| `GET` | `/api/tasks/user/priority/{priority}` | Незавершённые задачи по приоритету |
| `GET` | `/api/tasks/user/overdue` | Просроченные незавершённые задачи |
| `PUT` | `/api/tasks/{id}` | Обновить задачу |
| `DELETE` | `/api/tasks/{id}` | Удалить задачу |

## ⚠️ Обработка ошибок

Все ошибки обрабатываются централизованно через `GlobalExceptionHandler` и возвращаются в формате RFC 7807 `ProblemDetails`.  
Контроллеры не содержат `try/catch` — сервисы бросают типизированные доменные исключения, которые автоматически маппятся в HTTP-ответы.

| Исключение | HTTP-код | Когда используется |
|---|---|---|
| `NotFoundException` | 404 | Ресурс не найден |
| `ConflictException` | 409 | Дубликат email / username |
| `UnauthorizedException` | 401 | Неверные учётные данные |
| `ForbiddenException` | 403 | Нет прав на операцию |
| `ValidationException` | 400 | Ошибка бизнес-валидации |

Пример ответа при ошибке:

```json
{
  "status": 404,
  "title": "not_found",
  "detail": "Note with id 'a1b2c3...' was not found.",
  "instance": "/api/notes/a1b2c3..."
}
```

## 🧱 Структура проекта

```
NotesApi/                               # Корень репозитория
├── NotesApi/                           # Основное приложение
│   ├── Controllers/                    # Обработчики HTTP-запросов
│   │   ├── ApiControllerBase.cs        # Базовый контроллер (GetUserId и др.)
│   │   ├── AuthController.cs
│   │   ├── NotesController.cs
│   │   ├── TasksController.cs
│   │   └── UsersController.cs
│   ├── Data/                           # Контекст EF Core
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   ├── Dtos/                           # Объекты передачи данных (контракты API)
│   │   ├── Auth/
│   │   │   ├── AuthResponseDto.cs
│   │   │   └── LoginDto.cs
│   │   ├── Notes/
│   │   │   ├── CreateNoteDto.cs
│   │   │   ├── NoteResponseDto.cs
│   │   │   └── UpdateNoteDto.cs
│   │   ├── Tasks/
│   │   │   ├── CreateTaskDto.cs
│   │   │   ├── TaskResponseDto.cs
│   │   │   └── UpdateTaskDto.cs
│   │   └── Users/
│   │       ├── CreateUserDto.cs
│   │       ├── UpdateUserDto.cs
│   │       └── UserResponseDto.cs
│   ├── Exceptions/
│   │   ├── AppException.cs             # Базовый класс доменных исключений
│   │   └── DomainExceptions.cs         # NotFoundException, ConflictException и др.
│   ├── Middleware/
│   │   └── GlobalExceptionHandler.cs   # Централизованная обработка ошибок
│   ├── Migrations/                     # Миграции
│   ├── Models/                         # Доменные сущности (таблицы базы данных)
│   │   ├── IAuditable.cs
│   │   ├── Note.cs
│   │   ├── TaskItem.cs
│   │   └── User.cs
│   ├── Services/                       # Бизнес-логика
│   │   ├── AuthService.cs
│   │   ├── JwtService.cs
│   │   ├── NoteService.cs
│   │   ├── TaskService.cs
│   │   └── UserService.cs
│   ├── Validators/                     # Валидаторы FluentValidation
│   │   ├── CreateNoteDtoValidator.cs
│   │   ├── CreateTaskDtoValidator.cs
│   │   ├── CreateUserDtoValidator.cs
│   │   ├── LoginDtoValidator.cs
│   │   ├── UpdateNoteDtoValidator.cs
│   │   ├── UpdateTaskDtoValidator.cs
│   │   └── UpdateUserDtoValidator.cs
│   ├── Program.cs                      # Точка входа и DI-конфигурация
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── .env.example                    # Шаблон переменных окружения
│   └── NotesApi.csproj
├── .gitignore
├── LICENSE
├── README.md
├── README-EN.md
└── NotesApi.sln
```

## 🔧 Разработка

### Добавление новой функциональности

1. Создать сущность в `Models/`
2. Добавить DTO в `Dtos/`
3. Настроить сущность в `AppDbContext.OnModelCreating`
4. Реализовать интерфейс и класс сервиса в `Services/`
5. Добавить контроллер в `Controllers/` (наследовать от `ApiControllerBase`)
6. Создать валидатор в `Validators/`
7. Создать и применить миграцию:
   ```bash
   dotnet ef migrations add FeatureName --project NotesApi
   dotnet ef database update --project NotesApi
   ```

### Миграции

Проект использует `IDesignTimeDbContextFactory`, которая читает `.env` напрямую — строку подключения передавать вручную не нужно.

## 🛡️ Безопасность

- Пароли хешируются алгоритмом **BCrypt**
- Первичные ключи — **GUID** (непоследовательные, предотвращают перебор)
- JWT-секреты хранятся **только** в переменных окружения, никогда в `appsettings.json`
- Пользователи имеют доступ только к своим данным — проверка владения на уровне сервисов
- Чувствительное логирование EF Core отключено в Production

## 📄 Лицензия

Проект распространяется под лицензией MIT. Подробнее см. в файле `LICENSE`.

## 👤 Автор

**Obj-Proger**

---

**Предложения, баг-репорты и pull request-ы приветствуются!**