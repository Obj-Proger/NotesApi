# Notes API

[🇷🇺 Читать на русском](README.md)

A fully-featured REST API for managing notes, tasks, and users, built with **.NET 10**, **PostgreSQL**, and **JWT authentication**.  
Designed with centralized exception handling, automatic auditing, and production-ready configuration.

## 🚀 Features

- **User management** — registration, login, profile updates, account deletion
- **Notes** — CRUD with archiving and color labels
- **Tasks** — CRUD with priorities (Low / Medium / High), due dates, overdue filtering
- **Authentication** — JWT access + refresh tokens (ASP.NET Core JWT Bearer)
- **Centralized error handling** — `GlobalExceptionHandler` + typed domain exceptions, RFC 7807 `ProblemDetails` responses
- **Security** — BCrypt password hashing, GUID keys, secrets via environment variables
- **Automatic auditing** — `CreatedAt` and `UpdatedAt` fields managed by EF Core interceptors
- **Validation** — FluentValidation for all input DTOs
- **Swagger UI** — interactive API documentation at `/swagger`
- **CORS** — configurable cross-origin request support
- **Structured logging** — console logs with different verbosity for Development / Production

## 📋 Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- (Optional) Visual Studio 2022+, Rider, or VS Code

## ⚙️ Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Obj-Proger/notes-api.git
cd notes-api
```

### 2. Configure environment variables

Copy the example file and fill in your values:

```bash
cp NotesApi/.env.example NotesApi/.env
```

Open `NotesApi/.env` and configure:

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=notes_db
DB_USER=postgres
DB_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_super_secret_key_at_least_32_chars!
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_EXPIRATION_DAYS=7

# Application ports
APP_HTTP_PORT=5001
APP_HTTPS_PORT=7001

# Environment
APP_ENV=Development
```

### 3. Make sure PostgreSQL is running

```bash
# Windows (run as Administrator)
net start postgresql-x64-16

# Linux / macOS
sudo systemctl start postgresql
```

### 4. Restore packages and build

```bash
dotnet restore
dotnet build
```

### 5. Run the application

```bash
dotnet run --project NotesApi
```

Migrations are applied automatically on startup. Swagger UI is available at `http://localhost:5001/swagger`.

> **Manual migration** (if needed):
> ```bash
> dotnet ef database update --project NotesApi
> ```

## 📚 API Reference

Full interactive documentation is available via Swagger UI when the application is running.

### 🔐 Authentication

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

The response contains `accessToken`, `refreshToken`, and expiration time. Use `Bearer <accessToken>` in the `Authorization` header for all protected endpoints.

### 👤 Users `[Authorize]`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/users` | List all users |
| `GET` | `/api/users/{id}` | Get user profile by ID |
| `PUT` | `/api/users/{id}` | Update own profile |
| `DELETE` | `/api/users/{id}` | Delete own account |

### 📝 Notes `[Authorize]`

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/notes` | Create a note |
| `GET` | `/api/notes/{id}` | Get note by ID |
| `GET` | `/api/notes/user/all` | All active notes |
| `GET` | `/api/notes/user/archived` | Archived notes |
| `PUT` | `/api/notes/{id}` | Update a note (partial update) |
| `DELETE` | `/api/notes/{id}` | Delete a note |

### ✅ Tasks `[Authorize]`

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/tasks` | Create a task |
| `GET` | `/api/tasks/{id}` | Get task by ID |
| `GET` | `/api/tasks/user/all` | All tasks (filter: `?completed=true/false`) |
| `GET` | `/api/tasks/user/priority/{priority}` | Incomplete tasks by priority |
| `GET` | `/api/tasks/user/overdue` | Overdue incomplete tasks |
| `PUT` | `/api/tasks/{id}` | Update a task |
| `DELETE` | `/api/tasks/{id}` | Delete a task |

## ⚠️ Error Handling

All errors are handled centrally by `GlobalExceptionHandler` and returned as RFC 7807 `ProblemDetails`.  
Controllers contain no `try/catch` blocks — services throw typed domain exceptions that are automatically mapped to HTTP responses.

| Exception | HTTP Code | When Used |
|---|---|---|
| `NotFoundException` | 404 | Resource not found |
| `ConflictException` | 409 | Duplicate email / username |
| `UnauthorizedException` | 401 | Invalid credentials |
| `ForbiddenException` | 403 | Insufficient permissions |
| `ValidationException` | 400 | Business validation failure |

Example error response:

```json
{
  "status": 404,
  "title": "not_found",
  "detail": "Note with id 'a1b2c3...' was not found.",
  "instance": "/api/notes/a1b2c3..."
}
```

## 🧱 Project Structure

```
NotesApi/                               # Repository root
├── NotesApi/                           # Main application
│   ├── Controllers/                    # HTTP request handlers
│   │   ├── ApiControllerBase.cs        # Base controller (GetUserId, etc.)
│   │   ├── AuthController.cs
│   │   ├── NotesController.cs
│   │   ├── TasksController.cs
│   │   └── UsersController.cs
│   ├── Data/                           # EF Core DbContext
│   ├── Dtos/                           # Data Transfer Objects (API contracts)
│   │   ├── Auth/
│   │   ├── Notes/
│   │   ├── Tasks/
│   │   └── Users/
│   ├── Exceptions/
│   │   ├── AppException.cs             # Base class for domain exceptions
│   │   └── DomainExceptions.cs         # NotFoundException, ConflictException, etc.
│   ├── Middleware/
│   │   └── GlobalExceptionHandler.cs   # Centralized error handling
│   ├── Migrations/                     # Database migrations
│   ├── Models/                         # Domain entities (database tables)
│   ├── Services/                       # Business logic
│   ├── Validators/                     # FluentValidation validators
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── .env.example
│   └── NotesApi.csproj
├── .gitignore
├── LICENSE
├── README.md
├── README-EN.md
└── NotesApi.sln
```

## 🔧 Development

### Adding New Features

1. Create an entity in `Models/`
2. Add DTOs in `Dtos/`
3. Configure the entity in `AppDbContext.OnModelCreating`
4. Implement the service interface and class in `Services/`
5. Add a controller in `Controllers/` (inherit from `ApiControllerBase`)
6. Create a validator in `Validators/`
7. Create and apply a migration:
   ```bash
   dotnet ef migrations add FeatureName --project NotesApi
   dotnet ef database update --project NotesApi
   ```

### Migrations

The project uses `IDesignTimeDbContextFactory` which reads `.env` directly — no need to pass a connection string manually.

## 🛡️ Security

- Passwords are hashed with **BCrypt**
- Primary keys are **GUIDs** (non-sequential, resistant to enumeration)
- JWT secrets are stored **only** in environment variables, never in `appsettings.json`
- Users can only access their own data — ownership enforced at the service layer
- Sensitive EF Core logging is disabled in Production

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## 👤 Author

**Obj-Proger**

---

**Issues, bug reports, and pull requests are welcome!**