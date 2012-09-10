using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using System.IO;

namespace GunGame
{
    public class GunTools
    {
        public static GunPlayer GetGunPlayerByID(int index)
        {
            GunPlayer player = null;
            lock (GunMain.Players)
            {
                foreach (GunPlayer ply in GunMain.Players)
            {
                if (ply.Index == index)
                    return ply;
            }
            }
            return player;
        }
        public static LevelList LoadLevelList(string levlist)
        {
            string[] files = Directory.GetFiles(GunMain.SPath, "*.json");
            LevelList levels = new LevelList();
            foreach (string file in files)
            {
                if (file.Split('.')[0].EndsWith(levlist))
                {
                    StreamReader read = new StreamReader(GunMain.SPath + levlist + ".json");
                    levels = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelList>(read.ReadToEnd());
                    read.Dispose();
                    break;
                }
            }
            return levels;
        }
        public static void RemoveGame(GunGame game)
        {
            lock (GunMain.Games)
                GunMain.Games.Remove(game);
        }
        public static void GiveNewItems(GunPlayer ply, int level, LevelList list)
        {
            int x = ply.TSPlayer.TileX;
            int y = ply.TSPlayer.TileY;
            ply.TSPlayer.DamagePlayer(500);
            ply.TSPlayer.Teleport(x, y);
            string[] items = list.Levels[level];
            foreach (string item in items)
            {
                if (item != "null")
                {
                    Item give = TShock.Utils.GetItemByName(item)[0];
                    ply.GiveItem(give.type, give.name, give.width, give.height, give.maxStack);
                }
            }
            if (list.Knife != "null")
            {
                Item knife = TShock.Utils.GetItemByName(list.Knife)[0];
                ply.GiveItem(knife.type, knife.name, knife.width, knife.height, knife.maxStack);
            }
            ply.TSPlayer.Teleport(x, y);
        }
        public static void SpawnAndGiveItems(GunPlayer ply)
        {
            ply.TSPlayer.Spawn();
            ply.GiveCurrentLevel();
        }
        public static bool CheckIfFreshCharacter(GunPlayer ply)
        {
            foreach(Item item in ply.TSPlayer.TPlayer.armor)
            {
                if (item.type != 0)
                    return false;
            }
            if (ply.TSPlayer.TPlayer.statLifeMax != 100)
                return false;
            return true;
        }
    }
}
