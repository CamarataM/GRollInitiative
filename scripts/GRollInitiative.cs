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
	[Export]
	public Label TurnCountTextLabel;

	[Export]
	public Button NextCreatureButton;
	[Export]
	public Button PreviousCreatureButton;

	public Button AddCreatureButton;

	public TreeItem ActiveCreatureTreeItem = null;

	public TreeItem MakeEditableTreeItem = null;
	public double EditableCooldown = 0;
	public double EditableDebounce = 0;

	public const string TURN_NUMBER_METADATA_KEY = "turn_number";

	public override void _Ready() {
		TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, 0);

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

			UpdateUI();
		};

		Tree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					var selectedItem = Tree.GetSelected();

					if (selectedItem != null) {
						selectedItem.GetParent().RemoveChild(selectedItem);
						UpdateUI();
					}
				}
			}

			// TODO: This is currently bugged and not fully functional.
			if (inputEvent is InputEventMouseButton inputEventMouseButton) {
				if (EditableDebounce <= 0 && inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left) {
					var treeItem = Tree.GetItemAtPosition(inputEventMouseButton.GlobalPosition);

					if (treeItem != null) {
						treeItem.CallDeferred(TreeItem.MethodName.SetEditable, 1, true);
						treeItem.CallDeferred(TreeItem.MethodName.SetEditable, 2, true);

						if (EditableCooldown > 0 && Tree.GetColumnAtPosition(inputEventMouseButton.GlobalPosition) == 0) {
							GD.Print("TODO: Open image selector.");
						}

						EditableCooldown = 0.5;
						EditableDebounce = 0.1;
					}
				}
			}
		};

		Tree.ItemEdited += () => {
			UpdateUI();
		};

		NextCreatureButton.Pressed += () => {
			OffsetCurrentTreeItem(1);
		};

		PreviousCreatureButton.Pressed += () => {
			OffsetCurrentTreeItem(-1);
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

	public void RemoveHighlightTreeItem(TreeItem treeItem) {
		for (int i = 0; i < treeItem.GetTree().Columns; i++) {
			treeItem.ClearCustomBgColor(i);
		}
	}

	public void HighlightTreeItem(TreeItem treeItem) {
		for (int i = 0; i < treeItem.GetTree().Columns; i++) {
			treeItem.SetCustomBgColor(i, new Color(0, 1, 0, 0.5f));
		}
	}

	public void OffsetCurrentTreeItem(int offset) {
		if (offset != 0) {
			TreeItem nextCreatureTreeItem = null;

			if (ActiveCreatureTreeItem != null) {
				RemoveHighlightTreeItem(ActiveCreatureTreeItem);

				for (int i = 0; i < Math.Abs(offset); i++) {
					if (offset > 0) {
						nextCreatureTreeItem = ActiveCreatureTreeItem.GetNextInTree();
					} else {
						nextCreatureTreeItem = ActiveCreatureTreeItem.GetPrevInTree();
					}
				}

				if (nextCreatureTreeItem == null) {
					TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY).AsInt32() + Math.Sign(offset));
				}
			}

			if (nextCreatureTreeItem == null) {
				if (offset > 0) {
					nextCreatureTreeItem = Tree.GetRoot().GetChild(0);
				} else {
					nextCreatureTreeItem = Tree.GetRoot().GetChild(Tree.GetRoot().GetChildCount() - 1);
				}
			}

			ActiveCreatureTreeItem = nextCreatureTreeItem;
		}

		HighlightTreeItem(ActiveCreatureTreeItem);

		UpdateUI();
	}

	public void UpdateUI() {
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

		TurnCountTextLabel.Text = "Turn " + TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY, 0);
	}
}