using Godot;

public partial class CreatureResource : Resource {
	[Export]
	public string AvatarPath { get; set; }

	[Export]
	public string Name { get; set; }

	[Export]
	public Color TeamColor { get; set; }

	public CreatureResource() : this(null, null, new Color()) { }

	public CreatureResource(string avatarPath, string name, Color teamColor) {
		this.AvatarPath = avatarPath;
		this.Name = name;
		this.TeamColor = teamColor;
	}
}