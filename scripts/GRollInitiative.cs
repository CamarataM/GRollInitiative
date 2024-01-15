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
	public Button ClearAllCreaturesButton;
	[Export]
	public VScrollBar VScrollBar;
	[Export]
	public Label TurnCountTextLabel;

	[Export]
	public Button NextCreatureButton;
	[Export]
	public Button PreviousCreatureButton;

	public string DefaultAvatarPath = "res://resources/test_avatar.png";

	public Button AddCreatureButton;
	public Button EditCreatureButton;
	public TextureRect AddCreatureAvatarTextureRect;
	public ConfirmationDialog ClearAllCreaturesConfirmationDialog;
	public ColorPickerButton TeamColorPickerButton;
	public Button SaveToGalleryButton;
	public Tree GalleryTree;

	public TreeItem ActiveCreatureTreeItem = null;
	public TreeItem EditCreatureTreeItem = null;

	public double EditableCooldown = 0;
	public double EditableDebounce = 0;

	public const string TURN_NUMBER_METADATA_KEY = "turn_number";
	public const string AVATAR_PATH_METADATA_KEY = "avatar_path";
	public const string CREATURE_RESOURCE_METADATA_KEY = "creature_resource";

	// Taken From 01/13/2024 10:31 PM: https://docs.godotengine.org/en/stable/classes/class_filedialog.html#class-filedialog-property-filters
	public List<string> FileDialogFilterStringList = new List<string>() { "*.png, *.jpg, *.jpeg, *.svg, *.tga, *.webp ; Supported Images" };

	public static readonly string DefaultCurrentDirectory = ProjectSettings.GlobalizePath(OS.GetSystemDir(OS.SystemDir.Pictures));
	public string CurrentDirectory = DefaultCurrentDirectory;

	public int TreeIconWidthSize = 60;
	public int GalleryTreeIconWidthSize = 20;
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

		GalleryTree = AddCreatureWindow.GetNode<Tree>("MarginContainer/VBoxContainer/GalleryTree");
		GalleryTree.CreateItem();
		GalleryTree.SetColumnClipContent(0, true);
		GalleryTree.SetColumnCustomMinimumWidth(0, GalleryTreeIconWidthSize);

		GalleryTree.SetColumnClipContent(1, true);
		GalleryTree.SetColumnCustomMinimumWidth(1, 100);
		GalleryTree.SetColumnExpand(1, true);
		GalleryTree.SetColumnExpandRatio(1, 3);

		// Handle visibility toggle.
		AddCreatureToggleWindowButton.Pressed += () => {
			AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(GD.Load<Image>(DefaultAvatarPath));

			AddCreatureButton.Visible = true;
			EditCreatureButton.Visible = false;

			AddCreatureWindow.Visible = !AddCreatureWindow.Visible;
		};

		// Handle close button pressed by hiding visibility of the window.
		AddCreatureWindow.CloseRequested += () => {
			AddCreatureWindow.Visible = false;
			EditCreatureTreeItem = null;
		};

		this.GetWindow().FilesDropped += (string[] files) => {
			if (AddCreatureWindow.Visible) {
				foreach (var filePath in files) {
					if (System.IO.File.Exists(filePath)) {
						AddOrEditCreatureImageCallback(true, new string[] { filePath }, 0);
					} else {
						GD.PrintErr("Detected drag-drop with invalid file '" + filePath + "'");
					}
				}
			}
		};

		AddCreatureAvatarTextureRect = AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage");

		SaveToGalleryButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/TeamColorHBoxContainer/SaveToGalleryButton");

		AddCreatureButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/AddButton");
		EditCreatureButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/EditButton");
		foreach (var button in new Button[] { AddCreatureButton, EditCreatureButton }) {
			button.Pressed += () => {
				TreeItem treeItem = null;
				if (EditCreatureTreeItem != null) {
					treeItem = EditCreatureTreeItem;
				} else {
					treeItem = Tree.CreateItem();
				}

				// Write the CreatureResource to the current tree item.
				CreateInitiativeTreeItemFromCreatureResource(GetCreatureResourceFromAddCreatureWindow(), treeItem: treeItem);

				if (EditCreatureTreeItem != null) {
					AddCreatureWindow.Visible = false;
					EditCreatureTreeItem = null;
				}
			};
		}

		Tree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					int treeChildCount = Tree.GetRoot().GetChildCount();
					for (int i = 0; i < treeChildCount; i++) {
						var selectedItem = Tree.GetNextSelected(null);

						if (selectedItem != null) {
							if (selectedItem == ActiveCreatureTreeItem) {
								ActiveCreatureTreeItem = selectedItem.GetPrevInTree();
							}

							selectedItem.GetParent().RemoveChild(selectedItem);
						} else {
							break;
						}
					}

					UpdateUI();
				}

				if (inputEventKey.Pressed && inputEventKey.CtrlPressed && inputEventKey.Keycode == Key.A) {
					foreach (var child in Tree.GetRoot().GetChildren()) {
						for (int i = 0; i < child.GetTree().Columns; i++) {
							child.Select(i);
						}
					}
				}
			}

			if (inputEvent is InputEventMouseButton inputEventMouseButton) {
				if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick) {
					var treeItem = Tree.GetItemAtPosition(inputEventMouseButton.GlobalPosition);

					if (treeItem != null) {
						EditCreatureTreeItem = treeItem;

						AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage").Texture = treeItem.GetIcon(0);
						AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text = treeItem.GetText(1);
						AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value = (double) treeItem.GetMetadata(2);

						AddCreatureButton.Visible = false;
						EditCreatureButton.Visible = true;

						AddCreatureWindow.Visible = true;
					}
				}
			}
		};

		GalleryTree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventMouseButton inputEventMouseButton) {
				if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick) {
					var treeItem = GalleryTree.GetItemAtPosition(inputEventMouseButton.Position);

					if (treeItem != null) {
						CreateInitiativeTreeItemFromCreatureResource((CreatureResource) treeItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY));
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
			OpenCreatureAvatarFileDialog(nameof(AddOrEditCreatureImageCallback));
		};

		TeamColorPickerButton = AddCreatureWindow.GetNode<ColorPickerButton>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/TeamColorHBoxContainer/ColorPickerButton");
		TeamColorPickerButton.Color = new Color(0, 1f, 0, 0.5f);
		TeamColorPickerButton.GetPicker().AddPreset(new Color(0, 1f, 0, 0.5f));
		TeamColorPickerButton.GetPicker().AddPreset(new Color(1f, 0, 0, 0.5f));
		TeamColorPickerButton.GetPicker().AddPreset(new Color(0, 0f, 1f, 0.5f));
		TeamColorPickerButton.GetPicker().PresetsVisible = true;

		TeamColorPickerButton.VisibilityChanged += () => {
			// TODO: Doesn't seem to work as expected, which would be showing the presets on visibility change. Also doesn't work in process, so it might be bugged / only control showing the swatch button rather than actually opening the swatches?
			TeamColorPickerButton.GetPicker().Set(ColorPicker.PropertyName.PresetsVisible, true);
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

		ClearAllCreaturesButton.Pressed += () => {
			if (ClearAllCreaturesConfirmationDialog == null) {
				ClearAllCreaturesConfirmationDialog = new ConfirmationDialog();

				AddChild(ClearAllCreaturesConfirmationDialog);

				ClearAllCreaturesConfirmationDialog.Confirmed += () => {
					foreach (var child in Tree.GetRoot().GetChildren()) {
						Tree.GetRoot().RemoveChild(child);
					}
				};
			}

			ClearAllCreaturesConfirmationDialog.Position = (Vector2I) (this.GetWindow().GetPositionWithDecorations() + (this.GetWindow().GetSizeWithDecorations() * new Vector2(0.5f, 0.5f) - (ClearAllCreaturesConfirmationDialog.GetSizeWithDecorations() * new Vector2(0.5f, 0.5f))));
			ClearAllCreaturesConfirmationDialog.Visible = true;

			ClearAllCreaturesConfirmationDialog.DialogText = "Do you want to clear all " + Tree.GetRoot().GetChildCount() + " creatures?";
		};

		SaveToGalleryButton.Pressed += () => {
			var treeItem = CreateGeneralTreeItemFromCreatureResource(GalleryTree, GetCreatureResourceFromAddCreatureWindow());
			treeItem.SetIconMaxWidth(0, GalleryTreeIconWidthSize);
		};

		// TODO: Implement gallery feature, which allows for entries to be saved and used later.
		// 		 - Will need to have method to convert name-image pairs to file (easy).
		// 		 - Will need to handle missing images (easy).

		// TODO: Implement auto-saving of previous session (maybe not full save and load functionality, although with the OS-native file picker, it would be a lot easier).
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

	public void CreateInitiativeTreeItemFromCreatureResource(CreatureResource creatureResource, TreeItem treeItem = null) {
		var newTreeItem = CreateGeneralTreeItemFromCreatureResource(Tree, creatureResource: creatureResource, treeItem: treeItem);

		// Set the initiative column.
		newTreeItem.SetText(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value.ToString());
		newTreeItem.SetMetadata(2, AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value);
		newTreeItem.SetExpandRight(2, false);

		UpdateUI();
	}

	public TreeItem CreateGeneralTreeItemFromCreatureResource(Tree tree, CreatureResource creatureResource, TreeItem treeItem = null) {
		if (treeItem == null) {
			treeItem = tree.CreateItem();
		}

		treeItem.SetMeta(CREATURE_RESOURCE_METADATA_KEY, creatureResource);

		Image avatarImage = null;
		if (creatureResource.AvatarPath == DefaultAvatarPath) {
			// Load the image as a resource from the internal resources.
			avatarImage = GD.Load<Image>(creatureResource.AvatarPath);
		} else {
			// Load the image from the filesystem.
			avatarImage = Image.LoadFromFile(creatureResource.AvatarPath);
		}

		treeItem.SetIcon(0, ImageTexture.CreateFromImage(avatarImage));
		treeItem.SetIconMaxWidth(0, TreeIconWidthSize);
		treeItem.SetCustomBgColor(0, ((CreatureResource) treeItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY)).TeamColor);

		treeItem.SetText(1, creatureResource.Name);
		treeItem.SetAutowrapMode(1, TextServer.AutowrapMode.WordSmart);
		treeItem.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);
		treeItem.SetExpandRight(1, true);

		return treeItem;
	}

	public CreatureResource GetCreatureResourceFromAddCreatureWindow() {
		return new CreatureResource((string) AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY, DefaultAvatarPath), AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text, TeamColorPickerButton.Color);
	}

	public void OpenCreatureAvatarFileDialog(StringName callbackFunctionStringName) {
		DisplayServer.FileDialogShow("Select creature avatar image...", CurrentDirectory, "", false, DisplayServer.FileDialogMode.OpenFile, FileDialogFilterStringList.ToArray(), new Callable(this, callbackFunctionStringName));
	}

	public void AddOrEditCreatureImageCallback(bool status, string[] selectedPaths, int selectedFilterIndex) {
		if (status && selectedPaths.Length > 0) {
			// TODO: Log on error.
			try {
				var path = selectedPaths[0];

				if (System.IO.File.Exists(path)) {
					AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(path));

					CurrentDirectory = path;
					AddCreatureAvatarTextureRect.SetMeta(AVATAR_PATH_METADATA_KEY, path);
				}
			} catch (Exception) {
			}
		}
	}

	public void RemoveHighlightTreeItem(TreeItem treeItem) {
		for (int i = 1; i < treeItem.GetTree().Columns; i++) {
			treeItem.ClearCustomBgColor(i);
		}
	}

	public void HighlightTreeItem(TreeItem treeItem) {
		for (int i = 0; i < treeItem.GetTree().Columns; i++) {
			treeItem.SetCustomBgColor(i, ((CreatureResource) treeItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY)).TeamColor);
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

			treeItem.SetCustomBgColor(0, ((CreatureResource) treeItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY)).TeamColor);

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