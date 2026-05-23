using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
	private const int Rows = 20;
	private const int Cols = 10;
	private const int CellSize = 30;
	private const int XOffset = 250;
	private const int YOffset = 0;

	private int[,] _board = new int[Rows, Cols];
	private double _fallTimer = 0;
	private double _fallSpeed = 0.5;

	private int[,] _currentShape;
	private int _currentX;
	private int _currentY;
	private int _currentColor;

	private int[,] _nextShape;
	private int _nextColor;

	private Random _random = new Random();

	private static readonly Color[] ColorsList = new Color[]
	{
		Colors.Black,
		Colors.Cyan,
		Colors.Blue, // J
		Colors.Orange, // L
		new Color(1.0f, 1.0f, 0.0f), // Yellow for O shape
		Colors.Green,
		Colors.Purple,
		Colors.Red
	};

	private static readonly int[][,] Shapes = new int[][,]
	{
		new int[,] { {0, 0, 0, 0}, {1, 1, 1, 1}, {0, 0, 0, 0}, {0, 0, 0, 0} }, // I
		new int[,] { {2, 0, 0}, {2, 2, 2}, {0, 0, 0} }, // J
		new int[,] { {0, 0, 3}, {3, 3, 3}, {0, 0, 0} }, // L
		new int[,] { {4, 4}, {4, 4} }, // O
		new int[,] { {0, 5, 5}, {5, 5, 0}, {0, 0, 0} }, // S
		new int[,] { {0, 6, 0}, {6, 6, 6}, {0, 0, 0} }, // T
		new int[,] { {7, 7, 0}, {0, 7, 7}, {0, 0, 0} } // Z
	};

	public enum GameState { DifficultySelection, Playing, GameOver }
	private GameState _gameState = GameState.DifficultySelection;

	private int _scoreMultiplier = 1;

	private int _score = 0;
	private int _level = 1;
	private int _totalLinesCleared = 0;

	private List<int> _highscores = new List<int>();
	private const string HighscorePath = "user://highscore.txt";
	private AudioStreamPlayer _winSoundPlayer;
	private bool _hasReachedNewHighscoreThisGame = false;
	private int _currentHighscore = 0;

	private bool _hasReachedTop10ThisGame = false;
	private int _top10Threshold = 0;
	private CpuParticles2D _celebrationParticles;

	private RichTextLabel _helloLabel;
	private double _helloTimer = 3.0;

	private Button _btnEasy;
	private Button _btnNormal;
	private Button _btnHard;
	private Button _btnExtreme;

	public override void _Ready()
	{
		_winSoundPlayer = new AudioStreamPlayer();
		_winSoundPlayer.Stream = GD.Load<AudioStream>("res://win.wav");
		AddChild(_winSoundPlayer);

		_celebrationParticles = new CpuParticles2D();
		_celebrationParticles.Emitting = false;
		_celebrationParticles.OneShot = true;
		_celebrationParticles.Explosiveness = 0.8f;
		_celebrationParticles.Amount = 50;
		_celebrationParticles.Lifetime = 1.5f;
		_celebrationParticles.Position = new Vector2(XOffset + Cols * CellSize / 2, YOffset + Rows * CellSize / 2);
		_celebrationParticles.Spread = 180;
		_celebrationParticles.InitialVelocityMin = 100;
		_celebrationParticles.InitialVelocityMax = 300;
		_celebrationParticles.ScaleAmountMin = 4;
		_celebrationParticles.ScaleAmountMax = 8;

		var gradient = new Gradient();
		gradient.SetColor(0, Colors.Gold);
		gradient.SetColor(1, Colors.White);
		_celebrationParticles.ColorRamp = gradient;

		AddChild(_celebrationParticles);

		_helloLabel = new RichTextLabel();
		_helloLabel.BbcodeEnabled = true;
		_helloLabel.Text = "[center][color=red]H[/color][color=orange]e[/color][color=yellow]l[/color][color=green]l[/color][color=blue]o[/color][color=purple]![/color][/center]";
		_helloLabel.Position = new Vector2(XOffset, YOffset + Rows * CellSize / 2 - 20);
		_helloLabel.Size = new Vector2(Cols * CellSize, 40);
		_helloLabel.AddThemeFontSizeOverride("normal_font_size", 32);
		AddChild(_helloLabel);

		LoadHighscores();
		_currentHighscore = _highscores.Count > 0 ? _highscores[0] : 0;
		_top10Threshold = _highscores.Count == 10 ? _highscores[9] : 0;

		CreateDifficultyButtons();

		SetProcess(true);
	}

	private void CreateDifficultyButtons()
	{
		int buttonWidth = 150;
		int buttonHeight = 50;
		int centerX = XOffset + (Cols * CellSize) / 2 - buttonWidth / 2;
		int centerY = YOffset + (Rows * CellSize) / 2 - buttonHeight / 2;

		_btnEasy = new Button();
		_btnEasy.Text = "Easy";
		_btnEasy.Position = new Vector2(centerX, centerY - 90);
		_btnEasy.Size = new Vector2(buttonWidth, buttonHeight);
		_btnEasy.Pressed += () => OnDifficultySelected(1, 0.6);
		AddChild(_btnEasy);

		_btnNormal = new Button();
		_btnNormal.Text = "Normal";
		_btnNormal.Position = new Vector2(centerX, centerY - 30);
		_btnNormal.Size = new Vector2(buttonWidth, buttonHeight);
		_btnNormal.Pressed += () => OnDifficultySelected(2, 0.45);
		AddChild(_btnNormal);

		_btnHard = new Button();
		_btnHard.Text = "Hard";
		_btnHard.Position = new Vector2(centerX, centerY + 30);
		_btnHard.Size = new Vector2(buttonWidth, buttonHeight);
		_btnHard.Pressed += () => OnDifficultySelected(3, 0.3);
		AddChild(_btnHard);

		_btnExtreme = new Button();
		_btnExtreme.Text = "Extreme";
		_btnExtreme.Position = new Vector2(centerX, centerY + 90);
		_btnExtreme.Size = new Vector2(buttonWidth, buttonHeight);
		_btnExtreme.Pressed += () => OnDifficultySelected(4, 0.15);
		AddChild(_btnExtreme);
	}

	private void OnDifficultySelected(int multiplier, double initialSpeed)
	{
		StartGame(multiplier, initialSpeed);
	}

	private void StartGame(int multiplier, double initialSpeed)
	{
		_scoreMultiplier = multiplier;
		_fallSpeed = initialSpeed;
		_gameState = GameState.Playing;

		_btnEasy.Hide();
		_btnNormal.Hide();
		_btnHard.Hide();
		_btnExtreme.Hide();

		GenerateNextPiece();
		SpawnPiece();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_helloTimer > 0)
		{
			_helloTimer -= delta;
			if (_helloTimer <= 0)
			{
				_helloLabel.QueueFree();
			}
		}

		if (_gameState != GameState.Playing) return;

		_fallTimer += delta;
		if (_fallTimer >= _fallSpeed)
		{
			_fallTimer = 0;
			if (!MovePiece(0, 1))
			{
				PlacePiece();
				ClearLines();
				SpawnPiece();
			}
			QueueRedraw();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (_gameState == GameState.GameOver)
			{
				if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
				{
					RestartGame();
				}
				return;
			}

			if (_gameState != GameState.Playing)
			{
				return;
			}

			if (keyEvent.Keycode == Key.Left)
			{
				MovePiece(-1, 0);
			}
			else if (keyEvent.Keycode == Key.Right)
			{
				MovePiece(1, 0);
			}
			else if (keyEvent.Keycode == Key.Down)
			{
				if (!MovePiece(0, 1))
				{
					PlacePiece();
					ClearLines();
					SpawnPiece();
				}
				_fallTimer = 0;
			}
			else if (keyEvent.Keycode == Key.Up)
			{
				RotatePiece();
			}
			QueueRedraw();
		}
	}

	private void LoadHighscores()
	{
		if (FileAccess.FileExists(HighscorePath))
		{
			using var file = FileAccess.Open(HighscorePath, FileAccess.ModeFlags.Read);
			_highscores.Clear();
			while (!file.EofReached())
			{
				string line = file.GetLine();
				if (int.TryParse(line, out int score))
				{
					_highscores.Add(score);
				}
			}
			_highscores = _highscores.OrderByDescending(s => s).ToList();
		}
	}

	private bool AddHighscore(int score)
	{
		bool isNewHighscore = score > 0 && (_highscores.Count < 10 || score > _highscores.LastOrDefault());

		_highscores.Add(score);
		_highscores = _highscores.OrderByDescending(s => s).Take(10).ToList();

		using var file = FileAccess.Open(HighscorePath, FileAccess.ModeFlags.Write);
		foreach (int s in _highscores)
		{
			file.StoreLine(s.ToString());
		}

		return isNewHighscore;
	}

	private void GenerateNextPiece()
	{
		int index = _random.Next(Shapes.Length);
		_nextShape = (int[,])Shapes[index].Clone();
		_nextColor = index + 1;
	}

	private void SpawnPiece()
	{
		_currentShape = _nextShape;
		_currentColor = _nextColor;
		_currentX = Cols / 2 - _currentShape.GetLength(1) / 2;
		_currentY = 0;

		GenerateNextPiece();

		if (!IsValidPosition(_currentShape, _currentX, _currentY))
		{
			if (AddHighscore(_score))
			{
				_winSoundPlayer.Play();
			}
			_gameState = GameState.GameOver;
			GD.Print("Game Over! Score: " + _score);
		}
	}

	private void RestartGame()
	{
		_board = new int[Rows, Cols];
		_score = 0;
		_level = 1;
		_totalLinesCleared = 0;
		_fallSpeed = 0.5;
		_fallTimer = 0;
		_gameState = GameState.DifficultySelection;
		_hasReachedNewHighscoreThisGame = false;
		_currentHighscore = _highscores.Count > 0 ? _highscores[0] : 0;
		_hasReachedTop10ThisGame = false;
		_top10Threshold = _highscores.Count == 10 ? _highscores[9] : 0;

		_btnEasy.Show();
		_btnNormal.Show();
		_btnHard.Show();
		_btnExtreme.Show();

		QueueRedraw();
	}

	private bool MovePiece(int dx, int dy)
	{
		if (IsValidPosition(_currentShape, _currentX + dx, _currentY + dy))
		{
			_currentX += dx;
			_currentY += dy;
			return true;
		}
		return false;
	}

	private void RotatePiece()
	{
		int size = _currentShape.GetLength(0);
		int[,] newShape = new int[size, size];

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				newShape[x, size - 1 - y] = _currentShape[y, x];
			}
		}

		if (IsValidPosition(newShape, _currentX, _currentY))
		{
			_currentShape = newShape;
		}
	}

	private bool IsValidPosition(int[,] shape, int x, int y)
	{
		int size = shape.GetLength(0);
		for (int r = 0; r < size; r++)
		{
			for (int c = 0; c < size; c++)
			{
				if (shape[r, c] != 0)
				{
					int boardX = x + c;
					int boardY = y + r;

					if (boardX < 0 || boardX >= Cols || boardY >= Rows)
						return false;

					if (boardY >= 0 && _board[boardY, boardX] != 0)
						return false;
				}
			}
		}
		return true;
	}

	private void PlacePiece()
	{
		int size = _currentShape.GetLength(0);
		for (int r = 0; r < size; r++)
		{
			for (int c = 0; c < size; c++)
			{
				if (_currentShape[r, c] != 0)
				{
					int boardY = _currentY + r;
					int boardX = _currentX + c;
					if (boardY >= 0 && boardY < Rows && boardX >= 0 && boardX < Cols)
					{
						_board[boardY, boardX] = _currentColor;
					}
				}
			}
		}
	}

	private void ClearLines()
	{
		int linesCleared = 0;
		for (int y = Rows - 1; y >= 0; y--)
		{
			bool full = true;
			for (int x = 0; x < Cols; x++)
			{
				if (_board[y, x] == 0)
				{
					full = false;
					break;
				}
			}

			if (full)
			{
				linesCleared++;
				for (int yy = y; yy > 0; yy--)
				{
					for (int x = 0; x < Cols; x++)
					{
						_board[yy, x] = _board[yy - 1, x];
					}
				}
				for (int x = 0; x < Cols; x++)
				{
					_board[0, x] = 0;
				}
				y++; // check same line again
			}
		}

		if (linesCleared > 0)
		{
			_totalLinesCleared += linesCleared;
			_level = 1 + (_totalLinesCleared / 10);
			_score += linesCleared * 100 * _scoreMultiplier;
			_fallSpeed = Math.Max(0.05, _fallSpeed * 0.95);
			GD.Print("Score: " + _score);

			if (_score > _top10Threshold && _score > 0 && !_hasReachedTop10ThisGame)
			{
				_hasReachedTop10ThisGame = true;
				_celebrationParticles.Emitting = true;
			}

			if (_score > _currentHighscore && _currentHighscore > 0 && !_hasReachedNewHighscoreThisGame)
			{
				_hasReachedNewHighscoreThisGame = true;
				_winSoundPlayer.Play();
			}
		}
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(XOffset, YOffset, Cols * CellSize, Rows * CellSize), new Color(0.1f, 0.1f, 0.1f));

		if (_gameState == GameState.DifficultySelection)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(XOffset + 20, Rows * CellSize / 2 - 100), "Select Difficulty", HorizontalAlignment.Left, -1, 32, Colors.White);
			// We skip drawing the pieces and next shape below if we are in difficulty selection,
			// but we still want to draw the grid and highscores.
		}
		else
		{
			for (int y = 0; y < Rows; y++)
			{
				for (int x = 0; x < Cols; x++)
				{
					if (_board[y, x] != 0)
					{
						DrawBlock(x, y, ColorsList[_board[y, x]]);
					}
				}
			}

			if (_currentShape != null)
			{
				int size = _currentShape.GetLength(0);
				for (int y = 0; y < size; y++)
				{
					for (int x = 0; x < size; x++)
					{
						if (_currentShape[y, x] != 0)
						{
							DrawBlock(_currentX + x, _currentY + y, ColorsList[_currentColor]);
						}
					}
				}
			}
		}

		for (int y = 0; y <= Rows; y++)
		{
			DrawLine(new Vector2(XOffset, YOffset + y * CellSize), new Vector2(XOffset + Cols * CellSize, YOffset + y * CellSize), new Color(0.3f, 0.3f, 0.3f));
		}
		for (int x = 0; x <= Cols; x++)
		{
			DrawLine(new Vector2(XOffset + x * CellSize, YOffset), new Vector2(XOffset + x * CellSize, YOffset + Rows * CellSize), new Color(0.3f, 0.3f, 0.3f));
		}

		// Draw next shape info
		DrawString(ThemeDB.FallbackFont, new Vector2(10, 50), "Score: " + _score, HorizontalAlignment.Left, -1, 24, Colors.White);
		DrawString(ThemeDB.FallbackFont, new Vector2(10, 80), "Level: " + _level, HorizontalAlignment.Left, -1, 24, Colors.White);

		if (_gameState != GameState.DifficultySelection)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(XOffset + Cols * CellSize + 50, 50), "Next:", HorizontalAlignment.Left, -1, 24, Colors.White);

			if (_nextShape != null)
			{
				int nextSize = _nextShape.GetLength(0);
				int previewXOffset = XOffset + Cols * CellSize + 50;
				int previewYOffset = 80;

				for (int y = 0; y < nextSize; y++)
				{
					for (int x = 0; x < nextSize; x++)
					{
						if (_nextShape[y, x] != 0)
						{
							float px = previewXOffset + x * CellSize;
							float py = previewYOffset + y * CellSize;
							DrawRect(new Rect2(px + 1, py + 1, CellSize - 2, CellSize - 2), ColorsList[_nextColor]);
						}
					}
				}
			}
		}

		int highscoreXOffset = XOffset + Cols * CellSize + 50;
		int highscoreYOffset = 250;
		DrawString(ThemeDB.FallbackFont, new Vector2(highscoreXOffset, highscoreYOffset), "Highscores:", HorizontalAlignment.Left, -1, 24, Colors.White);
		for (int i = 0; i < _highscores.Count; i++)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(highscoreXOffset, highscoreYOffset + 30 + (i * 24)), $"{i + 1}. {_highscores[i]}", HorizontalAlignment.Left, -1, 20, Colors.White);
		}

		if (_gameState == GameState.GameOver)
		{
			// Overlay
			DrawRect(new Rect2(XOffset, YOffset, Cols * CellSize, Rows * CellSize), new Color(0, 0, 0, 0.7f));
			DrawString(ThemeDB.FallbackFont, new Vector2(XOffset + 20, Rows * CellSize / 2 - 20), "GAME OVER", HorizontalAlignment.Left, -1, 40, Colors.Red);
			DrawString(ThemeDB.FallbackFont, new Vector2(XOffset + 10, Rows * CellSize / 2 + 30), "Press ENTER to Restart", HorizontalAlignment.Left, -1, 20, Colors.White);
		}
	}

	private void DrawBlock(int x, int y, Color color)
	{
		float px = XOffset + x * CellSize;
		float py = YOffset + y * CellSize;
		DrawRect(new Rect2(px + 1, py + 1, CellSize - 2, CellSize - 2), color);
	}
}
