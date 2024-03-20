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
	}

	public List<CreatureProperty> CreaturePropertyColumnList = new List<CreatureProperty>() {
		CreatureProperty.IMAGE,
		CreatureProperty.NAME,
		CreatureProperty.INITIATIVE,
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

		for (int i = 0; i < this.ColumnCount; i++) {
			var creatureProperty = CreaturePropertyColumnList[i];

			// Clear the previous signals for callable.
			var panelContainer = this.GetChildContainers()[i];
			foreach (var signalConnectionDictionary in panelContainer.GetSignalConnectionList(PanelContainer.SignalName.GuiInput)) {
				var callable = signalConnectionDictionary["callable"];
				panelContainer.Disconnect(PanelContainer.SignalName.GuiInput, (Callable) callable);
			}

			panelContainer.GuiInput += (InputEvent @event) => {
				if (@event is InputEventMouseButton inputEventMouseButton) {
					if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick) {
						RemoveAllChildren(GRollInitiative.StaticEditPropertyCellConfirmationDialog);

						switch (creatureProperty) {
							case CreatureProperty.IMAGE:
							case CreatureProperty.NAME:
								var lineEdit = new LineEdit();
								lineEdit.Text = GetVariantFromPanelContainer(panelContainer).Value.AsString();
								lineEdit.TextSubmitted += (string newText) => {
									GRollInitiative.StaticEditPropertyCellConfirmationDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
								};

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

						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Position = (Vector2I) (GetWindow().Position + inputEventMouseButton.GlobalPosition);
						GRollInitiative.StaticEditPropertyCellConfirmationDialog.Show();
					}
				}
			};

			if (!creaturePropertyOverride.HasValue || creaturePropertyOverride.Value == creatureProperty) {
				RemoveAllChildren(panelContainer);

				switch (creatureProperty) {
					case CreatureProperty.IMAGE:
						if (ImagePath != null) {
							TextureRect textureRect = new TextureRect();
							textureRect.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(ImagePath));
							textureRect.SetMeta(IMAGE_PATH_METADATA_KEY, ImagePath);

							panelContainer.AddChild(textureRect);
						}

						break;
					case CreatureProperty.NAME:
						if (CreatureName != null) {
							Label nameLabel = new Label();
							nameLabel.Text = CreatureName;

							panelContainer.AddChild(nameLabel);
						}

						break;
					case CreatureProperty.HEALTH:
						Label healthLabel = new Label();
						healthLabel.Text = "HP: " + Health.ToString();

						panelContainer.AddChild(healthLabel);

						break;
					case CreatureProperty.INITIATIVE:
						Label initiativeLabel = new Label();
						initiativeLabel.Text = "Initiative: " + Initiative.ToString();

						panelContainer.AddChild(initiativeLabel);

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