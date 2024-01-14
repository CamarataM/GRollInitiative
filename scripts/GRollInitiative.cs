using Godot;
using System;
using System.Collections.Generic;

public partial class GRollInitiative : Control {
	[Export]
	public Tree Tree;
	[Export]
	public Window AddCreatureWindow;
	[Export]
	public Button AddCreatureToggleWindowButton;
	[Export]
	public Button AlwaysOnTopToggleButton;
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

	// Taken From 01/13/2024 10:31 PM: https://docs.godotengine.org/en/stable/classes/class_filedialog.html#class-filedialog-property-filters
	public List<string> FileDialogFilterStringList = new List<string>() { "*.png, *.jpg, *.jpeg, *.svg, *.tga, *.webp ; Supported Images" };
	public string DefaultCurrentDirectory = ProjectSettings.GlobalizePath(System.Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Pictures"));

	public int TreeIconWidthSize = 60;
	public double PreviousRatio = 0;

	public override void _Ready() {
		TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, 0);

		Tree.CreateItem();
		Tree.SetColumnClipContent(0, true);
		Tree.SetColumnCustomMinimumWidth(0, TreeIconWidthSize);

		Tree.SetColumnClipContent(1, true);
		Tree.SetColumnCustomMinimumWidth(1, 100);
		Tree.SetColumnExpand(1, true);
		Tree.SetColumnExpandRatio(1, 3);

		Tree.SetColumnCustomMinimumWidth(2, 10);

		// Handle visibility toggle.
		AddCreatureToggleWindowButton.Pressed += () => {
			AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage").Texture = GD.Load<Texture2D>("res://resources/test_avatar.png");

			AddCreatureWindow.Visible = !AddCreatureWindow.Visible;
		};

		// Handle close button pressed by hiding visibility of the window.
		AddCreatureWindow.CloseRequested += () => {
			AddCreatureWindow.Visible = false;
		};

		AddCreatureButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/AddButton");
		AddCreatureButton.Pressed += () => {
			var newItem = Tree.CreateItem();

			// newItem.SetIcon(0, GD.Load<Texture2D>("res://resources/test_avatar.png"));
			newItem.SetIcon(0, AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage").Texture);
			newItem.SetIconMaxWidth(0, TreeIconWidthSize);

			newItem.SetText(1, AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text);
			newItem.SetAutowrapMode(1, TextServer.AutowrapMode.WordSmart);
			newItem.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);
			newItem.SetExpandRight(1, true);

			newItem.SetText(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value.ToString());
			newItem.SetMetadata(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value);
			newItem.SetExpandRight(2, false);

			UpdateUI();
		};

		Tree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					var selectedItem = Tree.GetSelected();

					if (selectedItem != null) {
						if (selectedItem == ActiveCreatureTreeItem) {
							ActiveCreatureTreeItem = selectedItem.GetPrevInTree();
						}

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
							OpenFileDialog(nameof(EditCreatureImageCallback));
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
			OffsetActiveTreeItem(1);
		};

		PreviousCreatureButton.Pressed += () => {
			OffsetActiveTreeItem(-1);
		};

		AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton").Pressed += () => {
			OpenFileDialog(nameof(AddCreatureImageCallback));
		};

		AlwaysOnTopToggleButton.Toggled += (bool toggled) => {
			this.GetWindow().AlwaysOnTop = toggled;

			// Transient windows cannot be always-on-top (and vice-versa), so we need to toggle that off in the correct order to avoid an error.
			if (toggled) {
				AddCreatureWindow.Transient = false;
				AddCreatureWindow.AlwaysOnTop = true;
			} else {
				AddCreatureWindow.AlwaysOnTop = false;
				AddCreatureWindow.Transient = true;
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
			if (child is VScrollBar treeVScrollBar) {
				// Copy the parameters of the Tree scrollbar to the actual one.
				VScrollBar.MinValue = treeVScrollBar.MinValue;
				VScrollBar.MaxValue = treeVScrollBar.MaxValue;
				VScrollBar.Step = treeVScrollBar.Step;
				VScrollBar.CustomStep = treeVScrollBar.CustomStep;
				VScrollBar.Page = treeVScrollBar.Page;

				// Copy the ratio from the actual scrollbar to the tree scrollbar.
				if (PreviousRatio != VScrollBar.Ratio) {
					treeVScrollBar.Ratio = VScrollBar.Ratio;
				} else if (PreviousRatio != treeVScrollBar.Ratio) {
					VScrollBar.Ratio = treeVScrollBar.Ratio;
				}

				PreviousRatio = VScrollBar.Ratio;
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

	public void OpenFileDialog(StringName callbackFunctionStringName) {
		// TODO: Save previous valid path and open to that one instead of default.
		DisplayServer.FileDialogShow("Select creature avatar image...", DefaultCurrentDirectory, "", false, DisplayServer.FileDialogMode.OpenFile, FileDialogFilterStringList.ToArray(), new Callable(this, callbackFunctionStringName));
	}

	public void AddCreatureImageCallback(bool status, string[] selectedPaths, int selectedFilterIndex) {
		// TODO: Better validity checks.
		if (status && selectedPaths.Length > 0) {
			try {
				AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage").Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(selectedPaths[0]));
			} catch (Exception) {
			}
		}
	}

	public void EditCreatureImageCallback(bool status, string[] selectedPaths, int selectedFilterIndex) {
		if (status && selectedPaths.Length > 0) {
			try {
				Tree.GetSelected().SetIcon(0, ImageTexture.CreateFromImage(Image.LoadFromFile(selectedPaths[0])));
			} catch (Exception) {
			}
		}
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

	public void OffsetActiveTreeItem(int offset) {
		if (offset != 0) {
			TreeItem nextCreatureTreeItem = null;

			if (ActiveCreatureTreeItem != null) {
				RemoveHighlightTreeItem(ActiveCreatureTreeItem);

				nextCreatureTreeItem = ActiveCreatureTreeItem;
				for (int i = 0; i < Math.Abs(offset); i++) {
					if (nextCreatureTreeItem == null) {
						break;
					}

					if (offset > 0) {
						nextCreatureTreeItem = nextCreatureTreeItem.GetNextInTree();
					} else {
						nextCreatureTreeItem = nextCreatureTreeItem.GetPrevInTree();
					}
				}
			}

			if (nextCreatureTreeItem == null && Tree.GetRoot().GetChildCount() > 0) {
				if (offset > 0) {
					nextCreatureTreeItem = Tree.GetRoot().GetChild(0);
				} else {
					nextCreatureTreeItem = Tree.GetRoot().GetChild(Tree.GetRoot().GetChildCount() - 1);
				}

				TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY).AsInt32() + Math.Sign(offset));
			}

			ActiveCreatureTreeItem = nextCreatureTreeItem;
		}

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

		if (ActiveCreatureTreeItem != null) {
			HighlightTreeItem(ActiveCreatureTreeItem);
		}

		TurnCountTextLabel.Text = "Turn " + TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY, 0);
	}
}