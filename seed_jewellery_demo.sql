USE NiroteDb;
GO

-- ============================================================
--  Niroté Demo Seed
--  Uses sp_Categories_Create / sp_Products_Create OUTPUT params
-- ============================================================
DECLARE @catEarrings   INT, @catNecklaces INT, @catBangles   INT,
        @catRings      INT, @catMaangTikka INT, @catAnklets  INT,
        @catNosePins   INT, @catSets       INT;

DECLARE @catJhumkas    INT, @catStuds      INT, @catChandbalis INT,
        @catHoops      INT, @catDrops      INT;

DECLARE @catChokers    INT, @catLongChains INT, @catPendants  INT;

DECLARE @p  INT;   -- product id
DECLARE @pi INT;   -- product image id

-- ═══════════════════════════════════════════════════════════════
--  PARENT CATEGORIES
-- ═══════════════════════════════════════════════════════════════
EXEC sp_Categories_Create
     @Name='Earrings',@Slug='earrings',
     @Description='Traditional and contemporary earring designs',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=1,@ShowOnHomepage=1,@NewCategoryId=@catEarrings OUTPUT;

EXEC sp_Categories_Create
     @Name='Necklaces',@Slug='necklaces',
     @Description='Elegant necklaces for every occasion',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=400',
     @DisplayOrder=2,@ShowOnHomepage=1,@NewCategoryId=@catNecklaces OUTPUT;

EXEC sp_Categories_Create
     @Name='Bangles',@Slug='bangles',
     @Description='Beautiful bangles and bracelets',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1611085583191-a3b181a88401?w=400',
     @DisplayOrder=3,@ShowOnHomepage=1,@NewCategoryId=@catBangles OUTPUT;

EXEC sp_Categories_Create
     @Name='Rings',@Slug='rings',
     @Description='Statement rings and everyday bands',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=400',
     @DisplayOrder=4,@ShowOnHomepage=1,@NewCategoryId=@catRings OUTPUT;

EXEC sp_Categories_Create
     @Name='Maang Tikka',@Slug='maang-tikka',
     @Description='Bridal and festive head jewellery',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1589128777073-263566ae5e4d?w=400',
     @DisplayOrder=5,@ShowOnHomepage=1,@NewCategoryId=@catMaangTikka OUTPUT;

EXEC sp_Categories_Create
     @Name='Anklets',@Slug='anklets',
     @Description='Delicate and ornate ankle chains',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1630019925419-5cf4a3bedc2f?w=400',
     @DisplayOrder=6,@ShowOnHomepage=0,@NewCategoryId=@catAnklets OUTPUT;

EXEC sp_Categories_Create
     @Name='Nose Pins',@Slug='nose-pins',
     @Description='Traditional and modern nose jewellery',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=7,@ShowOnHomepage=0,@NewCategoryId=@catNosePins OUTPUT;

EXEC sp_Categories_Create
     @Name='Jewellery Sets',@Slug='jewellery-sets',
     @Description='Coordinated necklace and earring sets',
     @ParentId=NULL,
     @ImageUrl='https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=400',
     @DisplayOrder=8,@ShowOnHomepage=1,@NewCategoryId=@catSets OUTPUT;

-- ═══════════════════════════════════════════════════════════════
--  SUB-CATEGORIES  (Earrings)
-- ═══════════════════════════════════════════════════════════════
EXEC sp_Categories_Create
     @Name='Jhumkas',@Slug='jhumkas',
     @Description='Classic bell-shaped Jhumka earrings',
     @ParentId=@catEarrings,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=1,@ShowOnHomepage=0,@NewCategoryId=@catJhumkas OUTPUT;

EXEC sp_Categories_Create
     @Name='Studs',@Slug='studs',
     @Description='Minimalist stud earrings for everyday wear',
     @ParentId=@catEarrings,
     @ImageUrl='https://images.unsplash.com/photo-1635767798638-3e25273a8236?w=400',
     @DisplayOrder=2,@ShowOnHomepage=0,@NewCategoryId=@catStuds OUTPUT;

EXEC sp_Categories_Create
     @Name='Chandbalis',@Slug='chandbalis',
     @Description='Moon-shaped chandelier earrings',
     @ParentId=@catEarrings,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=3,@ShowOnHomepage=0,@NewCategoryId=@catChandbalis OUTPUT;

EXEC sp_Categories_Create
     @Name='Hoops',@Slug='hoops',
     @Description='Contemporary hoop earrings',
     @ParentId=@catEarrings,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=4,@ShowOnHomepage=0,@NewCategoryId=@catHoops OUTPUT;

EXEC sp_Categories_Create
     @Name='Drop Earrings',@Slug='drop-earrings',
     @Description='Elegant dangling drop earrings',
     @ParentId=@catEarrings,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=400',
     @DisplayOrder=5,@ShowOnHomepage=0,@NewCategoryId=@catDrops OUTPUT;

