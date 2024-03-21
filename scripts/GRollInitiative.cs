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
	public static ConfirmationDialog StaticEditPropertyCellConfirmationDialog;

	public string DefaultAvatarPath = "res://resources/test_avatar.png";
	public string DefaultGalleryFolderPath = ProjectSettings.GlobalizePath("user://gallery");

	public Button AddCreatureButton;
	public Button EditCreatureButton;
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

	public const string GALLERY_RESOURCE_FILE_EXTENSION = ".tres";
	public static readonly SlugHelper SlugHelper = new SlugHelper();

	public override void _Ready() {
		GRollInitiative.StaticEditPropertyCellConfirmationDialog = this.EditPropertyCellConfirmationDialog;

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

		foreach (var file in System.IO.Directory.EnumerateFiles(DefaultGalleryFolderPath)) {
			// TODO: Log error.
			try {
				if (file.EndsWith(GALLERY_RESOURCE_FILE_EXTENSION)) {
					var creatureResource = ResourceLoader.Load<CreatureResource>(file);
					// CreateGalleryTreeItemFromCreatureResource(creatureResource);
				}
			} catch (Exception) {
			}
		}

		// Handle visibility toggle.
		AddCreatureToggleWindowButton.Pressed += () => {
			// AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(GD.Load<Image>(DefaultAvatarPath));

			// AddCreatureButton.Visible = true;
			// EditCreatureButton.Visible = false;

			// AddCreatureWindow.Visible = !AddCreatureWindow.Visible;
			var creatureControl = new CreatureControl();
			creatureControl.CustomMinimumSize = new Vector2(0, 100);
			this.CreatureVBoxContainer.AddChild(creatureControl);

			creatureControl.InitiativeChanged += (_, newValue) => {
				this.CallDeferred(GRollInitiative.MethodName.UpdateUI);
			};

			UpdateUI();
		};

		// Handle close button pressed by hiding visibility of the window.
		AddCreatureWindow.CloseRequested += () => {
			AddCreatureWindow.Visible = false;
			// EditCreatureTreeItem = null;
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
				var creatureResource = GetCreatureResourceFromAddCreatureWindow();

				if (creatureResource != null) {
					// if (EditCreatureTreeItem != null) {
					// 	AddCreatureWindow.Visible = false;
					// 	EditCreatureTreeItem = null;
					// }
				}
			};
		}

		GalleryTree.GuiInput += (InputEvent inputEvent) => {
			if (inputEvent is InputEventKey inputEventKey) {
				if (inputEventKey.Pressed && inputEventKey.Keycode == Key.Delete) {
					int galleryTreeChildCount = GalleryTree.GetRoot().GetChildCount();
					for (int i = 0; i < galleryTreeChildCount; i++) {
						var selectedItem = GalleryTree.GetNextSelected(null);

						if (selectedItem != null) {
							var filePath = GetCreatureResourceFilePath((CreatureResource) selectedItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY));
							if (System.IO.File.Exists(filePath)) {
								System.IO.File.Move(filePath, filePath + ".dis");
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
						// CreateInitiativeTreeItemFromCreatureResource((CreatureResource) treeItem.GetMeta(CREATURE_RESOURCE_METADATA_KEY), AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value);
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

		// TODO: Reimplement
		// SaveToGalleryButton.Pressed += () => {
		// 	var creatureResource = GetCreatureResourceFromAddCreatureWindow();

		// 	if (creatureResource != null) {
		// 		CreateGalleryTreeItemFromCreatureResource(creatureResource);

		// 		foreach (var child in GalleryTree.GetRoot().GetChildren()) {
		// 			var childCreatureResource = (CreatureResource) child.GetMeta(CREATURE_RESOURCE_METADATA_KEY);
		// 			var childCreatureResourceFilePath = GetCreatureResourceFilePath(childCreatureResource);

		// 			var error = ResourceSaver.Save(childCreatureResource, ProjectSettings.GlobalizePath(childCreatureResourceFilePath));
		// 			if (error != Error.Ok) {
		// 				GD.PrintErr("Got error trying to save '" + childCreatureResource + "' to path '" + ProjectSettings.GlobalizePath(childCreatureResourceFilePath) + "' (error '" + error + "').");
		// 			}
		// 		}
		// 	}
		// };

		// TODO: Implement auto-saving of previous session (maybe not full save and load functionality, although with the OS-native file picker, it would be a lot easier).

		// TODO: Implement icon overlays for creature status (incapacitated, dead, etc).

		FillWithTestData();
	}

	public override void _Process(double delta) {
		// If minimized, set the visibility of the window to false.
		if (AddCreatureWindow.Mode == Window.ModeEnum.Minimized) {
			AddCreatureWindow.Visible = false;
		}

		AddCreatureWindow.Position = (Vector2I) (this.GetWindow().Position + AddCreatureToggleWindowButton.GlobalPosition + new Vector2(AddCreatureToggleWindowButton.GetRect().Size.X, 0) + new Vector2(3, 0));

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

	public string GetCreatureResourceFilePath(CreatureResource creatureResource) {
		return System.IO.Path.Join(DefaultGalleryFolderPath, SlugHelper.GenerateSlug(creatureResource.Name + "-" + creatureResource.TeamColor) + GALLERY_RESOURCE_FILE_EXTENSION);
	}

	public CreatureResource GetCreatureResourceFromAddCreatureWindow() {
		var name = AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text;

		if (name != null && name.Trim().Length > 0) {
			return new CreatureResource((string) AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY, DefaultAvatarPath), name, TeamColorPickerButton.Color);
		} else {
			if (NameInvalidConfirmationDialog == null) {
				NameInvalidConfirmationDialog = new AcceptDialog();

				AddCreatureWindow.AddChild(NameInvalidConfirmationDialog);
			}

			NameInvalidConfirmationDialog.Position = (Vector2I) (this.GetWindow().GetPositionWithDecorations() + (this.GetWindow().GetSizeWithDecorations() * new Vector2(0.5f, 0.5f) - (NameInvalidConfirmationDialog.GetSizeWithDecorations() * new Vector2(0.5f, 0.5f))));
			NameInvalidConfirmationDialog.Visible = true;

			NameInvalidConfirmationDialog.DialogText = "Invalid name for creature '" + name + "'.";
		}

		return null;
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

		// Sort from greatest initiative to least.
		creatureControlList.Sort((a, b) => { return a.Initiative.CompareTo(b.Initiative); });
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

		// CreateInitiativeTreeItemFromCreatureResource(new CreatureResource("screenshots/avatars/rpg_characters_avatar_1.png", "Ibris", new Color(0, 0.5f, 0, 0.5f)), 12);
		// CreateInitiativeTreeItemFromCreatureResource(new CreatureResource("screenshots/avatars/rpg_characters_avatar_2.png", "Ejun", new Color(0, 0.5f, 0, 0.5f)), 6);
		// CreateInitiativeTreeItemFromCreatureResource(new CreatureResource("screenshots/avatars/rpg_characters_avatar_3.png", "Anir", new Color(0, 0.5f, 0, 0.5f)), 16);
		// CreateInitiativeTreeItemFromCreatureResource(new CreatureResource("screenshots/avatars/rpg_characters_avatar_4.png", "Vampire 1", new Color(0.5f, 0, 0, 0.5f)), 14);

		// AddCreatureWindow.Visible = true;

		// AddCreatureWindow.GetNode<LineEdit>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/NameLineEdit").Text = "Vampire 2";
		// AddCreatureWindow.GetNode<SpinBox>("MarginContainer/VBoxContainer/MainHBoxContainer/SettingsVBoxContainer/InitiativeSpinBox").Value = 19;
		// AddCreatureAvatarTextureRect.SetMeta(AVATAR_PATH_METADATA_KEY, "screenshots/avatars/rpg_characters_avatar_4.png");
		// AddCreatureAvatarTextureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile((string) AddCreatureAvatarTextureRect.GetMeta(AVATAR_PATH_METADATA_KEY)));
		// TeamColorPickerButton.Color = new Color(0.5f, 0, 0, 0.5f);

		// for (int i = 0; i < 15; i++) {
		// 	NextCreatureButton.EmitSignal(Button.SignalName.Pressed);
		// }

		var creatureControl = new CreatureControl();
		creatureControl.CustomMinimumSize = new Vector2(0, 100);
		creatureControl.ImagePath = "res://screenshots/avatars/rpg_characters_avatar_1.png";
		creatureControl.CreatureName = "Ibris";
		this.CreatureVBoxContainer.AddChild(creatureControl);
	}
}