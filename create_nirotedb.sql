-- ============================================================
-- NiroteDb — Complete Database Creation Script
-- Run against master (or any context) — creates NiroteDb fresh
-- ============================================================

SET NOCOUNT ON;
GO

-- ─────────────────────────────────────────────────────────────
-- 0. Create database
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NiroteDb')
BEGIN
    CREATE DATABASE NiroteDb;
    PRINT 'NiroteDb created.';
END
GO

USE NiroteDb;
GO

-- ─────────────────────────────────────────────────────────────
-- 1. TABLES
-- ─────────────────────────────────────────────────────────────

CREATE TABLE dbo.Users (
    Id           INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(150)  NOT NULL,
    Email        NVARCHAR(256)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(512)  NOT NULL,
    Phone        NVARCHAR(15)   NULL,
    Role         NVARCHAR(20)   NOT NULL DEFAULT ('Customer'),
    IsActive     BIT            NOT NULL DEFAULT (1),
    CreatedAt    DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt    DATETIME2      NULL
);

CREATE TABLE dbo.Categories (
    Id            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(100)  NOT NULL,
    Slug          NVARCHAR(100)  NOT NULL UNIQUE,
    Description   NVARCHAR(500)  NULL,
    ParentId      INT            NULL REFERENCES dbo.Categories(Id),
    ImageUrl      NVARCHAR(500)  NULL,
    DisplayOrder  INT            NOT NULL DEFAULT (0),
    IsActive      BIT            NOT NULL DEFAULT (1),
    ShowOnHomepage BIT           NOT NULL DEFAULT (0),
    CreatedAt     DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt     DATETIME2      NULL
);

CREATE TABLE dbo.Products (
    Id               INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name             NVARCHAR(200)  NOT NULL,
    Description      NVARCHAR(MAX)  NULL,
    Price            DECIMAL(10,2)  NOT NULL,
    DiscountedPrice  DECIMAL(10,2)  NULL,
    StockQuantity    INT            NOT NULL DEFAULT (0),
    Fabric           NVARCHAR(100)  NULL,
    Color            NVARCHAR(100)  NULL,
    Occasion         NVARCHAR(100)  NULL,
    State            NVARCHAR(100)  NULL,
    HasBlousePiece   BIT            NOT NULL DEFAULT (0),
    CareInstructions NVARCHAR(500)  NULL,
    DeliveryDays     INT            NOT NULL DEFAULT (7),
    CategoryId       INT            NOT NULL REFERENCES dbo.Categories(Id),
    IsActive         BIT            NOT NULL DEFAULT (1),
    CreatedAt        DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt        DATETIME2      NULL
);

CREATE TABLE dbo.ProductImages (
    Id           INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT            NOT NULL REFERENCES dbo.Products(Id),
    ImageUrl     NVARCHAR(500)  NOT NULL,
    IsDefault    BIT            NOT NULL DEFAULT (0),
    DisplayOrder INT            NOT NULL DEFAULT (0),
    CreatedAt    DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME())
);

CREATE TABLE dbo.ProductOptions (
    Id           INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Type         NVARCHAR(50)   NOT NULL,
    Value        NVARCHAR(100)  NOT NULL,
    DisplayOrder INT            NULL DEFAULT (0)
);

CREATE TABLE dbo.Settings (
    Id    INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100)  NOT NULL UNIQUE,
    Value NVARCHAR(500)  NOT NULL
);

CREATE TABLE dbo.Addresses (
    Id        INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId    INT            NOT NULL REFERENCES dbo.Users(Id),
    FullName  NVARCHAR(150)  NOT NULL,
    Phone     NVARCHAR(15)   NOT NULL,
    Line1     NVARCHAR(250)  NOT NULL,
    Line2     NVARCHAR(250)  NULL,
    City      NVARCHAR(100)  NOT NULL,
    State     NVARCHAR(100)  NOT NULL,
    Pincode   NVARCHAR(10)   NOT NULL,
    IsDefault BIT            NOT NULL DEFAULT (0),
    CreatedAt DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2      NULL
);

CREATE TABLE dbo.Carts (
    Id        INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId    INT       NOT NULL UNIQUE REFERENCES dbo.Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE dbo.CartItems (
    Id        INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CartId    INT       NOT NULL REFERENCES dbo.Carts(Id),
    ProductId INT       NOT NULL REFERENCES dbo.Products(Id),
    Quantity  INT       NOT NULL DEFAULT (1),
    AddedAt   DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NULL,
    UNIQUE (CartId, ProductId)
);

CREATE TABLE dbo.Orders (
    Id                  INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId              INT            NOT NULL REFERENCES dbo.Users(Id),
    AddressId           INT            NOT NULL REFERENCES dbo.Addresses(Id),
    TotalAmount         DECIMAL(10,2)  NOT NULL,
    DiscountAmount      DECIMAL(10,2)  NOT NULL DEFAULT (0),
    FinalAmount         DECIMAL(10,2)  NOT NULL,
    PaymentMethod       NVARCHAR(20)   NOT NULL DEFAULT ('COD'),
    PaymentStatus       NVARCHAR(20)   NOT NULL DEFAULT ('Pending'),
    RazorpayOrderId     NVARCHAR(100)  NULL,
    RazorpayPaymentId   NVARCHAR(100)  NULL,
    RazorpaySignature   NVARCHAR(256)  NULL,
    OrderStatus         NVARCHAR(20)   NOT NULL DEFAULT ('Pending'),
    ShiprocketOrderId   NVARCHAR(100)  NULL,
    AWBCode             NVARCHAR(100)  NULL,
    Notes               NVARCHAR(500)  NULL,
    CancelledAt         DATETIME2      NULL,
    CancelReason        NVARCHAR(300)  NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2      NULL
);

CREATE TABLE dbo.OrderItems (
    Id           INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    OrderId      INT            NOT NULL REFERENCES dbo.Orders(Id),
    ProductId    INT            NOT NULL REFERENCES dbo.Products(Id),
    ProductName  NVARCHAR(200)  NOT NULL,
    ProductImage NVARCHAR(500)  NULL,
    UnitPrice    DECIMAL(10,2)  NOT NULL,
    Quantity     INT            NOT NULL,
    Subtotal     AS (UnitPrice * Quantity)
);

CREATE TABLE dbo.Wishlists (
    Id        INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId    INT       NOT NULL REFERENCES dbo.Users(Id),
    ProductId INT       NOT NULL REFERENCES dbo.Products(Id),
    AddedAt   DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    UNIQUE (UserId, ProductId)
);

CREATE TABLE dbo.Coupons (
    Id            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Code          NVARCHAR(50)   NULL,
    Description   NVARCHAR(200)  NOT NULL DEFAULT (''),
    DiscountType  NVARCHAR(20)   NOT NULL,
    DiscountValue DECIMAL(10,2)  NOT NULL,
    MinCartAmount DECIMAL(10,2)  NULL,
    MaxDiscount   DECIMAL(10,2)  NULL,
    StartDate     DATETIME2      NULL,
    EndDate       DATETIME2      NULL,
    IsActive      BIT            NOT NULL DEFAULT (1),
    UsageLimit    INT            NULL,
    UsedCount     INT            NOT NULL DEFAULT (0),
    FestivalName  NVARCHAR(100)  NULL,
    BankName      NVARCHAR(100)  NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT (GETUTCDATE())
);

CREATE TABLE dbo.Campaigns (
    Id            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(200)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    CategoryId    INT            NULL REFERENCES dbo.Categories(Id),
    DiscountType  NVARCHAR(20)   NOT NULL DEFAULT ('Percentage'),
    DiscountValue DECIMAL(18,2)  NOT NULL,
    StartDate     DATETIME2      NULL,
    EndDate       DATETIME2      NULL,
    IsActive      BIT            NOT NULL DEFAULT (1),
    CreatedAt     DATETIME2      NOT NULL DEFAULT (GETUTCDATE())
);

CREATE TABLE dbo.Reviews (
    Id         INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProductId  INT            NOT NULL REFERENCES dbo.Products(Id),
    UserId     INT            NOT NULL REFERENCES dbo.Users(Id),
    UserName   NVARCHAR(150)  NOT NULL,
    Rating     INT            NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Title      NVARCHAR(200)  NULL,
    Body       NVARCHAR(MAX)  NOT NULL,
    IsApproved BIT            NOT NULL DEFAULT (0),
    CreatedAt  DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME())
);

