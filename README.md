# 📌 TaskHub - Team Task Management System

A full-stack task management system built with .NET 8, designed for small to medium development teams to manage projects, tasks, and collaboration efficiently.

The system combines REST APIs, gRPC services, and a Blazor web UI, showcasing modern .NET backend architecture and practical distributed system design.

## 🚀 Key Features

### 👥 User & Authentication
- User registration and login
- JWT-based authentication
- Role-based access control (Admin / Member)
  
### 📁 Project Management
- Create and manage multiple projects
- Assign project ownership
- Project-level task grouping
  
### 📋 Task Management
- Create, update, delete tasks
- Task status workflow:
- Todo → In Progress → Done
- Assign tasks to users
- Priority levels (Low / Medium / High)
  
### 📊 Dashboard & Analytics
- Project progress overview
- Task completion statistics
- User workload overview
  
### 🔔 Internal Communication (gRPC)
- Notification service for task updates
- Analytics service for reporting
- Internal service-to-service communication using gRPC
  
## 🏗️ Architecture Overview

The system follows a layered architecture with service separation:
```text
Frontend (Blazor Web App)
        ↓
ASP.NET Core Web API (REST)
        ↓
Application / Service Layer
        ↓
Entity Framework Core
        ↓
SQL Server Database

+ gRPC Microservices
  - Notification Service
  - Analytics Service
```
## 🧰 Tech Stack

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core
- SQL Server
- gRPC
  
### Frontend
- Blazor Server / Blazor WebAssembly
  
### Authentication
- JWT (JSON Web Token)
  
### Other
- Dependency Injection (DI)
- AutoMapper
- Logging (Serilog optional)

## 🗄️ Database Design

### Users
- Id
- Email
- PasswordHash
- Role

### Projects
- Id
- Name
- OwnerId
- CreatedAt

### Tasks
- Id
- Title
- Description
- Status
- Priority
- ProjectId
- AssignedUserId
- CreatedAt
- UpdatedAt

### TaskHistory (optional enhancement)
- Id
- TaskId
- OldStatus
- NewStatus
- ChangedAt

## 🎯 Project Goals

This project is designed to demonstrate:
- Full-stack .NET development skills
- REST API design and best practices
- gRPC integration in real systems
- Database design with EF Core
- Clean layered architecture
- Practical authentication & authorization

## 📦 Future Improvements
- Real-time updates with SignalR
- Docker containerization
- Redis caching layer
- Email notification integration
- Frontend state management improvements
- Unit & integration testing