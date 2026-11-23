using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;

namespace SmartStock.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Deterministic: Only seed if demo business doesn't exist
        var demoBusinessExists = await context.Businesses.AnyAsync(b => b.BusinessName == "SmartStock Demo Store");
        if (demoBusinessExists)
        {
            return;
        }

        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var business = new Business
        {
            Id = businessId,
            BusinessName = "SmartStock Demo Store",
            OwnerName = "Ramesh Sharma",
            Phone = "+91 98765 43210",
            Address = "Shop No. 12, Main Market, Connaught Place, New Delhi",
            BusinessType = "Kirana",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        };
        context.Businesses.Add(business);

        // Password: Password123!
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
        var user = new User
        {
            Id = userId,
            BusinessId = businessId,
            FullName = "Ramesh Sharma",
            Email = "demo@smartstock.in",
            PasswordHash = passwordHash,
            Role = UserRole.Owner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        };
        context.Users.Add(user);

        // Suppliers
        var supMetro = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = "Metro Cash & Carry India",
            ContactPerson = "Anil Verma",
            Phone = "+91 98111 22334",
            Email = "metro.delhi@metrocash.in",
            Address = "Plot 14, Okhla Phase 1, New Delhi",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-38)
        };

        var supHul = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = "Hindustan Unilever Distributor",
            ContactPerson = "Sunil Gupta",
            Phone = "+91 98222 33445",
            Email = "sales@huldistributor.com",
            Address = "Godown 4, Kirti Nagar, New Delhi",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-38)
        };

        var supAmul = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = "Delhi Milk Union (Amul Supply)",
            ContactPerson = "Vikas Yadav",
            Phone = "+91 98333 44556",
            Email = "orders@amulsupplydelhi.com",
            Address = "Dairy Complex, Patparganj, New Delhi",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-38)
        };

        context.Suppliers.AddRange(supMetro, supHul, supAmul);

        // Categories
        var catGrocery = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Grocery", Description = "Daily staples, grains, flour, sugar and edible oils", CreatedAt = DateTime.UtcNow.AddDays(-38) };
        var catBeverages = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Beverages", Description = "Tea, coffee, packaged juices and soft drinks", CreatedAt = DateTime.UtcNow.AddDays(-38) };
        var catDairy = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Dairy", Description = "Fresh milk, butter, paneer, curd and cheese", CreatedAt = DateTime.UtcNow.AddDays(-38) };
        var catPersonalCare = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Personal Care", Description = "Soaps, shampoos, oral care and hair oil", CreatedAt = DateTime.UtcNow.AddDays(-38) };
        var catSnacks = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Snacks", Description = "Biscuits, chips, namkeen and instant noodles", CreatedAt = DateTime.UtcNow.AddDays(-38) };
        var catHousehold = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Household", Description = "Detergents, cleaners and dishwashing supplies", CreatedAt = DateTime.UtcNow.AddDays(-38) };

        context.Categories.AddRange(catGrocery, catBeverages, catDairy, catPersonalCare, catSnacks, catHousehold);

        // 36 Retail Products
        var products = new List<Product>
        {
            // Grocery
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Aashirvaad Shudh Chakki Atta 5kg", SKU = "GRO-ATT-001", Description = "Whole wheat flour", PurchasePrice = 210, SellingPrice = 245, CurrentStock = 45, MinimumStock = 15, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "India Gate Basmati Rice 5kg", SKU = "GRO-RIC-002", Description = "Premium long grain basmati rice", PurchasePrice = 420, SellingPrice = 490, CurrentStock = 30, MinimumStock = 10, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Tata Salt 1kg", SKU = "GRO-SLT-003", Description = "Vacuum evaporated iodized salt", PurchasePrice = 22, SellingPrice = 28, CurrentStock = 0, MinimumStock = 20, Unit = UnitType.Packet }, // OUT OF STOCK
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Fortune Sunlite Sunflower Oil 1L", SKU = "GRO-OIL-004", Description = "Refined sunflower oil pouch", PurchasePrice = 125, SellingPrice = 145, CurrentStock = 25, MinimumStock = 12, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Madhur Pure Sugar 1kg", SKU = "GRO-SUG-005", Description = "Sulphur-free refined sugar", PurchasePrice = 42, SellingPrice = 48, CurrentStock = 60, MinimumStock = 25, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Tata Sampann Toor Dal 1kg", SKU = "GRO-DAL-006", Description = "Unpolished Arhar / Toor Dal", PurchasePrice = 140, SellingPrice = 165, CurrentStock = 20, MinimumStock = 10, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Everest Turmeric Powder 100g", SKU = "GRO-SPIC-007", Description = "Pure turmeric ground powder", PurchasePrice = 28, SellingPrice = 36, CurrentStock = 35, MinimumStock = 15, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catGrocery.Id, Name = "Everest Coriander Powder 100g", SKU = "GRO-SPIC-008", Description = "Fragrant dhania powder", PurchasePrice = 30, SellingPrice = 38, CurrentStock = 40, MinimumStock = 15, Unit = UnitType.Packet },

            // Beverages
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Tata Tea Gold 250g", SKU = "BEV-TEA-001", Description = "Rich taste & aroma blend tea", PurchasePrice = 120, SellingPrice = 145, CurrentStock = 28, MinimumStock = 12, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Red Label Natural Care Tea 500g", SKU = "BEV-TEA-002", Description = "Tea with 5 Ayurvedic ingredients", PurchasePrice = 260, SellingPrice = 310, CurrentStock = 18, MinimumStock = 8, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Nescafe Classic Instant Coffee 50g", SKU = "BEV-COF-003", Description = "100% pure instant coffee jar", PurchasePrice = 155, SellingPrice = 185, CurrentStock = 15, MinimumStock = 8, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Frooti Mango Drink 1L", SKU = "BEV-JUC-004", Description = "Fresh 'N' Juicy mango drink", PurchasePrice = 55, SellingPrice = 70, CurrentStock = 22, MinimumStock = 10, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Coca-Cola 750ml", SKU = "BEV-SDA-005", Description = "Carbonated soft drink bottle", PurchasePrice = 32, SellingPrice = 40, CurrentStock = 30, MinimumStock = 15, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catBeverages.Id, Name = "Real Fruit Power Mixed Fruit Juice 1L", SKU = "BEV-JUC-006", Description = "Tetra pack mixed fruit juice", PurchasePrice = 95, SellingPrice = 120, CurrentStock = 14, MinimumStock = 8, Unit = UnitType.Piece },

            // Dairy
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catDairy.Id, Name = "Amul Butter 500g", SKU = "DRY-BUT-001", Description = "Pasteurized salted butter", PurchasePrice = 240, SellingPrice = 275, CurrentStock = 3, MinimumStock = 10, Unit = UnitType.Packet }, // LOW STOCK
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catDairy.Id, Name = "Amul Taaza Toned Milk 1L", SKU = "DRY-MLK-002", Description = "UHT treated fresh toned milk", PurchasePrice = 58, SellingPrice = 68, CurrentStock = 35, MinimumStock = 15, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catDairy.Id, Name = "Amul Malai Paneer 200g", SKU = "DRY-PAN-003", Description = "Soft and fresh paneer block", PurchasePrice = 78, SellingPrice = 92, CurrentStock = 5, MinimumStock = 10, Unit = UnitType.Packet }, // LOW STOCK
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catDairy.Id, Name = "Mother Dairy Dahi 400g", SKU = "DRY-CUR-004", Description = "Fresh and thick curd tub", PurchasePrice = 32, SellingPrice = 40, CurrentStock = 12, MinimumStock = 10, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catDairy.Id, Name = "Amul Cheese Slices 200g", SKU = "DRY-CHE-005", Description = "10 processed cheese slices", PurchasePrice = 120, SellingPrice = 140, CurrentStock = 16, MinimumStock = 8, Unit = UnitType.Packet },

            // Personal Care
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Dettol Original Bathing Soap 75g", SKU = "PC-SOP-001", Description = "Antibacterial protection soap", PurchasePrice = 30, SellingPrice = 38, CurrentStock = 50, MinimumStock = 20, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Dove Cream Beauty Bar 100g", SKU = "PC-SOP-002", Description = "Moisturizing beauty cream soap", PurchasePrice = 52, SellingPrice = 65, CurrentStock = 22, MinimumStock = 12, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Colgate Strong Teeth Toothpaste 150g", SKU = "PC-PST-003", Description = "Calcium & fluoride dental paste", PurchasePrice = 85, SellingPrice = 105, CurrentStock = 4, MinimumStock = 12, Unit = UnitType.Piece }, // LOW STOCK
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Clinic Plus Shampoo 175ml", SKU = "PC-SHM-004", Description = "Milk protein formula shampoo", PurchasePrice = 110, SellingPrice = 135, CurrentStock = 18, MinimumStock = 10, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Parachute Coconut Hair Oil 200ml", SKU = "PC-OIL-005", Description = "Pure edible coconut hair oil", PurchasePrice = 88, SellingPrice = 105, CurrentStock = 24, MinimumStock = 10, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catPersonalCare.Id, Name = "Lifebuoy Total Handwash 200ml", SKU = "PC-HDW-006", Description = "Germ protection liquid pump", PurchasePrice = 75, SellingPrice = 90, CurrentStock = 15, MinimumStock = 8, Unit = UnitType.Piece },

            // Snacks
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Parle-G Glucose Biscuits 250g", SKU = "SNK-BSC-001", Description = "India's favorite chai biscuit", PurchasePrice = 22, SellingPrice = 30, CurrentStock = 85, MinimumStock = 30, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Britannia Good Day Butter 200g", SKU = "SNK-BSC-002", Description = "Rich butter cookies", PurchasePrice = 32, SellingPrice = 40, CurrentStock = 40, MinimumStock = 20, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Haldiram's Bhujia Sev 400g", SKU = "SNK-NAM-003", Description = "Spicy crispy moth bean snack", PurchasePrice = 95, SellingPrice = 115, CurrentStock = 26, MinimumStock = 12, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Lay's Magic Masala Chips 50g", SKU = "SNK-CHP-004", Description = "Spicy potato wafers", PurchasePrice = 16, SellingPrice = 20, CurrentStock = 45, MinimumStock = 20, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Kurkure Masala Munch 90g", SKU = "SNK-NAM-005", Description = "Crispy spicy corn puffs", PurchasePrice = 16, SellingPrice = 20, CurrentStock = 50, MinimumStock = 20, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catSnacks.Id, Name = "Maggi 2-Minute Noodles 70g", SKU = "SNK-NDL-006", Description = "Masala instant wheat noodles", PurchasePrice = 11.5m, SellingPrice = 14, CurrentStock = 120, MinimumStock = 40, Unit = UnitType.Packet },

            // Household
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catHousehold.Id, Name = "Surf Excel Quick Wash Powder 1kg", SKU = "HH-DET-001", Description = "Tough stain removal detergent", PurchasePrice = 135, SellingPrice = 160, CurrentStock = 25, MinimumStock = 10, Unit = UnitType.Packet },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catHousehold.Id, Name = "Vim Dishwash Bar 300g", SKU = "HH-VIM-002", Description = "Lemon grease remover bar", PurchasePrice = 24, SellingPrice = 30, CurrentStock = 35, MinimumStock = 15, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catHousehold.Id, Name = "Harpic Power Plus 500ml", SKU = "HH-HRP-003", Description = "Disinfectant toilet cleaner", PurchasePrice = 82, SellingPrice = 99, CurrentStock = 2, MinimumStock = 10, Unit = UnitType.Piece }, // LOW STOCK
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catHousehold.Id, Name = "Colin Glass Cleaner 500ml", SKU = "HH-CLN-004", Description = "Surface and glass spray", PurchasePrice = 85, SellingPrice = 105, CurrentStock = 16, MinimumStock = 8, Unit = UnitType.Piece },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, CategoryId = catHousehold.Id, Name = "Lizol Floor Cleaner Citrus 500ml", SKU = "HH-LZL-005", Description = "Kills 99.9% germs surface cleaner", PurchasePrice = 92, SellingPrice = 110, CurrentStock = 18, MinimumStock = 10, Unit = UnitType.Piece }
        };

        context.Products.AddRange(products);

        // Generate Opening Stock Transactions for all products
        foreach (var p in products)
        {
            var initialStock = p.CurrentStock + 40; // Assume we started with extra and sold over the last 30 days
            var st = new StockTransaction
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                ProductId = p.Id,
                TransactionType = TransactionType.Opening,
                Quantity = initialStock,
                PreviousStock = 0,
                NewStock = initialStock,
                Notes = "Initial opening stock audit",
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            };
            context.StockTransactions.Add(st);
        }

        // Generate realistic 30-day historical sales records
        var rnd = new Random(42);
        var today = DateTime.UtcNow.Date;

        for (int day = 30; day >= 1; day--)
        {
            var saleDate = today.AddDays(-day).AddHours(rnd.Next(9, 21)).AddMinutes(rnd.Next(0, 59));
            // 2 to 4 sales transactions per day
            int salesPerDay = rnd.Next(2, 5);

            for (int s = 1; s <= salesPerDay; s++)
            {
                var sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    InvoiceNumber = $"INV-{saleDate:yyyyMMdd}-{s:D2}",
                    SaleDate = saleDate,
                    Notes = "Walk-in store sale",
                    CreatedAt = saleDate
                };

                // Pick 2 to 5 random products for this invoice
                var numItems = rnd.Next(2, 6);
                var pickedProducts = products.OrderBy(_ => rnd.Next()).Take(numItems).ToList();
                decimal total = 0;

                foreach (var prod in pickedProducts)
                {
                    decimal qty = rnd.Next(1, 4);
                    // Fast moving products sell more
                    if (prod.SKU is "SNK-NDL-006" or "SNK-BSC-001" or "DRY-MLK-002" or "GRO-SUG-005")
                    {
                        qty = rnd.Next(2, 6);
                    }

                    var item = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        SaleId = sale.Id,
                        ProductId = prod.Id,
                        Quantity = qty,
                        UnitPrice = prod.SellingPrice,
                        SubTotal = qty * prod.SellingPrice
                    };
                    sale.Items.Add(item);
                    total += item.SubTotal;

                    // Also add StockTransaction for historical auditing
                    context.StockTransactions.Add(new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = prod.Id,
                        TransactionType = TransactionType.Sale,
                        Quantity = qty,
                        PreviousStock = prod.CurrentStock + qty,
                        NewStock = prod.CurrentStock,
                        ReferenceId = sale.Id,
                        Notes = $"Sale invoice {sale.InvoiceNumber}",
                        CreatedAt = saleDate
                    });
                }

                sale.TotalAmount = total;
                context.Sales.Add(sale);
            }
        }

        // Low stock notifications for low stock products
        foreach (var p in products.Where(p => p.CurrentStock <= p.MinimumStock))
        {
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                ProductId = p.Id,
                Title = p.CurrentStock == 0 ? $"Out of Stock: {p.Name}" : $"Low Stock Alert: {p.Name}",
                Message = p.CurrentStock == 0 
                    ? $"{p.Name} (SKU: {p.SKU}) has 0 stock remaining. Immediate reorder required."
                    : $"{p.Name} (SKU: {p.SKU}) current stock ({p.CurrentStock} {p.Unit}) has reached or fallen below minimum threshold ({p.MinimumStock} {p.Unit}).",
                NotificationType = p.CurrentStock == 0 ? NotificationType.OutOfStock : NotificationType.LowStock,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-rnd.Next(1, 12))
            });
        }

        await context.SaveChangesAsync();
    }
}
