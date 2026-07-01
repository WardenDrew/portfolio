namespace Platform.Legacy.CodeGen.Models;

public class PTreeNodeBuilder
{
	// root node
	protected readonly PTree _root;

	// history tracking for ascending up layers
	protected readonly Stack<PTreeNode> _history = new();

	// The current node we are adding to, only changes when calling Node()
	protected PTreeNode _currentNode;

	// The last node we added, always updated after each node creation
	protected PTreeNode _lastNode;

	public PTreeNodeBuilder(string value, string prefix, char nodeSeparator, char permissionSeparator)
	{
		this._root = new PTree
		{
			Prefix = prefix,
			Separator = nodeSeparator,
			NodeSeparator = nodeSeparator,
			PermissionSeparator = permissionSeparator,
			Value = value,
			Description = null,
			IsIntermediate = true,
		};

		this._currentNode = this._root;
		this._lastNode = this._currentNode;
	}

	protected PTreeNode NewNode(string value, bool isPermission, bool isIntermediate)
	{
		return new PTreeNode
		{
			Prefix = this._currentNode.ToPath().Trim([this._root.NodeSeparator,]),
			Separator = isPermission ? this._root.PermissionSeparator : this._root.NodeSeparator,
			Value = value,
			Description = null,
			IsIntermediate = isIntermediate,
		};
	}

	public PTreeNodeBuilder AddNode(string value, out string path)
	{
		return AddAndDescend(newNode: NewNode(value: value, isPermission: false, isIntermediate: true), path: out path);
	}

	public PTreeNodeBuilder AddNode(string value)
	{
		return AddNode(value: value, path: out string _);
	}

	public PTreeNodeBuilder AddNode(string value, string? description, out string path)
	{
		return AddNode(value: value, path: out path).Description(description);
	}

	public PTreeNodeBuilder AddNode(string value, string? description)
	{
		return AddNode(value).Description(description);
	}

	public PTreeNodeBuilder AddEdge(string value, out string path)
	{
		return AddAndStay(newNode: NewNode(value: value, isPermission: false, isIntermediate: false), path: out path);
	}

	public PTreeNodeBuilder AddEdge(string value)
	{
		return AddEdge(value: value, path: out string _);
	}

	public PTreeNodeBuilder AddEdge(string value, string? description, out string path)
	{
		return AddEdge(value: value, path: out path).Description(description);
	}

	public PTreeNodeBuilder AddEdge(string value, string? description)
	{
		return AddEdge(value).Description(description);
	}

	public PTreeNodeBuilder AddPermission(string value, out string path)
	{
		return AddAndStay(newNode: NewNode(value: value, isPermission: true, isIntermediate: false), path: out path);
	}

	public PTreeNodeBuilder AddPermission(string value)
	{
		return AddPermission(value: value, path: out string _);
	}

	public PTreeNodeBuilder AddPermission(string value, string? description, out string path)
	{
		return AddPermission(value: value, path: out path).Description(description);
	}

	public PTreeNodeBuilder AddPermission(string value, string? description)
	{
		return AddPermission(value).Description(description);
	}

	public PTreeNodeBuilder MakeGroup(out string path)
	{
		path = this._lastNode.ToPath();
		this._root.KnownGroups.Add(key: path, value: []);
		return this;
	}

	public PTreeNodeBuilder Group(string group)
	{
		if (!this._root.KnownGroups.ContainsKey(group))
		{
			throw new InvalidOperationException("Attempted to add a node to a group that has not been created yet!");
		}

		this._root.KnownGroups[group].Add(this._lastNode.ToPath());
		this._lastNode.MemberOfGroups.Add(group);

		return this;
	}

	public PTreeNodeBuilder Description(string? description)
	{
		this._lastNode.Description = description;

		if (!this._lastNode.IsIntermediate)
		{
			this._root.FlattenedTree.Remove(this._lastNode.ToPath());
			this._root.FlattenedTree.Add(key: this._lastNode.ToPath(), value: this._lastNode.Description);
		}

		return this;
	}

	public PTreeNodeBuilder Separator(char separator)
	{
		this._lastNode.Separator = separator;

		if (!this._lastNode.IsIntermediate)
		{
			this._root.FlattenedTree.Remove(this._lastNode.ToPath());
			this._root.FlattenedTree.Add(key: this._lastNode.ToPath(), value: this._lastNode.Description);
		}

		return this;
	}

	public PTreeNodeBuilder Intermediate(bool isIntermediate = true)
	{
		if (this._lastNode.IsIntermediate && !isIntermediate)
		{
			this._root.FlattenedTree.Add(key: this._lastNode.ToPath(), value: this._lastNode.Description);
		}
		else if (!this._lastNode.IsIntermediate && isIntermediate)
		{
			this._root.FlattenedTree.Remove(this._lastNode.ToPath());
		}

		this._lastNode.IsIntermediate = isIntermediate;

		return this;
	}

	public PTreeNodeBuilder AddAndDescend(PTreeNode newNode, out string path)
	{
		this._currentNode.Add(newNode);

		this._history.Push(this._currentNode);

		this._currentNode = newNode;
		this._lastNode = this._currentNode;

		path = this._lastNode.ToPath();

		return this;
	}

	public PTreeNodeBuilder AddAndDescend(PTreeNode newNode)
	{
		return AddAndDescend(newNode: newNode, path: out string _);
	}

	public PTreeNodeBuilder AddAndStay(PTreeNode newNode, out string path)
	{
		this._currentNode.Add(newNode);
		this._lastNode = newNode;

		path = this._lastNode.ToPath();

		this._root.FlattenedTree.Add(key: path, value: this._lastNode.Description);

		return this;
	}

	public PTreeNodeBuilder AddAndStay(PTreeNode newNode)
	{
		return AddAndStay(newNode: newNode, path: out string _);
	}

	public PTreeNodeBuilder Ascend(out string path)
	{
		this._currentNode = this._history.Pop();
		this._lastNode = this._currentNode;

		path = this._lastNode.ToPath();

		return this;
	}

	public PTreeNodeBuilder Ascend()
	{
		return Ascend(out string _);
	}

	public PTree AsTree()
	{
		return this._root;
	}
}
