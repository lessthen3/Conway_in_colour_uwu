using Godot;
using System;

public partial class LifeGrid : TextureRect
{
    [Export] public int Cols = 128;
    [Export] public int Rows = 128;
    [Export] public int CellSize = 8;
    [Export] public int MajorEvery = 8;

    [Export] public Color GridColor = new(0, 0, 0, 0.35f);
    [Export] public Color MajorGridColor = new(0, 0, 0, 0.6f);

    Image _cells;
    Image _display;
    ImageTexture _texture;

    Image _next;


    void RebuildDisplay()
    {
        _display.Fill(Colors.White);

        // cells
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Cols; x++)
        {
            Color c = _cells.GetPixel(x, y);
            if (c.A <= 0f) continue;

            for (int py = 0; py < CellSize; py++)
            for (int px = 0; px < CellSize; px++)
                _display.SetPixel(
                    x * CellSize + px,
                    y * CellSize + py,
                    c
                );
        }

        // grid lines
        for (int x = 0; x <= Cols; x++)
        {
            Color col = (MajorEvery > 0 && x % MajorEvery == 0)
                ? MajorGridColor : GridColor;

            int px = x * CellSize;
            if (px < _display.GetWidth())
                for (int y = 0; y < _display.GetHeight(); y++)
                    _display.SetPixel(px, y, col);
        }

        for (int y = 0; y <= Rows; y++)
        {
            Color col = (MajorEvery > 0 && y % MajorEvery == 0)
                ? MajorGridColor : GridColor;

            int py = y * CellSize;
            if (py < _display.GetHeight())
                for (int x = 0; x < _display.GetWidth(); x++)
                    _display.SetPixel(x, py, col);
        }

        _texture.Update(_display);
    }

    public void PaintCell(int cx, int cy, Color c)
    {
        if (cx < 0 || cy < 0 || cx >= Cols || cy >= Rows)
            return;

        _cells.SetPixel(cx, cy, c);
        RebuildDisplay();
    }

    public void 
        StepSimulation()
    {

        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Cols; x++)
        {
            int alive = 0;
            Color sum = Colors.Black;

            for (int ny = -1; ny <= 1; ny++)
            for (int nx = -1; nx <= 1; nx++)
            {
                if (nx == 0 && ny == 0) continue;

                int px = x + nx;
                int py = y + ny;

                if (px < 0 || py < 0 || px >= Cols || py >= Rows)
                    continue;

                Color c = _cells.GetPixel(px, py);
                if (c.A > 0f)
                {
                    alive++;
                    sum += c;
                }
            }

            Color self = _cells.GetPixel(x, y);
            bool isAlive = self.A > 0f;

            if (isAlive && (alive == 2 || alive == 3))
            {
                _next.SetPixel(x, y, self);
            }
            else if (!isAlive && alive == 3)
            {
                Color born = sum / 3f;
                born.A = 1f;
                _next.SetPixel(x, y, born);
            }
            else
            {
                _next.SetPixel(x, y, Colors.Transparent);
            }
        }

        (_cells, _next) = (_next, _cells);
        RebuildDisplay();
    }

    public void BeginBatch() { /* no-op for now */ }
public void EndBatch() { RebuildDisplay(); }

public void PaintCellNoRedraw(int cx, int cy, Color c)
{
    if (cx < 0 || cy < 0 || cx >= Cols || cy >= Rows) return;
    _cells.SetPixel(cx, cy, c);
}


    public void ClearAll()
    {
        _cells.Fill(Colors.Transparent);  // kill everything
        _next.Fill(Colors.Transparent);   // keep buffers consistent (if you use _next)
        RebuildDisplay();
    }


    public override void _Ready()
    {
        _next = Image.CreateEmpty(Cols, Rows, false, Image.Format.Rgba8);

        _cells = Image.CreateEmpty(Cols, Rows, false, Image.Format.Rgba8);
        _cells.Fill(Colors.Transparent);

        _display = Image.CreateEmpty(
            Cols * CellSize,
            Rows * CellSize,
            false,
            Image.Format.Rgba8
        );

        _texture = ImageTexture.CreateFromImage(_display);
        Texture = _texture;

        TextureFilter = TextureFilterEnum.Nearest;
        StretchMode = StretchModeEnum.Keep;

        RebuildDisplay();
    }
}

