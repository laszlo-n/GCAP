using System.Collections.Generic;
using System.IO;
using System.Linq;

using JSONSerializer;

namespace Stat
{
    class FamilyTreeBuilder
    {
        public static List<FamilyTreeItem> BuildTree(int simID)
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

            return results;
        }
    }
}