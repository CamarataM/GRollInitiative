using Godot;
using System;

public partial class GRollInitiative : Control {
	[Export]
	public Tree Tree;
	[Export]
	public Window AddCreatureWindow;
	[Export]
	public Button AddCreatureToggleWindowButton;
	[Export]
	public VScrollBar ScrollBar;

	public Button AddCreatureButton;

	public override void _Ready() {
		Tree.CreateItem();
		Tree.SetColumnClipContent(0, true);

		// Handle visibility toggle.
		AddCreatureToggleWindowButton.Pressed += () => {
			AddCreatureWindow.Visible = !AddCreatureWindow.Visible;
		};

		// Handle close button pressed by hiding visibility of the window.
		AddCreatureWindow.CloseRequested += () => {
			AddCreatureWindow.Visible = false;
		};

		AddCreatureButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/AddButton");
		AddCreatureButton.Pressed += () => {
			var newItem = Tree.CreateItem();

			newItem.SetIcon(0, GD.Load<Texture2D>("res://resources/test_avatar.png"));
			newItem.SetIconMaxWidth(0, 60);

			newItem.SetText(1, AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text);
			newItem.SetAutowrapMode(1, TextServer.AutowrapMode.WordSmart);
			newItem.SetText(2, "" + AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value);

			for (int i = 1; i < Tree.Columns; i++) {
				newItem.SetEditable(i, true);
			}
		};
	}

	public override void _Process(double delta) {
		// If minimized, set the visibility of the window to false.
		if (AddCreatureWindow.Mode == Window.ModeEnum.Minimized) {
			AddCreatureWindow.Visible = false;
		}

		AddCreatureWindow.Position = (Vector2I) (this.GetWindow().Position + AddCreatureToggleWindowButton.GlobalPosition + new Vector2(AddCreatureToggleWindowButton.GetRect().Size.X, 0) + new Vector2(3, 0));

		foreach (var child in Tree.GetChildren(includeInternal: true)) {
			if (child is VScrollBar vScrollBar) {
				vScrollBar.Visible = false;
			}

			if (child is HScrollBar hScrollBar) {
				hScrollBar.Visible = false;
			}
		}

		foreach (var child in Tree.GetChildren(includeInternal: true)) {
			if (child is VScrollBar vScrollBar) {
				ScrollBar.MinValue = vScrollBar.MinValue;
				ScrollBar.MaxValue = vScrollBar.MaxValue;
				ScrollBar.Step = vScrollBar.Step;
				ScrollBar.CustomStep = vScrollBar.CustomStep;
				ScrollBar.Page = vScrollBar.Page;

				vScrollBar.Ratio = ScrollBar.Ratio;
			}
		}
	}
}