using Godot;
using Slugify;
using System;
using System.Collections.Generic;

public partial class GRollInitiative : Control {
	[Export]
	public VBoxContainer CreatureVBoxContainer;
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

	[Export]
	public ConfirmationDialog EditPropertyCellConfirmationDialog;

	public static GRollInitiative Instance;

	public static string DefaultAvatarPath = "res://resources/test_avatar.png";
	public static string DefaultGalleryFolderPath = ProjectSettings.GlobalizePath("user://gallery");

	public Button AddCreatureButton;
	public TextureRect AddCreatureAvatarTextureRect;
	public ConfirmationDialog ClearAllCreaturesConfirmationDialog;
	public ColorPickerButton TeamColorPickerButton;
	public Button SaveToGalleryButton;
	public Tree GalleryTree;

	public CreatureControl ActiveCreatureControl = null;

	public AcceptDialog NameInvalidConfirmationDialog;

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

	// public const string GALLERY_RESOURCE_FILE_EXTENSION = ".tres";
	public const string GALLERY_RESOURCE_FILE_EXTENSION = ".json";
	public static readonly SlugHelper SlugHelper = new SlugHelper();

	public override void _Ready() {
		GRollInitiative.Instance = this;

		System.IO.Directory.CreateDirectory(DefaultGalleryFolderPath);

		TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, 0);

		GalleryTree = AddCreatureWindow.GetNode<Tree>("MarginContainer/VBoxContainer/GalleryTree");
		GalleryTree.CreateItem();
		GalleryTree.SetColumnClipContent(0, true);
		GalleryTree.SetColumnCustomMinimumWidth(0, GalleryTreeIconWidthSize);

		GalleryTree.SetColumnClipContent(1, true);
		GalleryTree.SetColumnCustomMinimumWidth(1, 100);
		GalleryTree.SetColumnExpand(1, true);
		GalleryTree.SetColumnExpandRatio(1, 3);

		RefreshGallery();

		// Handle visibility toggle.
		AddCreatureToggleWindowButton.Pressed += () => {
			RefreshGallery();

			AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(GD.Load<Image>(DefaultAvatarPath));

			AddCreatureButton.Visible = true;

			AddCreatureWindow.Visible = !AddCreatureWindow.Visible;
		};

		// Handle close button pressed by hiding visibility of the window.
		AddCreatureWindow.CloseRequested += () => {
			AddCreatureWindow.Visible = false;
		};

		foreach (var window in new Window[] { this.GetWindow(), AddCreatureWindow }) {
			window.FilesDropped += (string[] files) => {
				if (!AddCreatureWindow.Visible) {
					AddCreatureWindow.Visible = true;
				}

				foreach (var filePath in files) {
					if (System.IO.File.Exists(filePath)) {
						AddOrEditCreatureImageCallback(true, new string[] { filePath }, 0);
					} else {
						GD.PrintErr("Detected drag-drop with invalid file '" + filePath + "'");
					}
				}
			};
		}

		AddCreatureAvatarTextureRect = AddCreatureWindow.GetNode<TextureRect>("MarginContainer/VBoxContainer/MainHBoxContainer/AvatarImageButton/AvatarImage");

		SaveToGalleryButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/TeamColorHBoxContainer/SaveToGalleryButton");

		AddCreatureButton = AddCreatureWindow.GetNode<Button>("MarginContainer/VBoxContainer/AddButton");
		AddCreatureButton.Pressed += () => {
			var creatureControl = CreateCreatureControlFromAddCreatureWindow();
			this.CreatureVBoxContainer.AddChild(creatureControl);

			UpdateUI();
		};

		GalleryTree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					int galleryTreeChildCount = GalleryTree.GetRoot().GetChildCount();
					for (int i = 0; i < galleryTreeChildCount; i++) {
						var selectedItem = GalleryTree.GetNextSelected(null);

						if (selectedItem != null) {
							var filePath = ProjectSettings.GlobalizePath(GetCreatureControlFilePath((CreatureControl) selectedItem.GetMetadata(0)));
							if (System.IO.File.Exists(filePath)) {
								GD.Print("Deleted '" + filePath + "'");
								System.IO.File.Move(filePath, filePath + ".dis", true);
							} else {
								GD.PrintErr("Could not delete '" + filePath + "'");
							}

							selectedItem.GetParent().RemoveChild(selectedItem);
						} else {
							break;
						}
					}