CREATE TABLE dbo.ReturnRequests (
    Id               INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    OrderId          INT            NOT NULL REFERENCES dbo.Orders(Id),
    UserId           INT            NOT NULL REFERENCES dbo.Users(Id),
    UserName         NVARCHAR(150)  NOT NULL,
    Reason           NVARCHAR(200)  NOT NULL,
    Description      NVARCHAR(500)  NULL,
    Status           NVARCHAR(20)   NOT NULL DEFAULT ('Pending'),
    AdminNote        NVARCHAR(500)  NULL,
    RefundStatus     NVARCHAR(20)   NOT NULL DEFAULT ('None'),
    RazorpayRefundId NVARCHAR(100)  NULL,
    CreatedAt        DATETIME2      NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt        DATETIME2      NULL
);

PRINT 'Tables created.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2. VIEWS
-- ─────────────────────────────────────────────────────────────

CREATE VIEW dbo.vw_ActiveProducts AS
SELECT
    p.Id, p.Name, p.Description, p.Price, p.DiscountedPrice,
    CASE
        WHEN p.DiscountedPrice IS NOT NULL AND p.DiscountedPrice < p.Price
        THEN CAST(ROUND((p.Price - p.DiscountedPrice) / p.Price * 100, 0) AS INT)
        ELSE 0
    END                 AS DiscountPercent,
    p.StockQuantity, p.Fabric, p.Color, p.HasBlousePiece,
    p.CareInstructions, p.DeliveryDays, p.CategoryId,
    c.Name              AS CategoryName,
    c.Slug              AS CategorySlug,
    pi_d.ImageUrl       AS DefaultImage,
    p.IsActive, p.CreatedAt, p.UpdatedAt
FROM       dbo.Products      p
INNER JOIN dbo.Categories    c    ON c.Id = p.CategoryId
LEFT  JOIN dbo.ProductImages pi_d ON pi_d.ProductId = p.Id AND pi_d.IsDefault = 1
WHERE p.IsActive = 1 AND c.IsActive = 1;
GO

CREATE VIEW dbo.vw_OrderSummary AS
SELECT
    o.Id AS OrderId, o.CreatedAt, o.UpdatedAt,
    o.TotalAmount, o.DiscountAmount, o.FinalAmount,
    o.PaymentMethod, o.PaymentStatus,
    o.RazorpayOrderId, o.RazorpayPaymentId,
    o.OrderStatus, o.ShiprocketOrderId, o.AWBCode,
    o.Notes, o.CancelledAt, o.CancelReason,
    u.Id    AS CustomerId,
    u.Name  AS CustomerName,
    u.Email AS CustomerEmail,
    u.Phone AS CustomerPhone,
    a.FullName AS DeliveryName,
    a.Phone    AS DeliveryPhone,
    a.Line1    AS AddressLine1,
    a.Line2    AS AddressLine2,
    a.City, a.State, a.Pincode,
    (SELECT COUNT(*) FROM dbo.OrderItems oi WHERE oi.OrderId = o.Id) AS ItemCount
FROM       dbo.Orders    o
INNER JOIN dbo.Users     u ON u.Id = o.UserId
INNER JOIN dbo.Addresses a ON a.Id = o.AddressId;
GO

PRINT 'Views created.';
GO

-- ─────────────────────────────────────────────────────────────
-- 3. STORED PROCEDURES — Categories
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Categories_Create
    @Name           NVARCHAR(100),
    @Slug           NVARCHAR(100),
    @Description    NVARCHAR(500)   = NULL,
    @ImageUrl       NVARCHAR(500)   = NULL,
    @DisplayOrder   INT             = 0,
    @ParentId       INT             = NULL,
    @ShowOnHomepage BIT             = 0,
    @NewCategoryId  INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = @Slug)
    BEGIN RAISERROR('A category with this slug already exists.', 16, 1); RETURN; END

    INSERT INTO dbo.Categories (Name, Slug, Description, ImageUrl, DisplayOrder, ParentId, ShowOnHomepage)
    VALUES (@Name, @Slug, @Description, @ImageUrl, @DisplayOrder, @ParentId, @ShowOnHomepage);
    SET @NewCategoryId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Categories_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Slug, Description, ParentId, ImageUrl, DisplayOrder, IsActive, ShowOnHomepage, CreatedAt, UpdatedAt
    FROM dbo.Categories
    WHERE (@IncludeInactive = 1 OR IsActive = 1)
    ORDER BY DisplayOrder ASC, Name ASC;
END;
GO

CREATE PROCEDURE dbo.sp_Categories_GetById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Slug, Description, ParentId, ImageUrl, DisplayOrder, IsActive, ShowOnHomepage, CreatedAt, UpdatedAt
    FROM dbo.Categories WHERE Id = @CategoryId;
END;
GO

