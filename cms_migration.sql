-- ============================================================
-- Niroté CMS Migration — Full Content Management Layer
-- Run against NiroteDb
-- ============================================================
USE NiroteDb;
GO

-- ────────────────────────────────────────────────────────────────
-- 1. TABLES
-- ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NavItems')
CREATE TABLE dbo.NavItems (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Label        NVARCHAR(100) NOT NULL,
    Url          NVARCHAR(500) NOT NULL DEFAULT '#',
    ParentId     INT NULL REFERENCES dbo.NavItems(Id),
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsEnabled    BIT NOT NULL DEFAULT 1,
    OpenInNewTab BIT NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Banners')
CREATE TABLE dbo.Banners (
    Id             INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Badge          NVARCHAR(100) NULL,
    Heading        NVARCHAR(300) NOT NULL,
    Subheading     NVARCHAR(600) NULL,
    ImageUrl       NVARCHAR(1000) NULL,
    Btn1Text       NVARCHAR(100) NULL,
    Btn1Url        NVARCHAR(500) NULL DEFAULT '/products',
    Btn2Text       NVARCHAR(100) NULL,
    Btn2Url        NVARCHAR(500) NULL DEFAULT '/products/sortBy/newest',
    IsActive       BIT NOT NULL DEFAULT 1,
    ScheduledStart DATETIME2 NULL,
    ScheduledEnd   DATETIME2 NULL,
    DisplayOrder   INT NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt      DATETIME2 NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Testimonials')
CREATE TABLE dbo.Testimonials (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    AuthorName   NVARCHAR(100) NOT NULL,
    AuthorCity   NVARCHAR(100) NULL,
    AuthorImage  NVARCHAR(500) NULL,
    Text         NVARCHAR(1000) NOT NULL,
    Rating       INT NOT NULL DEFAULT 5,
    IsActive     BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HomeSections')
CREATE TABLE dbo.HomeSections (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    SectionKey   NVARCHAR(50) NOT NULL UNIQUE,
    Heading      NVARCHAR(300) NULL,
    Subheading   NVARCHAR(600) NULL,
    CtaText      NVARCHAR(100) NULL,
    CtaUrl       NVARCHAR(500) NULL,
    IsEnabled    BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    UpdatedAt    DATETIME2 NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FaqCategories')
CREATE TABLE dbo.FaqCategories (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FaqItems')
CREATE TABLE dbo.FaqItems (
    Id           INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CategoryId   INT NOT NULL REFERENCES dbo.FaqCategories(Id),
    Question     NVARCHAR(500) NOT NULL,
    Answer       NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PolicyPages')
CREATE TABLE dbo.PolicyPages (
    Id        INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Slug      NVARCHAR(50) NOT NULL UNIQUE,
    Title     NVARCHAR(200) NOT NULL,
    Sections  NVARCHAR(MAX) NOT NULL DEFAULT '[]',
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SeoPages')
CREATE TABLE dbo.SeoPages (
    Id          INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    PageKey     NVARCHAR(100) NOT NULL UNIQUE,
    MetaTitle   NVARCHAR(200) NULL,
    MetaDesc    NVARCHAR(500) NULL,
    Keywords    NVARCHAR(500) NULL,
    OgImage     NVARCHAR(1000) NULL,
    UpdatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ────────────────────────────────────────────────────────────────
-- 2. STORED PROCEDURES — NAV
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Nav_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Label, Url, ParentId, DisplayOrder, IsEnabled, OpenInNewTab
    FROM dbo.NavItems ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Nav_Save
    @Id           INT = NULL,
    @Label        NVARCHAR(100),
    @Url          NVARCHAR(500),
    @ParentId     INT = NULL,
    @DisplayOrder INT = 0,
    @IsEnabled    BIT = 1,
    @OpenInNewTab BIT = 0,
    @NewId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.NavItems (Label,Url,ParentId,DisplayOrder,IsEnabled,OpenInNewTab)
        VALUES (@Label,@Url,@ParentId,@DisplayOrder,@IsEnabled,@OpenInNewTab);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.NavItems SET Label=@Label,Url=@Url,ParentId=@ParentId,
               DisplayOrder=@DisplayOrder,IsEnabled=@IsEnabled,OpenInNewTab=@OpenInNewTab
        WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Nav_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.NavItems WHERE ParentId = @Id;
    DELETE FROM dbo.NavItems WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Nav_Reorder
    @Id INT, @DisplayOrder INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.NavItems SET DisplayOrder = @DisplayOrder WHERE Id = @Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 3. STORED PROCEDURES — BANNERS
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Banners_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    SELECT Id,Badge,Heading,Subheading,ImageUrl,Btn1Text,Btn1Url,Btn2Text,Btn2Url,DisplayOrder
    FROM dbo.Banners
    WHERE IsActive = 1
      AND (ScheduledStart IS NULL OR ScheduledStart <= @Now)
      AND (ScheduledEnd   IS NULL OR ScheduledEnd   >= @Now)
    ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Banners_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Badge,Heading,Subheading,ImageUrl,Btn1Text,Btn1Url,Btn2Text,Btn2Url,
           IsActive,ScheduledStart,ScheduledEnd,DisplayOrder,CreatedAt,UpdatedAt
    FROM dbo.Banners ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Banners_Save
    @Id            INT = NULL,
    @Badge         NVARCHAR(100) = NULL,
    @Heading       NVARCHAR(300),
    @Subheading    NVARCHAR(600) = NULL,
    @ImageUrl      NVARCHAR(1000) = NULL,
    @Btn1Text      NVARCHAR(100) = NULL,
    @Btn1Url       NVARCHAR(500) = NULL,
    @Btn2Text      NVARCHAR(100) = NULL,
    @Btn2Url       NVARCHAR(500) = NULL,
    @IsActive      BIT = 1,
    @ScheduledStart DATETIME2 = NULL,
    @ScheduledEnd   DATETIME2 = NULL,
    @DisplayOrder  INT = 0,
    @NewId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.Banners(Badge,Heading,Subheading,ImageUrl,Btn1Text,Btn1Url,Btn2Text,Btn2Url,IsActive,ScheduledStart,ScheduledEnd,DisplayOrder)
        VALUES(@Badge,@Heading,@Subheading,@ImageUrl,@Btn1Text,@Btn1Url,@Btn2Text,@Btn2Url,@IsActive,@ScheduledStart,@ScheduledEnd,@DisplayOrder);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Banners SET Badge=@Badge,Heading=@Heading,Subheading=@Subheading,
            ImageUrl=@ImageUrl,Btn1Text=@Btn1Text,Btn1Url=@Btn1Url,Btn2Text=@Btn2Text,Btn2Url=@Btn2Url,
            IsActive=@IsActive,ScheduledStart=@ScheduledStart,ScheduledEnd=@ScheduledEnd,
            DisplayOrder=@DisplayOrder,UpdatedAt=SYSUTCDATETIME()
        WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Banners_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Banners WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 4. STORED PROCEDURES — HOME SECTIONS
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_HomeSections_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,SectionKey,Heading,Subheading,CtaText,CtaUrl,IsEnabled,DisplayOrder
    FROM dbo.HomeSections ORDER BY DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HomeSections_Save
    @SectionKey  NVARCHAR(50),
    @Heading     NVARCHAR(300) = NULL,
    @Subheading  NVARCHAR(600) = NULL,
    @CtaText     NVARCHAR(100) = NULL,
    @CtaUrl      NVARCHAR(500) = NULL,
    @IsEnabled   BIT = 1,
    @DisplayOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.HomeSections WHERE SectionKey=@SectionKey)
        UPDATE dbo.HomeSections SET Heading=@Heading,Subheading=@Subheading,CtaText=@CtaText,
               CtaUrl=@CtaUrl,IsEnabled=@IsEnabled,DisplayOrder=@DisplayOrder,UpdatedAt=SYSUTCDATETIME()
        WHERE SectionKey=@SectionKey;
    ELSE
        INSERT INTO dbo.HomeSections(SectionKey,Heading,Subheading,CtaText,CtaUrl,IsEnabled,DisplayOrder)
        VALUES(@SectionKey,@Heading,@Subheading,@CtaText,@CtaUrl,@IsEnabled,@DisplayOrder);
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 5. STORED PROCEDURES — TESTIMONIALS
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Testimonials_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,AuthorName,AuthorCity,AuthorImage,Text,Rating,DisplayOrder
    FROM dbo.Testimonials WHERE IsActive=1 ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Testimonials_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,AuthorName,AuthorCity,AuthorImage,Text,Rating,IsActive,DisplayOrder,CreatedAt
    FROM dbo.Testimonials ORDER BY DisplayOrder ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Testimonials_Save
    @Id          INT = NULL,
    @AuthorName  NVARCHAR(100),
    @AuthorCity  NVARCHAR(100) = NULL,
    @AuthorImage NVARCHAR(500) = NULL,
    @Text        NVARCHAR(1000),
    @Rating      INT = 5,
    @IsActive    BIT = 1,
    @DisplayOrder INT = 0,
    @NewId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.Testimonials(AuthorName,AuthorCity,AuthorImage,Text,Rating,IsActive,DisplayOrder)
        VALUES(@AuthorName,@AuthorCity,@AuthorImage,@Text,@Rating,@IsActive,@DisplayOrder);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Testimonials SET AuthorName=@AuthorName,AuthorCity=@AuthorCity,AuthorImage=@AuthorImage,
               Text=@Text,Rating=@Rating,IsActive=@IsActive,DisplayOrder=@DisplayOrder
        WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Testimonials_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Testimonials WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 6. STORED PROCEDURES — FAQ
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Faq_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Name,DisplayOrder,IsActive FROM dbo.FaqCategories WHERE IsActive=1 ORDER BY DisplayOrder ASC;
    SELECT fi.Id,fi.CategoryId,fi.Question,fi.Answer,fi.DisplayOrder,fi.IsActive
    FROM dbo.FaqItems fi
    JOIN dbo.FaqCategories fc ON fi.CategoryId=fc.Id
    WHERE fi.IsActive=1 AND fc.IsActive=1
    ORDER BY fi.DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Faq_GetAllAdmin
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Name,DisplayOrder,IsActive FROM dbo.FaqCategories ORDER BY DisplayOrder ASC;
    SELECT Id,CategoryId,Question,Answer,DisplayOrder,IsActive,CreatedAt FROM dbo.FaqItems ORDER BY CategoryId ASC, DisplayOrder ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_FaqCategory_Save
    @Id          INT = NULL,
    @Name        NVARCHAR(100),
    @DisplayOrder INT = 0,
    @IsActive    BIT = 1,
    @NewId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.FaqCategories(Name,DisplayOrder,IsActive) VALUES(@Name,@DisplayOrder,@IsActive);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.FaqCategories SET Name=@Name,DisplayOrder=@DisplayOrder,IsActive=@IsActive WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_FaqCategory_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.FaqItems WHERE CategoryId=@Id;
    DELETE FROM dbo.FaqCategories WHERE Id=@Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_FaqItem_Save
    @Id           INT = NULL,
    @CategoryId   INT,
    @Question     NVARCHAR(500),
    @Answer       NVARCHAR(MAX),
    @DisplayOrder INT = 0,
    @IsActive     BIT = 1,
    @NewId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.FaqItems(CategoryId,Question,Answer,DisplayOrder,IsActive)
        VALUES(@CategoryId,@Question,@Answer,@DisplayOrder,@IsActive);
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.FaqItems SET CategoryId=@CategoryId,Question=@Question,Answer=@Answer,
               DisplayOrder=@DisplayOrder,IsActive=@IsActive WHERE Id=@Id;
        SET @NewId = @Id;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_FaqItem_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.FaqItems WHERE Id=@Id;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 7. STORED PROCEDURES — POLICY PAGES
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Policy_Get
    @Slug NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Slug,Title,Sections,UpdatedAt FROM dbo.PolicyPages WHERE Slug=@Slug;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Policy_Save
    @Slug     NVARCHAR(50),
    @Title    NVARCHAR(200),
    @Sections NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.PolicyPages WHERE Slug=@Slug)
        UPDATE dbo.PolicyPages SET Title=@Title,Sections=@Sections,UpdatedAt=SYSUTCDATETIME() WHERE Slug=@Slug;
    ELSE
        INSERT INTO dbo.PolicyPages(Slug,Title,Sections) VALUES(@Slug,@Title,@Sections);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Policy_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Slug,Title,UpdatedAt FROM dbo.PolicyPages ORDER BY Slug;
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 8. STORED PROCEDURES — SEO
-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_Seo_Get
    @PageKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PageKey,MetaTitle,MetaDesc,Keywords,OgImage,UpdatedAt FROM dbo.SeoPages WHERE PageKey=@PageKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Seo_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PageKey,MetaTitle,MetaDesc,Keywords,OgImage,UpdatedAt FROM dbo.SeoPages ORDER BY PageKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Seo_Save
    @PageKey  NVARCHAR(100),
    @MetaTitle NVARCHAR(200) = NULL,
    @MetaDesc  NVARCHAR(500) = NULL,
    @Keywords  NVARCHAR(500) = NULL,
    @OgImage   NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.SeoPages WHERE PageKey=@PageKey)
        UPDATE dbo.SeoPages SET MetaTitle=@MetaTitle,MetaDesc=@MetaDesc,Keywords=@Keywords,OgImage=@OgImage,UpdatedAt=SYSUTCDATETIME()
        WHERE PageKey=@PageKey;
    ELSE
        INSERT INTO dbo.SeoPages(PageKey,MetaTitle,MetaDesc,Keywords,OgImage) VALUES(@PageKey,@MetaTitle,@MetaDesc,@Keywords,@OgImage);
END;
GO

-- ────────────────────────────────────────────────────────────────
-- 9. SEED DATA
-- ────────────────────────────────────────────────────────────────

-- Nav Items
IF NOT EXISTS (SELECT 1 FROM dbo.NavItems)
BEGIN
    INSERT INTO dbo.NavItems(Label,Url,ParentId,DisplayOrder,IsEnabled,OpenInNewTab) VALUES
    ('Home','/',NULL,0,1,0),
    ('Shop','/products',NULL,1,1,0),
    ('New Arrivals','/products/sortBy/newest',NULL,2,1,0),
    ('Collections','/collections',NULL,3,1,0),
    ('About','/about',NULL,4,1,0),
    ('Contact','/contact',NULL,5,1,0);
END;
GO

-- Home Sections
IF NOT EXISTS (SELECT 1 FROM dbo.HomeSections)
BEGIN
    INSERT INTO dbo.HomeSections(SectionKey,Heading,Subheading,CtaText,CtaUrl,IsEnabled,DisplayOrder) VALUES
    ('announcement',NULL,NULL,NULL,NULL,1,0),
    ('hero',NULL,NULL,NULL,NULL,1,1),
    ('categories','Shop by Style','Discover our curated collections','View All','/products',1,2),
    ('featured','Best Sellers','Our most loved pieces','View All','/products?featured=true',1,3),
    ('new-arrivals','New Arrivals','Fresh from our workshop','View All','/products/sortBy/newest',1,4),
    ('why-us','Why Choose NIROTÉ',NULL,NULL,NULL,1,5),
    ('testimonials','What Customers Say',NULL,NULL,NULL,1,6),
    ('newsletter','Join the NIROTÉ Circle','Be first to hear about new arrivals and exclusive offers.',NULL,NULL,1,7);
END;
GO

-- Default banner
IF NOT EXISTS (SELECT 1 FROM dbo.Banners)
BEGIN
    INSERT INTO dbo.Banners(Badge,Heading,Subheading,ImageUrl,Btn1Text,Btn1Url,Btn2Text,Btn2Url,IsActive,DisplayOrder)
    VALUES(
        'New Arrivals 2026',
        'Premium Earrings For Every Occasion',
        'Discover our exclusive collection of handcrafted earrings. From everyday studs to statement chandbalis, find your perfect pair.',
        NULL,
        'Shop Earrings','/products',
        'New Arrivals','/products/sortBy/newest',
        1,0
    );
END;
GO

-- Default testimonials
IF NOT EXISTS (SELECT 1 FROM dbo.Testimonials)
BEGIN
    INSERT INTO dbo.Testimonials(AuthorName,AuthorCity,Text,Rating,IsActive,DisplayOrder) VALUES
    ('Priya Sharma','Mumbai','Absolutely gorgeous earrings! The craftsmanship is exceptional and they arrived beautifully packaged. Will definitely order again.',5,1,0),
    ('Ananya Reddy','Bangalore','I bought the chandbali earrings for my sister''s wedding and everyone complimented them. NIROTÉ quality is unmatched.',5,1,1),
    ('Meera Patel','Ahmedabad','Fast shipping, lovely packaging, and the earrings are even more beautiful in person. Highly recommend!',5,1,2);
END;
GO

-- Default FAQ
IF NOT EXISTS (SELECT 1 FROM dbo.FaqCategories)
BEGIN
    INSERT INTO dbo.FaqCategories(Name,DisplayOrder,IsActive) VALUES
    ('Orders & Shipping',0,1),('Returns & Exchanges',1,1),('Products',2,1),('Payments',3,1),('Account',4,1);

    DECLARE @OrdId INT = (SELECT Id FROM dbo.FaqCategories WHERE Name='Orders & Shipping');
    DECLARE @RetId INT = (SELECT Id FROM dbo.FaqCategories WHERE Name='Returns & Exchanges');
    DECLARE @PrdId INT = (SELECT Id FROM dbo.FaqCategories WHERE Name='Products');
    DECLARE @PayId INT = (SELECT Id FROM dbo.FaqCategories WHERE Name='Payments');
    DECLARE @AccId INT = (SELECT Id FROM dbo.FaqCategories WHERE Name='Account');

    INSERT INTO dbo.FaqItems(CategoryId,Question,Answer,DisplayOrder,IsActive) VALUES
    (@OrdId,'How long does delivery take?','Standard delivery takes 5–7 business days. Express delivery (3–5 days) is available at checkout for select pincodes.',0,1),
    (@OrdId,'How do I track my order?','Once your order is shipped, you will receive an email and SMS with your tracking number. You can track your order on our Track Order page.',1,1),
    (@OrdId,'Do you offer free shipping?','Yes! We offer free shipping on all orders above ₹999.',2,1),
    (@RetId,'What is your return policy?','We accept returns within 48 hours of delivery. Items must be unused and in original packaging.',0,1),
    (@RetId,'How do I initiate a return?','Login to your account, go to Orders, select the item and click ''Request Return''. Our team will respond within 24 hours.',1,1),
    (@PrdId,'Are the earrings hypoallergenic?','Our gold-plated and 925 sterling silver earrings are suitable for sensitive ears. We recommend our sterling silver range for extra sensitivity.',0,1),
    (@PrdId,'How do I care for my jewellery?','Store in a cool, dry place away from sunlight. Avoid contact with water, perfume, and lotions. Clean gently with a soft cloth.',1,1),
    (@PayId,'What payment methods do you accept?','We accept all major credit/debit cards, UPI, net banking, and cash on delivery via Razorpay.',0,1),
    (@PayId,'Is it safe to pay online?','Yes. All transactions are secured with 256-bit SSL encryption through Razorpay, a trusted payment gateway.',1,1),
    (@AccId,'Do I need an account to order?','You can browse without an account, but you need to log in or register to place an order and track it.',0,1),
    (@AccId,'How do I reset my password?','On the login page, click ''Forgot Password'' and we will send a reset link to your registered email.',1,1);
END;
GO

-- Default policy pages
IF NOT EXISTS (SELECT 1 FROM dbo.PolicyPages)
BEGIN
    INSERT INTO dbo.PolicyPages(Slug,Title,Sections) VALUES
    ('shipping','Shipping Policy','[{"title":"Overview","body":"We offer reliable shipping across India. Most orders are dispatched within 24–48 hours of order confirmation."},{"title":"Delivery Timeframes","body":"Standard Delivery: 5–7 business days\nExpress Delivery: 3–5 business days (select pincodes)\nSame-Day Delivery: Available in select metro cities for orders placed before 12 PM."},{"title":"Shipping Charges","body":"Free shipping on orders above ₹999.\nStandard shipping: ₹99 for orders below ₹999.\nExpress shipping: ₹149 (charged at checkout)."},{"title":"Order Tracking","body":"Once your order is shipped, you will receive a tracking number via email and SMS. Use our Track Order page to follow your delivery in real time."},{"title":"International Shipping","body":"We currently ship within India only. International shipping is coming soon — sign up to our newsletter to be notified."}]'),
    ('returns','Return & Exchange Policy','[{"title":"Return Window","body":"We accept returns within 48 hours of delivery. This tight window ensures our products remain in pristine condition for all customers."},{"title":"Eligible Items","body":"Items eligible for return must be unused, unworn and in their original packaging with all tags intact."},{"title":"Non-Returnable Items","body":"Sale items, earrings worn even once (for hygiene reasons), custom orders, and items without original packaging cannot be returned."},{"title":"How to Initiate a Return","body":"1. Log in to your account\n2. Go to My Orders\n3. Select the item and click Request Return\n4. Our team will review and respond within 24 hours\n5. Pack the item securely and hand it to our courier"},{"title":"Refund Timeline","body":"Once we receive and inspect your return, refunds are processed within 5–7 business days to your original payment method."}]'),
    ('privacy','Privacy Policy','[{"title":"Information We Collect","body":"We collect information you provide directly: name, email, phone number, delivery address, and payment information (processed securely by Razorpay)."},{"title":"How We Use Your Information","body":"To process and fulfill your orders\nTo send order updates and shipping notifications\nTo send promotional emails (you can unsubscribe anytime)\nTo improve our website and services"},{"title":"Information Sharing","body":"We do not sell, trade, or share your personal information with third parties except as required to fulfill your orders (shipping partners, payment processors) or as required by law."},{"title":"Data Security","body":"Your data is protected using industry-standard SSL encryption. Payment information is processed directly by Razorpay and never stored on our servers."},{"title":"Your Rights","body":"You have the right to access, update, or delete your personal information at any time. Contact us at privacy@nirote.com for any requests."},{"title":"Cookies","body":"We use cookies to improve your browsing experience. You can disable cookies in your browser settings, though some site features may not work correctly."},{"title":"Contact","body":"For privacy-related queries, contact: privacy@nirote.com"}]'),
    ('terms','Terms & Conditions','[{"title":"Acceptance of Terms","body":"By accessing and using the NIROTÉ website, you accept and agree to be bound by these terms and conditions."},{"title":"Products","body":"We reserve the right to modify, discontinue or change prices of products at any time without notice. Product images are for illustrative purposes; actual colours may vary slightly."},{"title":"Orders","body":"All orders are subject to product availability and acceptance. We reserve the right to refuse or cancel any order."},{"title":"Pricing","body":"All prices are in Indian Rupees (INR) and inclusive of applicable taxes unless stated otherwise."},{"title":"Payment","body":"Payment must be made at the time of order. We accept cards, UPI, net banking and cash on delivery through Razorpay."},{"title":"Shipping","body":"Delivery timelines are estimates and not guarantees. We are not liable for delays caused by courier services or circumstances beyond our control."},{"title":"Returns","body":"Returns are subject to our Return Policy. Products must be returned in original condition within the specified timeframe."},{"title":"Intellectual Property","body":"All content on this website including images, text, logos and designs are the property of NIROTÉ and may not be used without written permission."},{"title":"Limitation of Liability","body":"NIROTÉ shall not be liable for any indirect, incidental, or consequential damages arising from the use of our products or website."},{"title":"Governing Law","body":"These terms are governed by the laws of India. Any disputes shall be subject to the jurisdiction of courts in the relevant state."},{"title":"Contact","body":"For any queries regarding these terms, contact: legal@nirote.com"}]');
END;
GO

-- Default SEO
IF NOT EXISTS (SELECT 1 FROM dbo.SeoPages)
BEGIN
    INSERT INTO dbo.SeoPages(PageKey,MetaTitle,MetaDesc,Keywords) VALUES
    ('home','NIROTÉ — Premium Artificial Jewellery | Earrings, Necklaces & More','Shop handcrafted premium jewellery at NIROTÉ. Explore chandbalis, hoops, studs, and statement earrings.','earrings, jewellery, artificial jewellery, chandbali, hoops, studs, niroté'),
    ('products','Shop All Jewellery | NIROTÉ','Browse our complete collection of premium handcrafted jewellery. Filter by style, price and more.','shop earrings, buy jewellery online, artificial jewellery india'),
    ('about','About NIROTÉ | Our Story','Learn about NIROTÉ — a premium jewellery brand crafting beautiful pieces for the modern Indian woman.','about niroté, jewellery brand story, handcrafted jewellery'),
    ('contact','Contact Us | NIROTÉ','Get in touch with the NIROTÉ team. We are happy to help with orders, returns, and product queries.','contact niroté, customer service jewellery'),
    ('faq','Frequently Asked Questions | NIROTÉ','Find answers to common questions about orders, shipping, returns, and our products.','faq, help, jewellery questions');
END;
GO

PRINT 'CMS Migration complete.';
GO
