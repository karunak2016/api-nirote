-- ============================================================
-- Niroté CMS Migration v3 — Collection Products Endpoints
-- Adds: sp_Products_GetByCollection
--       sp_ProductCollections_GetByProduct
--       sp_ProductCollections_SetForProduct
-- Run AFTER cms_migration_v2.sql
-- ============================================================
USE NiroteDb;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Products_GetByCollection
    @CollectionSlug NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.Id,
        p.Name,
        p.Price,
        p.DiscountedPrice,
        p.StockQuantity,
        p.Fabric,
        p.Color,
        p.State,
        c.Name                                                              AS CategoryName,
        (SELECT TOP 1 ImageUrl FROM dbo.ProductImages
         WHERE ProductId = p.Id AND IsDefault = 1)                         AS DefaultImageUrl,
        (SELECT AVG(CAST(Rating AS DECIMAL(3,2))) FROM dbo.Reviews
         WHERE ProductId = p.Id AND IsApproved = 1)                        AS AverageRating,
        (SELECT COUNT(*) FROM dbo.Reviews
         WHERE ProductId = p.Id AND IsApproved = 1)                        AS ReviewCount
    FROM dbo.Products p
    LEFT  JOIN dbo.Categories        c   ON c.Id  = p.CategoryId
    INNER JOIN dbo.ProductCollections pc  ON pc.ProductId    = p.Id
    INNER JOIN dbo.CmsCollections     col ON col.Id = pc.CollectionId
    WHERE p.IsActive = 1
      AND col.Slug   = @CollectionSlug
      AND col.IsActive = 1
    ORDER BY p.CreatedAt DESC;
END;
GO

-- Returns the IDs of all collections a product belongs to
CREATE OR ALTER PROCEDURE dbo.sp_ProductCollections_GetByProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CollectionId FROM dbo.ProductCollections WHERE ProductId = @ProductId;
END;
GO

-- Replaces all collection memberships for a product atomically
CREATE OR ALTER PROCEDURE dbo.sp_ProductCollections_SetForProduct
    @ProductId     INT,
    @CollectionIds NVARCHAR(MAX)   -- JSON array, e.g. "[1,3]" or "[]"
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.ProductCollections WHERE ProductId = @ProductId;
    IF @CollectionIds IS NOT NULL AND LEN(@CollectionIds) > 2
        INSERT INTO dbo.ProductCollections (ProductId, CollectionId)
        SELECT @ProductId, CAST(value AS INT)
        FROM OPENJSON(@CollectionIds);
END;
GO