CREATE PROCEDURE dbo.sp_Categories_Update
    @CategoryId     INT,
    @Name           NVARCHAR(100),
    @Slug           NVARCHAR(100),
    @Description    NVARCHAR(500)   = NULL,
    @ImageUrl       NVARCHAR(500)   = NULL,
    @DisplayOrder   INT             = 0,
    @IsActive       BIT             = 1,
    @ParentId       INT             = NULL,
    @ShowOnHomepage BIT             = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = @Slug AND Id <> @CategoryId)
    BEGIN RAISERROR('Another category already uses this slug.', 16, 1); RETURN; END

    UPDATE dbo.Categories SET
        Name = @Name, Slug = @Slug, Description = @Description, ImageUrl = @ImageUrl,
        DisplayOrder = @DisplayOrder, IsActive = @IsActive, ParentId = @ParentId,
        ShowOnHomepage = @ShowOnHomepage, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @CategoryId;

    IF @@ROWCOUNT = 0 RAISERROR('Category not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Categories_Delete
    @CategoryId   INT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrorMessage = NULL;
    IF EXISTS (SELECT 1 FROM dbo.Products WHERE CategoryId = @CategoryId AND IsActive = 1)
    BEGIN SET @ErrorMessage = 'Cannot delete: active products exist in this category.'; RETURN; END
    DELETE FROM dbo.Categories WHERE Id = @CategoryId;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 4. STORED PROCEDURES — Products
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Products_Create
    @Name             NVARCHAR(200),
    @Description      NVARCHAR(MAX)  = NULL,
    @Price            DECIMAL(18,2),
    @DiscountedPrice  DECIMAL(18,2)  = NULL,
    @StockQuantity    INT,
    @Fabric           NVARCHAR(100)  = NULL,
    @Color            NVARCHAR(100)  = NULL,
    @State            NVARCHAR(100)  = NULL,
    @HasBlousePiece   BIT            = 0,
    @CareInstructions NVARCHAR(500)  = NULL,
    @DeliveryDays     INT            = 7,
    @CategoryId       INT,
    @NewProductId     INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.Products
        (Name, Description, Price, DiscountedPrice, StockQuantity,
         Fabric, Color, State, HasBlousePiece, CareInstructions,
         DeliveryDays, CategoryId, IsActive, CreatedAt)
    VALUES
        (@Name, @Description, @Price, @DiscountedPrice, @StockQuantity,
         @Fabric, @Color, @State, @HasBlousePiece, @CareInstructions,
         @DeliveryDays, @CategoryId, 1, GETUTCDATE());
    SET @NewProductId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Products_GetAll
    @CategoryId  INT           = NULL,
    @MinPrice    DECIMAL(18,2) = NULL,
    @MaxPrice    DECIMAL(18,2) = NULL,
    @Color       NVARCHAR(100) = NULL,
    @Fabric      NVARCHAR(100) = NULL,
    @State       NVARCHAR(100) = NULL,
    @SearchTerm  NVARCHAR(200) = NULL,
    @MinRating   DECIMAL(3,2)  = NULL,
    @SortBy      NVARCHAR(50)  = 'newest',
    @Page        INT           = 1,
    @PageSize    INT           = 12,
    @TotalCount  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.Products p
    WHERE p.IsActive = 1
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId OR EXISTS (
              SELECT 1 FROM dbo.Categories sub WHERE sub.Id = p.CategoryId AND sub.ParentId = @CategoryId))
      AND (@MinPrice   IS NULL OR p.Price >= @MinPrice)
      AND (@MaxPrice   IS NULL OR p.Price <= @MaxPrice)
      AND (@Color      IS NULL OR p.Color = @Color)
      AND (@Fabric     IS NULL OR p.Fabric = @Fabric)
      AND (@State      IS NULL OR p.State = @State)
      AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%' OR p.Description LIKE '%' + @SearchTerm + '%');

    SELECT
        p.Id, p.Name, p.Description, p.Price, p.DiscountedPrice,
        p.StockQuantity, p.Fabric, p.Color, p.Occasion, p.State,
        p.HasBlousePiece, p.CareInstructions, p.DeliveryDays,
        p.CategoryId, p.IsActive, p.CreatedAt, p.UpdatedAt,
        c.Name AS CategoryName,
        (SELECT TOP 1 ImageUrl FROM dbo.ProductImages WHERE ProductId = p.Id AND IsDefault = 1) AS DefaultImage,
        (SELECT AVG(CAST(Rating AS DECIMAL(3,2))) FROM dbo.Reviews WHERE ProductId = p.Id AND IsApproved = 1) AS AverageRating,
        (SELECT COUNT(*) FROM dbo.Reviews WHERE ProductId = p.Id AND IsApproved = 1) AS ReviewCount
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON p.CategoryId = c.Id
    WHERE p.IsActive = 1
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId OR EXISTS (
              SELECT 1 FROM dbo.Categories sub WHERE sub.Id = p.CategoryId AND sub.ParentId = @CategoryId))
      AND (@MinPrice   IS NULL OR p.Price >= @MinPrice)
      AND (@MaxPrice   IS NULL OR p.Price <= @MaxPrice)
      AND (@Color      IS NULL OR p.Color = @Color)
      AND (@Fabric     IS NULL OR p.Fabric = @Fabric)
      AND (@State      IS NULL OR p.State = @State)
      AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%' OR p.Description LIKE '%' + @SearchTerm + '%')
    ORDER BY
        CASE WHEN @SortBy = 'newest'     THEN p.CreatedAt END DESC,
        CASE WHEN @SortBy = 'price_asc'  THEN p.Price END ASC,
        CASE WHEN @SortBy = 'price_desc' THEN p.Price END DESC,
        CASE WHEN @SortBy = 'name'       THEN p.Name END ASC,
        p.CreatedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE PROCEDURE dbo.sp_Products_GetById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.Id, p.Name, p.Description, p.Price, p.DiscountedPrice,
        p.StockQuantity, p.Fabric, p.Color, p.Occasion, p.State,
        p.HasBlousePiece, p.CareInstructions, p.DeliveryDays,
        p.CategoryId, p.IsActive, p.CreatedAt, p.UpdatedAt,
        c.Name AS CategoryName,
        NULL AS DefaultImage,
        (SELECT AVG(CAST(Rating AS DECIMAL(3,2))) FROM dbo.Reviews WHERE ProductId = p.Id AND IsApproved = 1) AS AverageRating,
        (SELECT COUNT(*) FROM dbo.Reviews WHERE ProductId = p.Id AND IsApproved = 1) AS ReviewCount
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON p.CategoryId = c.Id
    WHERE p.Id = @ProductId;

    SELECT Id, ProductId, ImageUrl, IsDefault, DisplayOrder, CreatedAt
    FROM dbo.ProductImages WHERE ProductId = @ProductId ORDER BY DisplayOrder;
END;
GO

CREATE PROCEDURE dbo.sp_Products_Update
    @Id               INT,
    @ProductId        INT,
    @Name             NVARCHAR(200),
    @Description      NVARCHAR(MAX)  = NULL,
    @Price            DECIMAL(18,2),
    @DiscountedPrice  DECIMAL(18,2)  = NULL,
    @StockQuantity    INT,
    @Fabric           NVARCHAR(100)  = NULL,
    @Color            NVARCHAR(100)  = NULL,
    @State            NVARCHAR(100)  = NULL,
    @HasBlousePiece   BIT            = 0,
    @CareInstructions NVARCHAR(500)  = NULL,
    @DeliveryDays     INT            = 7,
    @CategoryId       INT
AS
BEGIN
    UPDATE dbo.Products SET
        Name = @Name, Description = @Description, Price = @Price,
        DiscountedPrice = @DiscountedPrice, StockQuantity = @StockQuantity,
        Fabric = @Fabric, Color = @Color, State = @State,
        HasBlousePiece = @HasBlousePiece, CareInstructions = @CareInstructions,
        DeliveryDays = @DeliveryDays, CategoryId = @CategoryId,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ProductId;
END;
GO

CREATE PROCEDURE dbo.sp_Products_Deactivate
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Products SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @ProductId;
    IF @@ROWCOUNT = 0 RAISERROR('Product not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Products_UpdateStock
    @ProductId    INT,
    @Quantity     INT,
    @Operation    NVARCHAR(10),
    @Success      BIT OUTPUT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 1; SET @ErrorMessage = NULL;
    IF @Operation = 'Subtract'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @ProductId AND StockQuantity >= @Quantity)
        BEGIN SET @Success = 0; SET @ErrorMessage = 'Insufficient stock.'; RETURN; END
        UPDATE dbo.Products SET StockQuantity = StockQuantity - @Quantity, UpdatedAt = SYSUTCDATETIME() WHERE Id = @ProductId;
    END
    ELSE IF @Operation = 'Add'
        UPDATE dbo.Products SET StockQuantity = StockQuantity + @Quantity, UpdatedAt = SYSUTCDATETIME() WHERE Id = @ProductId;
    ELSE BEGIN SET @Success = 0; SET @ErrorMessage = 'Invalid operation. Use Add or Subtract.'; END
END;
GO

-- sp_GetProducts alias used in some older code paths
CREATE PROCEDURE dbo.sp_GetProducts
    @CategoryId  INT           = NULL,
    @MinPrice    DECIMAL(18,2) = NULL,
    @MaxPrice    DECIMAL(18,2) = NULL,
    @Color       NVARCHAR(100) = NULL,
    @Fabric      NVARCHAR(100) = NULL,
    @State       NVARCHAR(100) = NULL,
    @SearchTerm  NVARCHAR(200) = NULL,
    @SortBy      NVARCHAR(50)  = 'newest',
    @Page        INT           = 1,
    @PageSize    INT           = 12,
    @TotalCount  INT OUTPUT
