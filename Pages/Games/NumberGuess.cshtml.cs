using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.ComponentModel.DataAnnotations;

namespace _8lpets.Pages.Games
{
    public class GuessRecord
    {
        public int Guess { get; set; }
        public int Difference { get; set; }
    }

    public class NumberGuessModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();
        private const string SecretNumberKey = "SecretNumber";
        private const string PreviousGuessesKey = "PreviousGuesses";
        private const string GamesPlayedKey = "GamesPlayed";
        private const string TotalPointsWonKey = "TotalPointsWon";
        private const string BestGuessKey = "BestGuess";

        public NumberGuessModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public int User8lPoints { get; set; }
        public string? GameResult { get; set; }
        public bool IsCorrectGuess { get; set; }
        public int _8lPointsWon { get; set; }
        public int GamesPlayed { get; set; }
        public int Total8lPointsWon { get; set; }
        public int BestGuess { get; set; } = int.MaxValue;
        public List<GuessRecord> PreviousGuesses { get; set; } = new List<GuessRecord>();

        [BindProperty]
        [Required]
        [Range(1, 100, ErrorMessage = "Please enter a number between 1 and 100.")]
        public int UserGuess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Initialize game state
            await InitializeGameState();

            // Generate a new secret number if one doesn't exist
            if (!HttpContext.Session.Keys.Contains(SecretNumberKey))
            {
                HttpContext.Session.SetInt32(SecretNumberKey, _random.Next(1, 101));
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            if (!ModelState.IsValid)
            {
                await InitializeGameState();
                return Page();
            }

            // Get the secret number from session
            int? secretNumber = HttpContext.Session.GetInt32(SecretNumberKey);
            if (!secretNumber.HasValue)
            {
                secretNumber = _random.Next(1, 101);
                HttpContext.Session.SetInt32(SecretNumberKey, secretNumber.Value);
            }

            // Calculate the difference between the guess and the secret number
            int difference = Math.Abs(UserGuess - secretNumber.Value);

            // Determine the number of 8lPoints won based on the difference
            if (difference == 0)
            {
                _8lPointsWon = 100;
                GameResult = "Congratulations! You guessed the exact number!";
                IsCorrectGuess = true;

                // Generate a new secret number for the next game
                HttpContext.Session.SetInt32(SecretNumberKey, _random.Next(1, 101));
            }
            else
            {
                IsCorrectGuess = false;

                if (difference <= 5)
                {
                    _8lPointsWon = 50;
                    GameResult = $"Very close! The secret number was {secretNumber}. You were only {difference} away!";
                }
                else if (difference <= 10)
                {
                    _8lPointsWon = 25;
                    GameResult = $"Close! The secret number was {secretNumber}. You were {difference} away.";
                }
                else if (difference <= 20)
                {
                    _8lPointsWon = 10;
                    GameResult = $"Not bad! The secret number was {secretNumber}. You were {difference} away.";
                }
                else
                {
                    _8lPointsWon = 5;
                    GameResult = $"Try again! The secret number was {secretNumber}. You were {difference} away.";
                }
            }

            // Update the user's 8lPoints
            CurrentUser.NeoPoints += _8lPointsWon;
            await _context.SaveChangesAsync();
            User8lPoints = CurrentUser.NeoPoints;

            // Update game statistics
            UpdateGameStatistics(difference);

            return Page();
        }

        private async Task InitializeGameState()
        {
            // Get the user's 8lPoints
            User8lPoints = CurrentUser.NeoPoints;

            // Get game statistics from session
            GamesPlayed = HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0;
            Total8lPointsWon = HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0;
            BestGuess = HttpContext.Session.GetInt32(BestGuessKey) ?? int.MaxValue;

            // Get previous guesses from session
            var previousGuessesJson = HttpContext.Session.GetString(PreviousGuessesKey);
            if (!string.IsNullOrEmpty(previousGuessesJson))
            {
                PreviousGuesses = System.Text.Json.JsonSerializer.Deserialize<List<GuessRecord>>(previousGuessesJson) ?? new List<GuessRecord>();
            }
        }

        private void UpdateGameStatistics(int difference)
        {
            // Increment games played
            GamesPlayed = (HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(GamesPlayedKey, GamesPlayed);

            // Add to total points won
            Total8lPointsWon = (HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0) + _8lPointsWon;
            HttpContext.Session.SetInt32(TotalPointsWonKey, Total8lPointsWon);

            // Update best guess if this one is better
            int currentBestGuess = HttpContext.Session.GetInt32(BestGuessKey) ?? int.MaxValue;
            if (difference < currentBestGuess)
            {
                HttpContext.Session.SetInt32(BestGuessKey, difference);
                BestGuess = difference;
            }
            else
            {
                BestGuess = currentBestGuess;
            }

            // Add to previous guesses
            var previousGuessesJson = HttpContext.Session.GetString(PreviousGuessesKey);
            var previousGuesses = !string.IsNullOrEmpty(previousGuessesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<List<GuessRecord>>(previousGuessesJson)
                : new List<GuessRecord>();

            previousGuesses ??= new List<GuessRecord>();
            previousGuesses.Insert(0, new GuessRecord { Guess = UserGuess, Difference = difference });

            // Keep only the last 10 guesses
            if (previousGuesses.Count > 10)
            {
                previousGuesses = previousGuesses.Take(10).ToList();
            }

            HttpContext.Session.SetString(PreviousGuessesKey, System.Text.Json.JsonSerializer.Serialize(previousGuesses));
            PreviousGuesses = previousGuesses;
        }
    }
}
