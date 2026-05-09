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
		Colors.Blue,
		Colors.Orange,
		Colors.Yellow,
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
		new int[,] { {7, 7, 0}, {0, 7, 7}, {0, 0, 0} }  // Z
	};

	private int _score = 0;
	private int _level = 1;
	private int _totalLinesCleared = 0;
	private bool _gameOver = false;

	private List<int> _highscores = new List<int>();
	private const string HighscorePath = "user://highscore.txt";
	private AudioStreamPlayer _winSoundPlayer;
	private bool _hasReachedNewHighscoreThisGame = false;
	private int _currentHighscore = 0;

	public override void _Ready()
	{
		_winSoundPlayer = new AudioStreamPlayer();
		_winSoundPlayer.Stream = GD.Load<AudioStream>("res://win.wav");
		AddChild(_winSoundPlayer);

		LoadHighscores();
		_currentHighscore = _highscores.Count > 0 ? _highscores[0] : 0;

		GenerateNextPiece();
		SpawnPiece();
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (_gameOver) return;

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
			if (_gameOver)
			{
				if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
				{
					RestartGame();
				}
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

	private void AddHighscore(int score)
	{
		_highscores.Add(score);
		_highscores = _highscores.OrderByDescending(s => s).Take(10).ToList();

		using var file = FileAccess.Open(HighscorePath, FileAccess.ModeFlags.Write);
		foreach (int s in _highscores)
		{
			file.StoreLine(s.ToString());
		}
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
			AddHighscore(_score);
			_gameOver = true;
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
		_gameOver = false;
		_hasReachedNewHighscoreThisGame = false;
		_currentHighscore = _highscores.Count > 0 ? _highscores[0] : 0;
		GenerateNextPiece();
		SpawnPiece();
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
			_score += linesCleared * 100;
			_fallSpeed = Math.Max(0.1, _fallSpeed - 0.02);
			GD.Print("Score: " + _score);

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

		int highscoreXOffset = XOffset + Cols * CellSize + 50;
		int highscoreYOffset = 250;
		DrawString(ThemeDB.FallbackFont, new Vector2(highscoreXOffset, highscoreYOffset), "Highscores:", HorizontalAlignment.Left, -1, 24, Colors.White);
		for (int i = 0; i < _highscores.Count; i++)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(highscoreXOffset, highscoreYOffset + 30 + (i * 24)), $"{i + 1}. {_highscores[i]}", HorizontalAlignment.Left, -1, 20, Colors.White);
		}

		if (_gameOver)
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