-- ═══════════════════════════════════════════════════════════════
--  SUB-CATEGORIES  (Necklaces)
-- ═══════════════════════════════════════════════════════════════
EXEC sp_Categories_Create
     @Name='Chokers',@Slug='chokers',
     @Description='Trendy close-fit choker necklaces',
     @ParentId=@catNecklaces,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=400',
     @DisplayOrder=1,@ShowOnHomepage=0,@NewCategoryId=@catChokers OUTPUT;

EXEC sp_Categories_Create
     @Name='Long Chains',@Slug='long-chains',
     @Description='Statement long-chain necklaces',
     @ParentId=@catNecklaces,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=400',
     @DisplayOrder=2,@ShowOnHomepage=0,@NewCategoryId=@catLongChains OUTPUT;

EXEC sp_Categories_Create
     @Name='Pendants',@Slug='pendants',
     @Description='Beautiful pendant necklaces',
     @ParentId=@catNecklaces,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=400',
     @DisplayOrder=3,@ShowOnHomepage=0,@NewCategoryId=@catPendants OUTPUT;

-- ═══════════════════════════════════════════════════════════════
--  PRODUCTS  +  IMAGES
--  sp_Products_Create params: @Name,@Description,@Price,
--    @DiscountedPrice,@StockQuantity,@Fabric,@Color,@State,
--    @HasBlousePiece,@CareInstructions,@DeliveryDays,
--    @CategoryId,@NewProductId OUTPUT
-- ═══════════════════════════════════════════════════════════════

-- Jhumkas ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Kundan Jhumka Earrings',
     @Description='Hand-crafted Kundan Jhumkas with intricate gold filigree. Perfect for weddings and festive occasions.',
     @Price=1299,@StockQuantity=50,@Color='Gold',@Occasion='Wedding',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catJhumkas,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Pearl Drop Jhumkas',
     @Description='Lustrous freshwater pearls set in antique silver Jhumka frames. Timeless elegance.',
     @Price=899,@StockQuantity=35,@Color='Silver',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catJhumkas,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1611591437281-460bfbe1220a?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Meenakari Jhumka Earrings',
     @Description='Vibrant Meenakari enamel work Jhumkas in royal blue and gold. A celebration of Indian artistry.',
     @Price=1599,@StockQuantity=20,@Color='Blue',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catJhumkas,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Studs ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Diamond Cut Gold Studs',
     @Description='Classic diamond-cut gold studs in 22K gold plating. Perfect for everyday elegance.',
     @Price=799,@StockQuantity=80,@Color='Gold',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catStuds,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1635767798638-3e25273a8236?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Ruby Red Stone Studs',
     @Description='Vibrant synthetic ruby studs in a gold-plated setting. Bold and beautiful.',
     @Price=649,@StockQuantity=45,@Color='Red',@Occasion='Party',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catStuds,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1635767798638-3e25273a8236?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Chandbalis ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Gold Chandbali Earrings',
     @Description='Traditional crescent-shaped Chandbali earrings with pearl drops. Bridal beauty.',
     @Price=2199,@StockQuantity=15,@Color='Gold',@Occasion='Bridal',
     @HasBlousePiece=0,@DeliveryDays=7,@CategoryId=@catChandbalis,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Antique Silver Chandbali',
     @Description='Oxidised silver Chandbalis with turquoise inlay. Bohemian chic meets traditional design.',
     @Price=1499,@StockQuantity=25,@Color='Silver',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catChandbalis,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1630019852942-f89202989a59?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Chokers ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Kundan Choker Necklace',
     @Description='Opulent Kundan choker with layered floral motifs. The centrepiece of any bridal look.',
     @Price=3499,@StockQuantity=10,@Color='Gold',@Occasion='Bridal',
     @HasBlousePiece=0,@DeliveryDays=7,@CategoryId=@catChokers,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Velvet Thread Choker',
     @Description='Contemporary black velvet choker with gold coin pendants. Minimalist and modern.',
     @Price=799,@StockQuantity=60,@Color='Black',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catChokers,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Pendants ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Om Pendant Necklace',
     @Description='Spiritual Om symbol pendant in sterling silver on a delicate chain.',
     @Price=1099,@StockQuantity=40,@Color='Silver',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catPendants,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Peacock Pendant Necklace',
     @Description='Intricate peacock motif pendant with enamel detailing. A true work of art.',
     @Price=1799,@StockQuantity=20,@Color='Multi',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catPendants,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Bangles ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Lac Bangles Set of 6',
     @Description='Traditional Rajasthani lac bangles in vibrant colours. Set of 6 assorted bangles.',
     @Price=899,@StockQuantity=30,@Color='Multi',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catBangles,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1611085583191-a3b181a88401?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Antique Gold Kada',
     @Description='Heavy antique gold-plated Kada bangle with engraved floral patterns. Heirloom quality.',
     @Price=2499,@StockQuantity=15,@Color='Gold',@Occasion='Wedding',
     @HasBlousePiece=0,@DeliveryDays=7,@CategoryId=@catBangles,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1611085583191-a3b181a88401?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Kundan Stone Bangle Set',
     @Description='Pair of Kundan stone-studded gold bangles. Perfect for festivities.',
     @Price=1899,@StockQuantity=20,@Color='Gold',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catBangles,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1611085583191-a3b181a88401?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Rings ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Adjustable Floral Ring',
     @Description='Delicate flower-shaped adjustable ring in gold plating. One size fits all.',
     @Price=499,@StockQuantity=100,@Color='Gold',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catRings,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Emerald Stone Cocktail Ring',
     @Description='Bold cocktail ring with oversized synthetic emerald in a gold setting.',
     @Price=1299,@StockQuantity=25,@Color='Green',@Occasion='Party',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catRings,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Maang Tikka ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Kundan Bridal Maang Tikka',
     @Description='Elaborate Kundan Maang Tikka with cascading pearl chains. The ultimate bridal accessory.',
     @Price=2999,@StockQuantity=8,@Color='Gold',@Occasion='Bridal',
     @HasBlousePiece=0,@DeliveryDays=7,@CategoryId=@catMaangTikka,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1589128777073-263566ae5e4d?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Simple Gold Tikka',
     @Description='Minimalist gold Maang Tikka for everyday Indian wear. Lightweight and comfortable.',
     @Price=799,@StockQuantity=40,@Color='Gold',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catMaangTikka,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1589128777073-263566ae5e4d?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- Jewellery Sets ---------------------------------------------------
