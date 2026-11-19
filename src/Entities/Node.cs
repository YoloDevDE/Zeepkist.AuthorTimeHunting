namespace AuthorTimeHunting.Entities;

/// <summary>
///     One level as the GraphQL backend returns it. Concrete on purpose: Newtonsoft has to construct it.
/// </summary>
public class Node
{
	public string Name { get; set; }
	public float ValidationTimeAuthor { get; set; }
	public string FileAuthor { get; set; }
	public string FileUid { get; set; }
	public string WorkshopId { get; set; }

	public string AuthorId { get; set; }
	public float ValidationTimeGold { get; set; }
}
