# 🏫 BibekSchool

A **multi-role School Management System** built with **ASP.NET Core MVC**, designed to streamline school operations for Admins, Teachers, and Students  all from one clean, unified dashboard.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?style=flat&logo=dotnet)
![MySQL](https://img.shields.io/badge/Database-MySQL-4479A1?style=flat&logo=mysql&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

🔗 **Live Demo:** [bibekyadav-b6fhcbegd7c5gjha.centralindia-01.azurewebsites.net](https://bibekyadav-b6fhcbegd7c5gjha.centralindia-01.azurewebsites.net/)

---

## 📖 About the Project

**BibekSchool** is a full-featured school management web application that brings together everyone in a school's daily workflow administrators, teachers, and students into a single, role-based system. It was built to solve real problems schools face: scattered mark sheets, manual result calculation, and poor communication between staff and students.

The system supports **four distinct roles**, each with its own dashboard, permissions, and views ensuring the right people see the right data.

---

## ✨ Features

### 👑 MainAdmin
- Full control over the entire system
- Create and manage Admin accounts
- Oversee all schools, teachers, and students
- System-wide settings and configuration

### 🛠️ Admin
- Manage teachers and student records
- Assign classes and subjects
- View school-wide reports and analytics
- Manage notifications

### 👨‍🏫 Teacher
- Enter and update student marks
- View assigned classes and subjects
- Generate and publish results
- Send notifications to students

### 🎓 Student
- Register and manage personal account
- View marks and generated results
- Receive real-time notifications
- Simple, clean dashboard experience

### 🔐 Core System Features
- Role-based authentication & authorization
- Secure login/registration with encrypted sessions
- Responsive UI across all dashboards
- Mark entry → automatic result generation pipeline
- Notification system for announcements/updates
- Clean sidebar navigation with role-specific menus

---

## 🧰 Tech Stack

| Layer            | Technology                          |
|-------------------|--------------------------------------|
| **Framework**      | ASP.NET Core MVC (.NET 8)            |
| **Language**       | C#                                    |
| **Database**       | MySQL (via Pomelo EF Core provider)  |
| **ORM**            | Entity Framework Core                |
| **Frontend**       | Razor Views, HTML5, CSS3, Bootstrap  |
| **Authentication** | ASP.NET Core Identity                |
| **Deployment**     | Azure App Service / Docker (Railway) |

---

## 📂 Project Structure

```
BibekSchool/
├── Controllers/         # MVC Controllers (Account, Admin, Teacher, Student, etc.)
├── Models/               # Entity models & ViewModels
├── Views/                # Razor views organized by role
│   ├── Account/
│   ├── Admin/
│   ├── Teacher/
│   └── Student/
├── Data/                 # DbContext & DbSeeder
├── wwwroot/              # Static files (CSS, JS, images)
├── Dockerfile            # Docker config for containerized deployment
├── appsettings.json      # App configuration
└── Program.cs            # Application entry point
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MySQL Server](https://dev.mysql.com/downloads/) (or a Docker container)
- Visual Studio 2022/2026 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/bibekydv285-source/BibekSchool.git
   cd BibekSchool
   ```

2. **Configure the database connection**

   Update `appsettings.json` with your MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=BibekSchoolDb;User=root;Password=yourpassword;"
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. Open your browser at `https://localhost:5001`

### Run with Docker
```bash
docker build -t bibekschool .
docker run -p 8080:80 bibekschool
```

---

## 🔒 Security Note

> ⚠️ Before deploying publicly, make sure any seeded admin credentials in `Data/DbSeeder.cs` are moved to **environment variables** or **user secrets** rather than hardcoded values.

```bash
dotnet user-secrets set "AdminSeed:Password" "YourStrongPassword"
```

---

## 🗺️ Roadmap

- [ ] Add attendance tracking module
- [ ] Add fee management system
- [ ] Add parent portal / role
- [ ] Export results as PDF
- [ ] Email notifications integration

---

## 🤝 Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/YourFeature`)
3. Commit your changes (`git commit -m "Add YourFeature"`)
4. Push to the branch (`git push origin feature/YourFeature`)
5. Open a Pull Request

---

## 📄 License

© 2026 Bibek Yadav. All Rights Reserved.

This project is proprietary. You may view the source code, but copying,
modifying, or redistributing it without permission is not allowed.

## 📬 Contact

**Bibek Yadav**
- Portfolio: [bibek-yadav.com.np](https://bibek-yadav.com.np)
- GitHub: [@bibekydv285-source](https://github.com/bibekydv285-source)

---

<p align="center">⭐ If you found this project useful, consider giving it a star!</p>
