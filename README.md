[![UIT Logo](https://i.imgur.com/WmMnSRt.png)](https://www.uit.edu.vn/ "Trường Đại học Công nghệ Thông tin")

# **PHÁT TRIỂN ỨNG DỤNG TRÊN THIẾT BỊ DI ĐỘNG**

## Hệ thống Backend Mạng xã hội — Social Network API

![.NET](https://img.shields.io/badge/Platform-.NET%2010-512BD4?style=flat-square&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/Framework-ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-4169E1?style=flat-square&logo=postgresql)
![EF Core](https://img.shields.io/badge/ORM-Entity%20Framework%20Core%2010-512BD4?style=flat-square&logo=dotnet)
![RabbitMQ](https://img.shields.io/badge/Broker-RabbitMQ-FF6600?style=flat-square&logo=rabbitmq)
![gRPC](https://img.shields.io/badge/RPC-gRPC-4285F4?style=flat-square&logo=google)
![Cloudinary](https://img.shields.io/badge/Storage-Cloudinary-3448C5?style=flat-square&logo=cloudinary)
![Docker](https://img.shields.io/badge/Container-Docker-2496ED?style=flat-square&logo=docker)
![Azure](https://img.shields.io/badge/Platform-Azure%20VM-0078D4?style=flat-square&logo=microsoftazure)
![Terraform](https://img.shields.io/badge/IaC-Terraform-7B42BC?style=flat-square&logo=terraform)

---

## Thông tin đồ án

| Mục | Nội dung |
| --- | --- |
| **Tên đồ án** | Hệ thống Backend Mạng xã hội (Social Network API) |
| **Môn học** | Phát triển ứng dụng trên thiết bị di động |
| **Trường** | Đại học Công nghệ Thông tin – ĐHQG TP.HCM |
| **Năm học** | 2025 – 2026 |

---

## Thành viên thực hiện

| Họ và tên | MSSV |
| --- | --- |
| Phạm Thái Sơn | 23521361 |
| Lê Gia Quyền | 23521323 |
| Trương Thanh Quang | 23520295 |

---

## Mục tiêu đồ án

Đồ án xây dựng hệ thống **Backend RESTful API** cho ứng dụng mạng xã hội trên nền tảng Android, theo kiến trúc **Microservices** và **Clean Architecture**, với các mục tiêu chính:

- Thiết kế hệ thống **Microservices** gồm 6 service độc lập, giao tiếp qua **REST** và **gRPC**
- Áp dụng **Clean Architecture** (API / Application / Infrastructure / Domain) cho từng service
- Giao tiếp bất đồng bộ giữa các service qua **RabbitMQ** message broker
- Xác thực tập trung qua **Identity Service** (JWT Bearer)
- Upload và quản lý ảnh đại diện, ảnh bài đăng lên **Cloudinary**
- Triển khai toàn bộ hệ thống bằng **Docker Compose** trên **Azure VM**
- Quản lý hạ tầng theo mô hình **Infrastructure as Code** với **Terraform**
- CI/CD tự động qua **GitHub Actions**
- Ghi log hệ thống chuẩn với **Serilog**

---

## Công nghệ sử dụng

### Backend — mỗi service

| Công nghệ | Phiên bản | Vai trò |
| --- | --- | --- |
| .NET | `10.0` | Runtime platform |
| ASP.NET Core | `10.0` | Web API framework |
| Entity Framework Core | `10.0.x` | ORM — PostgreSQL |
| Npgsql.EF Core PostgreSQL | `10.0.0` | PostgreSQL provider cho EF Core |
| JWT Bearer Authentication | `10.0.x` | Xác thực token |
| Swashbuckle (Swagger) | `10.x` | Tài liệu API tự động |
| Serilog | `4.2.0` | Structured logging |
| gRPC (`Grpc.AspNetCore`) | `2.70.0` | Giao tiếp đồng bộ giữa services |
| RabbitMQ | — | Message broker bất đồng bộ |
| Cloudinary .NET SDK | — | Upload & quản lý ảnh |
| Health Checks (EF Core) | `10.0.x` | Kiểm tra trạng thái service |

### Infrastructure

| Công nghệ | Vai trò |
| --- | --- |
| Docker + Docker Compose | Containerize & orchestrate toàn bộ services |
| Azure VM | Hosting — chạy toàn bộ Docker containers |
| Terraform | Infrastructure as Code (Azure resources) |
| GitHub Actions | CI/CD tự động — build & deploy |
| Supabase (PostgreSQL) | Managed PostgreSQL database |

---

## Kiến trúc hệ thống

Hệ thống gồm **6 Microservices** độc lập, mỗi service có database riêng, giao tiếp với nhau qua REST, gRPC và RabbitMQ:

```
Android Client
    └─► REST / JWT Bearer
            └─► [Identity API]     ← Đăng ký, đăng nhập, cấp JWT
            └─► [User API]         ← Hồ sơ người dùng, Cloudinary
            └─► [Post API]         ← Bài đăng, like, comment
            └─► [Friend API]       ← Kết bạn, danh sách bạn bè
            └─► [Message API]      ← Tin nhắn trực tiếp
            └─► [Notification API] ← Thông báo hệ thống

Giao tiếp nội bộ:
    Friend API  ──gRPC──►  User API      (lấy thông tin user)
    Post API    ──gRPC──►  Friend API    (kiểm tra quyền xem bài)
    *           ──RabbitMQ──►  Notification API  (event-driven)
```

### Clean Architecture — mỗi service

```
[ServiceName].API              ← Controllers, Middleware, DI config
    └── [ServiceName].Application  ← Use Cases, DTOs, Interfaces
    └── [ServiceName].Domain       ← Entities, Domain Logic
    └── [ServiceName].Infrastructure ← EF Core, Repositories, External services
```

---

## Các Microservices

### 🔐 Identity Service — `Identity.API`

Xác thực tập trung cho toàn bộ hệ thống.

- Đăng ký tài khoản (register)
- Đăng nhập, cấp **JWT Access Token** + **Refresh Token**
- Xác thực & làm mới token (refresh)
- Đăng xuất, thu hồi token
- Đổi mật khẩu

---

### 👤 User Service — `User.API`

Quản lý hồ sơ và thông tin người dùng.

- Xem / cập nhật thông tin cá nhân
- Upload ảnh đại diện lên **Cloudinary** (`CloudinaryService.cs`)
- Tìm kiếm người dùng theo tên / username
- Lấy thông tin profile của người dùng khác
- Health check endpoint

---

### 🤝 Friend Service — `Friend.API`

Quản lý quan hệ bạn bè với giao tiếp **gRPC**.

- Gửi lời mời kết bạn
- Chấp nhận / từ chối lời mời kết bạn
- Huỷ kết bạn
- Xem danh sách bạn bè
- Xem danh sách lời mời đang chờ
- Expose **gRPC server** (`friendship.proto`) để các service khác truy vấn quan hệ bạn bè

---

### 📝 Post Service — `Post.API`

Quản lý bài đăng, tương tác mạng xã hội.

- Tạo / chỉnh sửa / xoá bài đăng
- Upload ảnh bài đăng lên **Cloudinary**
- Xem newsfeed (bài đăng của bạn bè)
- Like / unlike bài đăng
- Bình luận, trả lời bình luận
- Health check endpoint

---

### 💬 Message Service — `Message.API`

Quản lý tin nhắn trực tiếp giữa người dùng.

- Gửi tin nhắn trực tiếp (DM)
- Xem lịch sử hội thoại
- Danh sách các cuộc hội thoại
- Đánh dấu đã đọc
- Giao tiếp real-time qua **RabbitMQ**

---

### 🔔 Notification Service — `Notification.API`

Quản lý thông báo hệ thống, nhận event từ RabbitMQ.

- Nhận **events bất đồng bộ** từ các service khác qua **RabbitMQ** (`RabbitMQEventConsumer.cs`)
- Tạo thông báo khi có: lời mời kết bạn, like, comment, tin nhắn mới
- Lấy danh sách thông báo của người dùng
- Đánh dấu thông báo đã đọc
- Xoá thông báo

---

## Cấu trúc thư mục

```
Back_End/
├── src/
│   └── Services/
│       ├── Identity/
│       │   └── Identity.API/           # Controllers, JWT config
│       │   └── Identity.Application/   # Use cases, DTOs
│       │   └── Identity.Domain/        # Entities
│       │   └── Identity.Infrastructure/# EF Core, Repositories
│       │
│       ├── User/
│       │   └── User.API/
│       │   └── User.Application/
│       │   └── User.Domain/
│       │   └── User.Infrastructure/
│       │       ├── Services/CloudinaryService.cs
│       │       ├── Repositories/UserRepository.cs
│       │       ├── Data/UserDbContext.cs
│       │       └── Messaging/RabbitMQEventConsumer.cs
│       │
│       ├── Friend/
│       │   └── Friend.API/
│       │       └── Protos/friendship.proto   # gRPC contract
│       │   └── Friend.Application/
│       │   └── Friend.Domain/
│       │   └── Friend.Infrastructure/
│       │
│       ├── Post/
│       │   └── Post.API/
│       │   └── Post.Application/
│       │   └── Post.Domain/
│       │   └── Post.Infrastructure/
│       │
│       ├── Message/
│       │   └── Message.API/
│       │   └── Message.Application/
│       │   └── Message.Infrastructure/
│       │
│       └── Notification/
│           └── Notification.API/
│           └── Notification.Application/
│           └── Notification.Infrastructure/
│
├── Infrastructure/
│   └── terraform/
│       ├── main.tf             # Azure VM & resources
│       ├── variables.tf
│       └── providers.tf
│
├── .github/
│   └── workflows/
│       └── deploy.yml          # CI/CD: build Docker → Azure VM
│
├── docker-compose.yml          # Development
├── docker-compose.prod.yml     # Production
└── SocialNetwork.sln           # .NET Solution file
```

---

## Hướng dẫn chạy local

### Yêu cầu

```
.NET SDK  >= 10.0
Docker & Docker Compose
PostgreSQL (hoặc Supabase account)
RabbitMQ (hoặc chạy qua Docker)
Cloudinary account
```

### 1. Clone repository

```bash
git clone https://github.com/sonpham811z/Android-SocialNetwork-API.git
cd Android-SocialNetwork-API/Back_End
```

### 2. Cấu hình biến môi trường

Mỗi service có file `.env` riêng. Tạo từ file mẫu trong từng service:

```bash
# Ví dụ cho User service
cp src/Services/User/User.API/.env.example src/Services/User/User.API/.env
```

Các biến môi trường chính cần cấu hình:

```env
# Database
ConnectionStrings__DefaultConnection=''   # PostgreSQL connection string

# JWT (dùng chung từ Identity Service)
Jwt__Secret=''
Jwt__Issuer=''
Jwt__Audience=''

# RabbitMQ
RabbitMQ__Host=''
RabbitMQ__Username=''
RabbitMQ__Password=''

# Cloudinary (User & Post service)
Cloudinary__CloudName=''
Cloudinary__ApiKey=''
Cloudinary__ApiSecret=''
```

### 3. Chạy toàn bộ hệ thống bằng Docker Compose

```bash
# Development
docker-compose up --build

# Production
docker-compose -f docker-compose.prod.yml up --build -d
```

### 4. Chạy từng service riêng lẻ (development)

```bash
# Ví dụ chạy Identity Service
cd src/Services/Identity/Identity.API
dotnet run
```

---

## CI/CD & Triển khai Azure

Dự án triển khai toàn bộ lên **Azure VM** qua **Docker Compose**:

```
git push → GitHub
    └─► GitHub Actions: deploy.yml
            └─► docker-compose build
            └─► SSH vào Azure VM
            └─► docker-compose -f docker-compose.prod.yml up -d
```

### Hạ tầng (Terraform)

Azure resources được quản lý bằng Terraform:

```bash
cd Infrastructure/terraform
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"
```

---

## API Documentation

Mỗi service tích hợp **Swagger UI** tự động. Sau khi chạy, truy cập:

| Service | Swagger URL |
| --- | --- |
| Identity API | `http://localhost:[port]/swagger` |
| User API | `http://localhost:[port]/swagger` |
| Friend API | `http://localhost:[port]/swagger` |
| Post API | `http://localhost:[port]/swagger` |
| Message API | `http://localhost:[port]/swagger` |
| Notification API | `http://localhost:[port]/swagger` |

---

## Hướng phát triển

- [ ] Tích hợp **API Gateway** (YARP / Ocelot) làm điểm vào duy nhất
- [ ] Thêm **SignalR** cho chat và notification real-time
- [ ] Tích hợp **Distributed Tracing** (OpenTelemetry)
- [ ] Thêm **Redis** cache cho newsfeed
- [ ] Viết **Unit Test** và **Integration Test** cho từng service

---

## Liên hệ

Mọi thắc mắc vui lòng liên hệ nhóm thực hiện qua Issues của repository.

---

**© 2025–2026 – UIT · Phát triển ứng dụng trên thiết bị di động · ĐHQG TP.HCM**
