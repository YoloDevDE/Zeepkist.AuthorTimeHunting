namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One line of the setup checklist: something the mod is doing, and whether it is done.
///     Steps are added as they begin, so the list grows at the pace the work happens.
/// </summary>
public class SetupStep
{
	public SetupStep(string label)
	{
		Label = label;
	}

	public string Label { get; }

	public bool Done { get; set; }
}
