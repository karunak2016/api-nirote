-- ============================================================
-- Niroté CMS Migration v2 — Extended Content Management
-- Adds: WhyChooseUs, Instagram, Popups, EmailTemplates, CmsCollections
-- Updates: Banners (MobileImageUrl), Settings grouping
-- Run AFTER cms_migration.sql
-- ============================================================
USE NiroteDb;
GO

-- ────────────────────────────────────────────────────────────────
-- 1. UPDATE EXISTING TABLES
-- ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Banners') AND name = 'MobileImageUrl')
    ALTER TABLE dbo.Banners ADD MobileImageUrl NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Banners') AND name = 'Priority')
    ALTER TABLE dbo.Banners ADD Priority INT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Banners') AND name = 'TextAlign')
    ALTER TABLE dbo.Banners ADD TextAlign NVARCHAR(10) NOT NULL DEFAULT 'left';
GO

-- ────────────────────────────────────────────────────────────────
-- 2. NEW TABLES
-- ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WhyChooseUsItems')
CREATE TABLE dbo.WhyChooseUsItems (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    IconName     NVARCHAR(50) NOT NULL DEFAULT 'Gem',
    Title        NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(300) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InstagramPosts')
CREATE TABLE dbo.InstagramPosts (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ImageUrl     NVARCHAR(1000) NOT NULL,
    PostUrl      NVARCHAR(1000) NULL,
    Caption      NVARCHAR(300) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Popups')
CREATE TABLE dbo.Popups (
    Id            INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    PopupType     NVARCHAR(50) NOT NULL UNIQUE,
    Heading       NVARCHAR(200) NULL,
    Subheading    NVARCHAR(400) NULL,
    ImageUrl      NVARCHAR(1000) NULL,
    ButtonText    NVARCHAR(100) NULL,
    ButtonUrl     NVARCHAR(500) NULL,
    CouponCode    NVARCHAR(50) NULL,
    IsEnabled     BIT NOT NULL DEFAULT 0,
    TriggerDelay  INT NOT NULL DEFAULT 5,
    UpdatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailTemplates')
CREATE TABLE dbo.EmailTemplates (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TemplateType NVARCHAR(50) NOT NULL UNIQUE,
    Subject      NVARCHAR(200) NOT NULL,
    Body         NVARCHAR(MAX) NOT NULL,
    IsEnabled    BIT NOT NULL DEFAULT 1,
    UpdatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CmsCollections')
CREATE TABLE dbo.CmsCollections (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    Slug         NVARCHAR(100) NOT NULL UNIQUE,
    BannerUrl    NVARCHAR(1000) NULL,
    ImageUrl     NVARCHAR(1000) NULL,
    Description  NVARCHAR(MAX) NULL,
    SeoTitle     NVARCHAR(200) NULL,
    SeoDesc      NVARCHAR(500) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt    DATETIME2 NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductCollections')
CREATE TABLE dbo.ProductCollections (
    ProductId    INT NOT NULL,
    CollectionId INT NOT NULL REFERENCES dbo.CmsCollections(Id),
    PRIMARY KEY (ProductId, CollectionId)
);
GO

-- ────────────────────────────────────────────────────────────────
-- 3. STORED PROCEDURES — BANNERS V2
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Banners_Save_V2
    @Id             INT = NULL,
    @Badge          NVARCHAR(100) = NULL,
    @Heading        NVARCHAR(300),
    @Subheading     NVARCHAR(600) = NULL,
    @ImageUrl       NVARCHAR(1000) = NULL,
    @MobileImageUrl NVARCHAR(1000) = NULL,
    @TextAlign      NVARCHAR(10) = 'left',
    @Btn1Text       NVARCHAR(100) = NULL,
    @Btn1Url        NVARCHAR(500) = NULL,
    @Btn2Text       NVARCHAR(100) = NULL,
    @Btn2Url        NVARCHAR(500) = NULL,
    @IsActive       BIT = 1,
    @ScheduledStart DATETIME2 = NULL,
    @ScheduledEnd   DATETIME2 = NULL,
    @DisplayOrder   INT = 0,
    @Priority       INT = 0,
    @NewId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.Banners(Badge,Heading,Subheading,ImageUrl,MobileImageUrl,TextAlign,Btn1Text,Btn1Url,Btn2Text,Btn2Url,IsActive,ScheduledStart,ScheduledEnd,DisplayOrder,Priority)
        VALUES(@Badge,@Heading,@Subheading,@ImageUrl,@MobileImageUrl,@TextAlign,@Btn1Text,@Btn1Url,@Btn2Text,@Btn2Url,@IsActive,@ScheduledStart,@ScheduledEnd,@DisplayOrder,@Priority);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Banners SET Badge=@Badge,Heading=@Heading,Subheading=@Subheading,
            ImageUrl=@ImageUrl,MobileImageUrl=@MobileImageUrl,TextAlign=@TextAlign,
            Btn1Text=@Btn1Text,Btn1Url=@Btn1Url,Btn2Text=@Btn2Text,Btn2Url=@Btn2Url,
            IsActive=@IsActive,ScheduledStart=@ScheduledStart,ScheduledEnd=@ScheduledEnd,
            DisplayOrder=@DisplayOrder,Priority=@Priority,UpdatedAt=SYSUTCDATETIME()
        WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Banners_GetActive_V2
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    SELECT Id,Badge,Heading,Subheading,ImageUrl,MobileImageUrl,TextAlign,
           Btn1Text,Btn1Url,Btn2Text,Btn2Url,DisplayOrder,Priority
    FROM dbo.Banners
    WHERE IsActive=1
      AND (ScheduledStart IS NULL OR ScheduledStart <= @Now)
      AND (ScheduledEnd   IS NULL OR ScheduledEnd   >= @Now)
    ORDER BY Priority DESC, DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Banners_GetAll_V2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Badge,Heading,Subheading,ImageUrl,MobileImageUrl,TextAlign,Btn1Text,Btn1Url,
           Btn2Text,Btn2Url,IsActive,ScheduledStart,ScheduledEnd,DisplayOrder,Priority,CreatedAt,UpdatedAt
    FROM dbo.Banners ORDER BY Priority DESC, DisplayOrder ASC;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 4. STORED PROCEDURES — WHY CHOOSE US
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_WhyChooseUs_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,IconName,Title,Description,DisplayOrder
    FROM dbo.WhyChooseUsItems WHERE IsActive=1 ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_WhyChooseUs_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,IconName,Title,Description,DisplayOrder,IsActive
    FROM dbo.WhyChooseUsItems ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_WhyChooseUs_Save
    @Id          INT = NULL,
    @IconName    NVARCHAR(50),
    @Title       NVARCHAR(100),
    @Description NVARCHAR(300),
    @DisplayOrder INT = 0,
    @IsActive    BIT = 1,
    @NewId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.WhyChooseUsItems(IconName,Title,Description,DisplayOrder,IsActive)
        VALUES(@IconName,@Title,@Description,@DisplayOrder,@IsActive);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.WhyChooseUsItems SET IconName=@IconName,Title=@Title,Description=@Description,DisplayOrder=@DisplayOrder,IsActive=@IsActive WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_WhyChooseUs_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.WhyChooseUsItems WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 5. STORED PROCEDURES — INSTAGRAM
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Instagram_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,ImageUrl,PostUrl,Caption,DisplayOrder
    FROM dbo.InstagramPosts WHERE IsActive=1 ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Instagram_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,ImageUrl,PostUrl,Caption,DisplayOrder,IsActive,CreatedAt
    FROM dbo.InstagramPosts ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Instagram_Save
    @Id          INT = NULL,
    @ImageUrl    NVARCHAR(1000),
    @PostUrl     NVARCHAR(1000) = NULL,
    @Caption     NVARCHAR(300) = NULL,
    @DisplayOrder INT = 0,
    @IsActive    BIT = 1,
    @NewId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.InstagramPosts(ImageUrl,PostUrl,Caption,DisplayOrder,IsActive)
        VALUES(@ImageUrl,@PostUrl,@Caption,@DisplayOrder,@IsActive);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.InstagramPosts SET ImageUrl=@ImageUrl,PostUrl=@PostUrl,Caption=@Caption,DisplayOrder=@DisplayOrder,IsActive=@IsActive WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Instagram_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.InstagramPosts WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 6. STORED PROCEDURES — POPUPS
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Popups_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PopupType,Heading,Subheading,ImageUrl,ButtonText,ButtonUrl,CouponCode,IsEnabled,TriggerDelay,UpdatedAt
    FROM dbo.Popups ORDER BY PopupType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Popup_Save
    @PopupType    NVARCHAR(50),
    @Heading      NVARCHAR(200) = NULL,
    @Subheading   NVARCHAR(400) = NULL,
    @ImageUrl     NVARCHAR(1000) = NULL,
    @ButtonText   NVARCHAR(100) = NULL,
    @ButtonUrl    NVARCHAR(500) = NULL,
    @CouponCode   NVARCHAR(50) = NULL,
    @IsEnabled    BIT = 0,
    @TriggerDelay INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Popups WHERE PopupType=@PopupType)
        UPDATE dbo.Popups SET Heading=@Heading,Subheading=@Subheading,ImageUrl=@ImageUrl,
               ButtonText=@ButtonText,ButtonUrl=@ButtonUrl,CouponCode=@CouponCode,
               IsEnabled=@IsEnabled,TriggerDelay=@TriggerDelay,UpdatedAt=SYSUTCDATETIME()
        WHERE PopupType=@PopupType;
    ELSE
        INSERT INTO dbo.Popups(PopupType,Heading,Subheading,ImageUrl,ButtonText,ButtonUrl,CouponCode,IsEnabled,TriggerDelay)
        VALUES(@PopupType,@Heading,@Subheading,@ImageUrl,@ButtonText,@ButtonUrl,@CouponCode,@IsEnabled,@TriggerDelay);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Popups_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PopupType,Heading,Subheading,ImageUrl,ButtonText,ButtonUrl,CouponCode,TriggerDelay
    FROM dbo.Popups WHERE IsEnabled=1;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 7. STORED PROCEDURES — EMAIL TEMPLATES
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_EmailTemplates_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,TemplateType,Subject,Body,IsEnabled,UpdatedAt FROM dbo.EmailTemplates ORDER BY TemplateType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmailTemplate_GetByType
    @TemplateType NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,TemplateType,Subject,Body,IsEnabled,UpdatedAt FROM dbo.EmailTemplates WHERE TemplateType=@TemplateType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmailTemplate_Save
    @TemplateType NVARCHAR(50),
    @Subject      NVARCHAR(200),
    @Body         NVARCHAR(MAX),
    @IsEnabled    BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.EmailTemplates WHERE TemplateType=@TemplateType)
        UPDATE dbo.EmailTemplates SET Subject=@Subject,Body=@Body,IsEnabled=@IsEnabled,UpdatedAt=SYSUTCDATETIME()
        WHERE TemplateType=@TemplateType;
    ELSE
        INSERT INTO dbo.EmailTemplates(TemplateType,Subject,Body,IsEnabled)
        VALUES(@TemplateType,@Subject,@Body,@IsEnabled);
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 8. STORED PROCEDURES — CMS COLLECTIONS
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_CmsCollections_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Name,Slug,BannerUrl,ImageUrl,Description,SeoTitle,SeoDesc,DisplayOrder,IsActive,CreatedAt,UpdatedAt
    FROM dbo.CmsCollections ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CmsCollections_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Name,Slug,BannerUrl,ImageUrl,Description,DisplayOrder
    FROM dbo.CmsCollections WHERE IsActive=1 ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CmsCollection_GetBySlug
    @Slug NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id,c.Name,c.Slug,c.BannerUrl,c.ImageUrl,c.Description,c.SeoTitle,c.SeoDesc,c.DisplayOrder,c.IsActive
    FROM dbo.CmsCollections c WHERE c.Slug=@Slug AND c.IsActive=1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CmsCollection_Save
    @Id          INT = NULL,
    @Name        NVARCHAR(100),
    @Slug        NVARCHAR(100),
    @BannerUrl   NVARCHAR(1000) = NULL,
    @ImageUrl    NVARCHAR(1000) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @SeoTitle    NVARCHAR(200) = NULL,
    @SeoDesc     NVARCHAR(500) = NULL,
    @DisplayOrder INT = 0,
    @IsActive    BIT = 1,
    @NewId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.CmsCollections(Name,Slug,BannerUrl,ImageUrl,Description,SeoTitle,SeoDesc,DisplayOrder,IsActive)
        VALUES(@Name,@Slug,@BannerUrl,@ImageUrl,@Description,@SeoTitle,@SeoDesc,@DisplayOrder,@IsActive);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.CmsCollections SET Name=@Name,Slug=@Slug,BannerUrl=@BannerUrl,ImageUrl=@ImageUrl,
               Description=@Description,SeoTitle=@SeoTitle,SeoDesc=@SeoDesc,
               DisplayOrder=@DisplayOrder,IsActive=@IsActive,UpdatedAt=SYSUTCDATETIME()
        WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CmsCollection_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.ProductCollections WHERE CollectionId=@Id;
    DELETE FROM dbo.CmsCollections WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 9. SEED DATA — V2
-- ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.WhyChooseUsItems)
BEGIN
    INSERT INTO dbo.WhyChooseUsItems(IconName,Title,Description,DisplayOrder,IsActive) VALUES
    ('Gem','Premium Quality','Every piece is crafted from high-grade materials that stand the test of time.',0,1),
    ('Shield','Safe & Secure','Shop with confidence. 100% secure checkout and data protection guaranteed.',1,1),
    ('Truck','Fast Delivery','Free shipping on orders above ₹999. Express delivery available for select pincodes.',2,1),
    ('RotateCcw','Easy Returns','Hassle-free 48-hour return policy. No questions asked.',3,1),
    ('Headphones','24/7 Support','Our team is always here to help you with any queries.',4,1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Popups)
BEGIN
    INSERT INTO dbo.Popups(PopupType,Heading,Subheading,ButtonText,CouponCode,IsEnabled,TriggerDelay) VALUES
    ('newsletter','Get 10% Off Your First Order','Subscribe to our newsletter for exclusive offers and new arrivals.','Subscribe Now','WELCOME10',0,5),
    ('offer','Limited Time Offer!','Use code SAVE20 to get 20% off on all earrings today only.','Shop Now','SAVE20',0,3),
    ('festival','Festival Season Sale','Up to 40% off on select jewellery. Limited stock available!','Explore Sale',NULL,0,2),
    ('exit-intent','Wait! Don''t Miss Out','You have items in your cart. Complete your order and get free shipping!','Complete Order',NULL,0,0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.EmailTemplates)
BEGIN
    INSERT INTO dbo.EmailTemplates(TemplateType,Subject,Body,IsEnabled) VALUES
    ('order-confirmation','Order Confirmed – #{OrderNumber}','<h2>Thank you for your order!</h2><p>Hi {{CustomerName}},</p><p>Your order <strong>#{OrderNumber}</strong> has been confirmed and is being prepared.</p><p><strong>Order Total:</strong> ₹{{OrderTotal}}</p><p>We will notify you once your order is shipped.</p><p>Team NIROTÉ</p>',1),
    ('order-shipped','Your Order Has Been Shipped – #{OrderNumber}','<h2>Your order is on its way!</h2><p>Hi {{CustomerName}},</p><p>Your order <strong>#{OrderNumber}</strong> has been shipped.</p><p><strong>Tracking Number:</strong> {{TrackingNumber}}</p><p>Expected delivery: {{ExpectedDelivery}}</p><p>Team NIROTÉ</p>',1),
    ('order-delivered','Order Delivered – #{OrderNumber}','<h2>Your order has been delivered!</h2><p>Hi {{CustomerName}},</p><p>Your order <strong>#{OrderNumber}</strong> has been delivered. We hope you love your new jewellery!</p><p>Please take a moment to leave a review.</p><p>Team NIROTÉ</p>',1),
    ('order-cancelled','Order Cancelled – #{OrderNumber}','<h2>Order Cancellation Confirmed</h2><p>Hi {{CustomerName}},</p><p>Your order <strong>#{OrderNumber}</strong> has been cancelled. Any payment made will be refunded within 5–7 business days.</p><p>Team NIROTÉ</p>',1),
    ('refund','Refund Processed – #{OrderNumber}','<h2>Refund Processed</h2><p>Hi {{CustomerName}},</p><p>Your refund of ₹{{RefundAmount}} for order <strong>#{OrderNumber}</strong> has been processed and will reflect in 5–7 business days.</p><p>Team NIROTÉ</p>',1),
    ('welcome','Welcome to NIROTÉ!','<h2>Welcome to NIROTÉ!</h2><p>Hi {{CustomerName}},</p><p>Thank you for creating an account. You now have access to order tracking, wish lists, and exclusive member offers.</p><p>Use code <strong>WELCOME10</strong> for 10% off your first order!</p><p>Team NIROTÉ</p>',1),
    ('password-reset','Reset Your NIROTÉ Password','<h2>Password Reset Request</h2><p>Hi {{CustomerName}},</p><p>We received a request to reset your password. Click the link below to set a new password:</p><p><a href="{{ResetLink}}">Reset Password</a></p><p>This link expires in 24 hours. If you did not request this, ignore this email.</p><p>Team NIROTÉ</p>',1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CmsCollections)
BEGIN
    INSERT INTO dbo.CmsCollections(Name,Slug,Description,DisplayOrder,IsActive) VALUES
    ('Festive Favourites','festive-favourites','Handpicked jewellery perfect for weddings, festivals, and celebrations.',0,1),
    ('Everyday Essentials','everyday-essentials','Lightweight, elegant pieces you can wear every day with ease.',1,1),
    ('Statement Pieces','statement-pieces','Bold, eye-catching jewellery that makes you stand out.',2,1),
    ('Gifting Edit','gifting-edit','Beautifully curated gift-ready sets for every occasion.',3,1);
END;
GO

PRINT 'CMS Migration v2 complete.';
GO
