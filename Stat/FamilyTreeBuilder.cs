using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using JSONSerializer;

namespace Stat
{
    class FamilyTreeBuilder
    {
        private static int currentSim = -1;
        private static ReadOnlyCollection<FamilyTreeItem> families;

        public static void Unload()
        {
            currentSim = -1;
            families = null;
        }

        private static Dictionary<(int, string), int> GetWiring(JSONObject automaton)
        {
            var result = new Dictionary<(int, string), int>();

            JSONArray wiringArr = automaton.GetArrayChild("wiring");
            for(int i = 0; i < wiringArr.Count; i ++)
            {
                JSONObject change = (JSONObject)wiringArr[i];
                result.Add((change.GetIntChild("from"), change.GetStringChild("through")), change.GetIntChild("to"));
            }

            return result;
        }

        public static List<FamilyTreeItem> LoadTree(int simID, int maxRound = int.MaxValue, bool order = true)
        {
            List<FamilyTreeItem> results = new List<FamilyTreeItem>();
            Dictionary<int, FamilyTreeItem> tmpDic = new Dictionary<int, FamilyTreeItem>();

            string dir = Program.GetSimDir(simID);

            using(StreamReader be = new StreamReader($"{dir}/initial.gcasim"))
            {
                while(!be.EndOfStream)
                {
                    JSONObject chunk = new JSONObject(be.ReadLine());
                    JSONArray data = chunk.GetArrayChild("data");
                    for(int i = 0; i < data.Count; i ++)
                    {
                        JSONObject content = (JSONObject)data[i];
                        if(content.GetStringChild("Type") == "a")
                        {
                            int uid = content.GetIntChild("UID");
                            var wiring = GetWiring(content);

                            FamilyTreeItem item = new FamilyTreeItem(uid, 0, content.GetIntChild("startState") ,wiring);
                            results.Add(item);
                            tmpDic.Add(uid, item);
                        }
                    }
                }
            }
            RoundCount = 0;

            // key: UID, val: round of death
            Dictionary<int, int> deathList = new Dictionary<int, int>();

            for(int i = 1; File.Exists($"{dir}/round{i}.gcasim") && i <= maxRound; i ++)
            {
                using(StreamReader be = new StreamReader($"{dir}/round{i}.gcasim"))
                {
                    while(!be.EndOfStream)
                    {
                        JSONObject chunk = new JSONObject(be.ReadLine());
                        JSONArray spawns = chunk.GetArrayChild("spawns");
                        for(int j = 0; j < spawns.Count; j ++)
                        {
                            JSONObject spawn = (JSONObject)spawns[j];
                            int childUID = spawn.GetIntChild("childUID");
                            FamilyTreeItem child = new FamilyTreeItem(childUID, i, spawn.GetIntChild("startState"), GetWiring(spawn));
                            tmpDic.Add(childUID, child);
                            tmpDic[spawn.GetIntChild("parentUID")].Children.Add(child);
                        }

                        JSONArray deaths = chunk.GetArrayChild("deaths");
                        for(int j = 0; j < deaths.Count; j ++)
                        {
                            JSONObject death = (JSONObject)deaths[j];
                            deathList.Add(death.GetIntChild("UID"), i);
                        }


                    }
                }
                RoundCount ++;
            }

            if(order)
            {
                results = results.OrderByDescending(e => e.RecursiveCount).ToList();
            }
            
            FamilyTreeBuilder.currentSim = simID;
            FamilyTreeBuilder.families = new ReadOnlyCollection<FamilyTreeItem>(results);
            SetDeaths(deathList);

            return results;
        }

        private static void SetDeaths(Dictionary<int, int> deathList)
        {
            Queue<FamilyTreeItem> q = new Queue<FamilyTreeItem>();
            for(int i = 0; i < families.Count; i ++)
            {
                q.Enqueue(families[i]);
            }
            while(q.Count != 0)
            {
                FamilyTreeItem current = q.Dequeue();
                if(deathList.ContainsKey(current.UID))
                {
                    current.DeathRound = deathList[current.UID];
                }

                for(int i = 0; i < current.Children.Count; i ++)
                {
                    q.Enqueue(current.Children[i]);
                }
            }
        }

        public static bool IsLoaded { get { return FamilyTreeBuilder.currentSim != -1; } }

        public static FamilyTreeItem GetAutomaton(int id)
        {
            if(!IsLoaded)
            {
                throw new InvalidOperationException("No simulation is loaded.");
            }

            for(int i = 0; i < families.Count; i ++)
            {
                FamilyTreeItem result = families[i].SearchChild(id);
                if(result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static List<(int from, string through, int to)> GetWiring(int automatonID)
        {
            if(!FamilyTreeBuilder.IsLoaded)
            {
                throw new InvalidOperationException("No simulation is loaded.");
            }

            List<(int, string, int)> result = new List<(int, string, int)>();

            using(StreamReader be = new StreamReader($"{Program.GetSimDir(currentSim)}/initial.gcasim"))
            {
                while(!be.EndOfStream)
                {
                    JSONArray data = new JSONObject(be.ReadLine()).GetArrayChild("data");
                    for(int i = 0; i < data.Count; i ++)
                    {
                        JSONObject content = (JSONObject)data[i];
                        if(content.GetStringChild("Type") == "a" && content.GetIntChild("UID") == automatonID)
                        {
                            JSONArray wiring = content.GetArrayChild("wiring");
                            for(int j = 0; j < wiring.Count; j ++)
                            {
                                JSONObject stateChange = (JSONObject)wiring[j];
                                result.Add((stateChange.GetIntChild("from"), stateChange.GetStringChild("through"), stateChange.GetIntChild("to")));
                            }

                            return result;
                        }
                    }
                }
            }

            string dir = Program.GetSimDir(currentSim);
            for(int i = 1; File.Exists($"{dir}/round{i}.gcasim"); i ++)
            {
                using(StreamReader be = new StreamReader($"{dir}/round{i}.gcasim"))
                {
                    while(!be.EndOfStream)
                    {
                        JSONArray spawns = new JSONObject(be.ReadLine()).GetArrayChild("spawns");
                        for(int j = 0; j < spawns.Count; j ++)
                        {
                            JSONObject content = (JSONObject)spawns[j];
                            if(content.GetIntChild("childUID") == automatonID)
                            {
                                JSONArray wiring = content.GetArrayChild("wiring");
                                for(int k = 0; k < wiring.Count; k ++)
                                {
                                    JSONObject stateChange = (JSONObject)wiring[k];
                                    result.Add((stateChange.GetIntChild("from"), stateChange.GetStringChild("through"), stateChange.GetIntChild("to")));
                                }

                                return result;
                            }
                        }
                    }
                }
            }

            throw new KeyNotFoundException("No automaton exists with the given ID.");
        }

        public static ReadOnlyCollection<FamilyTreeItem> Families
        {
            get { return families; }
        }

        public static int CurrentSimID
        {
            get { return currentSim; }
        }

        public static int RoundCount
        {
            get; private set;
        }
    }
}