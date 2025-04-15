using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Text.Json;

namespace _8lpets.Pages.Games
{
    public class GameRecord
    {
        public string PlayerChoice { get; set; } = string.Empty;
        public string ComputerChoice { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int PointsWon { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    public class RockPaperScissorsModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();
        private const string GamesPlayedKey = "RPS_GamesPlayed";
        private const string WinsKey = "RPS_Wins";
        private const string TiesKey = "RPS_Ties";
        private const string LossesKey = "RPS_Losses";
        private const string TotalPointsWonKey = "RPS_TotalPointsWon";
        private const string RecentGamesKey = "RPS_RecentGames";
        private const string LastGameKey = "RPS_LastGame";

        public RockPaperScissorsModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public int User8lPoints { get; set; }
        public string? GameResult { get; set; }
        public string ResultAlertClass { get; set; } = "alert-info";
        public int _8lPointsWon { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Ties { get; set; }
        public int Losses { get; set; }
        public int Total8lPointsWon { get; set; }
        public double WinRate => GamesPlayed > 0 ? Math.Round((double)Wins / GamesPlayed * 100, 1) : 0;
        public List<GameRecord> RecentGames { get; set; } = new List<GameRecord>();
        public GameRecord? LastGame { get; set; }

        [BindProperty]
        public string PlayerChoice { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Initialize game state
            await InitializeGameState();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            if (string.IsNullOrEmpty(PlayerChoice) ||
                (PlayerChoice != "Rock" && PlayerChoice != "Paper" && PlayerChoice != "Scissors"))
            {
                ModelState.AddModelError("PlayerChoice", "Please select Rock, Paper, or Scissors.");
                await InitializeGameState();
                return Page();
            }

            // Generate computer's choice
            string[] choices = { "Rock", "Paper", "Scissors" };
            string computerChoice = choices[_random.Next(choices.Length)];

            // Determine the winner
            string result;
            int pointsWon;

            if (PlayerChoice == computerChoice)
            {
                result = "Tie";
                pointsWon = 5;
                GameResult = "It's a tie!";
                ResultAlertClass = "alert-warning";
            }
            else if ((PlayerChoice == "Rock" && computerChoice == "Scissors") ||
                     (PlayerChoice == "Paper" && computerChoice == "Rock") ||
                     (PlayerChoice == "Scissors" && computerChoice == "Paper"))
            {
                result = "Win";
                pointsWon = 25;
                GameResult = "You win!";
                ResultAlertClass = "alert-success";
            }
            else
            {
                result = "Loss";
                pointsWon = 0;
                GameResult = "You lose!";
                ResultAlertClass = "alert-danger";
            }

            // Update the user's 8lPoints
            CurrentUser.NeoPoints += pointsWon;
            await _context.SaveChangesAsync();
            User8lPoints = CurrentUser.NeoPoints;

            _8lPointsWon = pointsWon;

            // Create a game record
            var gameRecord = new GameRecord
            {
                PlayerChoice = PlayerChoice,
                ComputerChoice = computerChoice,
                Result = result,
                PointsWon = pointsWon,
                PlayedAt = DateTime.Now
            };

            // Update game statistics
            UpdateGameStatistics(gameRecord);

            return Page();
        }

        private async Task InitializeGameState()
        {
            // Get the user's 8lPoints
            User8lPoints = CurrentUser.NeoPoints;

            // Get game statistics from session
            GamesPlayed = HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0;
            Wins = HttpContext.Session.GetInt32(WinsKey) ?? 0;
            Ties = HttpContext.Session.GetInt32(TiesKey) ?? 0;
            Losses = HttpContext.Session.GetInt32(LossesKey) ?? 0;
            Total8lPointsWon = HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0;

            // Get recent games from session
            var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
            if (!string.IsNullOrEmpty(recentGamesJson))
            {
                RecentGames = JsonSerializer.Deserialize<List<GameRecord>>(recentGamesJson) ?? new List<GameRecord>();
            }

            // Get last game from session
            var lastGameJson = HttpContext.Session.GetString(LastGameKey);
            if (!string.IsNullOrEmpty(lastGameJson))
            {
                LastGame = JsonSerializer.Deserialize<GameRecord>(lastGameJson);
            }
        }

        private void UpdateGameStatistics(GameRecord gameRecord)
        {
            // Increment games played
            GamesPlayed = (HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(GamesPlayedKey, GamesPlayed);

            // Update wins, ties, losses
            if (gameRecord.Result == "Win")
            {
                Wins = (HttpContext.Session.GetInt32(WinsKey) ?? 0) + 1;
                HttpContext.Session.SetInt32(WinsKey, Wins);
            }
            else if (gameRecord.Result == "Tie")
            {
                Ties = (HttpContext.Session.GetInt32(TiesKey) ?? 0) + 1;
                HttpContext.Session.SetInt32(TiesKey, Ties);
            }
            else
            {
                Losses = (HttpContext.Session.GetInt32(LossesKey) ?? 0) + 1;
                HttpContext.Session.SetInt32(LossesKey, Losses);
            }

            // Add to total points won
            Total8lPointsWon = (HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0) + gameRecord.PointsWon;
            HttpContext.Session.SetInt32(TotalPointsWonKey, Total8lPointsWon);

            // Update recent games
            var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
            var recentGames = !string.IsNullOrEmpty(recentGamesJson)
                ? JsonSerializer.Deserialize<List<GameRecord>>(recentGamesJson)
                : new List<GameRecord>();

            recentGames ??= new List<GameRecord>();
            recentGames.Insert(0, gameRecord);

            // Keep only the last 5 games
            if (recentGames.Count > 5)
            {
                recentGames = recentGames.Take(5).ToList();
            }

            HttpContext.Session.SetString(RecentGamesKey, JsonSerializer.Serialize(recentGames));
            RecentGames = recentGames;

            // Update last game
            HttpContext.Session.SetString(LastGameKey, JsonSerializer.Serialize(gameRecord));
            LastGame = gameRecord;
        }
    }
}
