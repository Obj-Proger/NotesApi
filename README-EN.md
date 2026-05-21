# Notes API

[🇷🇺 Читать на русском](README.md)

A full-featured REST API for managing notes, tasks, and users, built with **.NET 10**, **PostgreSQL**, and **JWT authentication**.  
Designed with Clean Architecture principles, automatic auditing, and production-ready configuration.

## 🚀 Features

- **User Management** – registration, login, profile update, account deletion
- **Notes** – CRUD with archiving and color labels
- **Tasks** – CRUD with priorities (Low/Medium/High), due dates, overdue filtering
- **Authentication** – JWT access + refresh tokens (standard ASP.NET Core JWT Bearer)
- **Security** – BCrypt password hashing, GUID primary keys, secrets via environment variables
- **Automatic Auditing** – `CreatedAt` and `UpdatedAt` fields managed by database defaults and EF Core interceptors
- **Database** – PostgreSQL, Entity Framework Core, migrations, design-time factory
- **Validation** – FluentValidation for all input DTOs (replaces Data Annotations)
- **Swagger UI** – interactive API documentation at `/swagger`
- **CORS** – configurable cross-origin support
- **Structured Logging** – console logging with different verbosity for Development/Production
- **Clean Architecture** – separation into Models, DTOs, Services, Controllers

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- (Optional) Visual Studio 2024+, Rider, or VS Code

## ⚙️ Setup Instructions

### 1. Clone the repository

```bash
git clone https://github.com/Obj-Proger/notes-api.git
cd notes-api
```

### 2. Configure environment variables

Copy the example file and edit it with your settings:

```bash
cp NotesApi/.env.example NotesApi/.env
```

Open `NotesApi/.env` and fill in:

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=notes_db
DB_USER=postgres
DB_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_super_secret_key_minimum_32_characters_long!
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_EXPIRATION_DAYS=7

# HTTP and HTTPS ports
APP_HTTP_PORT=5001
APP_HTTPS_PORT=7001

# Environment
APP_ENV=Development
```

### 3. Restore packages and build

```bash
dotnet restore
dotnet build
```

### 4. Apply migrations

The project includes a **design-time factory** (`AppDbContextFactory`) that reads `.env` automatically.  
Simply run:

```bash
dotnet ef database update --project NotesApi
```

Or in the Visual Studio Package Manager Console:

```powershell
Update-Database
```

The PostgreSQL database will be created (if it doesn't exist) and all tables will be set up.

### 5. Run the application

```bash
dotnet run --project NotesApi
```

The API will start on the ports specified in `.env` (`http://localhost:5001` and `https://localhost:7001` by default).  
Swagger UI is available at `http://localhost:5001/swagger` or `https://localhost:7001/swagger`.

## 📚 API Documentation

All endpoints are documented interactively via Swagger UI when the application is running.  
Below is a quick reference.

### 🔐 Authentication

#### Register a new user
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

The response contains `accessToken`, `refreshToken`, and expiration.

### 👤 Users

All endpoints require authentication (`Bearer <accessToken>`).

- `GET /api/users` – list all users
- `GET /api/users/{id}` – get user by ID (GUID)
- `PUT /api/users/{id}` – update own profile (first name, last name, email)
- `DELETE /api/users/{id}` – delete own account

### 📝 Notes

- `POST /api/notes` – create a note
- `GET /api/notes/user/all` – get all non-archived notes
- `GET /api/notes/user/archived` – get archived notes
- `GET /api/notes/{id}` – get note by ID (GUID)
- `PUT /api/notes/{id}` – update note (partial updates supported)
- `DELETE /api/notes/{id}` – delete note

### ✅ Tasks

- `POST /api/tasks` – create a task
- `GET /api/tasks/user/all?completed=false` – list tasks (optional filter)
- `GET /api/tasks/user/priority/{priority}` – filter by priority (0=Low,1=Medium,2=High)
- `GET /api/tasks/user/overdue` – get overdue tasks
- `PUT /api/tasks/{id}` – update task
- `DELETE /api/tasks/{id}` – delete task

## 🧱 Project Structure

```
notes-api/                          # Repository root
├── NotesApi/                       # Main application
│   ├── Controllers/                # HTTP request handlers
│   │   ├── AuthController.cs
│   │   ├── NotesController.cs
│   │   ├── TasksController.cs
│   │   └── UsersController.cs
│   ├── Data/                       # EF Core DbContext
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   ├── Dtos/                       # Data Transfer Objects (API contracts)
│   │   ├── Auth/
│   │   ├── Notes/
│   │   ├── Tasks/
│   │   └── Users/
│   ├── Migrations/                 # Database migrations
│   ├── Models/                     # Domain entities (database tables)
│   │   ├── IAuditable.cs
│   │   ├── Note.cs
│   │   ├── TaskItem.cs
│   │   └── User.cs
│   ├── Services/                   # Business logic
│   │   ├── AuthService.cs
│   │   ├── JwtService.cs
│   │   ├── NoteService.cs
│   │   ├── TaskService.cs
│   │   └── UserService.cs
│   ├── Validators/                 # FluentValidation validators
│   │   ├── CreateNoteDtoValidator.cs
│   │   ├── CreateTaskDtoValidator.cs
│   │   ├── CreateUserDtoValidator.cs
│   │   ├── LoginDtoValidator.cs
│   │   ├── UpdateNoteDtoValidator.cs
│   │   ├── UpdateTaskDtoValidator.cs
│   │   └── UpdateUserDtoValidator.cs
│   ├── Program.cs                  # Entry point and DI configuration
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── .env.example                # Environment variables template
│   └── NotesApi.csproj
├── .gitignore
├── LICENSE
├── README.md
├── README-EN.md
└── NotesApi.sln
```

## 🔧 Development

### Adding a new feature

1. Create an entity in `Models/`
2. Add DTOs in `Dtos/`
3. Configure the entity in `AppDbContext.OnModelCreating`
4. Implement the service interface and class in `Services/`
5. Add a controller in `Controllers/`
6. Create a validator in `Validators/`
7. Create a migration:  
   `dotnet ef migrations add FeatureName --project NotesApi`
8. Apply to database:  
   `dotnet ef database update --project NotesApi`
9. Commit changes

### Migrations

The project uses an `IDesignTimeDbContextFactory` that reads `.env` directly, so there is no need to pass a connection string manually. Detailed EF Core logs are enabled in `Development` mode (see the `APP_ENV` variable).

### Testing

To add unit tests, create a new `NotesApi.Tests` project (e.g., xUnit) and mock the service interfaces.

## 🛡️ Security

- Passwords are hashed with **BCrypt** (via `BCrypt.Net-Next`).
- Primary keys are **GUIDs** (non-sequential, preventing enumeration).
- JWT secrets are stored **only** in environment variables (`.env`), never in `appsettings.json`.
- `appsettings.json` contains no sensitive data.
- Sensitive EF Core logging is **disabled** in Production.
- Input validation is extracted into separate FluentValidation classes, preventing invalid data from reaching the service layer.

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## 👤 Author

**Obj-Proger**

---

**Suggestions, bug reports, and pull requests are welcome!**