AS
BEGIN
    EXEC dbo.sp_Products_GetAll
        @CategoryId = @CategoryId, @MinPrice = @MinPrice, @MaxPrice = @MaxPrice,
        @Color = @Color, @Fabric = @Fabric, @State = @State, @SearchTerm = @SearchTerm,
        @SortBy = @SortBy, @Page = @Page, @PageSize = @PageSize, @TotalCount = @TotalCount OUTPUT;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 5. STORED PROCEDURES — Product Images & Options
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_ProductImages_Add
    @ProductId    INT,
    @ImageUrl     NVARCHAR(500),
    @IsDefault    BIT = 0,
    @DisplayOrder INT = 0,
    @NewImageId   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @IsDefault = 1
        UPDATE dbo.ProductImages SET IsDefault = 0 WHERE ProductId = @ProductId;
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductImages WHERE ProductId = @ProductId)
        SET @IsDefault = 1;
    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, IsDefault, DisplayOrder)
    VALUES (@ProductId, @ImageUrl, @IsDefault, @DisplayOrder);
    SET @NewImageId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_ProductImages_Delete
    @ImageId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ProductId INT, @WasDefault BIT;
    SELECT @ProductId = ProductId, @WasDefault = IsDefault FROM dbo.ProductImages WHERE Id = @ImageId;
    DELETE FROM dbo.ProductImages WHERE Id = @ImageId;
    IF @WasDefault = 1
    BEGIN
        WITH CTE AS (SELECT TOP 1 * FROM dbo.ProductImages WHERE ProductId = @ProductId ORDER BY DisplayOrder ASC)
        UPDATE CTE SET IsDefault = 1;
    END
END;
GO

CREATE PROCEDURE dbo.sp_ProductImages_GetByProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ProductId, ImageUrl, IsDefault, DisplayOrder, CreatedAt
    FROM dbo.ProductImages WHERE ProductId = @ProductId ORDER BY IsDefault DESC, DisplayOrder ASC;
END;
GO

