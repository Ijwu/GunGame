using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using System.IO;
using System.IO.Streams;

namespace GunGame
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
    internal class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }
    internal static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
        {
            {PacketTypes.PlayerKillMe, HandlePlayerKillMe},                
            {PacketTypes.ItemDrop, HandleItemDrop},
            {PacketTypes.PlayerDamage, HandlePlayerDamage}
        };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }
        private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
        {
            int index = args.Player.Index;
            byte PlayerID = (byte)args.Data.ReadByte();
            byte hitDirection = (byte)args.Data.ReadByte();
            Int16 Damage = (Int16)args.Data.ReadInt16();
            bool PVP = args.Data.ReadBoolean();
            GunPlayer player = GunTools.GetGunPlayerByID(PlayerID);
            GunPlayer attacker = player.KillingPlayer;
            if (player != null && attacker != null)
            {
                if (player.CurrentGame != null && player.CurrentGame.Running && player.KillingPlayer != null)
                {
                    lock (player.CurrentGame.Players)
                    {
                        if (player.CurrentGame.Players.Contains(attacker))
                        {
                            if (attacker.TSPlayer.TPlayer.inventory[attacker.TSPlayer.TPlayer.selectedItem].name == "Light's Bane")
                            {
                                TShock.Utils.Broadcast(String.Format("{0} stole a level from {1}!", attacker.TSPlayer.Name, player.TSPlayer.Name), Color.Aqua);
                                player.DecreaseLevel();
                                attacker.IncreaseLevel();
                                player.TSPlayer.TPlayer.dead = true;
                            }
                            else
                            {
                                attacker.IncreaseLevel();
                                player.TSPlayer.TPlayer.dead = true;
                                GunTools.SpawnAndGiveItems(player);
                            }
                        }
                    }
                }
                else
                {
                    player.TSPlayer.TPlayer.dead = true;
                    GunTools.SpawnAndGiveItems(player);
                }
            }
            player.KillingPlayer = null;
            return false;
        }
        private static bool HandleItemDrop(GetDataHandlerArgs args)
        {
            int index = args.Player.Index;
            var id = args.Data.ReadInt16();
            var posx = args.Data.ReadSingle();
            var posy = args.Data.ReadSingle();
            var velx = args.Data.ReadSingle();
            var vely = args.Data.ReadSingle();
            var stack = args.Data.ReadByte();
            var prefix = args.Data.ReadByte();
            var type = args.Data.ReadInt16();
            GunPlayer ply = GunTools.GetGunPlayerByID(index);

            if (ply.CurrentGame != null && ply.CurrentGame.Running)
            {
                //Console.WriteLine(TShock.Utils.GetItemById(type).name);
                foreach (string[] arr in ply.CurrentGame.GameLevels.Levels)
                {
                    foreach (string item in arr)
                    {
                        if (TShock.Utils.GetItemById(type).name == item || ply.CurrentGame.GameLevels.Knife == TShock.Utils.GetItemById(type).name)
                            return true;
                    }
                }
            }

            return false;
        }
        private static bool HandlePlayerDamage(GetDataHandlerArgs args)
        {
            int index = args.Player.Index; //Attacking Player
            byte PlayerID = (byte)args.Data.ReadByte(); //Damaged Player
            byte hitDirection = (byte)args.Data.ReadByte();
            Int16 Damage = (Int16)args.Data.ReadInt16();
            var player = GunTools.GetGunPlayerByID(PlayerID);
            bool PVP = args.Data.ReadBoolean();
            byte Crit = (byte)args.Data.ReadByte();

            if (player != null && GunTools.GetGunPlayerByID(index) != null)
            {
                if (index != PlayerID)
                {
                    player.KillingPlayer = GunTools.GetGunPlayerByID(index);
                }
                else
                    player.KillingPlayer = null;
            }
            return false;
        }
    }
}

