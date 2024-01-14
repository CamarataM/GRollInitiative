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
	public VScrollBar VScrollBar;

	public Button AddCreatureButton;

	public TreeItem MakeEditableTreeItem = null;
	public double EditableCooldown = 0;
	public double EditableDebounce = 0;
	public override void _Ready() {
		Tree.CreateItem();
		Tree.SetColumnClipContent(0, true);
		Tree.SetColumnClipContent(1, true);

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
			newItem.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);

			newItem.SetText(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value.ToString());
			newItem.SetMetadata(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value);

			RefreshItems();
		};

		Tree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					var selectedItem = Tree.GetSelected();

					if (selectedItem != null) {
						selectedItem.GetParent().RemoveChild(selectedItem);
						RefreshItems();
					}
				}
			}

			// TODO: This is currently bugged and not fully functional.
			if (inputEvent is InputEventMouseButton inputEventMouseButton) {
				if (EditableDebounce <= 0 && inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left) {
					MakeEditableTreeItem = Tree.GetItemAtPosition(inputEventMouseButton.GlobalPosition);

					if (EditableCooldown > 0 && Tree.GetColumnAtPosition(inputEventMouseButton.GlobalPosition) == 0) {
						GD.Print("TODO: Open image selector.");
					}

					EditableCooldown = 0.5;
					EditableDebounce = 0.1;
				}
			}
		};

		Tree.ItemEdited += () => {
			RefreshItems();
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
				// Copy the parameters of the Tree scrollbar to the actual one.
				VScrollBar.MinValue = vScrollBar.MinValue;
				VScrollBar.MaxValue = vScrollBar.MaxValue;
				VScrollBar.Step = vScrollBar.Step;
				VScrollBar.CustomStep = vScrollBar.CustomStep;
				VScrollBar.Page = vScrollBar.Page;

				// Copy the ratio from the actual scrollbar to the tree scrollbar.
				vScrollBar.Ratio = VScrollBar.Ratio;
			}
		}

		if (MakeEditableTreeItem != null) {
			MakeEditableTreeItem.SetEditable(1, true);
			MakeEditableTreeItem.SetEditable(2, true);

			MakeEditableTreeItem = null;
		}

		if (EditableCooldown <= 0) {
			foreach (var treeItem in Tree.GetRoot().GetChildren()) {
				treeItem.SetEditable(1, false);
				// This is needed to ensure that the word's autowrap correctly if they are changed.
				treeItem.SetAutowrapMode(1, TextServer.AutowrapMode.WordSmart);
				treeItem.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);

				treeItem.SetEditable(2, false);
			}
		}

		EditableCooldown -= delta;
		EditableDebounce -= delta;
	}

	public void RefreshItems() {
		foreach (var treeItem in Tree.GetRoot().GetChildren()) {
			// Set the current metadata value from the text value. If the value cannot be parsed, set the text to the previous metadata value.
			if (double.TryParse(treeItem.GetText(2), out double parsedValue)) {
				treeItem.SetMetadata(2, parsedValue);
			} else {
				treeItem.SetText(2, treeItem.GetMetadata(2).ToString());
			}

			for (int i = 0; i < treeItem.GetParent().GetChildCount(); i++) {
				var prevTreeItem = treeItem.GetPrevInTree();

				if (prevTreeItem == null || prevTreeItem == treeItem) {
					break;
				}

				if ((double) treeItem.GetMetadata(2) > (double) prevTreeItem.GetMetadata(2)) {
					treeItem.MoveBefore(prevTreeItem);
				}
			}
		}
	}
}