CREATE PROCEDURE dbo.sp_ProductImages_SetDefault
    @ImageId   INT,
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ProductImages SET IsDefault = 0 WHERE ProductId = @ProductId;
    UPDATE dbo.ProductImages SET IsDefault = 1 WHERE Id = @ImageId AND ProductId = @ProductId;
    IF @@ROWCOUNT = 0 RAISERROR('Image not found for this product.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_ProductOptions_Create
    @Type  NVARCHAR(50),
    @Value NVARCHAR(100),
    @NewId INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.ProductOptions (Type, Value) VALUES (@Type, @Value);
    SET @NewId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_ProductOptions_GetByType
    @Type NVARCHAR(50)
AS
BEGIN
    SELECT Id, Type, Value, DisplayOrder FROM dbo.ProductOptions WHERE Type = @Type ORDER BY DisplayOrder, Value;
END;
GO

CREATE PROCEDURE dbo.sp_ProductOptions_Update
    @Id    INT,
    @Value NVARCHAR(100)
AS
BEGIN
    UPDATE dbo.ProductOptions SET Value = @Value WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.sp_ProductOptions_Delete
    @Id INT
AS
BEGIN
    DELETE FROM dbo.ProductOptions WHERE Id = @Id;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 6. STORED PROCEDURES — Settings
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Settings_GetByKey
    @Key NVARCHAR(100)
AS
BEGIN
    SELECT Id, [Key], [Value] FROM dbo.Settings WHERE [Key] = @Key;
END;
GO

CREATE PROCEDURE dbo.sp_Settings_Set
    @Key   NVARCHAR(100),
    @Value NVARCHAR(500)
AS
BEGIN
    UPDATE dbo.Settings SET [Value] = @Value WHERE [Key] = @Key;
    IF @@ROWCOUNT = 0
        INSERT INTO dbo.Settings ([Key], [Value]) VALUES (@Key, @Value);
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 7. STORED PROCEDURES — Users
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Users_Create
    @Name         NVARCHAR(150),
    @Email        NVARCHAR(256),
    @PasswordHash NVARCHAR(512),
    @Phone        NVARCHAR(15) = NULL,
    @Role         NVARCHAR(20) = 'Customer',
    @NewUserId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
    BEGIN RAISERROR('Email address already registered.', 16, 1); RETURN; END
    INSERT INTO dbo.Users (Name, Email, PasswordHash, Phone, Role)
    VALUES (@Name, @Email, @PasswordHash, @Phone, @Role);
    SET @NewUserId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Users_GetByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Email, PasswordHash, Phone, Role, IsActive, CreatedAt
    FROM dbo.Users WHERE Email = @Email;
END;
GO

CREATE PROCEDURE dbo.sp_Users_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Email, Phone, Role, IsActive, CreatedAt, UpdatedAt
    FROM dbo.Users WHERE Id = @UserId;
END;
GO

CREATE PROCEDURE dbo.sp_Users_Update
    @UserId INT,
    @Name   NVARCHAR(150),
    @Phone  NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET Name = @Name, Phone = @Phone, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId;
    IF @@ROWCOUNT = 0 RAISERROR('User not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Users_UpdatePassword
    @UserId       INT,
    @PasswordHash NVARCHAR(512)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET PasswordHash = @PasswordHash, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId;
    IF @@ROWCOUNT = 0 RAISERROR('User not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Users_GetAll
    @Role       NVARCHAR(20)  = NULL,
    @SearchTerm NVARCHAR(200) = NULL,
    @Page       INT           = 1,
    @PageSize   INT           = 20,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    SELECT @TotalCount = COUNT(*) FROM dbo.Users
    WHERE (@Role IS NULL OR Role = @Role)
      AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%' OR Email LIKE '%' + @SearchTerm + '%');
    SELECT Id, Name, Email, Phone, Role, IsActive, CreatedAt, UpdatedAt FROM dbo.Users
    WHERE (@Role IS NULL OR Role = @Role)
      AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%' OR Email LIKE '%' + @SearchTerm + '%')
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE PROCEDURE dbo.sp_Users_Deactivate
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId;
    IF @@ROWCOUNT = 0 RAISERROR('User not found.', 16, 1);
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 8. STORED PROCEDURES — Addresses
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Addresses_Create
    @UserId       INT,
    @FullName     NVARCHAR(150),
    @Phone        NVARCHAR(15),
    @Line1        NVARCHAR(250),
    @Line2        NVARCHAR(250) = NULL,
    @City         NVARCHAR(100),
    @State        NVARCHAR(100),
    @Pincode      NVARCHAR(10),
    @IsDefault    BIT = 0,
    @NewAddressId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Addresses WHERE UserId = @UserId) SET @IsDefault = 1;
    IF @IsDefault = 1
        UPDATE dbo.Addresses SET IsDefault = 0, UpdatedAt = SYSUTCDATETIME() WHERE UserId = @UserId;
    INSERT INTO dbo.Addresses (UserId, FullName, Phone, Line1, Line2, City, State, Pincode, IsDefault)
    VALUES (@UserId, @FullName, @Phone, @Line1, @Line2, @City, @State, @Pincode, @IsDefault);
    SET @NewAddressId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Addresses_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, FullName, Phone, Line1, Line2, City, State, Pincode, IsDefault, CreatedAt, UpdatedAt
    FROM dbo.Addresses WHERE UserId = @UserId ORDER BY IsDefault DESC, CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Addresses_GetById
    @AddressId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, FullName, Phone, Line1, Line2, City, State, Pincode, IsDefault, CreatedAt, UpdatedAt
    FROM dbo.Addresses WHERE Id = @AddressId;
END;
GO

CREATE PROCEDURE dbo.sp_Addresses_Update
    @AddressId INT,
    @UserId    INT,
    @FullName  NVARCHAR(150),
    @Phone     NVARCHAR(15),
    @Line1     NVARCHAR(250),
    @Line2     NVARCHAR(250) = NULL,
    @City      NVARCHAR(100),
    @State     NVARCHAR(100),
    @Pincode   NVARCHAR(10),
    @IsDefault BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF @IsDefault = 1
        UPDATE dbo.Addresses SET IsDefault = 0, UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId AND Id <> @AddressId;
    UPDATE dbo.Addresses SET
        FullName = @FullName, Phone = @Phone, Line1 = @Line1, Line2 = @Line2,
        City = @City, State = @State, Pincode = @Pincode, IsDefault = @IsDefault,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @AddressId AND UserId = @UserId;
    IF @@ROWCOUNT = 0 RAISERROR('Address not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Addresses_Delete
    @AddressId    INT,
    @UserId       INT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrorMessage = NULL;
    IF EXISTS (SELECT 1 FROM dbo.Orders WHERE AddressId = @AddressId)
    BEGIN SET @ErrorMessage = 'Cannot delete: this address is linked to existing orders.'; RETURN; END
    DELETE FROM dbo.Addresses WHERE Id = @AddressId AND UserId = @UserId;
    IF @@ROWCOUNT = 0 SET @ErrorMessage = 'Address not found.';
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 9. STORED PROCEDURES — Cart
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Carts_GetOrCreate
    @UserId INT,
    @CartId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @CartId = Id FROM dbo.Carts WHERE UserId = @UserId;
    IF @CartId IS NULL
    BEGIN
        INSERT INTO dbo.Carts (UserId) VALUES (@UserId);
        SET @CartId = SCOPE_IDENTITY();
    END
END;
GO

CREATE PROCEDURE dbo.sp_Carts_GetWithItems
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id AS CartId, c.UserId, c.CreatedAt, c.UpdatedAt,
           (SELECT SUM(ci2.Quantity) FROM dbo.CartItems ci2 WHERE ci2.CartId = c.Id) AS TotalItems
    FROM dbo.Carts c WHERE c.UserId = @UserId;

    SELECT ci.Id AS CartItemId, ci.CartId, ci.Quantity, ci.AddedAt,
           p.Id AS ProductId, p.Name AS ProductName, p.Price, p.DiscountedPrice,
           CASE WHEN p.DiscountedPrice IS NOT NULL AND p.DiscountedPrice < p.Price
                THEN CAST(ROUND((p.Price - p.DiscountedPrice) / p.Price * 100, 0) AS INT) ELSE 0 END AS DiscountPercent,
           p.StockQuantity, p.IsActive, p.DeliveryDays,
           pi_d.ImageUrl AS ProductImage,
           CASE WHEN p.DiscountedPrice IS NOT NULL THEN p.DiscountedPrice * ci.Quantity ELSE p.Price * ci.Quantity END AS LineTotal
    FROM dbo.CartItems ci
    INNER JOIN dbo.Carts    c    ON c.Id = ci.CartId AND c.UserId = @UserId
    INNER JOIN dbo.Products p    ON p.Id = ci.ProductId
    LEFT  JOIN dbo.ProductImages pi_d ON pi_d.ProductId = p.Id AND pi_d.IsDefault = 1
    ORDER BY ci.AddedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Carts_Clear
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE ci FROM dbo.CartItems ci INNER JOIN dbo.Carts c ON c.Id = ci.CartId WHERE c.UserId = @UserId;
END;
GO

CREATE PROCEDURE dbo.sp_CartItems_AddOrUpdate
    @UserId       INT,
    @ProductId    INT,
    @Quantity     INT,
    @CartItemId   INT OUTPUT,
    @NewQuantity  INT OUTPUT,
    @Success      BIT OUTPUT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 1; SET @ErrorMessage = NULL;
    DECLARE @CartId INT;
    EXEC dbo.sp_Carts_GetOrCreate @UserId = @UserId, @CartId = @CartId OUTPUT;
    DECLARE @Stock INT, @IsActive BIT;
    SELECT @Stock = StockQuantity, @IsActive = IsActive FROM dbo.Products WHERE Id = @ProductId;
    IF @IsActive IS NULL OR @IsActive = 0 BEGIN SET @Success = 0; SET @ErrorMessage = 'Product is not available.'; RETURN; END
    DECLARE @ExistingQty INT = 0;
    SELECT @ExistingQty = ISNULL(Quantity, 0) FROM dbo.CartItems WHERE CartId = @CartId AND ProductId = @ProductId;
    DECLARE @DesiredTotal INT = @ExistingQty + @Quantity;
    IF @DesiredTotal > @Stock BEGIN SET @Success = 0; SET @ErrorMessage = 'Requested quantity exceeds available stock (' + CAST(@Stock AS NVARCHAR) + ' available).'; RETURN; END
    IF EXISTS (SELECT 1 FROM dbo.CartItems WHERE CartId = @CartId AND ProductId = @ProductId)
    BEGIN
        UPDATE dbo.CartItems SET Quantity = @DesiredTotal, UpdatedAt = SYSUTCDATETIME() WHERE CartId = @CartId AND ProductId = @ProductId;
        SELECT @CartItemId = Id, @NewQuantity = Quantity FROM dbo.CartItems WHERE CartId = @CartId AND ProductId = @ProductId;
    END ELSE BEGIN
        INSERT INTO dbo.CartItems (CartId, ProductId, Quantity) VALUES (@CartId, @ProductId, @Quantity);
        SET @CartItemId = SCOPE_IDENTITY(); SET @NewQuantity = @Quantity;
    END
    UPDATE dbo.Carts SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @CartId;
END;
GO

CREATE PROCEDURE dbo.sp_CartItems_Remove
    @CartItemId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE ci FROM dbo.CartItems ci INNER JOIN dbo.Carts c ON c.Id = ci.CartId AND c.UserId = @UserId WHERE ci.Id = @CartItemId;
    IF @@ROWCOUNT = 0 RAISERROR('Cart item not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_CartItems_UpdateQuantity
    @CartItemId   INT,
    @UserId       INT,
    @Quantity     INT,
    @Success      BIT OUTPUT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 1; SET @ErrorMessage = NULL;
    IF @Quantity <= 0 BEGIN SET @Success = 0; SET @ErrorMessage = 'Quantity must be at least 1.'; RETURN; END
    DECLARE @Stock INT;
    SELECT @Stock = p.StockQuantity FROM dbo.CartItems ci
    INNER JOIN dbo.Carts    c ON c.Id = ci.CartId AND c.UserId = @UserId
    INNER JOIN dbo.Products p ON p.Id = ci.ProductId WHERE ci.Id = @CartItemId;
    IF @Stock IS NULL BEGIN SET @Success = 0; SET @ErrorMessage = 'Cart item not found.'; RETURN; END
    IF @Quantity > @Stock BEGIN SET @Success = 0; SET @ErrorMessage = 'Only ' + CAST(@Stock AS NVARCHAR) + ' units in stock.'; RETURN; END
    UPDATE dbo.CartItems SET Quantity = @Quantity, UpdatedAt = SYSUTCDATETIME() WHERE Id = @CartItemId;
    UPDATE dbo.Carts SET UpdatedAt = SYSUTCDATETIME() WHERE Id = (SELECT CartId FROM dbo.CartItems WHERE Id = @CartItemId);
END;
GO

CREATE PROCEDURE dbo.sp_CartItems_GetCount
    @UserId    INT,
    @ItemCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @ItemCount = ISNULL(SUM(ci.Quantity), 0)
    FROM dbo.CartItems ci INNER JOIN dbo.Carts c ON c.Id = ci.CartId WHERE c.UserId = @UserId;
END;
GO

CREATE PROCEDURE dbo.sp_CartItems_Validate
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ci.Id AS CartItemId, p.Id AS ProductId, p.Name AS ProductName,
           ci.Quantity AS RequestedQty, p.StockQuantity AS AvailableQty, p.IsActive,
           CASE WHEN p.IsActive = 0 THEN 'Product is no longer available.'
                WHEN p.StockQuantity < ci.Quantity THEN 'Only ' + CAST(p.StockQuantity AS NVARCHAR) + ' unit(s) in stock.'
                ELSE NULL END AS Issue
    FROM dbo.CartItems ci
    INNER JOIN dbo.Carts    c ON c.Id = ci.CartId AND c.UserId = @UserId
    INNER JOIN dbo.Products p ON p.Id = ci.ProductId
    WHERE p.IsActive = 0 OR p.StockQuantity < ci.Quantity;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 10. STORED PROCEDURES — Orders
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Orders_Place
    @UserId         INT,
    @AddressId      INT,
    @PaymentMethod  NVARCHAR(20),
    @TotalAmount    DECIMAL(10,2),
    @DiscountAmount DECIMAL(10,2) = 0,
    @FinalAmount    DECIMAL(10,2),
    @Notes          NVARCHAR(500) = NULL,
    @ItemsJson      NVARCHAR(MAX),
    @NewOrderId     INT OUTPUT,
    @Success        BIT OUTPUT,
    @ErrorMessage   NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 1; SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Addresses WHERE Id = @AddressId AND UserId = @UserId)
        BEGIN SET @Success = 0; SET @ErrorMessage = 'Invalid delivery address.'; ROLLBACK TRANSACTION; RETURN; END
        IF EXISTS (
            SELECT 1 FROM OPENJSON(@ItemsJson) WITH (ProductId INT '$.ProductId', Quantity INT '$.Quantity') j
            INNER JOIN dbo.Products p ON p.Id = j.ProductId WHERE p.StockQuantity < j.Quantity OR p.IsActive = 0)
        BEGIN SET @Success = 0; SET @ErrorMessage = 'One or more products are out of stock or unavailable.'; ROLLBACK TRANSACTION; RETURN; END
        INSERT INTO dbo.Orders (UserId, AddressId, TotalAmount, DiscountAmount, FinalAmount, PaymentMethod, Notes)
        VALUES (@UserId, @AddressId, @TotalAmount, @DiscountAmount, @FinalAmount, @PaymentMethod, @Notes);
        SET @NewOrderId = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ProductImage, UnitPrice, Quantity)
        SELECT @NewOrderId, j.ProductId, j.ProductName, j.ProductImage, j.UnitPrice, j.Quantity
        FROM OPENJSON(@ItemsJson) WITH (ProductId INT '$.ProductId', ProductName NVARCHAR(200) '$.ProductName', ProductImage NVARCHAR(500) '$.ProductImage', UnitPrice DECIMAL(10,2) '$.UnitPrice', Quantity INT '$.Quantity') AS j;
        UPDATE p SET p.StockQuantity = p.StockQuantity - j.Quantity, p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Products p INNER JOIN OPENJSON(@ItemsJson) WITH (ProductId INT '$.ProductId', Quantity INT '$.Quantity') AS j ON j.ProductId = p.Id;
        DELETE ci FROM dbo.CartItems ci INNER JOIN dbo.Carts c ON c.Id = ci.CartId WHERE c.UserId = @UserId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Success = 0; SET @ErrorMessage = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.sp_Orders_GetById
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_OrderSummary WHERE OrderId = @OrderId;
    SELECT oi.Id, oi.OrderId, oi.ProductId, oi.ProductName, oi.ProductImage, oi.UnitPrice, oi.Quantity, oi.Subtotal
    FROM dbo.OrderItems oi WHERE oi.OrderId = @OrderId ORDER BY oi.Id ASC;
END;
GO

CREATE PROCEDURE dbo.sp_Orders_GetByUser
    @UserId     INT,
    @Page       INT = 1,
    @PageSize   INT = 10,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    SELECT @TotalCount = COUNT(*) FROM dbo.Orders WHERE UserId = @UserId;
    SELECT * FROM dbo.vw_OrderSummary WHERE CustomerId = @UserId
    ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE PROCEDURE dbo.sp_Orders_GetAll
    @OrderStatus   NVARCHAR(20)  = NULL,
    @PaymentStatus NVARCHAR(20)  = NULL,
    @FromDate      DATETIME2     = NULL,
    @ToDate        DATETIME2     = NULL,
    @SearchTerm    NVARCHAR(200) = NULL,
    @Page          INT           = 1,
    @PageSize      INT           = 20,
    @TotalCount    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    SELECT @TotalCount = COUNT(*) FROM dbo.vw_OrderSummary
    WHERE (@OrderStatus IS NULL OR OrderStatus = @OrderStatus)
      AND (@PaymentStatus IS NULL OR PaymentStatus = @PaymentStatus)
      AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR CreatedAt <= @ToDate)
      AND (@SearchTerm IS NULL OR CustomerName LIKE '%' + @SearchTerm + '%' OR CustomerEmail LIKE '%' + @SearchTerm + '%');
    SELECT * FROM dbo.vw_OrderSummary
    WHERE (@OrderStatus IS NULL OR OrderStatus = @OrderStatus)
      AND (@PaymentStatus IS NULL OR PaymentStatus = @PaymentStatus)
      AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR CreatedAt <= @ToDate)
      AND (@SearchTerm IS NULL OR CustomerName LIKE '%' + @SearchTerm + '%' OR CustomerEmail LIKE '%' + @SearchTerm + '%')
    ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE PROCEDURE dbo.sp_Orders_UpdateStatus
    @OrderId           INT,
    @OrderStatus       NVARCHAR(20),
    @PaymentStatus     NVARCHAR(20)  = NULL,
    @ShiprocketOrderId NVARCHAR(100) = NULL,
    @AWBCode           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Orders SET
        OrderStatus = @OrderStatus,
        PaymentStatus = ISNULL(@PaymentStatus, PaymentStatus),
        ShiprocketOrderId = ISNULL(@ShiprocketOrderId, ShiprocketOrderId),
        AWBCode = ISNULL(@AWBCode, AWBCode),
        CancelledAt = CASE WHEN @OrderStatus = 'Cancelled' AND CancelledAt IS NULL THEN SYSUTCDATETIME() ELSE CancelledAt END,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;
    IF @@ROWCOUNT = 0 RAISERROR('Order not found.', 16, 1);
