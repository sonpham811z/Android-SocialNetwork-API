-- ─────────────────────────────────────────────────────────────────────────────
-- Tính năng "Bình luận Campus Board" (BoardComments) — Post service
--
-- Post service KHÔNG tự chạy migration lúc khởi động, nên chạy file SQL này
-- THỦ CÔNG trên database (Supabase SQL Editor hoặc psql) để tạo bảng.
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "Post"."BoardComments" (
    "Id"          uuid                     NOT NULL,
    "BoardPostId" uuid                     NOT NULL,
    "AuthorId"    uuid                     NOT NULL,
    "Content"     character varying(1000)  NOT NULL,
    "IsAnonymous" boolean                  NOT NULL,
    "CreatedAt"   timestamp with time zone NOT NULL,
    "IsDeleted"   boolean                  NOT NULL DEFAULT false,
    "DeletedAt"   timestamp with time zone NULL,
    CONSTRAINT "PK_BoardComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BoardComments_BoardPosts_BoardPostId" FOREIGN KEY ("BoardPostId")
        REFERENCES "Post"."BoardPosts" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_BoardComments_BoardPostId"
    ON "Post"."BoardComments" ("BoardPostId");

CREATE INDEX IF NOT EXISTS "IX_BoardComments_AuthorId"
    ON "Post"."BoardComments" ("AuthorId");
