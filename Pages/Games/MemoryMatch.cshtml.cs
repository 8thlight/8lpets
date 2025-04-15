using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Text.Json;

namespace _8lpets.Pages.Games
{
    public class MemoryCard
    {
        public string Icon { get; set; } = string.Empty;
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
        public int Value { get; set; }
    }

    public class MemoryGameRecord
    {
        public int Moves { get; set; }
        public int PointsWon { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public string FormattedTime => $"{TimeTaken.Minutes:D2}:{TimeTaken.Seconds:D2}";
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    public class MemoryMatchModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();
        private const string CardsKey = "MemoryMatch_Cards";
        private const string GameStartTimeKey = "MemoryMatch_StartTime";
        private const string MovesKey = "MemoryMatch_Moves";
        private const string PairsFoundKey = "MemoryMatch_PairsFound";
        private const string FirstCardIndexKey = "MemoryMatch_FirstCardIndex";
        private const string PreviousCardsKey = "MemoryMatch_PreviousCards";
        private const string GamesPlayedKey = "MemoryMatch_GamesPlayed";
        private const string BestScoreKey = "MemoryMatch_BestScore";
        private const string BestTimeKey = "MemoryMatch_BestTime";
        private const string TotalPointsWonKey = "MemoryMatch_TotalPointsWon";
        private const string RecentGamesKey = "MemoryMatch_RecentGames";

        public MemoryMatchModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public int User8lPoints { get; set; }
        public List<MemoryCard> Cards { get; set; } = new List<MemoryCard>();
        public int BoardSize => 16; // 4x4 grid (16 cards)
        public int Moves { get; set; }
        public int PairsFound { get; set; }
        public bool GameCompleted => PairsFound == BoardSize / 2;
        public int _8lPointsWon { get; set; }
        public DateTime GameStartTime { get; set; }
        public TimeSpan GameTime => DateTime.Now - GameStartTime;
        public string FormattedTime => $"{GameTime.Minutes:D2}:{GameTime.Seconds:D2}";
        public int GamesPlayed { get; set; }
        public int BestScore { get; set; } = int.MaxValue;
        public string BestTime { get; set; } = "99:99";
        public int Total8lPointsWon { get; set; }
        public List<MemoryGameRecord> RecentGames { get; set; } = new List<MemoryGameRecord>();
        public int? FirstCardIndex { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await InitializeGameState();

            // Check if we need to start a new game
            if (!HttpContext.Session.Keys.Contains(CardsKey))
            {
                await StartNewGame();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostFlipCardAsync(int cardIndex)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await InitializeGameState();

            // Get the current game state
            var cardsJson = HttpContext.Session.GetString(CardsKey);
            if (string.IsNullOrEmpty(cardsJson))
            {
                return RedirectToPage();
            }

            Cards = JsonSerializer.Deserialize<List<MemoryCard>>(cardsJson) ?? new List<MemoryCard>();
            Moves = HttpContext.Session.GetInt32(MovesKey) ?? 0;
            PairsFound = HttpContext.Session.GetInt32(PairsFoundKey) ?? 0;
            FirstCardIndex = HttpContext.Session.GetInt32(FirstCardIndexKey);
            GameStartTime = DateTime.Parse(HttpContext.Session.GetString(GameStartTimeKey) ?? DateTime.Now.ToString("o"));

            // Check if there are previous unmatched cards that need to be flipped back
            var previousCardsJson = HttpContext.Session.GetString(PreviousCardsKey);
            if (!string.IsNullOrEmpty(previousCardsJson))
            {
                var previousCards = JsonSerializer.Deserialize<List<int>>(previousCardsJson);
                if (previousCards != null && previousCards.Count == 2)
                {
                    // Flip the previous unmatched cards back over
                    Cards[previousCards[0]].IsFlipped = false;
                    Cards[previousCards[1]].IsFlipped = false;

                    // Clear the previous cards
                    HttpContext.Session.Remove(PreviousCardsKey);
                }
            }

            // Ignore if the card is already flipped or matched
            if (Cards[cardIndex].IsFlipped || Cards[cardIndex].IsMatched)
            {
                // Save the updated cards
                HttpContext.Session.SetString(CardsKey, JsonSerializer.Serialize(Cards));
                return RedirectToPage();
            }

            // Flip the card
            Cards[cardIndex].IsFlipped = true;

            // Check if this is the first card flipped
            if (!FirstCardIndex.HasValue)
            {
                // This is the first card, save its index
                FirstCardIndex = cardIndex;
                HttpContext.Session.SetInt32(FirstCardIndexKey, cardIndex);
            }
            else
            {
                // This is the second card, check for a match
                Moves++;
                HttpContext.Session.SetInt32(MovesKey, Moves);

                if (Cards[cardIndex].Value == Cards[FirstCardIndex.Value].Value)
                {
                    // Match found!
                    Cards[cardIndex].IsMatched = true;
                    Cards[FirstCardIndex.Value].IsMatched = true;
                    PairsFound++;
                    HttpContext.Session.SetInt32(PairsFoundKey, PairsFound);

                    // Check if the game is completed
                    if (PairsFound == BoardSize / 2)
                    {
                        await HandleGameCompletion();
                    }
                }
                else
                {
                    // No match, store these cards to be flipped back on the next card click
                    var unmatchedCards = new List<int> { FirstCardIndex.Value, cardIndex };
                    HttpContext.Session.SetString(PreviousCardsKey, JsonSerializer.Serialize(unmatchedCards));
                }

                // Reset the first card index
                FirstCardIndex = null;
                HttpContext.Session.Remove(FirstCardIndexKey);
            }

            // Save the updated cards
            HttpContext.Session.SetString(CardsKey, JsonSerializer.Serialize(Cards));

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostNewGameAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await StartNewGame();
            return RedirectToPage();
        }

        private async Task InitializeGameState()
        {
            // Get the user's 8lPoints
            User8lPoints = CurrentUser.NeoPoints;

            // Get game statistics from session
            GamesPlayed = HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0;
            BestScore = HttpContext.Session.GetInt32(BestScoreKey) ?? int.MaxValue;
            var bestTimeString = HttpContext.Session.GetString(BestTimeKey);
            BestTime = !string.IsNullOrEmpty(bestTimeString) ? bestTimeString : "99:99";
            Total8lPointsWon = HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0;

            // Get recent games from session
            var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
            if (!string.IsNullOrEmpty(recentGamesJson))
            {
                RecentGames = JsonSerializer.Deserialize<List<MemoryGameRecord>>(recentGamesJson) ?? new List<MemoryGameRecord>();
            }

            // Get current game state
            var cardsJson = HttpContext.Session.GetString(CardsKey);
            if (!string.IsNullOrEmpty(cardsJson))
            {
                Cards = JsonSerializer.Deserialize<List<MemoryCard>>(cardsJson) ?? new List<MemoryCard>();
                Moves = HttpContext.Session.GetInt32(MovesKey) ?? 0;
                PairsFound = HttpContext.Session.GetInt32(PairsFoundKey) ?? 0;
                FirstCardIndex = HttpContext.Session.GetInt32(FirstCardIndexKey);
                var startTimeString = HttpContext.Session.GetString(GameStartTimeKey);
                GameStartTime = !string.IsNullOrEmpty(startTimeString) ? DateTime.Parse(startTimeString) : DateTime.Now;

                // Check for previous unmatched cards
                var previousCardsJson = HttpContext.Session.GetString(PreviousCardsKey);
                if (!string.IsNullOrEmpty(previousCardsJson))
                {
                    // We have previous unmatched cards, but we don't need to do anything with them here
                    // They will be handled in the OnPostFlipCardAsync method
                }
            }
        }

        private async Task StartNewGame()
        {
            // Create a new set of cards
            Cards = new List<MemoryCard>();

            // Define the icons to use (8 pairs for a 4x4 grid)
            string[] icons = {
                "fa-dog", "fa-cat", "fa-fish", "fa-dragon",
                "fa-hippo", "fa-frog", "fa-spider", "fa-dove"
            };

            // Create pairs of cards
            for (int i = 0; i < BoardSize / 2; i++)
            {
                Cards.Add(new MemoryCard { Icon = icons[i], Value = i });
                Cards.Add(new MemoryCard { Icon = icons[i], Value = i });
            }

            // Shuffle the cards
            Cards = Cards.OrderBy(c => _random.Next()).ToList();

            // Reset game state
            Moves = 0;
            PairsFound = 0;
            FirstCardIndex = null;
            GameStartTime = DateTime.Now;

            // Save the game state to session
            HttpContext.Session.SetString(CardsKey, JsonSerializer.Serialize(Cards));
            HttpContext.Session.SetInt32(MovesKey, Moves);
            HttpContext.Session.SetInt32(PairsFoundKey, PairsFound);
            HttpContext.Session.Remove(FirstCardIndexKey);
            HttpContext.Session.Remove(PreviousCardsKey);
            HttpContext.Session.SetString(GameStartTimeKey, GameStartTime.ToString("o"));
        }

        private async Task HandleGameCompletion()
        {
            // Calculate points
            int matchPoints = PairsFound * 10; // 10 points per match
            int completionBonus = 20; // Bonus for completing the game

            // Time bonus (up to 30 points based on how quickly they finished)
            int timeBonus = 0;
            if (GameTime.TotalSeconds < 30)
            {
                timeBonus = 30; // Max bonus for under 30 seconds
            }
            else if (GameTime.TotalSeconds < 60)
            {
                timeBonus = 20; // Good bonus for under 1 minute
            }
            else if (GameTime.TotalSeconds < 90)
            {
                timeBonus = 10; // Small bonus for under 1.5 minutes
            }

            // Calculate total points
            _8lPointsWon = matchPoints + completionBonus + timeBonus;

            // Update the user's 8lPoints
            CurrentUser.NeoPoints += _8lPointsWon;
            await _context.SaveChangesAsync();
            User8lPoints = CurrentUser.NeoPoints;

            // Update game statistics
            GamesPlayed = (HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(GamesPlayedKey, GamesPlayed);

            // Update best score if this game was better
            if (Moves < BestScore || BestScore == int.MaxValue)
            {
                BestScore = Moves;
                HttpContext.Session.SetInt32(BestScoreKey, BestScore);
            }

            // Update best time if this game was faster
            TimeSpan bestTimeSpan;
            if (TimeSpan.TryParse($"00:{BestTime}", out bestTimeSpan))
            {
                if (GameTime < bestTimeSpan)
                {
                    BestTime = FormattedTime;
                    HttpContext.Session.SetString(BestTimeKey, BestTime);
                }
            }

            // Add to total points won
            Total8lPointsWon = (HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0) + _8lPointsWon;
            HttpContext.Session.SetInt32(TotalPointsWonKey, Total8lPointsWon);

            // Add to recent games
            var gameRecord = new MemoryGameRecord
            {
                Moves = Moves,
                PointsWon = _8lPointsWon,
                TimeTaken = GameTime,
                PlayedAt = DateTime.Now
            };

            var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
            var recentGames = !string.IsNullOrEmpty(recentGamesJson)
                ? JsonSerializer.Deserialize<List<MemoryGameRecord>>(recentGamesJson)
                : new List<MemoryGameRecord>();

            recentGames ??= new List<MemoryGameRecord>();
            recentGames.Insert(0, gameRecord);

            // Keep only the last 5 games
            if (recentGames.Count > 5)
            {
                recentGames = recentGames.Take(5).ToList();
            }

            HttpContext.Session.SetString(RecentGamesKey, JsonSerializer.Serialize(recentGames));
            RecentGames = recentGames;
        }
    }
}
