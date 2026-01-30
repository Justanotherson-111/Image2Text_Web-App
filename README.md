# Image2Text Web App

A full-stack **Image-to-Text (OCR) web application** that extracts text from uploaded images using modern OCR engines and a scalable backend architecture.

The system is designed with **production-grade patterns** including JWT authentication, background OCR processing, PostgreSQL persistence, Dockerized services, and optional microservice-based OCR engines.

---

## ✨ Features

- 📤 Upload images and extract text via OCR  
- 🔐 JWT-based authentication & authorization  
- 👤 Role-based users (Admin / User)  
- ⚙️ Background OCR processing (non-blocking)  
- 📄 Export extracted text (PDF / text files)  
- 🧠 Multiple OCR engines:
  - Tesseract OCR  
  - PaddleOCR (optional microservice)  
- 🐳 Fully Dockerized (Backend, Frontend, DB, OCR)  
- 🌐 HTTPS support using **mkcert**  
- 📦 PostgreSQL with EF Core migrations & auto-seeding  
- 📡 SignalR ready (future real-time updates)

---

## 🧰 Tech Stack

### Backend
- **ASP.NET Core (.NET 9)**
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- QuestPDF
- Tesseract OCR

### Frontend
- React + Vite
- Nginx
- HTTPS via mkcert

### Infrastructure
- Docker & Docker Compose
- PostgreSQL 16
- mkcert (local TLS)
- Optional cloud OCR deployment (AWS, etc.)

---

## 📁 Project Structure

```
Project/
├── backend/
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Database/
│   ├── Hubs/
│   ├── Uploads/
│   ├── ExtractedText/
│   └── Program.cs
├── frontend/
│   ├── src/
│   ├── default.conf
│   └── certs/
├── docker-compose.yml
└── README.md
```
---

## ⚙️ Installation & Setup
🔧 Prerequisites

You must install manually:

Docker & Docker Compose

mkcert

Git

---
## 🔐 HTTPS Setup (Required)

This project uses mkcert for local HTTPS.

1️⃣ Install mkcert
```
# Linux
sudo apt install libnss3-tools

# macOS
brew install mkcert

# Windows
choco install mkcert
```

2️⃣ Create local CA
```
mkcert -install
```

3️⃣ Generate certificates
```
cd frontend
mkcert localhost
```

Place generated files into:
```
frontend/certs/
├── localhost.pem
└── localhost-key.pem
```

Or just rent server......

## 🐳 Docker Setup
1️⃣ Clone repository
```
git clone https://github.com/Justanotherson-111/Image2Text_Web-App.git
cd Image2Text_Web-App
```
2️⃣ Update database & secrets

⚠️ IMPORTANT:
You must change database credentials and JWT secrets in docker-compose.yml.
```
POSTGRES_USER: your_user
POSTGRES_PASSWORD: your_password
POSTGRES_DB: your_db
Jwt__Key: your_super_secret_key
ADMIN_PASSWORD: your_admin_password
```
3️⃣ Start core services
```
docker compose --profile core up --build


Services started:

PostgreSQL

Backend API: http://localhost:8080

Frontend: https://localhost
```
4️⃣ (Optional) Start OCR microservices
```
docker compose --profile ocr up --build


Includes:

PaddleOCR

Text Corrector service
```
### 👤 Default Admin Account

On first startup, the backend automatically seeds an admin account:

Email: admin@example.com
Password: ADMIN_PASSWORD (from environment variable)


⚠️ Change this password immediately in production.

### 🔑 Authentication

JWT Bearer authentication

Token sources:

Authorization: Bearer <token>

Optional HttpOnly cookie

Strict validation:

Issuer

Audience

Lifetime

Signing key

Zero clock skew

### 📦 Static Files

The backend serves:

Uploaded images → /uploads

Extracted text files → /extracted-text

These directories are mounted as Docker volumes.

## ☁️ Cloud Deployment (Optional) ==> Recommened for production purpose
*** This is demo version so in order to production-ready, we need to improve the system a lot. 

## 🧪 API & Swagger

Swagger is enabled in development:

http://localhost:8080/swagger

## 🛡️ Security Notes

BCrypt password hashing

JWT with strong signing keys

Rate limiting service

Forwarded headers support (reverse proxy ready)

Data Protection keys persisted via volume

## 🚧 Status

This project is actively developed and not feature-complete yet.

Planned improvements:

Real-time OCR progress via SignalR

Advanced OCR language switching

Admin dashboard

Cloud storage integration

## 📜 License

This project is licensed under the MIT License.

## 🙌 Author

Son Phan
Software Engineering · System Design · Security-focused Backend
