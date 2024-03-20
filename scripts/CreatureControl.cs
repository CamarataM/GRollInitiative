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

	// Need a default constructor for Godot to be able to make the Node.
	public CreatureControl() {
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
							case CreatureProperty.NAME:
								var lineEdit = new LineEdit();
								lineEdit.Text = GetVariantFromPanelContainer(panelContainer).Value.AsString();
								lineEdit.TextSubmitted += (string newText) => {
									GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
								};

								// Grab the focus in the next frame (cannot this frame, as the lineEdit doesn't exist).
								lineEdit.CallDeferred(LineEdit.MethodName.GrabFocus);
								// Set the caret column to the final character (expected location for a text edit box).
								lineEdit.CaretColumn = lineEdit.Text.Length;

								GRollInitiative.StaticEditPropertyCellConfirmationDialog.AddChild(lineEdit);
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
									this.ImagePath = GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetChild<LineEdit>(0).Text;
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
								default:
									GD.PrintErr("Unhandled CreatureProperty '" + creatureProperty + "'");
									break;
							}
						};

						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Position = (Vector2I) (GetWindow().Position + inputEventMouseButton.GlobalPosition - (GRollInitiative.StaticEditPropertyCellConfirmationDialog.Size / 2) + new Vector2(0, 30));
						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Show();
					}
				}
			};

			if (!creaturePropertyOverride.HasValue || creaturePropertyOverride.Value == creatureProperty) {
				RemoveAllChildren(panelContainer);

				switch (creatureProperty) {
					case CreatureProperty.IMAGE:
						if (ImagePath != null) {
							var imageTextureRect = new TextureRect();
							imageTextureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
							imageTextureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

							if (System.IO.File.Exists(ProjectSettings.GlobalizePath(ImagePath))) {
								imageTextureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(ImagePath));
							} else {
								// TODO: Implement better invalid image texture.
								imageTextureRect.Texture = new PlaceholderTexture2D();
								GD.PrintErr("Path '" + ImagePath + "' does not exist.");
							}

							imageTextureRect.SetMeta(IMAGE_PATH_METADATA_KEY, ImagePath);

							panelContainer.AddChild(imageTextureRect);
						}

						break;
					case CreatureProperty.NAME:
						if (CreatureName != null) {
							var nameLabel = new Label();
							nameLabel.Text = CreatureName;

							panelContainer.AddChild(nameLabel);
						}

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

						for (int i = 0; i < 9; i++) {
							var spellSlotHBoxContainer = new HBoxContainer();

							// spellSlotHBoxContainer.AddChild(new Label() {
							// 	Text = (i + 1).ToString(),
							// });

							spellSlotHBoxContainer.AddChild(new SpinBox());

							spellSlotsHFlowContainer.AddChild(spellSlotHBoxContainer);
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