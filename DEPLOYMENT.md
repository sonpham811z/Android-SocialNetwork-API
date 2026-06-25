# Hướng dẫn cấu hình & Deploy — Social Network API

Tài liệu này hướng dẫn cấu hình **toàn bộ biến môi trường (ENV)** và **file FCM private key**
để deploy hệ thống lên server (VM). Đọc kỹ trước khi chạy.

> Cập nhật mới nhất: bổ sung **Notification service** (push notification FCM + SignalR real-time)
> vào `docker-compose`, CI/CD và cấu hình bí mật.

---

## 1. Tổng quan kiến trúc deploy

7 service chạy bằng Docker Compose trên VM:

| Service | Container | Port | DB | Bí mật cần |
| --- | --- | --- | --- | --- |
| Identity | identity-service | 5210 | PostgreSQL | JWT, Google, Brevo |
| User | user-service | 5220 | PostgreSQL | JWT, Cloudinary |
| Friend | friend-service | 5176 (gRPC), 5178 (REST) | PostgreSQL | JWT |
| Post | post-service | 5175 | PostgreSQL | JWT, Cloudinary |
| Message | message-service | 5177 | MongoDB | JWT, Agora, Cloudinary |
| **Notification** | **notification-service** | **5095** | PostgreSQL | **JWT, FCM (file)** |
| RabbitMQ | rabbitmq | 5672 / 15672 | — | — |
| Redis | redis | 6379 | — | — |

Frontend (Flutter) gọi qua reverse proxy domain `https://sonpham-socialnet-api.duckdns.org`
với các path: `/identity`, `/user`, `/friend`, `/post`, `/message`, `/notification`.
→ Reverse proxy (Nginx/Caddy) cần thêm route `/notification` → `notification-service:5095`
(bao gồm cả WebSocket cho SignalR hub `/notification/hubs/notification`).

---

## 2. Bảng đầy đủ biến môi trường (.env)

File `.env` được dùng bởi `docker-compose.prod.yml`. Tạo từ template:

```bash
cp .env.example .env
# rồi điền giá trị thật
```

| Biến | Service dùng | Mô tả / Lấy ở đâu |
| --- | --- | --- |
| `IMAGE_OWNER` | tất cả | Tên owner GitHub **viết thường** (vd: `sonpham811z`) |
| `IMAGE_TAG` | tất cả | Tag image, mặc định `latest` (CI dùng git SHA) |
| `DB_CONNECTION` | Identity, User, Friend, Post, **Notification** | Connection string PostgreSQL (Supabase/Neon). Dạng: `Host=...;Database=...;Username=...;Password=...;SSL Mode=VerifyFull;Channel Binding=Require;` |
| `MONGODB_CONNECTION` | Message | Connection string MongoDB Atlas: `mongodb+srv://user:pass@cluster.mongodb.net/?appName=Message` |
| `JWT_SECRET` | tất cả | Khóa ký JWT (≥ 32 ký tự). **Phải giống nhau** ở mọi service (Identity ký, các service khác verify) |
| `GOOGLE_CLIENT_ID` | Identity | Google OAuth Client ID (`...apps.googleusercontent.com`) |
| `GOOGLE_CLIENT_SECRET` | Identity | Google OAuth Client Secret |
| `BREVO_API_KEY` | Identity | API key Brevo (gửi email xác thực / reset password), dạng `xkeysib-...` |
| `APP_BASE_URL` | Identity | URL public để chèn vào link xác thực email (vd `http://VM_IP:5210` hoặc domain) |
| `CLOUDINARY_CLOUD_NAME` | User, Post, Message | Cloudinary cloud name |
| `CLOUDINARY_API_KEY` | User, Post, Message | Cloudinary API key |
| `CLOUDINARY_API_SECRET` | User, Post, Message | Cloudinary API secret |
| `AGORA_APP_ID` | Message | Agora App ID (phải **giống** frontend `--dart-define=AGORA_APP_ID`) |
| `AGORA_APP_CERTIFICATE` | Message | Agora App Certificate (sinh token gọi điện) |

> **Lưu ý:** FCM (push notification) **KHÔNG** dùng biến `.env` — nó dùng **file JSON** (xem mục 3).
> Notification service tái sử dụng `DB_CONNECTION` + `JWT_SECRET`, không cần thêm biến mới.

Mapping ENV → cấu hình .NET (dùng `__` thay cho `:`):
`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey` (Identity/User/Friend/Post),
`Jwt__Key` (Message + Notification), `Cloudinary__*`, `Agora__*`, `Google__*`, `Email__ApiKey`.

---

## 3. File FCM private key (Notification service)

Notification service đọc **trực tiếp** file service-account JSON của Firebase
(qua cấu hình `Firebase__CredentialsPath`), không phải biến môi trường.

### 3.1. Lấy file từ Firebase Console

1. Vào <https://console.firebase.google.com> → chọn project của bạn.
2. ⚙️ **Project settings** → tab **Service accounts**.
3. Bấm **Generate new private key** → **Generate key**.
4. Trình duyệt tải về 1 file JSON dạng `your-project-firebase-adminsdk-xxxxx.json`.
   File này chứa `project_id`, `private_key`, `client_email`... → **bí mật tuyệt đối, không commit Git.**

