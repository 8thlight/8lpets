using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;

namespace _8lpets.Services
{
    public class ShopRestockService : BackgroundService
    {
        // Static property to track the last restock time
        public static DateTime LastRestockTime { get; private set; } = DateTime.Now;

        // Event that can be subscribed to for restock notifications
        public static event EventHandler? ShopRestocked;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShopRestockService> _logger;
        private readonly TimeSpan _restockInterval = TimeSpan.FromMinutes(10);

        // List of items to restock
        private readonly List<Item> _itemTemplates = new List<Item>
        {
            new Item { Name = "Omelette", Description = "A tasty omelette to feed your pet", Price = 100, Type = "Food", ImageUrl = "https://placehold.co/600x400/FFA500/FFFFFF/png?text=Omelette" },
            new Item { Name = "Pizza", Description = "A delicious pizza slice", Price = 150, Type = "Food", ImageUrl = "https://placehold.co/600x400/FF0000/FFFFFF/png?text=Pizza" },
            new Item { Name = "Burger", Description = "A juicy burger", Price = 200, Type = "Food", ImageUrl = "https://placehold.co/600x400/8B4513/FFFFFF/png?text=Burger" },
            new Item { Name = "Plushie", Description = "A cute plushie toy", Price = 300, Type = "Toy", ImageUrl = "https://placehold.co/600x400/FFC0CB/FFFFFF/png?text=Plushie" },
            new Item { Name = "Ball", Description = "A bouncy ball for your pet to play with", Price = 200, Type = "Toy", ImageUrl = "https://placehold.co/600x400/0000FF/FFFFFF/png?text=Ball" },
            new Item { Name = "Frisbee", Description = "A flying disc for your pet", Price = 250, Type = "Toy", ImageUrl = "https://placehold.co/600x400/FF00FF/FFFFFF/png?text=Frisbee" },
            new Item { Name = "Health Potion", Description = "Restores your pet's health", Price = 500, Type = "Medicine", ImageUrl = "https://placehold.co/600x400/00FF00/FFFFFF/png?text=Health+Potion" },
            new Item { Name = "Bandage", Description = "Heals minor injuries", Price = 150, Type = "Medicine", ImageUrl = "https://placehold.co/600x400/FFFFFF/000000/png?text=Bandage" }
        };

        public ShopRestockService(IServiceProvider serviceProvider, ILogger<ShopRestockService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Shop Restock Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Shop Restock Service is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await RestockShop();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while restocking the shop.");
                }

                await Task.Delay(_restockInterval, stoppingToken);
            }

            _logger.LogInformation("Shop Restock Service is stopping.");
        }

        private async Task RestockShop()
        {
            // Create a scope to resolve scoped services (DbContext)
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<_8lpetsDbContext>();

            // Get current shop items
            var currentShopItems = await dbContext.Items
                .Where(i => i.UserId == null)
                .ToListAsync();

            // Check if we already have 20 or more items in the store
            bool fullRestock = currentShopItems.Count >= 20;

            // Remove all current shop items (complete restock)
            if (currentShopItems.Any())
            {
                dbContext.Items.RemoveRange(currentShopItems);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Removed {count} items from the shop for {restockType}.",
                    currentShopItems.Count, fullRestock ? "full restock" : "partial restock");
            }

            // Add items from the templates up to a maximum of 20 items
            var newItems = new List<Item>();
            var random = new Random();

            // Shuffle the templates to get a random selection of items
            var shuffledTemplates = _itemTemplates.OrderBy(x => random.Next()).ToList();

            // Keep adding items until we reach 20 or run out of templates
            while (newItems.Count < 20 && shuffledTemplates.Any())
            {
                // Take templates in batches to ensure variety
                foreach (var template in shuffledTemplates)
                {
                    // Stop if we've reached 20 items
                    if (newItems.Count >= 20)
                        break;

                    // Create a new item based on the template
                    // Vary the price slightly for each instance
                    int priceVariation = random.Next(-20, 21); // -20 to +20 price variation
                    int finalPrice = Math.Max(50, template.Price + priceVariation); // Ensure minimum price of 50

                    var newItem = new Item
                    {
                        Name = template.Name,
                        Description = template.Description,
                        Price = finalPrice,
                        Type = template.Type,
                        ImageUrl = template.ImageUrl,
                        UserId = null
                    };

                    newItems.Add(newItem);
                }

                // If we still need more items, shuffle again for another round
                // This ensures we get a good mix of items before duplicates
                if (newItems.Count < 20)
                {
                    shuffledTemplates = shuffledTemplates.OrderBy(x => random.Next()).ToList();
                }
            }

            // Add the new items to the database
            await dbContext.Items.AddRangeAsync(newItems);
            await dbContext.SaveChangesAsync();

            // Update the last restock time
            LastRestockTime = DateTime.Now;

            // Trigger the event
            ShopRestocked?.Invoke(this, EventArgs.Empty);

            string restockType = fullRestock ? "full restock" : "partial restock";
            _logger.LogInformation("Completed {restockType} of the shop with {count}/20 items at {time}.",
                restockType, newItems.Count, LastRestockTime);
        }
    }
}
