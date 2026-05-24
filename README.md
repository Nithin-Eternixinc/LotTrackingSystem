# LTS — Lot Tracking System

A Windows desktop application built to simulate and manage 
semiconductor wafer manufacturing workflows. Engineers and 
operators can track wafers from raw inventory through lot 
creation, carrier assignment, and multi-station process execution.


## Tech Stack

| Layer | Technology |
|---|---|
| Language | C#  |
| UI Framework | WPF (Windows Presentation Foundation) |
| Pattern | MVVM (Model-View-ViewModel) |
| Database | SQLite via Entity Framework Core |
| Unit Testing | xUnit + Moq |

## Project Structure
LTS/
├── LTS.UI/             → Views, ViewModels, Styles, Converters
├── LTS.Application/    → Services, Repositories, DbContext
├── LTS.Common/         → Models, Interfaces, Constants
└── LTS.Tests/          → Unit tests


## Default Login Credentials

| Username | Password | Role |
|---|---|---|
| Engineer01 | 123 | Engineer |
| Operator01 | 123 | Operator |


## Setup and Run

### Requirements
- Windows OS
- Visual Studio 2022 or later
- .NET 8 SDK

### Steps

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/LTS-Lot-Tracking-System.git

# 2. Open solution
# Open LTS.sln in Visual Studio

# 3. Restore NuGet packages
# Visual Studio does this automatically on build
# Or run: dotnet restore

# 4. Run the application
# Set LTS.UI as startup project
# Press F5 or click Run

# 5. Database is created automatically on first run
# SQLite file: lts_database.db (gitignored)
```

### Run Tests

```bash
dotnet test
```


## MVVM Flow
View → ViewModel → Service → Repository → Database
User clicks button
↓
ViewModel validates input
↓
Service applies business rules
↓
Repository executes DB query
↓
EF Core → SQLite
##login page
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/d3eb1a61-3b18-479d-b0eb-2d5a5ea96027" />

#Dashboard
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/a3c71fae-f750-4b24-bcc1-1a4d63ac8117" />
##supplier
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/c367feeb-07a9-402b-a700-56be1686757b" />
##wafer
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/13da893c-e975-415c-8485-b8511308c127" />
##Carrier
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/6d3236e1-fd99-4d40-ad36-6440a29e0025" />
##lot 
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/1cf7b318-88df-4ede-8aae-9b67ed7a74a1" />






