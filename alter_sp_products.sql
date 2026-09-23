USE NiroteDb;
GO
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
    @NewProductId     INT OUTPUT
AS
BEGIN
    INSERT INTO dbo.Products
        (Name, Description, Price, DiscountedPrice, StockQuantity,
         Fabric, Color, Occasion, State, HasBlousePiece, CareInstructions,
         DeliveryDays, CategoryId, IsActive, CreatedAt)
    VALUES
        (@Name, @Description, @Price, @DiscountedPrice, @StockQuantity,
         @Fabric, @Color, @Occasion, @State, @HasBlousePiece, @CareInstructions,
         @DeliveryDays, @CategoryId, 1, GETUTCDATE());
    SET @NewProductId = SCOPE_IDENTITY();
END;
GO
