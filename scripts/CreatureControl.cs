using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class CreatureControl : ResizableHContainer {
	public const string IMAGE_PATH_METADATA_KEY = "image_path";

	public enum CreatureProperty {
		IMAGE,
		NAME,
		HEALTH,
		INITIATIVE,
		SPELL_SLOTS,
	}

	public List<CreatureProperty> CreaturePropertyColumnList = new List<CreatureProperty>() {
		CreatureProperty.IMAGE,
		CreatureProperty.NAME,
		CreatureProperty.INITIATIVE,
		CreatureProperty.SPELL_SLOTS,
	};

	private string _ImagePath = null;
	[Export]
	public string ImagePath {
		get => _ImagePath;
		set {
			_ImagePath = value;

			Render(CreatureProperty.IMAGE);
		}
	}

	private Color _TeamColor = new Color(0, 0.5f, 0, 0.5f);
	[Export]
	public Color TeamColor {
		get => _TeamColor;
		set {
			_TeamColor = value;

			Render(CreatureProperty.IMAGE);
		}
	}

	private string _CreatureName = null;
	[Export]
	public string CreatureName {
		get => _CreatureName;
		set {
			_CreatureName = value;

			Render(CreatureProperty.NAME);
		}
	}

	private int _Health = 0;
	[Export]
	public int Health {
		get => _Health;
		set {
			_Health = value;

			Render(CreatureProperty.HEALTH);
		}
	}

	private int _Initiative = 0;
	[Export]
	public int Initiative {
		get => _Initiative;
		set {
			_Initiative = value;

			Render(CreatureProperty.INITIATIVE);
		}
	}

	private Godot.Collections.Array<bool> _EnabledSpellSlots = new Godot.Collections.Array<bool>();
	[Export]
	public Godot.Collections.Array<bool> EnabledSpellSlots {
		get => _EnabledSpellSlots;
		set {
			_EnabledSpellSlots = value;

			Render(CreatureProperty.SPELL_SLOTS);
		}
	}

	// Need a default constructor for Godot to be able to make the Node.
	public CreatureControl() {
		this.ImagePath = "";
		this.ColumnCount = 5;

		this.CallDeferred(CreatureControl.MethodName.Render);
	}

	public static void RemoveAllChildren(Node node) {
		foreach (var child in new List<Node>(node.GetChildren())) {
			child.QueueFree();
			node.RemoveChild(child);
		}
	}

	public void Render() {
		this.Render(null);
	}

	public void Render(CreatureProperty? creaturePropertyOverride = null) {
		this.ColumnCount = CreaturePropertyColumnList.Count;

		for (int columnIndex = 0; columnIndex < this.ColumnCount; columnIndex++) {
			var creatureProperty = CreaturePropertyColumnList[columnIndex];

			// Clear the previous signals for callable.
			var panelContainer = this.GetChildContainers()[columnIndex];
			foreach (var signalConnectionDictionary in panelContainer.GetSignalConnectionList(PanelContainer.SignalName.GuiInput)) {
				var callable = signalConnectionDictionary["callable"];
				panelContainer.Disconnect(PanelContainer.SignalName.GuiInput, (Callable) callable);
			}

			panelContainer.GuiInput += (InputEvent @event) => {
				if (@event is InputEventMouseButton inputEventMouseButton) {
					if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick) {
						// Remove all previous input children on the confirmation dialog.
						RemoveAllChildren(GRollInitiative.StaticEditPropertyCellConfirmationDialog);

						// Based on the current creature property for the panel container, add a new input control.
						// TODO: Reuse previous elements, remaking them if applicable.
						switch (creatureProperty) {
							case CreatureProperty.IMAGE:
								var imagePropertyParentContainer = new VBoxContainer();

								var imageLineEdit = new LineEdit();
								imageLineEdit.Text = GetVariantFromPanelContainer(panelContainer).Value.AsString();
								imageLineEdit.TextSubmitted += (string newText) => {
									GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
								};

								// Grab the focus in the next frame (cannot this frame, as the lineEdit doesn't exist).
								imageLineEdit.CallDeferred(LineEdit.MethodName.GrabFocus);
								// Set the caret column to the final character (expected location for a text edit box).
								imageLineEdit.CaretColumn = imageLineEdit.Text.Length;

								imagePropertyParentContainer.AddChild(imageLineEdit);

								var imageColorPickerButton = new ColorPickerButton();
								imageColorPickerButton.Color = TeamColor;
								imageColorPickerButton.CustomMinimumSize = new Vector2(0, 35);
								imageColorPickerButton.GetPicker().AddPreset(new Color(0, 0.5f, 0, 0.5f));
								imageColorPickerButton.GetPicker().AddPreset(new Color(0.5f, 0, 0, 0.5f));
								imageColorPickerButton.GetPicker().AddPreset(new Color(0, 0f, 0.5f, 0.5f));
								imagePropertyParentContainer.AddChild(imageColorPickerButton);

								GRollInitiative.StaticEditPropertyCellConfirmationDialog.AddChild(imagePropertyParentContainer);

								break;
							case CreatureProperty.NAME:
								var nameLineEdit = new LineEdit();
								nameLineEdit.Text = GetVariantFromPanelContainer(panelContainer).Value.AsString();
								nameLineEdit.TextSubmitted += (string newText) => {
									GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
								};

								// Grab the focus in the next frame (cannot this frame, as the lineEdit doesn't exist).
								nameLineEdit.CallDeferred(LineEdit.MethodName.GrabFocus);
								// Set the caret column to the final character (expected location for a text edit box).
								nameLineEdit.CaretColumn = nameLineEdit.Text.Length;

								GRollInitiative.StaticEditPropertyCellConfirmationDialog.AddChild(nameLineEdit);
								break;
							case CreatureProperty.HEALTH:
							case CreatureProperty.INITIATIVE:
								var spinBox = new SpinBox();
								spinBox.Value = GetVariantFromPanelContainer(panelContainer).Value.AsInt32();
								spinBox.GetLineEdit().TextSubmitted += (string newText) => {
									spinBox.Apply();
									GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
								};

								// Grab the focus in the next frame (cannot this frame, as the lineEdit doesn't exist).
								spinBox.GetLineEdit().CallDeferred(LineEdit.MethodName.GrabFocus);
								// Set the caret column to the final character (expected location for a text edit box).
								spinBox.GetLineEdit().CaretColumn = spinBox.GetLineEdit().Text.Length;

								GRollInitiative.StaticEditPropertyCellConfirmationDialog.AddChild(spinBox);
								break;
							case CreatureProperty.SPELL_SLOTS:
								// Create a parent container to contain the sub-container which will hold the spell slot controls, the add button, and the spinbox.
								var spellSlotsControlVBoxContainer = new VBoxContainer();
								// Create a container which will contain the spell slot controls.
								var spellSlotsCheckButtonVBoxContainer = new VBoxContainer();

								var addButtonAmountSpinbox = new SpinBox();
								addButtonAmountSpinbox.Value = 9;

								HBoxContainer CreateSpellSlotContainer() {
									// Create a HBoxContainer which will contain the SwitchButton and the delete button for the spell slot.
									var spellSlotsEnabledContainer = new HBoxContainer();
									spellSlotsEnabledContainer.AddChild(new CheckButton());

									var deleteButton = new Button();
									deleteButton.Text = "X";
									deleteButton.Pressed += () => {
										deleteButton.GetParent().QueueFree();
										deleteButton.GetParent().GetParent().RemoveChild(deleteButton.GetParent());

										RefreshIndexes();
									};

									spellSlotsEnabledContainer.AddChild(deleteButton);

									spellSlotsCheckButtonVBoxContainer.AddChild(spellSlotsEnabledContainer);

									return spellSlotsEnabledContainer;
								}

								// Set the indexes of the spell slot CheckButtons based on their index in the children list of their parent VBoxContainer.
								void RefreshIndexes() {
									int spellSlotIndex = 0;
									foreach (var child in spellSlotsCheckButtonVBoxContainer.GetChildren()) {
										if (child is HBoxContainer hBoxContainer) {
											hBoxContainer.GetChild<CheckButton>(0).Text = "Slot " + (spellSlotIndex + 1);

											spellSlotIndex += 1;
										}
									}
								}

								// Add any existing spell slots for the current creature.
								foreach (var enabledSlot in this.EnabledSpellSlots) {
									var spellSlotContainer = CreateSpellSlotContainer();
									spellSlotContainer.GetChild<CheckButton>(0).ButtonPressed = enabledSlot;
								}
								RefreshIndexes();

								var addButton = new Button();
								addButton.Text = "Add";
								addButton.Pressed += () => {
									for (int amountToAdd = 0; amountToAdd < addButtonAmountSpinbox.Value; amountToAdd++) {
										CreateSpellSlotContainer();
									}

									RefreshIndexes();
								};

								var deleteAllButton = new Button();
								deleteAllButton.Text = "Delete All";
								deleteAllButton.Pressed += () => {
									foreach (var child in spellSlotsCheckButtonVBoxContainer.GetChildren()) {
										if (child is HBoxContainer hBoxContainer) {
											hBoxContainer.QueueFree();
										}
									}
								};

								spellSlotsControlVBoxContainer.AddChild(addButton);
								spellSlotsControlVBoxContainer.AddChild(addButtonAmountSpinbox);
								spellSlotsControlVBoxContainer.AddChild(deleteAllButton);
								spellSlotsControlVBoxContainer.AddChild(spellSlotsCheckButtonVBoxContainer);

								GRollInitiative.StaticEditPropertyCellConfirmationDialog.AddChild(spellSlotsControlVBoxContainer);
								break;
							default:
								GD.PrintErr("Unhandled CreatureProperty '" + creatureProperty + "'");
								break;
						}

						// Clear the previous signals for callable.
						foreach (var signalConnectionDictionary in GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetSignalConnectionList(ConfirmationDialog.SignalName.Confirmed)) {
							var callable = signalConnectionDictionary["callable"];
							GRollInitiative.StaticEditPropertyCellConfirmationDialog.Disconnect(ConfirmationDialog.SignalName.Confirmed, (Callable) callable);
						}

						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Confirmed += () => {
							switch (creatureProperty) {
								case CreatureProperty.IMAGE:
									this.ImagePath = GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<VBoxContainer>(0).GetChild<LineEdit>(0).Text;
									this.TeamColor = GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<VBoxContainer>(0).GetChild<ColorPickerButton>(1).Color;
									break;
								case CreatureProperty.NAME:
									this.CreatureName = GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<LineEdit>(0).Text;
									break;
								case CreatureProperty.HEALTH:
									this.Health = (int) GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<SpinBox>(0).Value;
									break;
								case CreatureProperty.INITIATIVE:
									this.Initiative = (int) GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<SpinBox>(0).Value;
									break;
								case CreatureProperty.SPELL_SLOTS:
									this.EnabledSpellSlots.Clear();

									// Iterate each spell slot container, checking if it is checked or not, setting that index value into the EnabledSpellSlots Array.
									foreach (var child in GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<VBoxContainer>(0).GetChildren()) {
										if (child is VBoxContainer) {
											int spellSlotIndex = 0;
											foreach (var spellSlotParentContainerChild in child.GetChildren()) {
												if (spellSlotParentContainerChild is HBoxContainer hBoxContainer) {
													var checkButton = hBoxContainer.GetChild<CheckButton>(0);

													if (spellSlotIndex >= this.EnabledSpellSlots.Count) {
														this.EnabledSpellSlots.Add(checkButton.ButtonPressed);
													} else {
														this.EnabledSpellSlots[spellSlotIndex] = checkButton.ButtonPressed;
													}

													spellSlotIndex += 1;
												}
											}
										}
									}

									Render();

									break;
								default:
									GD.PrintErr("Unhandled CreatureProperty '" + creatureProperty + "'");
									break;
							}
						};

						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Position = (Vector2I) (GetWindow().Position + inputEventMouseButton.GlobalPosition - (GRollInitiative.StaticEditPropertyCellConfirmationDialog.Size / 2) + new Vector2(0, 30));

						// Set the size of the confirmation dialog equal to the minimum size required plus an offset.
						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Size = (Vector2I) GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetContentsMinimumSize() + new Vector2I(100, 10);

						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Show();
					}
				}
			};

			if (!creaturePropertyOverride.HasValue || creaturePropertyOverride.Value == creatureProperty) {
				RemoveAllChildren(panelContainer);

				switch (creatureProperty) {
					case CreatureProperty.IMAGE:
						var imageTextureRect = new TextureRect();
						imageTextureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
						imageTextureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

						bool setImagePath = false;
						if (ImagePath != null && ImagePath.Trim().Length > 0) {
							if (System.IO.File.Exists(ProjectSettings.GlobalizePath(ImagePath))) {
								imageTextureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(ImagePath));
								setImagePath = true;
							}
						}

						if (!setImagePath) {
							// TODO: Implement better invalid image texture.
							imageTextureRect.Texture = new PlaceholderTexture2D();
							GD.PrintErr("Path '" + ImagePath + "' does not exist.");
						}

						imageTextureRect.SetMeta(IMAGE_PATH_METADATA_KEY, ImagePath);

						panelContainer.AddChild(imageTextureRect);

						ColorChildContainer(panelContainer, TeamColor);

						break;
					case CreatureProperty.NAME:
						var nameLabel = new Label();
						nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;

						if (CreatureName != null) {
							nameLabel.Text = CreatureName;
						}

						panelContainer.AddChild(nameLabel);

						break;
					case CreatureProperty.HEALTH:
						var healthLabel = new Label();
						healthLabel.Text = "HP: " + Health.ToString();

						panelContainer.AddChild(healthLabel);

						break;
					case CreatureProperty.INITIATIVE:
						var initiativeLabel = new Label();
						initiativeLabel.Text = "Initiative: " + Initiative.ToString();

						panelContainer.AddChild(initiativeLabel);

						break;
					case CreatureProperty.SPELL_SLOTS:
						var spellSlotsHFlowContainer = new HFlowContainer();
						spellSlotsHFlowContainer.Alignment = FlowContainer.AlignmentMode.Center;
						spellSlotsHFlowContainer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
						spellSlotsHFlowContainer.GrowVertical = GrowDirection.Both;

						bool haveEnabledSpellSlots = false;

						// Set the spell slot text and update the visibility based on whether the spell slot is enabled or not.
						for (int i = 0; i < EnabledSpellSlots.Count; i++) {
							var spellSlotHBoxContainer = new HBoxContainer();

							spellSlotHBoxContainer.AddChild(new Label() {
								Text = (i + 1).ToString(),
							});

							spellSlotHBoxContainer.AddChild(new SpinBox());

							// Set the visibility of the spell slot container to be equal to whether the spell slot is enabled or not.
							spellSlotHBoxContainer.Visible = EnabledSpellSlots[i];
							if (spellSlotHBoxContainer.Visible) {
								haveEnabledSpellSlots = true;
							}

							spellSlotsHFlowContainer.AddChild(spellSlotHBoxContainer);
						}

						// Check if we have any visible children. If not, add a label which states that we have no spell slots in the container.
						if (!haveEnabledSpellSlots) {
							spellSlotsHFlowContainer.AddChild(new Label() {
								Text = "No Spell Slots",
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
							});
						}

						panelContainer.AddChild(spellSlotsHFlowContainer);

						break;
					default:
						GD.PrintErr("Unhandled CreatureProperty '" + creatureProperty + "'");
						break;
				}
			}
		}
	}

	public Variant? GetVariantFromPanelContainer(PanelContainer panelContainer) {
		Variant? returnValue = null;

		if (panelContainer.GetChild(0) is Label label) {
			return returnValue = label.Text;
		} else if (panelContainer.GetChild(0) is SpinBox spinBox) {
			return returnValue = spinBox.Value;
		} else if (panelContainer.GetChild(0) is TextureRect textureRect) {
			return returnValue = textureRect.GetMeta(IMAGE_PATH_METADATA_KEY, "");
		}

		return returnValue;
	}
}