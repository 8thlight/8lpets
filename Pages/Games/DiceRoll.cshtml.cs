using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Text.Json;

namespace _8lpets.Pages.Games
{
    public class DiceRollRecord
    {
        public List<int> Dice { get; set; } = new List<int>();
        public string DiceValues => string.Join("-", Dice);
        public int Total => Dice.Sum();
        public int PointsWon { get; set; }
        public DateTime RolledAt { get; set; } = DateTime.Now;
        public bool HasMatches => Dice.GroupBy(d => d).Any(g => g.Count() > 1);
        public bool HasTriple => Dice.Count == 3 && Dice.GroupBy(d => d).Any(g => g.Count() == 3);
        public bool HasDouble => Dice.GroupBy(d => d).Any(g => g.Count() == 2);
        public bool IsMaxRoll => Dice.Count > 0 && Dice.All(d => d == 6);
    }

    public class DiceRollModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();
        private const string TotalRollsKey = "DiceRoll_TotalRolls";
        private const string BestRollKey = "DiceRoll_BestRoll";
        private const string TotalPointsWonKey = "DiceRoll_TotalPointsWon";
        private const string RecentRollsKey = "DiceRoll_RecentRolls";
        private const string LastRollKey = "DiceRoll_LastRoll";
        private const string NumberOfDiceKey = "DiceRoll_NumberOfDice";
        private const string IsRollingKey = "DiceRoll_IsRolling";

        public DiceRollModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public int User8lPoints { get; set; }
        public string? GameResult { get; set; }
        public string ResultAlertClass { get; set; } = "alert-info";
        public int TotalRolls { get; set; }
        public string BestRoll { get; set; } = "None";
        public int Total8lPointsWon { get; set; }
        public List<DiceRollRecord> RecentRolls { get; set; } = new List<DiceRollRecord>();
        public DiceRollRecord? LastRoll { get; set; }
        public bool IsRolling { get; set; }

        [BindProperty]
        public int NumberOfDice { get; set; } = 2;

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Initialize game state
            await InitializeGameState();

            // Reset the rolling animation flag
            HttpContext.Session.Remove(IsRollingKey);
            IsRolling = false;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Validate input
            if (NumberOfDice < 1 || NumberOfDice > 3)
            {
                NumberOfDice = 2;
            }

            // Save the number of dice preference
            HttpContext.Session.SetInt32(NumberOfDiceKey, NumberOfDice);

            // Set the rolling animation flag
            HttpContext.Session.SetInt32(IsRollingKey, 1);
            IsRolling = true;

            // Roll the dice
            var diceRoll = new DiceRollRecord();

            for (int i = 0; i < NumberOfDice; i++)
            {
                diceRoll.Dice.Add(_random.Next(1, 7));
            }

            // Calculate points
            int points = diceRoll.Total;

            // Apply bonuses
            if (diceRoll.IsMaxRoll && diceRoll.Dice.Count == 3)
            {
                // Triple 6s (6-6-6) is a jackpot!
                points = 100;
                GameResult = "JACKPOT! Triple 6s!";
                ResultAlertClass = "alert-success";
            }
            else if (diceRoll.HasTriple)
            {
                // Triple of any other number
                points *= 3;
                GameResult = "Triple! 3x points!";
                ResultAlertClass = "alert-success";
            }
            else if (diceRoll.HasDouble)
            {
                // Double of any number
                points *= 2;
                GameResult = "Double! 2x points!";
                ResultAlertClass = "alert-info";
            }
            else
            {
                GameResult = "You rolled!";
                ResultAlertClass = "alert-info";
            }

            diceRoll.PointsWon = points;

            // Update the user's 8lPoints
            CurrentUser.NeoPoints += points;
            await _context.SaveChangesAsync();
            User8lPoints = CurrentUser.NeoPoints;

            // Update game statistics
            UpdateGameStatistics(diceRoll);

            // Save the last roll
            HttpContext.Session.SetString(LastRollKey, JsonSerializer.Serialize(diceRoll));
            LastRoll = diceRoll;

            await InitializeGameState();

            return Page();
        }

        private async Task InitializeGameState()
        {
            // Get the user's 8lPoints
            User8lPoints = CurrentUser.NeoPoints;

            // Get game statistics from session
            TotalRolls = HttpContext.Session.GetInt32(TotalRollsKey) ?? 0;
            
            // Display the best roll
            var bestRollJson = HttpContext.Session.GetString(BestRollKey);
            if (!string.IsNullOrEmpty(bestRollJson))
            {
                var bestRoll = JsonSerializer.Deserialize<DiceRollRecord>(bestRollJson);
                BestRoll = bestRoll?.DiceValues ?? "None";
            }
            else
            {
                BestRoll = "None";
            }
            
            Total8lPointsWon = HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0;

            // Get recent rolls from session
            var recentRollsJson = HttpContext.Session.GetString(RecentRollsKey);
            if (!string.IsNullOrEmpty(recentRollsJson))
            {
                RecentRolls = JsonSerializer.Deserialize<List<DiceRollRecord>>(recentRollsJson) ?? new List<DiceRollRecord>();
            }

            // Get last roll from session
            var lastRollJson = HttpContext.Session.GetString(LastRollKey);
            if (!string.IsNullOrEmpty(lastRollJson))
            {
                LastRoll = JsonSerializer.Deserialize<DiceRollRecord>(lastRollJson);
            }

            // Get number of dice preference
            NumberOfDice = HttpContext.Session.GetInt32(NumberOfDiceKey) ?? 2;

            // Get rolling animation flag
            IsRolling = HttpContext.Session.GetInt32(IsRollingKey) == 1;
        }

        private void UpdateGameStatistics(DiceRollRecord roll)
        {
            // Increment total rolls
            TotalRolls = (HttpContext.Session.GetInt32(TotalRollsKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(TotalRollsKey, TotalRolls);

            // Update best roll if this one is better
            var bestRollJson = HttpContext.Session.GetString(BestRollKey);
            
            if (string.IsNullOrEmpty(bestRollJson))
            {
                // First roll becomes the best roll
                BestRoll = roll.DiceValues;
                HttpContext.Session.SetString(BestRollKey, JsonSerializer.Serialize(roll));
            }
            else
            {
                var currentBestRoll = JsonSerializer.Deserialize<DiceRollRecord>(bestRollJson);
                if (roll.PointsWon > currentBestRoll.PointsWon)
                {
                    BestRoll = roll.DiceValues;
                    HttpContext.Session.SetString(BestRollKey, JsonSerializer.Serialize(roll));
                }
            }

            // Add to total points won
            Total8lPointsWon = (HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0) + roll.PointsWon;
            HttpContext.Session.SetInt32(TotalPointsWonKey, Total8lPointsWon);

            // Update recent rolls
            var recentRollsJson = HttpContext.Session.GetString(RecentRollsKey);
            var recentRolls = !string.IsNullOrEmpty(recentRollsJson)
                ? JsonSerializer.Deserialize<List<DiceRollRecord>>(recentRollsJson)
                : new List<DiceRollRecord>();

            recentRolls ??= new List<DiceRollRecord>();
            recentRolls.Insert(0, roll);

            // Keep only the last 5 rolls
            if (recentRolls.Count > 5)
            {
                recentRolls = recentRolls.Take(5).ToList();
            }

            HttpContext.Session.SetString(RecentRollsKey, JsonSerializer.Serialize(recentRolls));
            RecentRolls = recentRolls;
        }
    }
}
