using Godot;
using System;

namespace ConwaysGame;

public partial class ConwaysGame : Node2D
{
    LifeGrid _grid;
    ColorPickerButton _picker;

	TextureButton pm_PlayButton;
	TextureButton pm_PauseButton;
	TextureButton pm_ResetButton;

	Timer pm_Ticker;

	HSlider pm_Speed;

	HBoxContainer pm_ColourPalette;

	Label pm_CurrentBrushSize;

	TextureButton[] _swatches = new TextureButton[8];
	Color[] _palette = new Color[8];
	ColorRect[] _fills = new ColorRect[8];
	int _activeIndex = 0;

	ColorRect _currentPreview;

    public override void _Ready()
    {
        _grid = GetNode<LifeGrid>("Canny/Life_Grid");
        _picker = GetNode<ColorPickerButton>("Canny/UI/Colour_Picker");

		pm_PlayButton = GetNode<TextureButton>("Canny/UI/Play_Button");
		pm_PauseButton = GetNode<TextureButton>("Canny/UI/Pause_Button");
		pm_ResetButton = GetNode<TextureButton>("Canny/UI/Reset_Button");

		pm_Ticker = GetNode<Timer>("Ticker");

		pm_Speed = GetNode<HSlider>("Canny/UI/Speed");

		pm_CurrentBrushSize = GetNode<Label>("Canny/UI/Brush_Size");

		pm_CurrentBrushSize.Text = $"Brush: {1}×{1}";

		pm_ColourPalette = GetNode<HBoxContainer>("Canny/UI/Palette_Bar");

		for (int i = 0; i < 8; i++)
		{
			_swatches[i] = pm_ColourPalette.GetNode<TextureButton>($"Swatch{i}");
			_fills[i] = _swatches[i].GetNode<ColorRect>("Fill");

			int idx = i;

			// default palette (whatever you want)
			_palette[i] = Colors.White ; 
			ApplySwatchVisual(i);

			_swatches[i].Pressed += () => OnSwatchPressed(idx);
		}

		pm_Speed.ValueChanged += OnSpeedChange;

		pm_Ticker.Timeout += OnTick;

		pm_PlayButton.Pressed += OnPlayPressed;
		pm_PauseButton.Pressed += OnPausePressed;
		pm_ResetButton.Pressed += OnClearPressed;

		pm_Speed.MinValue = 1;
		pm_Speed.MaxValue = 30;
		pm_Speed.Step = 1;

		pm_Speed.Value = 10; // default speed
		OnSpeedChange(pm_Speed.Value);

		_currentPreview = GetNode<ColorRect>("Canny/UI/Current_Colour_Preview");

		_picker.ColorChanged += c =>
		{
			_currentPreview.Color = c;
			// optional: also update active palette slot to follow brush color automatically:
			// _palette[_activeIndex] = c; ApplySwatchVisual(_activeIndex);
		};

		_currentPreview.Color = _picker.Color;
    }

void ApplySwatchVisual(int idx)
{
    if ((uint)idx >= 8u) return;

    var fill = _fills[idx];
    if (fill == null) return;

    fill.Color = _palette[idx];
}


void OnSwatchPressed(int idx)
{
    bool shiftHeld =
        Input.IsKeyPressed(Key.Shift);

    if (shiftHeld)
    {
        // FORCE opaque unless you actually want transparent swatches
        var c = _picker.Color;
        c.A = 1.0f;

        _palette[idx] = c;
        ApplySwatchVisual(idx);
    }
    else
    {
        _activeIndex = idx;
        _picker.Color = _palette[idx];
        for (int i = 0; i < 8; i++) ApplySwatchVisual(i);
    }
}


	void OnBrushSizeChanged(double value)
	{
		pm_CurrentBrushSize.Text = $"Brush: {value}×{value}";
	}

	void OnPlayPressed()
	{
		pm_Ticker.Start();   // starts ticking
	}

	void OnPausePressed()
	{
		pm_Ticker.Stop();
	}

	void OnTick()
	{
		_grid.StepSimulation();
	}

	void OnClearPressed()
	{
		pm_Ticker.Stop();
		_grid.ClearAll();
	}


	void OnSpeedChange(double fp_Val)
	{
		// value = steps per second
		double stepsPerSecond = Math.Max(fp_Val, 0.1);
		pm_Ticker.WaitTime = 1.0 / stepsPerSecond;
	}

	bool _painting = false;

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
				_painting = mb.Pressed;
		}

		if (e is InputEventMouseMotion && _painting)
			PaintAtMouse();

		if (e is InputEventMouseButton mubby && mubby.Pressed)
    	{
			if (mubby.ButtonIndex == MouseButton.WheelUp && Input.IsKeyPressed(Key.Ctrl))
				ChangeBrush(1);

			if (mubby.ButtonIndex == MouseButton.WheelDown && Input.IsKeyPressed(Key.Ctrl))
				ChangeBrush(-1);
		}

	if (e is InputEventKey k && k.Pressed && !k.Echo)
    {
        if (k.Keycode == Key.Escape)
            GetTree().Quit();
    }
	}

	int _brushRadius = 0; // 0 = 1 cell, 1 = 3x3, 2 = 5x5 ...
	bool _circleBrush = true;

	void ChangeBrush(int delta)
	{
		_brushRadius = Mathf.Clamp(_brushRadius + delta, 0, 10);

		int diameter = _brushRadius * 2 + 1;

		OnBrushSizeChanged(diameter);
	}

	void PaintAtMouse()
	{
		Vector2 local = _grid.GetLocalMousePosition();
		int cx = Mathf.FloorToInt(local.X / _grid.CellSize);
		int cy = Mathf.FloorToInt(local.Y / _grid.CellSize);

		PaintBrush(cx, cy, _picker.Color);
	}

	void PaintBrush(int cx, int cy, Color color)
	{
		int r = _brushRadius;

		_grid.BeginBatch();

		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
		{
			if (_circleBrush && (dx*dx + dy*dy) > r*r)
				continue;

			_grid.PaintCellNoRedraw(cx + dx, cy + dy, color);
		}

		_grid.EndBatch();
	}
}