EXEC sp_Products_Create
     @Name='Bridal Kundan Set',
     @Description='Complete 5-piece bridal set: necklace, earrings, maang tikka, bangles and ring. Pure opulence.',
     @Price=8999,@StockQuantity=5,@Color='Gold',@Occasion='Bridal',
     @HasBlousePiece=0,@DeliveryDays=10,@CategoryId=@catSets,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Festive Gold Set',
     @Description='3-piece festive set with necklace and matching jhumka earrings. Ready to celebrate.',
     @Price=3499,@StockQuantity=12,@Color='Gold',@Occasion='Festival',
     @HasBlousePiece=0,@DeliveryDays=7,@CategoryId=@catSets,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

EXEC sp_Products_Create
     @Name='Pearl Elegance Set',
     @Description='Freshwater pearl necklace with matching stud earrings. Understated luxury.',
     @Price=2199,@StockQuantity=18,@Color='White',@Occasion='Casual',
     @HasBlousePiece=0,@DeliveryDays=5,@CategoryId=@catSets,@NewProductId=@p OUTPUT;
EXEC sp_ProductImages_Add @ProductId=@p,
     @ImageUrl='https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=600',
     @IsDefault=1,@DisplayOrder=1,@NewImageId=@pi OUTPUT;

-- ═══════════════════════════════════════════════════════════════
--  SETTINGS
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.Settings ([Key], [Value]) VALUES
  ('BannerImage',         'https://images.unsplash.com/photo-1630019852942-f89202989a59?w=1600'),
  ('BannerHeading',       'Timeless Jewellery, Crafted with Soul'),
  ('BannerSubheading',    'Discover our collection of handcrafted Indian jewellery'),
  ('OfferStrip1',         'Free shipping on orders above Rs.999'),
  ('OfferStrip2',         'Use code NIROTE10 for 10% off your first order'),
  ('OfferStrip3',         'New arrivals every week - explore the collection'),
  ('MarqueeText',         'Handcrafted Jewellery | Free Shipping Above Rs.999 | New Arrivals Weekly | Use Code NIROTE10 for 10% Off | Bridal Collections Available'),
  ('PromoBannerHeading',  'Festive Sale - Up to 30% Off'),
  ('PromoBannerDesc',     'Celebrate in style with our curated festive collection. Limited time offer.'),
  ('PromoBannerDiscount', '30'),
  ('PromoBannerLink',     '/products?occasion=Festival');

-- ═══════════════════════════════════════════════════════════════
--  DEMO COUPON
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.Coupons
  (Code, Description, DiscountType, DiscountValue, MinCartAmount, MaxDiscount,
   StartDate, EndDate, UsageLimit, IsActive, FestivalName)
VALUES
  ('NIROTE10', 'Welcome 10% off on your first order', 'Percentage', 10, 500, 300,
   GETUTCDATE(), DATEADD(MONTH, 6, GETUTCDATE()), 500, 1, NULL);

SELECT COUNT(*) AS Categories FROM dbo.Categories;
SELECT COUNT(*) AS Products FROM dbo.Products;
SELECT COUNT(*) AS ProductImages FROM dbo.ProductImages;
SELECT COUNT(*) AS Settings FROM dbo.Settings;
SELECT COUNT(*) AS Coupons FROM dbo.Coupons;
PRINT 'Nirote seed complete.';
GO
