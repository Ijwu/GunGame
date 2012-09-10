using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using Hooks;
using System.IO;
using System.ComponentModel;

namespace GunGame
{
    [APIVersion(1, 12)]
    public class GunMain : TerrariaPlugin
    {
        public override string Author
        {
            get { return "Ijwu"; }
        }

        public override string Description
        {
            get { return "GunGame Game Mode"; }
        }

        public override string Name
        {
            get { return "GunGame"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        public GunMain(Main game)
            : base(game)
        {

        }

        public static List<GunPlayer> Players = new List<GunPlayer>();
        public static List<GunGame> Games = new List<GunGame>();
        public static string SPath;
        public static LevelList Default {get { return GunTools.LoadLevelList("default"); } }
        //public static Dictionary<GunPlayer, bool> DeathDict = new Dictionary<GunPlayer, bool>();

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            ServerHooks.Join += OnJoin;
            ServerHooks.Leave += OnLeave;
            NetHooks.GetData += OnGetData;
            //GameHooks.Update += OnUpdate;

            GetDataHandlers.InitGetDataHandler();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                ServerHooks.Join -= OnJoin;
                ServerHooks.Leave -= OnLeave;
                NetHooks.GetData -= OnGetData;
                //GameHooks.Update -= OnUpdate;
            }
            base.Dispose(disposing);
        }

        private void OnInitialize()
        {
            SPath = Path.Combine(TShockAPI.TShock.SavePath, "GunGame/");

            if (!Directory.Exists(SPath))
                Directory.CreateDirectory(SPath);

            DefaultList.MakeDefault();
            Commands.ChatCommands.Add(new Command("joinGG", JoinGame, "joingg"));
            Commands.ChatCommands.Add(new Command("joinGG", StartGame, "startgg"));
            Commands.ChatCommands.Add(new Command("joinGG", ReadyUp, "readygg"));
            Commands.ChatCommands.Add(new Command("joinGG", QuitGame, "quitgg"));
        }
        private void OnJoin(int who, HandledEventArgs args)
        {
            if (TShock.Players[who].TPlayer.difficulty == 1)
                lock (Players)
                    Players.Add(new GunPlayer(who));
        }
        private void OnLeave(int who)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(who);
            if (ply != null)
            {
                if (ply.CurrentGame != null)
                    ply.CurrentGame.SilentRemoveMember(ply.Index);
                Players.Remove(ply);
            }
        }
        //private void OnUpdate()
        //{
        //    lock (Players)
        //    {
        //        foreach (GunPlayer ply in Players)
        //        {
        //            if (!DeathDict.Keys.Contains(ply))
        //                DeathDict.Add(ply, ply.TSPlayer.TPlayer.dead);
        //            else
        //            {
        //                if (ply.TSPlayer.TPlayer.dead != DeathDict[ply])
        //                {
        //                    if (ply.TSPlayer.TPlayer.dead)
        //                    {
        //                        DeathDict[ply] = ply.TSPlayer.TPlayer.dead;
        //                        GunTools.SpawnAndGiveItems(ply);
        //                    }
        //                    else
        //                        DeathDict[ply] = ply.TSPlayer.TPlayer.dead;
        //                }
        //            }
        //        }

        //    }

