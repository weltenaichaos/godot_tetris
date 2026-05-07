using Godot;
using System;

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
	private bool _gameOver = false;

	public override void _Ready()
	{
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
		if (_gameOver) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
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

	private void SpawnPiece()
	{
		int index = _random.Next(Shapes.Length);
		_currentShape = (int[,])Shapes[index].Clone();
		_currentColor = index + 1;
		_currentX = Cols / 2 - _currentShape.GetLength(1) / 2;
		_currentY = 0;

		if (!IsValidPosition(_currentShape, _currentX, _currentY))
		{
			_gameOver = true;
			GD.Print("Game Over! Score: " + _score);
		}
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
			_score += linesCleared * 100;
			_fallSpeed = Math.Max(0.1, _fallSpeed - 0.02);
			GD.Print("Score: " + _score);
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

		DrawString(ThemeDB.FallbackFont, new Vector2(10, 50), "Score: " + _score, HorizontalAlignment.Left, -1, 24, Colors.White);
		if (_gameOver)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(10, 100), "GAME OVER", HorizontalAlignment.Left, -1, 32, Colors.Red);
		}
	}

	private void DrawBlock(int x, int y, Color color)
	{
		float px = XOffset + x * CellSize;
		float py = YOffset + y * CellSize;
		DrawRect(new Rect2(px + 1, py + 1, CellSize - 2, CellSize - 2), color);
	}
}
