namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>Small whole numbers as text, out of a table.</summary>
public static class UiNumbers
{
	private const int _max = 256;

	private static readonly string[] _labels = Build();

	public static string Text(int value)
	{
		return value >= 0 && value < _labels.Length ? _labels[value] : value.ToString();
	}

	private static string[] Build()
	{
		string[] labels = new string[_max];

		for (int i = 0; i < _max; i++)
		{
			labels[i] = i.ToString();
		}

		return labels;
	}
}
