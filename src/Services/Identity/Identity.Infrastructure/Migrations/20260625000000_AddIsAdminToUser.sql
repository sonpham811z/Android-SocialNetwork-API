-- ─────────────────────────────────────────────────────────────────────────────
-- Thêm cột "IsAdmin" vào Users (Identity service) — quyền quản trị (duyệt báo
-- cáo, ẩn/khôi phục bài). Identity KHÔNG tự chạy migration → chạy SQL thủ công.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE "Identity"."Users"
    ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT false;

-- Cấp quyền admin cho tài khoản của bạn (thay email bên dưới), rồi ĐĂNG NHẬP LẠI
-- để JWT mới chứa claim isAdmin=true:
-- UPDATE "Identity"."Users" SET "IsAdmin" = true WHERE "Email" = 'thaisonpham243@gmail.com';
