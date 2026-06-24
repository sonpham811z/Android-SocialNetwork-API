-- ─────────────────────────────────────────────────────────────────────────────
-- Tính năng "Lưu bài viết" (SavedPosts / bookmark) — Post service
--
-- Post service KHÔNG tự chạy migration lúc khởi động, nên chạy file SQL này
-- THỦ CÔNG trên database (Supabase SQL Editor hoặc psql) để tạo bảng.
--   Supabase: Dashboard → SQL Editor → New query → dán đoạn dưới → Run
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "Post"."SavedPosts" (
    "Id"        uuid                     NOT NULL,
    "PostId"    uuid                     NOT NULL,
    "UserId"    uuid                     NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean                  NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_SavedPosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SavedPosts_Posts_PostId" FOREIGN KEY ("PostId")
        REFERENCES "Post"."Posts" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SavedPosts_UserId"
    ON "Post"."SavedPosts" ("UserId");

-- Partial unique index: cho phép lưu lại sau khi bỏ lưu (soft-delete)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SavedPosts_PostId_UserId"
    ON "Post"."SavedPosts" ("PostId", "UserId")
    WHERE "IsDeleted" = false;
