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

        public static List<FamilyTreeItem> LoadTree(int simID)
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
                            FamilyTreeItem item = new FamilyTreeItem(uid);
                            results.Add(item);
                            tmpDic.Add(uid, item);
                        }
                    }
                }
            }

            for(int i = 1; File.Exists($"{dir}/round{i}.gcasim"); i ++)
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
                            FamilyTreeItem child = new FamilyTreeItem(childUID);
                            tmpDic.Add(childUID, child);
                            tmpDic[spawn.GetIntChild("parentUID")].Children.Add(child);
                        }
                    }
                }
            }

            results = results.OrderByDescending(e => e.RecursiveCount).ToList();
            
            FamilyTreeBuilder.currentSim = simID;
            FamilyTreeBuilder.families = new ReadOnlyCollection<FamilyTreeItem>(results);

            return results;
        }

        public static bool IsLoaded { get { return FamilyTreeBuilder.currentSim != -1; } }

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
    }
}