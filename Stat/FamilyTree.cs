using System.Collections.Generic;

namespace Stat
{
    class FamilyTreeItem
    {
        public int UID { get; }

        public List<FamilyTreeItem> Children { get; }

        public FamilyTreeItem(int uid)
        {
            this.UID        = uid;
            this.Children   = new List<FamilyTreeItem>();
        }

        public int RecursiveCount
        {
            get
            {
                int result = 1;

                for(int i = 0; i < this.Children.Count; i ++)
                {
                    result += Children[i].RecursiveCount;
                }

                return result;
            }
        }
    }
}