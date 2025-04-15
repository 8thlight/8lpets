using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Text.Json;

namespace _8lpets.Pages.Games
{
    public class CheckersMove
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsCapture { get; set; }
        public int CaptureRow { get; set; }
        public int CaptureCol { get; set; }
    }

    public class CheckersGameRecord
    {
        public bool PlayerWon { get; set; }
        public int CapturedPieces { get; set; }
        public int PointsWon { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    public class CheckersModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();
        private const string BoardKey = "Checkers_Board";
        private const string IsPlayerTurnKey = "Checkers_IsPlayerTurn";
        private const string SelectedRowKey = "Checkers_SelectedRow";
        private const string SelectedColKey = "Checkers_SelectedCol";
        private const string PlayerCapturedPiecesKey = "Checkers_PlayerCapturedPieces";
        private const string ComputerCapturedPiecesKey = "Checkers_ComputerCapturedPieces";
        private const string GameCompletedKey = "Checkers_GameCompleted";
        private const string PlayerWonKey = "Checkers_PlayerWon";
        private const string GamesPlayedKey = "Checkers_GamesPlayed";
        private const string GamesWonKey = "Checkers_GamesWon";
        private const string TotalPointsWonKey = "Checkers_TotalPointsWon";
        private const string RecentGamesKey = "Checkers_RecentGames";

        public CheckersModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public int User8lPoints { get; set; }
        public List<int> Board { get; set; } = new List<int>(new int[64]); // 0=empty, 1=player, 2=player king, 3=computer, 4=computer king
        public bool IsPlayerTurn { get; set; } = true;
        public int? SelectedRow { get; set; }
        public int? SelectedCol { get; set; }
        public List<CheckersMove> ValidMoves { get; set; } = new List<CheckersMove>();
        public bool GameCompleted { get; set; }
        public bool PlayerWon { get; set; }
        public int PlayerCapturedPieces { get; set; }
        public int ComputerCapturedPieces { get; set; }
        public int _8lPointsWon { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int Total8lPointsWon { get; set; }
        public List<CheckersGameRecord> RecentGames { get; set; } = new List<CheckersGameRecord>();

        public int PlayerPiecesCount => CountPieces(1) + CountPieces(2);
        public int ComputerPiecesCount => CountPieces(3) + CountPieces(4);
        public string WinRateFormatted => GamesPlayed > 0 ? $"{(double)GamesWon / GamesPlayed:P0}" : "0%";

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await InitializeGameState();

            // Check if we need to start a new game
            if (!HttpContext.Session.Keys.Contains(BoardKey))
            {
                await StartNewGame();
            }
            else if (!IsPlayerTurn && !GameCompleted)
            {
                // Computer's turn
                await MakeComputerMove();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSelectPieceAsync(int row, int col)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await InitializeGameState();

            // Validate selection
            if (!IsPlayerTurn || GameCompleted)
            {
                return RedirectToPage();
            }

            int piece = GetPiece(row, col);
            if (piece != 1 && piece != 2) // Not a player piece
            {
                return RedirectToPage();
            }

            // Set the selected piece
            SelectedRow = row;
            SelectedCol = col;
            HttpContext.Session.SetInt32(SelectedRowKey, row);
            HttpContext.Session.SetInt32(SelectedColKey, col);

            // Calculate valid moves for the selected piece
            ValidMoves = GetValidMoves(row, col, piece);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMovePieceAsync(int toRow, int toCol)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            await InitializeGameState();

            // Validate move
            if (!IsPlayerTurn || GameCompleted || !SelectedRow.HasValue || !SelectedCol.HasValue)
            {
                return RedirectToPage();
            }

            int fromRow = SelectedRow.Value;
            int fromCol = SelectedCol.Value;
            int piece = GetPiece(fromRow, fromCol);

            // Get valid moves and check if the requested move is valid
            ValidMoves = GetValidMoves(fromRow, fromCol, piece);
            var move = ValidMoves.FirstOrDefault(m => m.ToRow == toRow && m.ToCol == toCol);

            if (move == null)
            {
                return RedirectToPage();
            }

            // Execute the move
            SetPiece(fromRow, fromCol, 0); // Remove piece from original position
            SetPiece(toRow, toCol, piece); // Place piece in new position

            // Check if the piece should be promoted to king
            if (piece == 1 && toRow == 0)
            {
                SetPiece(toRow, toCol, 2); // Promote to king
                _8lPointsWon += 10; // Bonus for getting a king
            }

            // Handle capture
            if (move.IsCapture)
            {
                SetPiece(move.CaptureRow, move.CaptureCol, 0); // Remove captured piece
                PlayerCapturedPieces++;
                _8lPointsWon += 5; // Points for capturing a piece
                HttpContext.Session.SetInt32(PlayerCapturedPiecesKey, PlayerCapturedPieces);
            }

            // Check if the game is over
            if (ComputerPiecesCount == 0)
            {
                await HandleGameCompletion(true);
            }
            else
            {
                // Switch turn to computer
                IsPlayerTurn = false;
                HttpContext.Session.SetInt32(IsPlayerTurnKey, 0);

                // Reset selection
                SelectedRow = null;
                SelectedCol = null;
                HttpContext.Session.Remove(SelectedRowKey);
                HttpContext.Session.Remove(SelectedColKey);
            }

            // Save the updated board
            SaveBoardToSession();

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

        private Task InitializeGameState()
        {
            // Get the user's 8lPoints
            if (CurrentUser != null)
            {
                User8lPoints = CurrentUser.NeoPoints;
            }

            // Get game statistics from session
            GamesPlayed = HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0;
            GamesWon = HttpContext.Session.GetInt32(GamesWonKey) ?? 0;
            Total8lPointsWon = HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0;

            // Get recent games from session
            var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
            if (!string.IsNullOrEmpty(recentGamesJson))
            {
                RecentGames = JsonSerializer.Deserialize<List<CheckersGameRecord>>(recentGamesJson) ?? new List<CheckersGameRecord>();
            }

            // Get current game state
            var boardJson = HttpContext.Session.GetString(BoardKey);
            if (!string.IsNullOrEmpty(boardJson))
            {
                Board = JsonSerializer.Deserialize<List<int>>(boardJson) ?? new List<int>(new int[64]);
                IsPlayerTurn = HttpContext.Session.GetInt32(IsPlayerTurnKey) == 1;
                SelectedRow = HttpContext.Session.GetInt32(SelectedRowKey);
                SelectedCol = HttpContext.Session.GetInt32(SelectedColKey);
                PlayerCapturedPieces = HttpContext.Session.GetInt32(PlayerCapturedPiecesKey) ?? 0;
                ComputerCapturedPieces = HttpContext.Session.GetInt32(ComputerCapturedPiecesKey) ?? 0;
                GameCompleted = HttpContext.Session.GetInt32(GameCompletedKey) == 1;
                PlayerWon = HttpContext.Session.GetInt32(PlayerWonKey) == 1;
            }

            // Calculate valid moves if a piece is selected
            if (SelectedRow.HasValue && SelectedCol.HasValue)
            {
                int piece = GetPiece(SelectedRow.Value, SelectedCol.Value);
                ValidMoves = GetValidMoves(SelectedRow.Value, SelectedCol.Value, piece);
            }

            return Task.CompletedTask;
        }

        private Task StartNewGame()
        {
            // Initialize the board
            Board = new List<int>(new int[64]);
            for (int i = 0; i < 64; i++)
            {
                Board[i] = 0; // Initialize all squares to empty
            }

            // Place player pieces (bottom of the board)
            for (int row = 5; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 == 1) // Only on dark squares
                    {
                        SetPiece(row, col, 1); // Player piece
                    }
                }
            }

            // Place computer pieces (top of the board)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 == 1) // Only on dark squares
                    {
                        SetPiece(row, col, 3); // Computer piece
                    }
                }
            }

            // Reset game state
            IsPlayerTurn = true;
            SelectedRow = null;
            SelectedCol = null;
            PlayerCapturedPieces = 0;
            ComputerCapturedPieces = 0;
            GameCompleted = false;
            PlayerWon = false;
            _8lPointsWon = 0;
            ValidMoves.Clear();

            // Save the game state to session
            SaveBoardToSession();
            HttpContext.Session.SetInt32(IsPlayerTurnKey, 1);
            HttpContext.Session.Remove(SelectedRowKey);
            HttpContext.Session.Remove(SelectedColKey);
            HttpContext.Session.SetInt32(PlayerCapturedPiecesKey, 0);
            HttpContext.Session.SetInt32(ComputerCapturedPiecesKey, 0);
            HttpContext.Session.SetInt32(GameCompletedKey, 0);
            HttpContext.Session.SetInt32(PlayerWonKey, 0);

            return Task.CompletedTask;
        }

        private void SaveBoardToSession()
        {
            HttpContext.Session.SetString(BoardKey, JsonSerializer.Serialize(Board));
        }

        // Helper method to convert 2D coordinates to 1D index
        private int GetIndex(int row, int col)
        {
            return row * 8 + col;
        }

        // Helper method to get piece at 2D coordinates
        private int GetPiece(int row, int col)
        {
            return Board[GetIndex(row, col)];
        }

        // Helper method to set piece at 2D coordinates
        private void SetPiece(int row, int col, int value)
        {
            Board[GetIndex(row, col)] = value;
        }

        private List<CheckersMove> GetValidMoves(int row, int col, int piece)
        {
            var moves = new List<CheckersMove>();

            // Direction of movement depends on the piece type
            List<(int dr, int dc)> directions = new List<(int, int)>();

            if (piece == 1) // Player regular piece (can only move up)
            {
                directions.Add((-1, -1)); // Up-left
                directions.Add((-1, 1));  // Up-right
            }
            else if (piece == 2) // Player king (can move in any direction)
            {
                directions.Add((-1, -1)); // Up-left
                directions.Add((-1, 1));  // Up-right
                directions.Add((1, -1));  // Down-left
                directions.Add((1, 1));   // Down-right
            }
            else if (piece == 3) // Computer regular piece (can only move down)
            {
                directions.Add((1, -1));  // Down-left
                directions.Add((1, 1));   // Down-right
            }
            else if (piece == 4) // Computer king (can move in any direction)
            {
                directions.Add((-1, -1)); // Up-left
                directions.Add((-1, 1));  // Up-right
                directions.Add((1, -1));  // Down-left
                directions.Add((1, 1));   // Down-right
            }

            // Check for captures first (mandatory in checkers)
            foreach (var (dr, dc) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;

                // Check if the adjacent square is within bounds and contains an opponent's piece
                if (IsValidPosition(newRow, newCol) && IsOpponentPiece(piece, GetPiece(newRow, newCol)))
                {
                    int jumpRow = newRow + dr;
                    int jumpCol = newCol + dc;

                    // Check if the square beyond is empty and within bounds
                    if (IsValidPosition(jumpRow, jumpCol) && GetPiece(jumpRow, jumpCol) == 0)
                    {
                        moves.Add(new CheckersMove
                        {
                            FromRow = row,
                            FromCol = col,
                            ToRow = jumpRow,
                            ToCol = jumpCol,
                            IsCapture = true,
                            CaptureRow = newRow,
                            CaptureCol = newCol
                        });
                    }
                }
            }

            // If there are capture moves, return only those (captures are mandatory)
            if (moves.Count > 0)
            {
                return moves;
            }

            // If no captures, check for regular moves
            foreach (var (dr, dc) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;

                // Check if the square is empty and within bounds
                if (IsValidPosition(newRow, newCol) && GetPiece(newRow, newCol) == 0)
                {
                    moves.Add(new CheckersMove
                    {
                        FromRow = row,
                        FromCol = col,
                        ToRow = newRow,
                        ToCol = newCol,
                        IsCapture = false
                    });
                }
            }

            return moves;
        }

        private bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }

        private bool IsOpponentPiece(int piece, int otherPiece)
        {
            // Player pieces are 1 and 2, computer pieces are 3 and 4
            return (piece == 1 || piece == 2) ? (otherPiece == 3 || otherPiece == 4) : (otherPiece == 1 || otherPiece == 2);
        }

        private int CountPieces(int pieceType)
        {
            int count = 0;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (GetPiece(row, col) == pieceType)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private async Task MakeComputerMove()
        {
            // Get all possible moves for computer pieces
            var allMoves = new List<CheckersMove>();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    int piece = GetPiece(row, col);
                    if (piece == 3 || piece == 4) // Computer piece
                    {
                        allMoves.AddRange(GetValidMoves(row, col, piece));
                    }
                }
            }

            // If there are no moves, player wins
            if (allMoves.Count == 0)
            {
                await HandleGameCompletion(true);
                return;
            }

            // Prioritize capture moves
            var captureMoves = allMoves.Where(m => m.IsCapture).ToList();
            var moveToMake = captureMoves.Count > 0
                ? captureMoves[_random.Next(captureMoves.Count)]
                : allMoves[_random.Next(allMoves.Count)];

            // Execute the move
            int selectedPiece = GetPiece(moveToMake.FromRow, moveToMake.FromCol);
            SetPiece(moveToMake.FromRow, moveToMake.FromCol, 0);
            SetPiece(moveToMake.ToRow, moveToMake.ToCol, selectedPiece);

            // Check if the piece should be promoted to king
            if (selectedPiece == 3 && moveToMake.ToRow == 7)
            {
                SetPiece(moveToMake.ToRow, moveToMake.ToCol, 4); // Promote to king
            }

            // Handle capture
            if (moveToMake.IsCapture)
            {
                SetPiece(moveToMake.CaptureRow, moveToMake.CaptureCol, 0);
                ComputerCapturedPieces++;
                HttpContext.Session.SetInt32(ComputerCapturedPiecesKey, ComputerCapturedPieces);
            }

            // Check if the game is over
            if (PlayerPiecesCount == 0)
            {
                await HandleGameCompletion(false);
            }
            else
            {
                // Switch turn to player
                IsPlayerTurn = true;
                HttpContext.Session.SetInt32(IsPlayerTurnKey, 1);
            }

            // Save the updated board
            SaveBoardToSession();
        }

        private async Task HandleGameCompletion(bool playerWon)
        {
            GameCompleted = true;
            PlayerWon = playerWon;
            HttpContext.Session.SetInt32(GameCompletedKey, 1);
            HttpContext.Session.SetInt32(PlayerWonKey, playerWon ? 1 : 0);

            if (playerWon)
            {
                // Calculate points
                int capturePoints = PlayerCapturedPieces * 5;
                int winBonus = 50;
                _8lPointsWon = capturePoints + winBonus;

                // Update the user's 8lPoints
                if (CurrentUser != null)
                {
                    CurrentUser.NeoPoints += _8lPointsWon;
                    await _context.SaveChangesAsync();
                    User8lPoints = CurrentUser.NeoPoints;
                }

                // Update game statistics
                GamesPlayed = (HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0) + 1;
                GamesWon = (HttpContext.Session.GetInt32(GamesWonKey) ?? 0) + 1;
                HttpContext.Session.SetInt32(GamesPlayedKey, GamesPlayed);
                HttpContext.Session.SetInt32(GamesWonKey, GamesWon);
            }
            else
            {
                // Update game statistics
                GamesPlayed = (HttpContext.Session.GetInt32(GamesPlayedKey) ?? 0) + 1;
                HttpContext.Session.SetInt32(GamesPlayedKey, GamesPlayed);
            }

            // Add to total points won
            if (playerWon)
            {
                Total8lPointsWon = (HttpContext.Session.GetInt32(TotalPointsWonKey) ?? 0) + _8lPointsWon;
                HttpContext.Session.SetInt32(TotalPointsWonKey, Total8lPointsWon);
            }

            // Add to recent games
            if (playerWon)
            {
                var gameRecord = new CheckersGameRecord
                {
                    PlayerWon = playerWon,
                    CapturedPieces = PlayerCapturedPieces,
                    PointsWon = _8lPointsWon,
                    PlayedAt = DateTime.Now
                };

                var recentGamesJson = HttpContext.Session.GetString(RecentGamesKey);
                var recentGames = !string.IsNullOrEmpty(recentGamesJson)
                    ? JsonSerializer.Deserialize<List<CheckersGameRecord>>(recentGamesJson)
                    : new List<CheckersGameRecord>();

                recentGames ??= new List<CheckersGameRecord>();
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
}