        //}
        private static void QuitGame(CommandArgs args)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(args.Player.Index);
            if (ply != null)
            {
                if (ply.CurrentGame != null)
                {
                    lock (ply)
                    {
                        ply.CurrentGame.RemoveMember(ply.Index);
                        ply.Ready = false;
                        ply.TSPlayer.SendMessage("You have quit your current GunGame.", Color.Aqua);
                    }
                }
                else
                    args.Player.SendMessage("You must be in a GunGame before being able to quit one.", Color.Red);
            }
            else
                args.Player.SendMessage("You are not on mediumcore difficulty, and so cannot participate in GunGames.", Color.Red);
        }
        private static void ReadyUp(CommandArgs args)
        {
            GunPlayer ply = GunTools.GetGunPlayerByID(args.Player.Index);
            if (ply != null)
            {
                if (ply.CurrentGame != null && !ply.CurrentGame.Running)
                {
                    ply.Ready = (!ply.Ready);
                    args.Player.SendMessage(String.Format("You are{0}ready.", (ply.Ready ? " " : " not ")), Color.Aqua);
                    lock (ply.CurrentGame.Players)
                    {
                        foreach (GunPlayer gm in ply.CurrentGame.Players)
                        {
                            if (gm != ply)
                            {
                                gm.TSPlayer.SendMessage(String.Format("{0} is {1} ready.", ply.TSPlayer.Name, (ply.Ready ? "now" : "NOT")), Color.Aqua);
                            }
                        }
                    }
                    ply.CurrentGame.StartGame();
                }
                else
                    args.Player.SendMessage("You must join a GunGame before readying/unreadying.", Color.Red);
            }
            else
                args.Player.SendMessage("You are not on mediumcore difficulty, and so cannot participate in GunGames.", Color.Red);
        }
        private static void JoinGame(CommandArgs args)
        {
            if (args.TPlayer.difficulty == 1)
            {
                if (GunTools.GetGunPlayerByID(args.Player.Index).CurrentGame == null)
                {
                    switch (args.Parameters.Count)
                    {
                        case 1:
                            {
                                lock (Games)
                                {
                                    foreach (GunGame gm in Games)
                                    {
                                        if (gm.Name == args.Parameters[0])
                                        {
                                            gm.AddMember(args.Player.Index);
                                            return;
                                        }
                                    }
                                    args.Player.SendMessage("Game not found.", Color.Red);
                                }
                                break;
                            }
                        case 2:
                            {
                                lock (Games)
                                {
                                    foreach (GunGame gm in Games)
                                    {
                                        if (gm.Name == args.Parameters[0] && gm.Password == args.Parameters[1])
                                        {
                                            gm.AddMember(args.Player.Index);
                                            return;
                                        }
                                        else if (gm.Password != args.Parameters[1])
                                        {
                                            args.Player.SendMessage("Wrong password for game \'" + gm.Name + "\'", Color.Red);
                                        }
                                    }
                                    args.Player.SendMessage("Game not found.", Color.Red);
                                }
                                break;
                            }
                    }
                }
                else
                    args.Player.SendMessage("You may not join a GunGame whilst in a GunGame.", Color.Red);
            }
            else
                args.Player.SendMessage("You are not on mediumcore difficulty, and so cannot participate in GunGames.", Color.Red);
        }
        private static void StartGame(CommandArgs args)
        {
            if (args.TPlayer.difficulty == 1)
            {
                if (GunTools.GetGunPlayerByID(args.Player.Index).CurrentGame == null)
                {
                    switch (args.Parameters.Count)
                    {
                        case 1:
                            {
                                lock (Games)
                                {
                                    foreach (GunGame game in Games)
                                    {
                                        if (game.Name == args.Parameters[0])
                                        {
                                            args.Player.SendMessage("A GunGame with that name presently exists. Please choose a different name.", Color.Red);
                                            return;
                                        }
                                    }
                                }
                                GunGame gm = new GunGame(args.Parameters[0]);
                                gm.AddMember(args.Player.Index);
                                Games.Add(gm);
                                break;
                            }

                        case 2:
                            {
                                lock (Games)
                                {
                                    foreach (GunGame game in Games)
                                    {
                                        if (game.Name == args.Parameters[0])
                                        {
                                            args.Player.SendMessage("A GunGame with that name presently exists. Please choose a different name.", Color.Red);
                                            break;
                                        }
                                    }
                                }
                                GunGame gm = new GunGame(args.Parameters[0], "", args.Parameters[1]);
                                gm.AddMember(args.Player.Index);
                                Games.Add(gm);
                                break;
                            }

                        case 3:
                            {
                                lock (Games)
                                {
                                    foreach (GunGame game in Games)
                                    {
                                        if (game.Name == args.Parameters[0])
                                        {
                                            args.Player.SendMessage("A GunGame with that name presently exists. Please choose a different name.", Color.Red);
                                            break;
                                        }
                                    }
                                }
                                GunGame gm = new GunGame(args.Parameters[0], args.Parameters[2], args.Parameters[1]);
                                gm.AddMember(args.Player.Index);
                                Games.Add(gm);
                                break;
                            }

                        default:
                            args.Player.SendMessage("Invalid syntax. Proper use: /startgg <Game Name> [Password] [Gun Set]", Color.Red);
                            break;
                    }
                }
                else
                    args.Player.SendMessage("You should not be in a GunGame before attempting to start one.", Color.Red);
            }
            else
                args.Player.SendMessage("You are not on mediumcore difficulty, and so cannot participate in GunGames.", Color.Red);
        }
        private void OnGetData(GetDataEventArgs e)
        {
            PacketTypes type = e.MsgID;
            var player = TShock.Players[e.Msg.whoAmI];

            if (player == null)
            {
                e.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                e.Handled = true;
                return;
            }

            using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        e.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}
