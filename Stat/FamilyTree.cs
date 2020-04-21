using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

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

        public Bitmap GenerateImage()
        {
            float heightPercent = 1 - (float)System.Math.Sqrt(0.75);
            float side = 400;
            float extra = heightPercent * side;

            Bitmap result = new Bitmap(1000, 1000);
            using(Graphics g = Graphics.FromImage(result))
            using(Pen blackPen = new Pen(Brushes.Black, 1))
            {
                // whiten image
                g.FillRectangle(Brushes.White, 0, 0, 1000, 1000);

                // draw circles and fill starting state
                float[] xs = { 0,   200, 600, 800, 600, 200 };
                float[] ys = { 400, 0 + extra, 0 + extra, 400, 800 - extra, 800 - extra };

                for(int i = 0; i < xs.Length; i ++)
                {
                    g.DrawArc(blackPen, xs[i], ys[i], 200, 200, 0, 360);
                }
                g.FillEllipse(Brushes.Black, xs[this.StartState], ys[this.StartState], 200, 200);

                // draw state changes
                foreach(KeyValuePair<(int, string), int> change in this.wiring)
                {
                    g.DrawLine(blackPen, xs[change.Key.Item1] + 100, ys[change.Key.Item1] + 100, xs[change.Value] + 100, ys[change.Value] + 100);
                }
            }

            return result;
        }
    }
}