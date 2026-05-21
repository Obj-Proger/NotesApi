# Notes API

Полнофункциональное REST API для управления заметками, задачами и пользователями, построенное на **.NET 10**, **PostgreSQL** и **JWT-аутентификации**.  
Разработано с учётом принципов Clean Architecture, автоматического аудита и настроек, готовых к production.

## 🚀 Возможности

- **Управление пользователями** – регистрация, вход, обновление профиля, удаление аккаунта
- **Заметки** – CRUD с архивированием, цветовыми метками
- **Задачи** – CRUD с приоритетами (Низкий/Средний/Высокий), сроками, фильтрацией просроченных
- **Аутентификация** – JWT access + refresh токены (стандартный ASP.NET Core JWT Bearer)
- **Безопасность** – хеширование паролей BCrypt, GUID-ключи, секреты через переменные окружения
- **Автоматический аудит** – поля `CreatedAt` и `UpdatedAt` управляются базой данных и перехватчиками EF Core
- **База данных** – PostgreSQL, Entity Framework Core, миграции, фабрика времени разработки
- **Валидация** – FluentValidation для всех входных DTO (замена атрибутов Data Annotations)
- **Swagger UI** – интерактивная документация API по адресу `/swagger`
- **CORS** – настраиваемая поддержка кросс-доменных запросов
- **Структурированное логирование** – консольные логи с разной детализацией для Development/Production
- **Чистая архитектура** – разделение на модели, DTO, сервисы, контроллеры

## 📋 Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- (Опционально) Visual Studio 2024+, Rider или VS Code

## ⚙️ Инструкция по запуску

### 1. Клонирование репозитория

```bash
git clone https://github.com/Obj-Proger/notes-api.git
cd notes-api
```

### 2. Настройка переменных окружения

Скопируйте файл-пример и отредактируйте его под свои значения:

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

### 3. Восстановление пакетов и сборка

```bash
dotnet restore
dotnet build
```

### 4. Применение миграций

В проекте используется **фабрика времени разработки** (`AppDbContextFactory`), которая автоматически читает `.env`.  
Просто выполните:

```bash
dotnet ef database update --project NotesApi
```

Или в консоли диспетчера пакетов Visual Studio:

```powershell
Update-Database
```

База данных PostgreSQL будет создана (если отсутствует), и все таблицы будут развёрнуты.

### 5. Запуск приложения

```bash
dotnet run --project NotesApi
```

API запустится на портах, указанных в `.env` (`http://localhost:5001` и `https://localhost:7001` по умолчанию).  
Swagger UI доступен по адресу `http://localhost:5001/swagger` или `https://localhost:7001/swagger`.

## 📚 Документация API

Вся документация доступна в интерактивном виде через Swagger UI при запущенном приложении.  
Ниже приведены краткие примеры запросов.

### 🔐 Аутентификация

#### Регистрация нового пользователя
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

#### Вход в систему
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "ivan@example.com",
  "password": "SecurePass123!"
}
```

Ответ содержит `accessToken`, `refreshToken` и срок действия.

### 👤 Пользователи

Все методы требуют аутентификации (`Bearer <accessToken>`).

- `GET /api/users` – список всех пользователей
- `GET /api/users/{id}` – получить пользователя по ID (GUID)
- `PUT /api/users/{id}` – обновить свой профиль (имя, фамилия, email)
- `DELETE /api/users/{id}` – удалить свой аккаунт

### 📝 Заметки

- `POST /api/notes` – создать заметку
- `GET /api/notes/user/all` – все неархивированные заметки
- `GET /api/notes/user/archived` – архивированные заметки
- `GET /api/notes/{id}` – получить заметку по ID (GUID)
- `PUT /api/notes/{id}` – обновить заметку (частичное обновление)
- `DELETE /api/notes/{id}` – удалить заметку

### ✅ Задачи

- `POST /api/tasks` – создать задачу
- `GET /api/tasks/user/all?completed=false` – список задач (опциональный фильтр)
- `GET /api/tasks/user/priority/{priority}` – фильтр по приоритету (0=Низкий,1=Средний,2=Высокий)
- `GET /api/tasks/user/overdue` – просроченные задачи
- `PUT /api/tasks/{id}` – обновить задачу
- `DELETE /api/tasks/{id}` – удалить задачу

## 🧱 Структура проекта

```
notes-api/                          # Корень репозитория
├── NotesApi/                       # Основное приложение
│   ├── Controllers/                # Обработчики HTTP-запросов
│   │   ├── AuthController.cs
│   │   ├── NotesController.cs
│   │   ├── TasksController.cs
│   │   └── UsersController.cs
│   ├── Data/                       # Контекст EF Core
│   │   ├── AppDbContext.cs
│   │   ├── AppDbContextFactory.cs
│   ├── Dtos/                       # Объекты передачи данных (контракты API)
│   │   ├── Auth/
│   │   ├── Notes/
│   │   ├── Tasks/
│   │   └── Users/
│   ├── Migrations/                 # Миграции
│   ├── Models/                     # Доменные сущности (таблицы базы данных)
│   │   ├── IAuditable.cs
│   │   ├── Note.cs
│   │   ├── TaskItem.cs
│   │   └── User.cs
│   ├── Services/                   # Бизнес-логика
│   │   ├── AuthService.cs
│   │   ├── JwtService.cs
│   │   ├── NoteService.cs
│   │   ├── TaskService.cs
│   │   └── UserService.cs
│   ├── Validators/                 # Валидаторы FluentValidation
│   │   ├── CreateNoteDtoValidator.cs
│   │   ├── CreateTaskDtoValidator.cs
│   │   ├── CreateUserDtoValidator.cs
│   │   ├── LoginDtoValidator.cs
│   │   ├── UpdateNoteDtoValidator.cs
│   │   ├── UpdateTaskDtoValidator.cs
│   │   └── UpdateUserDtoValidator.cs
│   ├── Program.cs                  # Точка входа и DI-конфигурация
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── .env.example                # Шаблон переменных окружения
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
5. Добавить контроллер в `Controllers/`
6. Создать валидатор в `Validators/`
7. Создать миграцию:  
   `dotnet ef migrations add FeatureName --project NotesApi`
8. Применить к базе данных:  
   `dotnet ef database update --project NotesApi`
9. Закоммитить изменения

### Миграции

Проект использует `IDesignTimeDbContextFactory`, которая читает `.env` напрямую, поэтому нет необходимости вручную передавать строку подключения. Подробные логи EF Core включаются в режиме `Development` (см. переменную `APP_ENV`).

### Тестирование

Для добавления юнит-тестов создайте новый проект `NotesApi.Tests` (например, xUnit) и mock-объекты для интерфейсов сервисов.

## 🛡️ Безопасность

- Пароли хешируются алгоритмом **BCrypt** (через `BCrypt.Net-Next`).
- Первичные ключи — **GUID** (непоследовательные, предотвращают перебор).
- JWT-секреты хранятся **только** в переменных окружения (`.env`), никогда в `appsettings.json`.
- `appsettings.json` не содержит секретных данных.
- Чувствительное логирование EF Core **отключено** в Production.
- Валидация входных данных вынесена в отдельные классы FluentValidation, что предотвращает передачу некорректных данных на уровень сервисов.

## 📄 Лицензия

Проект распространяется под лицензией MIT. Подробнее см. в файле `LICENSE`.

## 👤 Автор

**Obj-Proger**

---

**Предложения, баг-репорты и pull request-ы приветствуются!**