> Đây phải là project Firebase **trùng** với app Android (file `google-services.json` của frontend
> phải cùng `project_id`), nếu không push notification sẽ không tới đúng app.

### 3.2. Đặt file đúng chỗ

Container mount file theo đường dẫn cố định:
`/app/secrets/fcm-service-account.json` (đã khai báo trong `docker-compose.yml`).

| Môi trường | Đường dẫn file trên host |
| --- | --- |
| Chạy local (docker compose) | `./secrets/fcm-service-account.json` (cùng cấp `docker-compose.yml`) |
| Trên VM | `/opt/socialnet/secrets/fcm-service-account.json` |

Tạo thủ công trên VM:

```bash
mkdir -p /opt/socialnet/secrets
# copy file JSON lên rồi đổi tên:
mv your-project-firebase-adminsdk-xxxxx.json /opt/socialnet/secrets/fcm-service-account.json
chmod 600 /opt/socialnet/secrets/fcm-service-account.json
```

> File `secrets/` và `*fcm-service-account.json` đã được thêm vào `.gitignore`.

---

## 4. Cấu hình cho CI/CD (GitHub Actions)

Workflow `.github/workflows/deploy.yml` tự build image, push lên GHCR, rồi SSH vào VM,
**tự ghi `.env`** và **tự ghi file FCM** từ GitHub Secrets.

### 4.1. Danh sách GitHub Secrets cần tạo

Vào **Repo → Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `VM_HOST` | ✅ | IP / hostname VM |
| `VM_USER` | ✅ | user SSH (vd `azureuser`) |
| `SSH_PRIVATE_KEY` | ✅ | private key SSH để deploy |
| `GHCR_TOKEN` | ✅ | GitHub PAT (scope `read:packages`) để VM pull image private |
| `DB_CONNECTION` | ✅ | PostgreSQL connection string |
| `MONGODB_CONNECTION` | ✅ | MongoDB connection string |
| `JWT_SECRET` | ✅ | khóa JWT chung |
| `GOOGLE_CLIENT_ID` | ✅ | Google OAuth |
| `GOOGLE_CLIENT_SECRET` | ✅ | Google OAuth |
| `BREVO_API_KEY` | ✅ | email |
| `CLOUDINARY_CLOUD_NAME` | ✅ | upload ảnh |
| `CLOUDINARY_API_KEY` | ✅ | upload ảnh |
| `CLOUDINARY_API_SECRET` | ✅ | upload ảnh |
| `AGORA_APP_ID` | ✅ | gọi điện *(mới — trước đây CI chưa ghi)* |
| `AGORA_APP_CERTIFICATE` | ✅ | gọi điện *(mới)* |
| `FCM_SERVICE_ACCOUNT` | ✅ | **MỚI** — dán **toàn bộ nội dung** file JSON ở mục 3.1 vào đây |

> `APP_BASE_URL` được suy ra tự động từ `VM_HOST` (`http://VM_HOST:5210`) trong workflow.

### 4.2. Cách dán `FCM_SERVICE_ACCOUNT`

Mở file JSON bằng editor, **copy nguyên văn toàn bộ** (cả dấu `{ }`), dán vào ô value của secret.
Workflow sẽ ghi nó ra `/opt/socialnet/secrets/fcm-service-account.json` khi deploy.

> ⚠️ Giữ nguyên xuống dòng. Trường `private_key` chứa `\n` literal — **không** sửa.

### 4.3. Kích hoạt deploy

Workflow chạy khi push vào nhánh `production` (đường dẫn `src/Services/**`) hoặc bấm
**Run workflow** thủ công (workflow_dispatch).

---

## 5. Deploy thủ công (không qua CI)

Trên VM, tại `/opt/socialnet`:

```bash
# 1. Đảm bảo có file .env (mục 2) và secrets/fcm-service-account.json (mục 3)
ls -la .env secrets/fcm-service-account.json

# 2. Login GHCR để pull image private
echo "$GHCR_TOKEN" | docker login ghcr.io -u <github_user> --password-stdin

# 3. Pull + chạy (dùng image đã build sẵn)
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --no-build --remove-orphans
```

Hoặc build trực tiếp trên VM (không cần GHCR):

```bash
docker compose -f docker-compose.yml --env-file .env up -d --build
```

Kiểm tra:

```bash
docker compose ps
docker compose logs -f notification        # xem log Notification service
docker logs notification-service | grep -i firebase
```

Nếu thấy lỗi `The Application Default Credentials are not available` hoặc service crash khi khởi động
→ file FCM chưa được mount đúng (kiểm tra lại mục 3.2).

---

## 6. Checklist trước khi deploy

- [ ] Đã tạo đủ GitHub Secrets ở mục 4.1 (đặc biệt `FCM_SERVICE_ACCOUNT`, `AGORA_*`).
- [ ] File `fcm-service-account.json` cùng `project_id` với `google-services.json` của app.
- [ ] Reverse proxy đã thêm route `/notification` → `notification-service:5095` (kèm WebSocket).
- [ ] PostgreSQL đã cho phép kết nối từ Notification service (cùng `DB_CONNECTION`).
- [ ] `JWT_SECRET` giống nhau ở mọi service.
- [ ] `.env` và `secrets/` **không** bị commit lên Git.
```
