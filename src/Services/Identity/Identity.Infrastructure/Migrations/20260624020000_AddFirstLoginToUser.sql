-- ─────────────────────────────────────────────────────────────────────────────
-- Thêm cột "FirstLogin" vào Users (Identity service) — điều khiển hiển thị
-- phần giới thiệu (onboarding) theo tài khoản, không phụ thuộc bộ nhớ thiết bị.
--
-- Identity service KHÔNG tự chạy migration lúc khởi động → chạy file SQL này
-- THỦ CÔNG trên database (Supabase SQL Editor hoặc psql).
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE "Identity"."Users"
    ADD COLUMN IF NOT EXISTS "FirstLogin" boolean NOT NULL DEFAULT true;

-- Tài khoản đã tồn tại (đã hoạt động) → đặt false để không hiện lại giới thiệu.
-- Người dùng đăng ký MỚI sau lệnh này sẽ mặc định true (hiện giới thiệu 1 lần).
UPDATE "Identity"."Users" SET "FirstLogin" = false;