END;
GO

CREATE PROCEDURE dbo.sp_Orders_Cancel
    @OrderId      INT,
    @UserId       INT,
    @CancelReason NVARCHAR(300) = NULL,
    @Success      BIT OUTPUT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 1; SET @ErrorMessage = NULL;
    DECLARE @CurrentStatus NVARCHAR(20);
    SELECT @CurrentStatus = OrderStatus FROM dbo.Orders WHERE Id = @OrderId AND UserId = @UserId;
    IF @CurrentStatus IS NULL BEGIN SET @Success = 0; SET @ErrorMessage = 'Order not found.'; RETURN; END
    IF @CurrentStatus NOT IN ('Pending', 'Paid') BEGIN SET @Success = 0; SET @ErrorMessage = 'Only Pending or Paid orders can be cancelled.'; RETURN; END
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE p SET p.StockQuantity = p.StockQuantity + oi.Quantity, p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Products p INNER JOIN dbo.OrderItems oi ON oi.ProductId = p.Id WHERE oi.OrderId = @OrderId;
        UPDATE dbo.Orders SET OrderStatus = 'Cancelled', CancelledAt = SYSUTCDATETIME(), CancelReason = @CancelReason, UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @OrderId AND UserId = @UserId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Success = 0; SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.sp_OrderItems_GetByOrder
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT oi.Id, oi.OrderId, oi.ProductId, oi.ProductName, oi.ProductImage, oi.UnitPrice, oi.Quantity, oi.Subtotal,
           p.IsActive AS ProductStillActive, p.StockQuantity AS CurrentStock
    FROM dbo.OrderItems oi INNER JOIN dbo.Products p ON p.Id = oi.ProductId
    WHERE oi.OrderId = @OrderId ORDER BY oi.Id ASC;
END;
GO