					UpdateUI();
				}
			}

			if (inputEvent is InputEventMouseButton inputEventMouseButton) {
				if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick) {
					var treeItem = GalleryTree.GetItemAtPosition(inputEventMouseButton.Position);

					if (treeItem != null) {
						var treeItemCreatureControl = (CreatureControl) treeItem.GetMetadata(0);
						var newCreatureControl = (CreatureControl) treeItemCreatureControl.Duplicate();

						CreateAndAddCreatureControl(newCreatureControl);
					}
				}
			}
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
		TeamColorPickerButton.Color = new Color(0, 0.5f, 0, 0.5f);
		TeamColorPickerButton.GetPicker().AddPreset(new Color(0, 0.5f, 0, 0.5f));
		TeamColorPickerButton.GetPicker().AddPreset(new Color(0.5f, 0, 0, 0.5f));
		TeamColorPickerButton.GetPicker().AddPreset(new Color(0, 0f, 0.5f, 0.5f));
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
					foreach (var child in this.CreatureVBoxContainer.GetChildren()) {
						if (child is CreatureControl creatureControl) {
							creatureControl.QueueFree();
						}
					}

					ActiveCreatureControl = null;
				};
			}

			ClearAllCreaturesConfirmationDialog.Position = (Vector2I) (this.GetWindow().GetPositionWithDecorations() + (this.GetWindow().GetSizeWithDecorations() * new Vector2(0.5f, 0.5f) - (ClearAllCreaturesConfirmationDialog.GetSizeWithDecorations() * new Vector2(0.5f, 0.5f))));
			ClearAllCreaturesConfirmationDialog.Visible = true;

			int creatureControlAmount = 0;
			foreach (var child in this.CreatureVBoxContainer.GetChildren()) {
				if (child is CreatureControl creatureControl) {
					creatureControlAmount += 1;
				}
			}

			ClearAllCreaturesConfirmationDialog.DialogText = "Do you want to clear all " + creatureControlAmount + " creatures?";
		};

		SaveToGalleryButton.Pressed += () => {
			var creatureControl = CreateCreatureControlFromAddCreatureWindow();
			SaveCreatureControlToGallery(creatureControl);
		};

		// TODO: Implement auto-saving of previous session (maybe not full save and load functionality, although with the OS-native file picker, it would be a lot easier).

		// TODO: Implement icon overlays for creature status (incapacitated, dead, etc).

		// FillWithTestData();
	}

	public override void _Process(double delta) {
		// If minimized, set the visibility of the window to false.
		if (AddCreatureWindow.Mode == Window.ModeEnum.Minimized) {
			AddCreatureWindow.Visible = false;
		}

		// Need to check if it isn't in the right position due to this temporarily changing the focus back to Godot on Kubuntu 22.04.
		var correctAddCreatureWindowPosition = (Vector2I) (this.GetWindow().Position + AddCreatureToggleWindowButton.GlobalPosition + new Vector2(AddCreatureToggleWindowButton.GetRect().Size.X, 0) + new Vector2(3, 0));
		if (AddCreatureWindow.Position != correctAddCreatureWindowPosition) {
			AddCreatureWindow.Position = correctAddCreatureWindowPosition;
		}

		if (EditableCooldown <= 0) {
		}

		EditableCooldown -= delta;
		EditableDebounce -= delta;

		// Check that each creature control is not greater than the width of the creature scroll container. If it is, reset it to it's minimum size.
		foreach (var child in this.CreatureVBoxContainer.GetChildren()) {
			if (child is CreatureControl creatureControl) {
				if (creatureControl.Size.X > this.CreatureVBoxContainer.GetParent<ScrollContainer>().Size.X) {
					foreach (var container in creatureControl.GetChildContainers()) {
						container.CustomMinimumSize = container.GetMinimumSize();
						container.Size = new Vector2(0, 0);
					}
				}
			}
		}

		// TODO: Make the column size all the same.
	}

	public static void SaveCreatureControlToGallery(CreatureControl creatureControl) {
		var creatureControlFilePath = ProjectSettings.GlobalizePath(GetCreatureControlFilePath(creatureControl));

		GD.Print("Saved '" + creatureControl.CreatureName + "' to '" + creatureControlFilePath + "'");
		System.IO.File.WriteAllText(creatureControlFilePath, creatureControl.ToJSON());

		GRollInitiative.Instance.RefreshGallery();
	}

	public void RefreshGallery() {
		foreach (var child in new List<TreeItem>(GalleryTree.GetRoot().GetChildren())) {
			child.GetParent().RemoveChild(child);
		}

		foreach (var file in System.IO.Directory.EnumerateFiles(DefaultGalleryFolderPath)) {
			// TODO: Log error.
			try {
				if (file.EndsWith(GALLERY_RESOURCE_FILE_EXTENSION)) {
					var creatureControl = CreateCreatureControl(CreatureControl.FromFile(file));
					var treeItem = GalleryTree.GetRoot().CreateChild();

					if(creatureControl.ImagePath != null && System.IO.File.Exists(creatureControl.ImagePath)) {
						treeItem.SetIcon(0, ImageTexture.CreateFromImage(Image.LoadFromFile(creatureControl.ImagePath)));
					} else {
						GD.PrintErr("Path '" + creatureControl.ImagePath + "' does not exists or is invalid.");
					}
	
					treeItem.SetIconMaxWidth(0, TreeIconWidthSize);
					treeItem.SetCustomBgColor(0, creatureControl.TeamColor);
					treeItem.SetMetadata(0, creatureControl);

					treeItem.SetText(1, creatureControl.CreatureName);
					treeItem.SetAutowrapMode(1, TextServer.AutowrapMode.WordSmart);
					treeItem.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);
					treeItem.SetExpandRight(1, true);
				}
			} catch (Exception) {
			}
		}
	}

	public CreatureControl CreateCreatureControlFromAddCreatureWindow() {
		var creatureControl = CreateCreatureControl();
		
		if(AddCreatureAvatarTextureRect.HasMeta(AVATAR_PATH_METADATA_KEY)) {
			creatureControl.ImagePath = AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY).AsString();
		} else {
			creatureControl.ImagePath = null;
		}

		creatureControl.CreatureName = AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text;
		creatureControl.Initiative = AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value;
		creatureControl.TeamColor = AddCreatureWindow.GetNode<ColorPickerButton>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/TeamColorHBoxContainer/ColorPickerButton").Color;

		return creatureControl;
	}

	public CreatureControl CreateAndAddCreatureControl(CreatureControl creatureControlOverride = null) {
		var creatureControl = CreateCreatureControl(creatureControlOverride: creatureControlOverride);
		this.CreatureVBoxContainer.AddChild(creatureControl);

		UpdateUI();

		return creatureControl;
	}

	public CreatureControl CreateCreatureControl(CreatureControl creatureControlOverride = null) {
		CreatureControl creatureControl = creatureControlOverride != null ? creatureControlOverride : new CreatureControl();
		if (creatureControl.CreatureName == null) {
			creatureControl.CreatureName = "Creature";
		}

		creatureControl.CustomMinimumSize = new Vector2(0, 100);

		creatureControl.InitiativeChanged += (_, newValue) => {
			this.CallDeferred(GRollInitiative.MethodName.UpdateUI);
		};

		creatureControl.Deleted += () => {
			// If the deleted creature control was the active creature control, attempt to move to the previous index control, moving to the next if we cannot go backwards any more.
			if (creatureControl == ActiveCreatureControl) {
				if (creatureControl.GetIndex() == 0) {
					NextCreatureButton.EmitSignal(Button.SignalName.Pressed);
				} else {
					PreviousCreatureButton.EmitSignal(Button.SignalName.Pressed);
				}
			}
		};

		return creatureControl;
	}

	public static string GetCreatureControlFilePath(CreatureControl creatureControl) {
		var rawFileName = creatureControl.CreatureName + "-";
		rawFileName += Math.Round(creatureControl.TeamColor.R, 2) + " ";
		rawFileName += Math.Round(creatureControl.TeamColor.G, 2) + " ";
		rawFileName += Math.Round(creatureControl.TeamColor.B, 2) + " ";
		rawFileName += Math.Round(creatureControl.TeamColor.A, 2);

		return System.IO.Path.Join(DefaultGalleryFolderPath, SlugHelper.GenerateSlug(rawFileName) + GALLERY_RESOURCE_FILE_EXTENSION);
	}

	// public CreatureResource GetCreatureResourceFromAddCreatureWindow() {
	// 	var name = AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text;

	// 	if (name != null && name.Trim().Length > 0) {
	// 		return new CreatureResource((string) AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY, DefaultAvatarPath), name, TeamColorPickerButton.Color);
	// 	} else {
	// 		if (NameInvalidConfirmationDialog == null) {
	// 			NameInvalidConfirmationDialog = new AcceptDialog();

	// 			AddCreatureWindow.AddChild(NameInvalidConfirmationDialog);
	// 		}

	// 		NameInvalidConfirmationDialog.Position = (Vector2I) (this.GetWindow().GetPositionWithDecorations() + (this.GetWindow().GetSizeWithDecorations() * new Vector2(0.5f, 0.5f) - (NameInvalidConfirmationDialog.GetSizeWithDecorations() * new Vector2(0.5f, 0.5f))));
	// 		NameInvalidConfirmationDialog.Visible = true;

	// 		NameInvalidConfirmationDialog.DialogText = "Invalid name for creature '" + name + "'.";
	// 	}

	// 	return null;
	// }

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
					AddCreatureAvatarTextureRect.SetMeta(AVATAR_PATH_METADATA_KEY, path);

					CurrentDirectory = path;
				}
			} catch (Exception) {
			}
		}
	}

	public void RemoveHighlightTreeItem(CreatureControl creatureControl) {
		for (int i = 1; i < creatureControl.GetChildContainers().Count; i++) {
			creatureControl.ColorChildContainerIndex(i, null);
		}
	}

	public void HighlightTreeItem(CreatureControl creatureControl) {
		for (int i = 0; i < creatureControl.GetChildContainers().Count; i++) {
			creatureControl.ColorChildContainerIndex(i, creatureControl.TeamColor);
		}
	}

	public void OffsetActiveTreeItem(int offset) {
		if (offset != 0) {
			CreatureControl nextCreatureControl = null;

			if (ActiveCreatureControl != null) {
				// Check if the active CreatureControl is in the scene. If it isn't, then it must have been removed without being processed (same frame), so select either the beginning or end of the CreatureControl list.
				if (ActiveCreatureControl.IsInsideTree()) {
					RemoveHighlightTreeItem(ActiveCreatureControl);

					nextCreatureControl = ActiveCreatureControl;
					for (int i = 0; i < Math.Abs(offset); i++) {
						if (nextCreatureControl == null) {
							break;
						}

						var nextIndex = nextCreatureControl.GetIndex() + (1 * Math.Sign(offset));
						if (nextIndex >= 0 && nextIndex < nextCreatureControl.GetParent().GetChildCount()) {
							var potentialNextCreatureControl = nextCreatureControl.GetParent().GetChild(nextIndex);
							if (potentialNextCreatureControl is CreatureControl creatureControl) {
								nextCreatureControl = creatureControl;
							} else {
								nextCreatureControl = null;
							}
						} else {
							nextCreatureControl = null;
						}
					}
				}
			}

			if (nextCreatureControl == null && this.CreatureVBoxContainer.GetChildCount() > 0) {
				var childrenList = this.CreatureVBoxContainer.GetChildren();
				if (offset < 0) {
					childrenList.Reverse();
				}

				foreach (var child in childrenList) {
					if (child is CreatureControl creatureControl) {
						nextCreatureControl = creatureControl;
						break;
					}
				}

				TurnCountTextLabel.SetMeta(TURN_NUMBER_METADATA_KEY, TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY).AsInt32() + Math.Sign(offset));
			}

			ActiveCreatureControl = nextCreatureControl;
		}

		UpdateUI();
	}

	public void UpdateUI() {
		var creatureControlList = new List<CreatureControl>();
		foreach (var child in this.CreatureVBoxContainer.GetChildren()) {
			if (child is CreatureControl creatureControl) {
				creatureControlList.Add(creatureControl);
			}
		}

		// Sort from greatest initiative to least, creature name, then finally hashcode. Checking if the values are equal, then finally comparing hashcodes is to ensure that the sort is stable. If this isn't here, the next and previous button will have strange behaviors as the CreatureControls with equal initiatives shift pseudo-randomly.
		creatureControlList.Sort((a, b) => {
			if (a.Initiative != b.Initiative) {
				return a.Initiative.CompareTo(b.Initiative);
			} else if (a.CreatureName != b.CreatureName) {
				return a.CreatureName.CompareTo(b.CreatureName);
			} else {
				return a.GetHashCode().CompareTo(b.GetHashCode());
			}
		});
		// Reverse the order of the initiative list.
		creatureControlList.Reverse();
		// For each control in the sorted list, move that control to the front.
		foreach (var creatureControl in creatureControlList) {
			creatureControl.MoveToFront();
		}

		if (ActiveCreatureControl != null) {
			HighlightTreeItem(ActiveCreatureControl);
		}

		TurnCountTextLabel.Text = "Turn " + TurnCountTextLabel.GetMeta(TURN_NUMBER_METADATA_KEY, 0);
	}

	public void FillWithTestData() {
		GD.PrintErr("!!! TEST DATA FILLED, APPLICATION WILL NOT WORK AS EXPECTED WITH MANUAL INPUT !!!");

		var testCreatureNameToImageToTeamColorToInitiativeTupleList = new List<(string Name, string ImagePath, Color TeamColor, int Initiative, Godot.Collections.Array<CreatureControl.SpellSlotData> SpellSlots)>() {
			("Ibris", "screenshots/avatars/rpg_characters_avatar_1.png", new Color(0, 0.5f, 0, 0.5f), 12, null),
			("Ejun", "screenshots/avatars/rpg_characters_avatar_2.png", new Color(0, 0.5f, 0, 0.5f), 6, new Godot.Collections.Array<CreatureControl.SpellSlotData>(){
				new CreatureControl.SpellSlotData(0, 4),
				new CreatureControl.SpellSlotData(1, 2),
				new CreatureControl.SpellSlotData(2, 1),
				new CreatureControl.SpellSlotData(3, 1),
			}),
			("Anir", "screenshots/avatars/rpg_characters_avatar_3.png", new Color(0, 0.5f, 0, 0.5f), 16, null),
			("Vampire 1", "screenshots/avatars/rpg_characters_avatar_4.png", new Color(0.5f, 0, 0, 0.5f), 14, null),
		};

		foreach (var creatureTuple in testCreatureNameToImageToTeamColorToInitiativeTupleList) {
			var creatureControl = CreateAndAddCreatureControl();
			creatureControl.CreatureName = creatureTuple.Name;
			creatureControl.ImagePath = creatureTuple.ImagePath;
			creatureControl.TeamColor = creatureTuple.TeamColor;
			creatureControl.Initiative = creatureTuple.Initiative;

			if (creatureTuple.SpellSlots != null) {
				creatureControl.SpellSlots = creatureTuple.SpellSlots;
				creatureControl.Render();
			}
		}

		AddCreatureWindow.Visible = true;

		AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text = "Vampire 2";
		AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value = 19;
		AddCreatureAvatarTextureRect.SetMeta(AVATAR_PATH_METADATA_KEY, "screenshots/avatars/rpg_characters_avatar_4.png");
		AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile((string) AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY)));
		TeamColorPickerButton.Color = new Color(0.5f, 0, 0, 0.5f);

		SaveToGalleryButton.EmitSignal(Button.SignalName.Pressed);

		// Clear gallery tree items which don't match the test data.
		foreach (var child in new List<TreeItem>(GalleryTree.GetRoot().GetChildren())) {
			if (child.GetText(1) != "Vampire 2") {
				child.GetParent().RemoveChild(child);
			}
		}

		for (int i = 0; i < 15; i++) {
			NextCreatureButton.EmitSignal(Button.SignalName.Pressed);
		}
	}
}
