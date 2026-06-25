-- ─────────────────────────────────────────────────────────────────────────────
-- Tính năng "Báo cáo bài viết" (PostReports) + cờ ẩn bài của admin (Posts.IsHidden)
-- Post service KHÔNG tự chạy migration → chạy SQL này THỦ CÔNG trên Supabase.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE "Post"."Posts"
    ADD COLUMN IF NOT EXISTS "IsHidden" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS "Post"."PostReports" (
    "Id"         uuid                     NOT NULL,
    "PostId"     uuid                     NOT NULL,
    "ReporterId" uuid                     NOT NULL,
    "Reason"     character varying(500)   NOT NULL,
    "Status"     integer                  NOT NULL,
    "CreatedAt"  timestamp with time zone NOT NULL,
    "ReviewedAt" timestamp with time zone NULL,
    "ReviewedBy" uuid                     NULL,
    CONSTRAINT "PK_PostReports" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_PostReports_PostId"
    ON "Post"."PostReports" ("PostId");
CREATE INDEX IF NOT EXISTS "IX_PostReports_Status"
    ON "Post"."PostReports" ("Status");
CREATE INDEX IF NOT EXISTS "IX_PostReports_PostId_ReporterId_Status"
    ON "Post"."PostReports" ("PostId", "ReporterId", "Status");