CREATE PROCEDURE dbo.sp_OrderItems_GetByProduct
    @ProductId INT,
    @Page      INT = 1,
    @PageSize  INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    SELECT oi.Id, oi.OrderId, oi.Quantity, oi.UnitPrice, oi.Subtotal,
           o.CreatedAt AS OrderDate, o.OrderStatus, o.PaymentStatus,
           u.Name AS CustomerName, u.Email AS CustomerEmail
    FROM dbo.OrderItems oi INNER JOIN dbo.Orders o ON o.Id = oi.OrderId INNER JOIN dbo.Users u ON u.Id = o.UserId
    WHERE oi.ProductId = @ProductId ORDER BY o.CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE PROCEDURE dbo.sp_OrderItems_GetRevenueByProduct
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT oi.ProductId, oi.ProductName, SUM(oi.Quantity) AS TotalQuantitySold, SUM(oi.Subtotal) AS TotalRevenue
    FROM dbo.OrderItems oi INNER JOIN dbo.Orders o ON o.Id = oi.OrderId AND o.PaymentStatus = 'Paid'
    WHERE (@FromDate IS NULL OR o.CreatedAt >= @FromDate) AND (@ToDate IS NULL OR o.CreatedAt <= @ToDate)
    GROUP BY oi.ProductId, oi.ProductName ORDER BY TotalRevenue DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Payment_Complete
    @OrderId           INT,
    @RazorpayOrderId   NVARCHAR(100),
    @RazorpayPaymentId NVARCHAR(100),
    @RazorpaySignature NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Orders SET
        RazorpayOrderId = @RazorpayOrderId, RazorpayPaymentId = @RazorpayPaymentId, RazorpaySignature = @RazorpaySignature,
        PaymentStatus = 'Paid', OrderStatus = 'Paid', UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;
    IF @@ROWCOUNT = 0 RAISERROR('Order not found.', 16, 1);
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 11. STORED PROCEDURES — Wishlists, Coupons, Campaigns
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Wishlists_Add
    @UserId    INT,
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Wishlists WHERE UserId = @UserId AND ProductId = @ProductId)
        INSERT INTO dbo.Wishlists (UserId, ProductId) VALUES (@UserId, @ProductId);
END;
GO

CREATE PROCEDURE dbo.sp_Wishlists_Remove
    @UserId    INT,
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Wishlists WHERE UserId = @UserId AND ProductId = @ProductId;
END;
GO

CREATE PROCEDURE dbo.sp_Wishlists_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT w.Id AS WishlistId, w.AddedAt, p.Id AS ProductId, p.Name AS ProductName,
           p.Price, p.DiscountedPrice,
           CASE WHEN p.DiscountedPrice IS NOT NULL AND p.DiscountedPrice < p.Price
                THEN CAST(ROUND((p.Price - p.DiscountedPrice) / p.Price * 100, 0) AS INT) ELSE 0 END AS DiscountPercent,
           p.StockQuantity, p.IsActive, pi_d.ImageUrl AS DefaultImage
    FROM dbo.Wishlists w
    INNER JOIN dbo.Products      p    ON p.Id = w.ProductId
    LEFT  JOIN dbo.ProductImages pi_d ON pi_d.ProductId = p.Id AND pi_d.IsDefault = 1
    WHERE w.UserId = @UserId ORDER BY w.AddedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Wishlists_Check
    @UserId       INT,
    @ProductId    INT,
    @IsWishlisted BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @IsWishlisted = CASE WHEN EXISTS (SELECT 1 FROM dbo.Wishlists WHERE UserId = @UserId AND ProductId = @ProductId) THEN 1 ELSE 0 END;
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_Create
    @Code          NVARCHAR(100) = NULL,
    @Description   NVARCHAR(500),
    @DiscountType  NVARCHAR(20),
    @DiscountValue DECIMAL(18,2),
    @MinCartAmount DECIMAL(18,2) = NULL,
    @MaxDiscount   DECIMAL(18,2) = NULL,
    @StartDate     DATETIME2     = NULL,
    @EndDate       DATETIME2     = NULL,
    @UsageLimit    INT           = NULL,
    @FestivalName  NVARCHAR(100) = NULL,
    @BankName      NVARCHAR(100) = NULL,
    @NewCouponId   INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.Coupons (Code, Description, DiscountType, DiscountValue, MinCartAmount, MaxDiscount, StartDate, EndDate, UsageLimit, FestivalName, BankName, IsActive, CreatedAt)
    VALUES (@Code, @Description, @DiscountType, @DiscountValue, @MinCartAmount, @MaxDiscount, @StartDate, @EndDate, @UsageLimit, @FestivalName, @BankName, 1, GETUTCDATE());
    SET @NewCouponId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Description, DiscountType, DiscountValue, MinCartAmount, MaxDiscount,
           StartDate, EndDate, IsActive, UsageLimit, UsedCount, FestivalName, CreatedAt
    FROM dbo.Coupons ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_GetByCode
    @Code NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Description, DiscountType, DiscountValue, MinCartAmount, MaxDiscount,
           StartDate, EndDate, IsActive, UsageLimit, UsedCount, FestivalName, CreatedAt
    FROM dbo.Coupons WHERE Code = @Code;
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Description, DiscountType, DiscountValue, MinCartAmount, MaxDiscount,
           StartDate, EndDate, IsActive, UsageLimit, UsedCount, FestivalName, CreatedAt
    FROM dbo.Coupons
    WHERE IsActive = 1 AND Code IS NULL
      AND (StartDate IS NULL OR StartDate <= GETUTCDATE())
      AND (EndDate IS NULL OR EndDate >= GETUTCDATE())
      AND (UsageLimit IS NULL OR UsedCount < UsageLimit);
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_GetBankOffers
AS
BEGIN
    SELECT Id, Code, Description, DiscountType, DiscountValue, BankName, MinCartAmount, MaxDiscount, EndDate
    FROM dbo.Coupons
    WHERE IsActive = 1 AND BankName IS NOT NULL
      AND (StartDate IS NULL OR StartDate <= GETUTCDATE())
      AND (EndDate IS NULL OR EndDate >= GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_Update
    @Id            INT,
    @Code          NVARCHAR(100) = NULL,
    @Description   NVARCHAR(500),
    @DiscountType  NVARCHAR(20),
    @DiscountValue DECIMAL(18,2),
    @MinCartAmount DECIMAL(18,2) = NULL,
    @MaxDiscount   DECIMAL(18,2) = NULL,
    @StartDate     DATETIME2     = NULL,
    @EndDate       DATETIME2     = NULL,
    @UsageLimit    INT           = NULL,
    @FestivalName  NVARCHAR(100) = NULL,
    @BankName      NVARCHAR(100) = NULL,
    @IsActive      BIT
AS
BEGIN
    UPDATE dbo.Coupons SET
        Code = @Code, Description = @Description, DiscountType = @DiscountType, DiscountValue = @DiscountValue,
        MinCartAmount = @MinCartAmount, MaxDiscount = @MaxDiscount, StartDate = @StartDate, EndDate = @EndDate,
        UsageLimit = @UsageLimit, FestivalName = @FestivalName, BankName = @BankName, IsActive = @IsActive
    WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.sp_Coupons_IncrementUsage
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Coupons SET UsedCount = UsedCount + 1 WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.sp_Campaigns_Create
    @Name          NVARCHAR(200),
    @Description   NVARCHAR(500) = NULL,
    @CategoryId    INT           = NULL,
    @DiscountType  NVARCHAR(20),
    @DiscountValue DECIMAL(18,2),
    @StartDate     DATETIME2     = NULL,
    @EndDate       DATETIME2     = NULL,
    @NewCampaignId INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.Campaigns (Name, Description, CategoryId, DiscountType, DiscountValue, StartDate, EndDate, IsActive, CreatedAt)
    VALUES (@Name, @Description, @CategoryId, @DiscountType, @DiscountValue, @StartDate, @EndDate, 1, GETUTCDATE());
    SET @NewCampaignId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Campaigns_GetAll
AS
BEGIN
    SELECT Id, Name, Description, CategoryId, DiscountType, DiscountValue, StartDate, EndDate, IsActive, CreatedAt
    FROM dbo.Campaigns ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Campaigns_GetActive
AS
BEGIN
    SELECT Id, Name, Description, CategoryId, DiscountType, DiscountValue, StartDate, EndDate, IsActive, CreatedAt
    FROM dbo.Campaigns WHERE IsActive = 1
      AND (StartDate IS NULL OR StartDate <= GETUTCDATE())
      AND (EndDate IS NULL OR EndDate >= GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.sp_Campaigns_Update
    @Id            INT,
    @Name          NVARCHAR(200),
    @Description   NVARCHAR(500) = NULL,
    @CategoryId    INT           = NULL,
    @DiscountType  NVARCHAR(20),
    @DiscountValue DECIMAL(18,2),
    @StartDate     DATETIME2     = NULL,
    @EndDate       DATETIME2     = NULL,
    @IsActive      BIT
AS
BEGIN
    UPDATE dbo.Campaigns SET
        Name = @Name, Description = @Description, CategoryId = @CategoryId,
        DiscountType = @DiscountType, DiscountValue = @DiscountValue,
        StartDate = @StartDate, EndDate = @EndDate, IsActive = @IsActive
    WHERE Id = @Id;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 12. STORED PROCEDURES — Reviews
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Reviews_Create
    @ProductId  INT,
    @UserId     INT,
    @UserName   NVARCHAR(150),
    @Rating     INT,
    @Title      NVARCHAR(200) = NULL,
    @Body       NVARCHAR(MAX),
    @IsApproved BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Reviews WHERE ProductId = @ProductId AND UserId = @UserId)
    BEGIN RAISERROR('You have already reviewed this product.', 16, 1); RETURN; END
    INSERT INTO dbo.Reviews (ProductId, UserId, UserName, Rating, Title, Body, IsApproved)
    VALUES (@ProductId, @UserId, @UserName, @Rating, @Title, @Body, @IsApproved);
END;
GO

CREATE PROCEDURE dbo.sp_Reviews_GetByProduct
    @ProductId   INT,
    @ApprovedOnly BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ProductId, UserId, UserName, Rating, Title, Body, IsApproved, CreatedAt
    FROM dbo.Reviews WHERE ProductId = @ProductId AND (@ApprovedOnly = 0 OR IsApproved = 1)
    ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Reviews_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ProductId, UserId, UserName, Rating, Title, Body, IsApproved, CreatedAt
    FROM dbo.Reviews ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Reviews_Approve
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Reviews SET IsApproved = 1 WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.sp_Reviews_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Reviews WHERE Id = @Id;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 13. STORED PROCEDURES — Returns
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Returns_Create
    @OrderId      INT,
    @UserId       INT,
    @UserName     NVARCHAR(150),
    @Reason       NVARCHAR(200),
    @Description  NVARCHAR(500) = NULL,
    @NewReturnId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ReturnRequests (OrderId, UserId, UserName, Reason, Description)
    VALUES (@OrderId, @UserId, @UserName, @Reason, @Description);
    SET @NewReturnId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE dbo.sp_Returns_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.ReturnRequests WHERE UserId = @UserId ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Returns_GetByOrder
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.ReturnRequests WHERE OrderId = @OrderId;
END;
GO

CREATE PROCEDURE dbo.sp_Returns_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.ReturnRequests ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_Returns_UpdateStatus
    @Id        INT,
    @Status    NVARCHAR(20),
    @AdminNote NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ReturnRequests SET Status = @Status, AdminNote = @AdminNote, UpdatedAt = GETUTCDATE() WHERE Id = @Id;
END;
GO

-- ─────────────────────────────────────────────────────────────
-- 14. STORED PROCEDURES — Dashboard
-- ─────────────────────────────────────────────────────────────

CREATE PROCEDURE dbo.sp_Dashboard_GetStats
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM dbo.Orders WHERE PaymentStatus = 'Paid') AS TotalOrders,
        (SELECT ISNULL(SUM(FinalAmount), 0) FROM dbo.Orders WHERE PaymentStatus = 'Paid') AS TotalRevenue,
        (SELECT COUNT(*) FROM dbo.Orders WHERE CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)) AS OrdersToday,
        (SELECT ISNULL(SUM(FinalAmount), 0) FROM dbo.Orders WHERE PaymentStatus = 'Paid' AND CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)) AS RevenueToday,
        (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1) AS TotalProducts,
        (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1 AND StockQuantity <= 5) AS LowStockProducts,
        (SELECT COUNT(*) FROM dbo.Users WHERE Role = 'Customer') AS TotalCustomers,
        (SELECT COUNT(*) FROM dbo.Orders WHERE OrderStatus = 'Pending') AS PendingOrders,
        (SELECT COUNT(*) FROM dbo.Orders WHERE OrderStatus = 'Shipped') AS ShippedOrders;
    SELECT FORMAT(CreatedAt, 'yyyy-MM') AS Month, SUM(FinalAmount) AS Revenue, COUNT(*) AS OrderCount
    FROM dbo.Orders WHERE PaymentStatus = 'Paid' AND CreatedAt >= DATEADD(MONTH, -6, SYSUTCDATETIME())
    GROUP BY FORMAT(CreatedAt, 'yyyy-MM') ORDER BY Month ASC;
    SELECT TOP 5 oi.ProductId, oi.ProductName, SUM(oi.Quantity) AS TotalSold, SUM(oi.Subtotal) AS TotalRevenue
    FROM dbo.OrderItems oi INNER JOIN dbo.Orders o ON o.Id = oi.OrderId AND o.PaymentStatus = 'Paid'
    GROUP BY oi.ProductId, oi.ProductName ORDER BY TotalRevenue DESC;
    SELECT TOP 10 * FROM dbo.vw_OrderSummary ORDER BY CreatedAt DESC;
