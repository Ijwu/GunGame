using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GunGame
{
    class DefaultList
    {
        public static void MakeDefault()
        {
            if (Directory.Exists(GunMain.SPath + "default.json"))
                return;
            else
                DefList();
        }
        private static void DefList()
        {
            //At first I manually make the default. Since I have no easy way of building a LevelList object, I resort to deserializing a JSON string that just represents the default.
            string JSONDefault = "{\"Knife\":\"Light's Bane\", \"Levels\": [[\"Blowpipe\", \"Seed\"], [\"Wooden Boomerang\"], [\"Wooden Bow\", \"Wooden Arrow\"], [\"Shuriken\"], [\"Iron Bow\", \"Wooden Arrow\"], [\"Throwing Knife\"], [\"Enchanted Boomerang\"], [\"Demon Bow\", \"Wooden Arrow\"], [\"Flintlock Pistol\", \"Musket Ball\"], [\"Handgun\", \"Musket Ball\"], [\"Shotgun\", \"Musket Ball\"], [\"Musket\", \"Musket Ball\"], [\"Phoenix Blaster\", \"Musket Ball\"], [\"Clockwork Assault Rifle\", \"Musket Ball\"], [\"Light Disk\"], [\"Minishark\", \"Musket Ball\"], [\"Grenade\"], [\"null\"]]}";
            LevelList test = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelList>(JSONDefault);
            //After I have the default LevelList object, I'll just reserialize it with formatting and write to the file.
            JSONDefault = Newtonsoft.Json.JsonConvert.SerializeObject(test, Newtonsoft.Json.Formatting.Indented);
            StreamWriter write = new StreamWriter(GunMain.SPath + "default.json");
            write.Write(JSONDefault);
            write.Flush();
            write.Close();
            write.Dispose();
        }
    }
}
