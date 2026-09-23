USE NiroteDb;
GO

-- 1. Add VideoUrl column to Products
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'VideoUrl')
BEGIN
    ALTER TABLE dbo.Products ADD VideoUrl NVARCHAR(1000) NULL;
END
GO

-- 2. Update sp_Products_Create to accept VideoUrl
ALTER PROCEDURE dbo.sp_Products_Create
    @Name             NVARCHAR(200),
    @Description      NVARCHAR(MAX)  = NULL,
    @Price            DECIMAL(18,2),
    @DiscountedPrice  DECIMAL(18,2)  = NULL,
    @StockQuantity    INT,
    @Fabric           NVARCHAR(100)  = NULL,
    @Color            NVARCHAR(100)  = NULL,
    @Occasion         NVARCHAR(100)  = NULL,
    @State            NVARCHAR(100)  = NULL,
    @HasBlousePiece   BIT            = 0,
    @CareInstructions NVARCHAR(500)  = NULL,
    @DeliveryDays     INT            = 7,
    @CategoryId       INT,
    @VideoUrl         NVARCHAR(1000) = NULL,
    @NewProductId     INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.Products
        (Name, Description, Price, DiscountedPrice, StockQuantity,
         Fabric, Color, Occasion, State, HasBlousePiece, CareInstructions,
         DeliveryDays, CategoryId, VideoUrl, IsActive, CreatedAt)
    VALUES
        (@Name, @Description, @Price, @DiscountedPrice, @StockQuantity,
         @Fabric, @Color, @Occasion, @State, @HasBlousePiece, @CareInstructions,
         @DeliveryDays, @CategoryId, @VideoUrl, 1, GETUTCDATE());
    SET @NewProductId = SCOPE_IDENTITY();
END;
GO

-- 3. Update sp_Products_Update to accept VideoUrl
ALTER PROCEDURE dbo.sp_Products_Update
    @ProductId        INT,
    @Id               INT            = NULL,
    @Name             NVARCHAR(200),
    @Description      NVARCHAR(MAX)  = NULL,
    @Price            DECIMAL(18,2),
    @DiscountedPrice  DECIMAL(18,2)  = NULL,
    @StockQuantity    INT,
    @Fabric           NVARCHAR(100)  = NULL,
    @Color            NVARCHAR(100)  = NULL,
    @Occasion         NVARCHAR(100)  = NULL,
    @State            NVARCHAR(100)  = NULL,
    @HasBlousePiece   BIT            = 0,
    @CareInstructions NVARCHAR(500)  = NULL,
    @DeliveryDays     INT            = 7,
    @CategoryId       INT,
    @VideoUrl         NVARCHAR(1000) = NULL
AS
BEGIN
    UPDATE dbo.Products SET
        Name             = @Name,
        Description      = @Description,
        Price            = @Price,
        DiscountedPrice  = @DiscountedPrice,
        StockQuantity    = @StockQuantity,
        Fabric           = @Fabric,
        Color            = @Color,
        Occasion         = @Occasion,
        State            = @State,
        HasBlousePiece   = @HasBlousePiece,
        CareInstructions = @CareInstructions,
        DeliveryDays     = @DeliveryDays,
        CategoryId       = @CategoryId,
        VideoUrl         = @VideoUrl,
        UpdatedAt        = GETUTCDATE()
    WHERE Id = @ProductId;
END;
GO

-- 4. Update sp_Products_GetById to return VideoUrl
ALTER PROCEDURE dbo.sp_Products_GetById
    @ProductId INT
AS
BEGIN
    SELECT
        p.Id, p.Name, p.Description, p.Price, p.DiscountedPrice,
        p.StockQuantity, p.Fabric, p.Color, p.Occasion, p.State,
        p.HasBlousePiece, p.CareInstructions, p.DeliveryDays,
        p.CategoryId, p.IsActive, p.IsFeatured, p.VideoUrl,
        p.CreatedAt, p.UpdatedAt,
        c.Name AS CategoryName,
        AVG(CAST(r.Rating AS FLOAT)) AS AverageRating,
        COUNT(r.Id) AS ReviewCount
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
    LEFT JOIN dbo.Reviews r ON r.ProductId = p.Id AND r.IsApproved = 1
    WHERE p.Id = @ProductId
    GROUP BY p.Id, p.Name, p.Description, p.Price, p.DiscountedPrice,
             p.StockQuantity, p.Fabric, p.Color, p.Occasion, p.State,
             p.HasBlousePiece, p.CareInstructions, p.DeliveryDays,
             p.CategoryId, p.IsActive, p.IsFeatured, p.VideoUrl,
             p.CreatedAt, p.UpdatedAt, c.Name;

    SELECT Id, ProductId, ImageUrl, IsDefault, DisplayOrder, CreatedAt
    FROM dbo.ProductImages
    WHERE ProductId = @ProductId
    ORDER BY IsDefault DESC, DisplayOrder ASC;
END;
GO
