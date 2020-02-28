using System.Collections.Generic;

namespace EFOP.WorldElements
{
	class FamilyTree
	{
		public FamilyTreeNode Root { get; }
		
		public FamilyTree(Automaton rootNode)
		{
			this.Root = new FamilyTreeNode(rootNode, null, true);
		}
		
		public bool ContainsAutomaton(Automaton a)
		{
			return this.Root.ContainsAutomaton(a);
		}
		
		public void AddToTree(Automaton parent, Automaton child)
		{
			this.Root.AddToTree(parent, child);
		}
	}
	
	class FamilyTreeNode
	{
		public bool IsRootNode { get; }
		public Dictionary<int, FamilyTreeNode> Children { get; }
		public Automaton Parent { get; }
		public Automaton Item { get; }
		
		public FamilyTreeNode(Automaton item, Automaton parent, bool isRootNode = false)
		{
			this.Item		= item;
			this.Parent		= parent;
			this.IsRootNode	= isRootNode;
			this.Children	= new Dictionary<int, FamilyTreeNode>();
		}
		
		public bool ContainsAutomaton(Automaton a)
		{
			if(this.Item.UID == a.UID || this.Children.ContainsKey(a.UID))
				return true;
			
			bool result = false;
			foreach(KeyValuePair<int, FamilyTreeNode> child in this.Children)
			{
				result = child.Value.ContainsAutomaton(a);
				if(result)
					break;
			}
			return result;
		}
		
		public void AddToTree(Automaton parent, Automaton child)
		{
			if(this.Item.UID == parent.UID)
			{
				this.Children.Add(child.UID, new FamilyTreeNode(child, parent));
				return;
			}
			
			foreach(KeyValuePair<int, FamilyTreeNode> node in this.Children)
			{
				if(node.Value.ContainsAutomaton(parent))
				{
					node.Value.AddToTree(parent, child);
					break;
				}
			}
		}
		
		public int ItemCount()
		{
			int result = 1;
			foreach(KeyValuePair<int, FamilyTreeNode> child in this.Children)
			{
				result += child.Value.ItemCount();
			}
			return result;
		}
	}
}