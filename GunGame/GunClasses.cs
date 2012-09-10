using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace GunGame
{
    public class GunPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public int CurrentLevel { get; set; }
        public int Progress { get; set; }
        public GunGame CurrentGame { get; set; }
        public bool Ready = false;
        public GunPlayer KillingPlayer { get; set; }
        public int count = 0;

        public GunPlayer(int index)
        {
            Index = index;
        }

        public void IncreaseLevel()
        {
            if (CurrentGame != null && CurrentGame.Running)
            {
                if (CurrentLevel + 1 < CurrentGame.MaxLevel)
                {
                    CurrentLevel++;
                    GunTools.GiveNewItems(this, CurrentLevel, CurrentGame.GameLevels);
                }
                else
                    CurrentGame.Winner(this);
            }
        }
        public void DecreaseLevel()
        {
            if (CurrentGame != null && CurrentGame.Running)
            {
                if (CurrentLevel > 0)
                {
                    CurrentLevel--;
                    GunTools.GiveNewItems(this, CurrentLevel, CurrentGame.GameLevels);
                }
                else
                {
                    GunTools.GiveNewItems(this, CurrentLevel, CurrentGame.GameLevels);
                }
            }
        }
        public void GiveCurrentLevel()
        {
            if (CurrentGame != null && CurrentGame.Running)
            {
                GunTools.GiveNewItems(this, CurrentLevel, CurrentGame.GameLevels);
            }
        }
        public void GiveItem(int type, string name, int width, int height, int stack, int prefix = 0)
        {
            //count++;
            //Console.WriteLine("This has been called " + count + " times.");
            int itemid = Item.NewItem((int)TSPlayer.X, (int)TSPlayer.Y, width, height, type, stack, true, prefix);

            Main.item[itemid].SetDefaults(name);
            Main.item[itemid].wet = Collision.WetCollision(Main.item[itemid].position, Main.item[itemid].width, Main.item[itemid].height);
            Main.item[itemid].stack = stack;
            Main.item[itemid].owner = Index;
            Main.item[itemid].prefix = (byte)prefix;
            NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, "", itemid, 0f, 0f, 0f);
            NetMessage.SendData((int)PacketTypes.ItemOwner, -1, -1, "", itemid, 0f, 0f, 0f);
        }
    }

    public class GunGame
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public bool Running = false;
        public List<GunPlayer> Players = new List<GunPlayer>();
        public int Amount = 0;
        public int Ready { get; set; }
        public LevelList GameLevels { get; set; }
        public int MaxLevel { get; set; }

        public GunGame(string name, string levlist = "", string password = "")
        {
            Name = name;
            if (password != "")
                Password = password;
            if (levlist != "")
                GameLevels = GunTools.LoadLevelList(levlist);
            else
                GameLevels = GunMain.Default;
            MaxLevel = GameLevels.Levels.Count;
        }

        public void Broadcast(string message, Color color)
        {
            lock (Players)
            {
                foreach (GunPlayer ply in Players)
                {
                    ply.TSPlayer.SendMessage(message, color);
                }
            }
        }
        public void AddMember(int index)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(index);
            Console.WriteLine(ply.TSPlayer.Name);
            lock (Players)
                Players.Add(ply);
            ply.CurrentGame = this;
            ply.CurrentLevel = 0;
            ply.Progress = 0;
            Amount++;
            ply.TSPlayer.SendMessage(String.Format("You were added to the GunGame: \"{0}\"", this.Name), Color.Aqua);
            string inply = "";
            lock (Players)
            {
                ply.TSPlayer.SendMessage("Players in current GunGame: " + inply, Color.Aqua);
                foreach (GunPlayer gm in Players)
                {
                    if (gm != ply)
                    {
                        inply += String.Format(" {0}", gm.TSPlayer.Name);
                        //String.Concat(inply, String.Format(" {0}", gm.TSPlayer.Name));
                        gm.TSPlayer.SendMessage(ply.TSPlayer.Name + " has joined the GunGame.", Color.Aqua);
                    }
                }
            }
            if (!Running)
            {
                if (Amount < 2)
                {
                    ply.TSPlayer.SendMessage("Cannot start the GunGame until at least 2 people are in the game.", Color.Aqua);
                }
                else
                    StartGame();
            }
            else
            {
                ply.TSPlayer.SendMessage("You've joined a game in progress!", Color.Aqua);
                GunTools.SpawnAndGiveItems(ply);
            }

        }
        public void RemoveMember(int index)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(index);
            lock (Players)
            {
                Players.Remove(ply);
                foreach (GunPlayer gm in Players)
                {
                    gm.TSPlayer.SendMessage(ply.TSPlayer.Name + " has left the GunGame.", Color.Aqua);
                }
            }
            ply.TSPlayer.DamagePlayer(500);
            ply.CurrentGame = null;
            ply.KillingPlayer = null;
            Amount--;
            if (Amount == 0)
                GunTools.RemoveGame(this);
        }
        public void SilentRemoveMember(int index)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(index);
            ply.CurrentGame = null;
            ply.KillingPlayer = null;
            lock (Players)
            {
                Players.Remove(ply);
            }
            Amount--;
            if (Amount == 0)
                GunTools.RemoveGame(this);
        }
        public void StartGame()
        {
            lock (Players)
            {
                if (CheckReady() && Amount >= 2)
                {
                    Running = true;
                    foreach (GunPlayer ply in Players)
                    {
                        ply.KillingPlayer = null;
                        GunTools.SpawnAndGiveItems(ply);
                        ply.TSPlayer.SendMessage("The GunGame has begun! Kill each other to get level ups!", Color.Aqua);
                    }
                }
                else
                {
                    foreach (GunPlayer ply in Players)
                    {
                        ply.TSPlayer.SendMessage(String.Format("Need at least {0} more player(s) ready.", (Math.Ceiling((Amount * .7) - Ready)).ToString()), Color.Aqua);
                    }
                }
            }  
        }
        public void EndGame()
        {
            Running = false;
            lock (Players)
            {
                foreach (GunPlayer ply in Players)
                {
                    ply.CurrentLevel = 0;
                    ply.TSPlayer.SendMessage("The game has ended. A new game will begin as soon as enough players are ready.", Color.Aqua);
                    ply.Ready = false;
                }
            }
            StartGame(); 
        }
        public void Winner(GunPlayer ply)
        {
            lock (Players)
            {
                foreach (GunPlayer gm in Players)
                {
                    gm.TSPlayer.SendMessage(String.Format("The game is over and {0} has won!",ply.TSPlayer.Name));
                }
            }
            EndGame();
        }
        public bool CheckReady()
        {
            Ready = 0;
            lock (Players)
            {
                foreach (GunPlayer ply in Players)
                {
                    if (ply.Ready)
                        Ready++;
                }
            }
            double pct = Ready / Amount;
            if (pct >= .7)
                return true;
            return false;
        }
    }

    public class LevelList
    {
        public string Knife;
        public List<string[]> Levels;

        public void Print()
        {
            foreach (string[] str in Levels)
            {
                foreach (string st in str)
                {
                    Console.WriteLine(st);
                }
            }
        }
    }
}

