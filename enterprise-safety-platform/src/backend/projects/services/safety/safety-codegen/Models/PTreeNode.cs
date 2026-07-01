using System.Collections;

namespace Platform.Legacy.CodeGen.Models;

public class PTreeNode : IEnumerable<PTreeNode>
{
	public required string Prefix { get; set; }
	public char Separator { get; set; }
	public required string Value { get; set; }
	public string? Description { get; set; }
	public bool IsIntermediate { get; set; }

	public List<string> MemberOfGroups { get; set; } = [];

	protected readonly Dictionary<string, PTreeNode> children = new();

	public override string ToString()
	{
		return $"{this.ToPath()}: {this.Description}{Environment.NewLine}";
	}

	public string ToPath()
	{
		return $"{this.Prefix}{this.Separator}{this.Value}".Trim([this.Separator,]);
	}

	public IEnumerator<PTreeNode> GetEnumerator()
	{
		return this.children.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.children.Values.GetEnumerator();
	}

	public PTreeNode this[string key]
	{
		get => this.children[key];
		set => this.children[key] = value;
	}

	public void Add(PTreeNode node)
	{
		this.children.Add(key: node.Value, value: node);
	}

	public void Remove(string key)
	{
		this.children.Remove(key);
	}

	public bool TryGetValue(string key, out PTreeNode? value)
	{
		return this.children.TryGetValue(key: key, value: out value);
	}

	public ICollection<PTreeNode> GetValues()
	{
		return this.children.Values;
	}
}
