# Marathon Manager

A full-stack web application for managing marathon races, built with **ASP.NET Core** (API) and **ASP.NET Core MVC** (Web frontend). Developed as part of the PRN232 course at FPT University.

---

## Overview

Marathon Manager allows race organizers to create and manage running events, while runners can browse races, register, track their results, and view their personal stats. An admin panel provides full control over users, roles, and content.

---

## Project Structure

```

<img width="931" height="618" alt="MarathonManager_structure drawio" src="https://github.com/user-attachments/assets/770e0eb3-636c-41bf-a897-8a4e3d4c1954" />

---

## Features

### Authentication & Authorization
- Register / Login with JWT tokens
- Role-based access control: **Admin**, **Organizer**, **Runner**
- Seed endpoint to initialize default roles and admin account

### Runner
- Browse available (approved) races with pagination
- Register for a race distance; re-register after cancellation
- Fake payment simulation for testing (`/fake-payment`)
- View and cancel registrations
- View race results with completion time, overall rank, gender rank, and age category rank
- Full profile page with personal stats: total races, distances run, top-3 finishes, best times per distance category (5K / 10K / Half / Full marathon)

### Organizer
- Create and manage races (name, location, date, image, status)
- Define multiple race distances per event (distance in km, fee, max participants, start time)
- Manage registrations and assign bib numbers

### Admin
- User management: view, activate/deactivate accounts, update roles
- Blog post moderation: create, update, delete posts
- Full oversight of all races and registrations

### Blog
- Blog posts linked to specific races
- Supports likes and comments
- Public listing and detail views

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core Web API (.NET) |
| Frontend | ASP.NET Core MVC, Razor Views |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Auth | ASP.NET Core Identity + JWT Bearer |
| API Docs | Swagger / OpenAPI |

---

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (version used in project)
- SQL Server (local or remote)
- Visual Studio 2022 or VS Code

### 1. Clone the repository
```bash
git clone https://github.com/hongls0/PRN232.git
cd PRN232
```

### 2. Configure the database connection

In `MarathonManager.API/appsettings.json`, update the connection string:
```json
"ConnectionStrings": {
  "MyCnn": "Server=YOUR_SERVER;Database=MarathonManager;Trusted_Connection=True;"
}
```

Also set your JWT settings:
```json
"Jwt": {
  "Key": "your-secret-key-here",
  "Issuer": "MarathonManagerAPI",
  "Audience": "MarathonManagerWeb"
}
```

### 3. Apply database migrations
```bash
cd MarathonManager.API
dotnet ef database update
```

### 4. Seed roles and admin account

After running the API, call this endpoint once:
```
POST /api/auth/seed-roles
```
This creates the **Admin**, **Organizer**, and **Runner** roles and a default admin account:
- Email: `admin@marathon.com`
- Password: `Admin@123`

### 5. Run the projects

Run the API (default: `https://localhost:7280`):
```bash
cd MarathonManager.API
dotnet run
```

Run the Web frontend (default: `https://localhost:7281`):
```bash
cd MarathonManager.Web
dotnet run
```

Swagger UI is available at: `https://localhost:7280/swagger`

---

## API Endpoints Summary

| Method | Endpoint | Role | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register new account |
| POST | `/api/auth/login` | Public | Login and receive JWT |
| POST | `/api/auth/seed-roles` | Public | Initialize roles + admin |
| GET | `/api/races/runner/available` | Runner | List available races |
| POST | `/api/races/runner/register` | Runner | Register for a race |
| GET | `/api/races/runner/registrations` | Runner | My registrations |
| DELETE | `/api/races/runner/registrations/{id}` | Runner | Cancel registration |
| POST | `/api/races/runner/registrations/{id}/fake-payment` | Runner | Simulate payment |
| GET | `/api/races/runner/results` | Runner | My race results |
| GET | `/api/races/runner/profile` | Runner | Profile + stats |
| PUT | `/api/races/runner/profile` | Runner | Update profile |

---

## Team

Developed by Group — PRN232, FPT University.