END;
GO

CREATE PROCEDURE dbo.sp_GetDashboardStats
AS
BEGIN
    EXEC dbo.sp_Dashboard_GetStats;
END;
GO

-- sp_PlaceOrder / sp_VerifyAndCompletePayment / sp_UpdateOrderStatus aliases
CREATE PROCEDURE dbo.sp_PlaceOrder
    @UserId INT, @AddressId INT, @PaymentMethod NVARCHAR(20),
    @TotalAmount DECIMAL(10,2), @DiscountAmount DECIMAL(10,2) = 0, @FinalAmount DECIMAL(10,2),
    @Notes NVARCHAR(500) = NULL, @ItemsJson NVARCHAR(MAX),
    @NewOrderId INT OUTPUT, @Success BIT OUTPUT, @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    EXEC dbo.sp_Orders_Place @UserId=@UserId, @AddressId=@AddressId, @PaymentMethod=@PaymentMethod,
        @TotalAmount=@TotalAmount, @DiscountAmount=@DiscountAmount, @FinalAmount=@FinalAmount,
        @Notes=@Notes, @ItemsJson=@ItemsJson, @NewOrderId=@NewOrderId OUTPUT, @Success=@Success OUTPUT, @ErrorMessage=@ErrorMessage OUTPUT;
END;
GO

CREATE PROCEDURE dbo.sp_VerifyAndCompletePayment
    @OrderId INT, @RazorpayOrderId NVARCHAR(100), @RazorpayPaymentId NVARCHAR(100), @RazorpaySignature NVARCHAR(256)
AS
BEGIN
    EXEC dbo.sp_Payment_Complete @OrderId=@OrderId, @RazorpayOrderId=@RazorpayOrderId, @RazorpayPaymentId=@RazorpayPaymentId, @RazorpaySignature=@RazorpaySignature;
END;
GO

CREATE PROCEDURE dbo.sp_UpdateOrderStatus
    @OrderId INT, @OrderStatus NVARCHAR(20), @PaymentStatus NVARCHAR(20) = NULL, @ShiprocketOrderId NVARCHAR(100) = NULL, @AWBCode NVARCHAR(100) = NULL
AS
BEGIN
    EXEC dbo.sp_Orders_UpdateStatus @OrderId=@OrderId, @OrderStatus=@OrderStatus, @PaymentStatus=@PaymentStatus, @ShiprocketOrderId=@ShiprocketOrderId, @AWBCode=@AWBCode;
END;
GO

PRINT '✓ NiroteDb schema created — all tables, views and stored procedures ready.';
GO
