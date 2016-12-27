using log4net;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET;
using System;
using System.Collections.Generic;
using MiNET.Items;
using MiNET.Net;
using MiNET.Worlds;

namespace KeepInventory
{
    [Plugin(PluginName = "KeepInventory", Description = "KeepInventory for MiNET", PluginVersion = "1.0", Author = "haniokasai")]
    public class KeepInventory : Plugin
    {
        protected static ILog _log = LogManager.GetLogger("KeepInventory");

        Dictionary<string, string> inv = new Dictionary<string, string>();
        Dictionary<string, string> arm = new Dictionary<string, string>();

        protected override void OnEnable()
        {
            Context.Server.PlayerFactory.PlayerCreated += PlayerFactory_PlayerCreated;
            _log.Warn("Loaded");
        }

        private void PlayerFactory_PlayerCreated(object sender, PlayerEventArgs e)
        {
            var player = e.Player;
            player.HealthManager.PlayerTakeHit += Player_PlayerTakeHit;
            player.Teleport();
        }

        private void Player_PlayerTakeHit(object sender, HealthEventArgs e)
        {

           
            Player player = (Player)e.TargetEntity;//受けるほう
            player.BroadcastEntityEvent();
            if (0 >= player.HealthManager.Health)
            {
                //https://github.com/DarkLexFirst/SkyBlock-test/blob/25483451a6a2333da3b10bdf33ead546552df26f/SkyBlock%20betaRelease/SkyBlock%20betaRelease/Managers/InventoryManager.cs
                string Inv = player.Inventory.GetSlots()[0].Id + "," + player.Inventory.GetSlots()[0].Metadata + "," + player.Inventory.GetSlots()[0].Count;
                for (var i = 1; i < player.Inventory.Slots.Count; i++)
                {
                    Inv = Inv + "|" + player.Inventory.GetSlots()[i].Id + "," + player.Inventory.GetSlots()[i].Metadata + "," + player.Inventory.GetSlots()[i].Count;
                }
                string Arm = player.Inventory.Boots.Id + "," + player.Inventory.Boots.Metadata + "|" + player.Inventory.Leggings.Id + "," + player.Inventory.Leggings.Metadata + "|" + player.Inventory.Chest.Id + "," + player.Inventory.Chest.Metadata + "|" + player.Inventory.Helmet.Id + "," + player.Inventory.Helmet.Metadata;
                for (var i = 1; i < player.Inventory.Slots.Count; i++) { player.Inventory.Slots[i] = new ItemAir(); }
                inv.Add(player.Username, Inv);
                arm.Add(player.Username, Arm);
                player.Inventory.Clear();
                player.HealthManager.TakeHit((Player)e.SourceEntity,100,player.HealthManager.LastDamageCause);
                player.SendMessage("aaa");

                if (player != null)
                {
                    player.SendUpdateAttributes();
                    player.BroadcastEntityEvent();
                }

                player.BroadcastSetEntityData();
                player.DespawnEntity();
                var mcpeRespawn = McpeRespawn.CreateObject();
                mcpeRespawn.x = player.SpawnPosition.X;
                mcpeRespawn.y = player.SpawnPosition.Y;
                mcpeRespawn.z = player.SpawnPosition.Z;
                player.SendPackage(mcpeRespawn);
                player.HealthManager.ResetHealth();
            }

        }

        [PacketHandler, Send]
        public void RespawnHandler(McpeRespawn packet, Player player)
        {
            Console.WriteLine("respawn");
            string[] PlayerInv = inv[player.Username].Split('|');
            Item item;
            for (var i = 0; i < player.Inventory.Slots.Count; i++)
            {
                item = ItemFactory.GetItem(Convert.ToInt16(PlayerInv[i].Split(',')[0]), Convert.ToInt16(PlayerInv[i].Split(',')[1]), Convert.ToByte(PlayerInv[i].Split(',')[2]));
                if (item.Count != 0 && item.Id != 0)
                    player.Inventory.Slots[i] = item;
            }
            string[] PlayerArm = arm[player.Username].Split('|');
            player.Inventory.Boots = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[0].Split(',')[0]), Convert.ToInt16(PlayerArm[0].Split(',')[1]));
            player.Inventory.Leggings = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[1].Split(',')[0]), Convert.ToInt16(PlayerArm[1].Split(',')[1]));
            player.Inventory.Chest = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[2].Split(',')[0]), Convert.ToInt16(PlayerArm[2].Split(',')[1]));
            player.Inventory.Helmet = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[3].Split(',')[0]), Convert.ToInt16(PlayerArm[3].Split(',')[1]));
            player.SendPlayerInventory();
        }

        [Command(Name = "gm")]
        public void GameMode(Player player, int gameMode)
        {
            player.SetGameMode((GameMode)gameMode);

            player.Level.BroadcastMessage($"{player.Username} changed to game mode {(GameMode)gameMode}.", type: MessageType.Raw);
        }
    }
}
