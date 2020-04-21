using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

using static System.Math;

namespace Stat
{
    class FamilyTreeItem
    {
        private readonly ReadOnlyDictionary<(int, int), int> angles = new ReadOnlyDictionary<(int, int), int>(
        new Dictionary<(int, int), int>()
        {
            {(0, 1), 210}, {(0, 2), 240}, {(0, 3), 270}, {(0, 4), 300}, {(0, 5), 330},
            {(1, 0), 30}, {(1, 2), 270}, {(1, 3), 300}, {(1, 4), 330}, {(1, 5), 0},
            {(2, 0), 60}, {(2, 1), 90}, {(2, 3), 330}, {(2, 4), 0}, {(2, 5), 30},
            {(3, 0), 90}, {(3, 1), 120}, {(3, 2), 150}, {(3, 4), 30}, {(3, 5), 60},
            {(4, 0), 120}, {(4, 1), 150}, {(4, 2), 180}, {(4, 3), 210}, {(4, 5), 90},
            {(5, 0), 150}, {(5, 1), 180}, {(5, 2), 210}, {(5, 3), 240}, {(5, 4), 270}
        });

        public int UID { get; }

        public int StartState { get; }

        public int SpawnRound { get; }
        public int DeathRound { get; set; } = -1;

        public List<FamilyTreeItem> Children { get; }

        public ReadOnlyDictionary<(int, string), int> wiring;

        public FamilyTreeItem(int uid, int spawnRound, int startState, Dictionary<(int, string), int> wiring)
        {
            this.UID        = uid;
            this.SpawnRound = spawnRound;
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
            float heightPercent = 1 - (float)Sqrt(0.75);
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

                Func<float, float> ToRad = (float deg) =>
                {
                    return (float)(PI / 180 * deg);
                };

                // draw state changes
                foreach(KeyValuePair<(int, string), int> change in this.wiring)
                {
                    if(change.Key.Item1 != change.Value)
                    {
                        float   xstart = xs[change.Key.Item1] + 100 - 100 * (float)Sin(ToRad(angles[(change.Key.Item1, change.Value)])),
                                ystart = ys[change.Key.Item1] + 100 + 100 * (float)Cos(ToRad(angles[(change.Key.Item1, change.Value)])),
                                xend   = xs[change.Value]     + 100 - 100 * (float)Sin(ToRad(angles[(change.Value, change.Key.Item1)])),
                                yend   = ys[change.Value]     + 100 + 100 * (float)Cos(ToRad(angles[(change.Value, change.Key.Item1)]));
                        g.DrawLine(blackPen, xstart, ystart, xend, yend);

                        g.FillRectangle(Brushes.Black, xend - 10, yend - 10, 20, 20);
                    }
                }
            }

            return result;
        }
    }
}