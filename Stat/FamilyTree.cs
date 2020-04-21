using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Stat
{
    class FamilyTreeItem
    {
        public int UID { get; }

        public int StartState { get; }

        public List<FamilyTreeItem> Children { get; }

        public ReadOnlyDictionary<(int, string), int> wiring;

        public FamilyTreeItem(int uid, int startState, Dictionary<(int, string), int> wiring)
        {
            this.UID        = uid;
            this.StartState = startState;
            this.Children   = new List<FamilyTreeItem>();
            this.wiring     = new ReadOnlyDictionary<(int, string), int>(wiring);
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

        public int ComputeResult(string input)
        {
            int result = this.StartState;

            for(int i = 0; i < input.Length; i ++)
            {
                result = wiring[(result, input[i].ToString())];
            }

            return result;
        }

        public FamilyTreeItem SearchChild(int id)
        {
            if(this.UID == id)
            {
                return this;
            }

            for(int i = 0; i < this.Children.Count; i ++)
            {
                FamilyTreeItem result = this.Children[i].SearchChild(id);
                if(result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}