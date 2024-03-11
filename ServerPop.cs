using ConVar;
using Oxide.Game.Rust.Libraries;
using System.Text;
using static Oxide.Core.Logging.Logger;

namespace Oxide.Plugins
{
    [Info("ServerPop", "Mabel", "1.0.4"), Description("Show server pop in chat with !pop trigger.")]
    class ServerPop : RustPlugin
    {
        private bool showOnlinePlayers;
        private bool showSleepingPlayers;
        private bool showJoiningPlayers;
        private bool showQueuedPlayers;

        private string chatPrefix = "<size=16><color=#FFA500>| ServerPop |</color></size>";
        private string valueColorHex = "#FFA500";
        private string valueColorHexBlue = "#89CFF0";
        private string wipeMessage = "";

        private ulong customSteamID = 0;
        private bool globalResponse = true;

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["ShowOnlinePlayers"] = true;
            Config["WipeMessage"] = "Weekly wipe every Thursday 8PM GMT+8. BP Wipe every first Thursday of the month 8PM GMT+8.";
            Config["ShowSleepingPlayers"] = true;
            Config["ShowJoiningPlayers"] = true;
            Config["ShowQueuedPlayers"] = true;
            Config["ChatPrefix"] = chatPrefix;
            Config["ValueColorHex"] = valueColorHex;
            Config["ChatIconSteamID"] = customSteamID;
            Config["GlobalResponse"] = globalResponse; // true = global response, false = player response
            SaveConfig();
        }

        void Init()
        {
            LoadConfigValues();
        }

        void LoadConfigValues()
        {
            showOnlinePlayers = Config.Get<bool>("ShowOnlinePlayers");
            showSleepingPlayers = Config.Get<bool>("ShowSleepingPlayers");
            showJoiningPlayers = Config.Get<bool>("ShowJoiningPlayers");
            showQueuedPlayers = Config.Get<bool>("ShowQueuedPlayers");
            wipeMessage = Config.Get<string>("WipeMessage");
            chatPrefix = Config.Get<string>("ChatPrefix");
            valueColorHex = Config.Get<string>("ValueColorHex");
            customSteamID = Config.Get<ulong>("ChatIconSteamID");

            object globalResponseObj = Config.Get("GlobalResponse");
            if (globalResponseObj != null && globalResponseObj is bool)
            {
                globalResponse = (bool)globalResponseObj;
            }
            else
            {
                globalResponse = true;

                Config["GlobalResponse"] = globalResponse;
                Config.Save();
            }
        }

        private void OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            if (message == "!pop")
            {
                SendMessage(player);
            }
            if (message == "!steamid")
            {
                player.ChatMessage(player.UserIDString);
                //player.blueprints.UnlockAll();
            }
            if (message == "!wipe")
            {
                Server.Broadcast(wipeMessage, null, customSteamID);
            }
            /* if (channel.Equals(ConVar.Chat.ChatChannel.Global))
             {
                 if(player.IPlayer.BelongsToGroup("vip"))
                     SendMessageGlobal(player, message, player.userID);
             }
             return null;*/
        }
        private void OnPlayerInit(BasePlayer player)
        {
            //unlocks all BP
            //player.blueprints.UnlockAll();
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            WelcomeNotice(player);

            Server.Broadcast("A player has logged in.", null, customSteamID);
        }

        private void OnPlayerRespawn(BasePlayer player)
        {
            WelcomeNotice(player);
        }

        private void SendMessageGlobal(BasePlayer player, string message, ulong steamid)
        {
            StringBuilder popMessage = new StringBuilder($"{ColorizeText("[VIP]", valueColorHex)} {ColorizeText(player.displayName, valueColorHexBlue)} : ");
            popMessage.Append(message);
            Server.Broadcast(popMessage.ToString(), null, steamid);
        }


        private void SendMessage(BasePlayer player)
        {
            StringBuilder popMessage = new StringBuilder($"{chatPrefix}\n\n");

            if (showOnlinePlayers)
                popMessage.AppendLine($"{ColorizeText($"{BasePlayer.activePlayerList.Count} / {ConVar.Server.maxplayers}", valueColorHex)} player's online\n");

            if (showSleepingPlayers)
                popMessage.AppendLine($"{ColorizeText(BasePlayer.sleepingPlayerList.Count.ToString(), valueColorHex)} player's sleeping\n");

            if (showJoiningPlayers)
                popMessage.AppendLine($"{ColorizeText(ServerMgr.Instance.connectionQueue.Joining.ToString(), valueColorHex)} player's joining\n");

            if (showQueuedPlayers)
                popMessage.AppendLine($"{ColorizeText(ServerMgr.Instance.connectionQueue.Queued.ToString(), valueColorHex)} player's queued\n");

            if (globalResponse)
            {
                Server.Broadcast(popMessage.ToString(), null, customSteamID);
            }
            else
            {
                player.ChatMessage(popMessage.ToString());
            }
        }

        string ColorizeText(string text, string hexColor)
        {
            return $"<color={hexColor}>{text}</color>";
        }

        void WelcomeNotice(BasePlayer player)
        {
            string wipe = ColorizeText("!wipe", valueColorHex);
            string pop = ColorizeText("!pop", valueColorHex);
            string kit = ColorizeText("/kit", valueColorHex);
            player.ChatMessage(ColorizeText("Welcome to Beginners Vanilla Server!", valueColorHex));
            player.ChatMessage("Type " + pop + " in chat to check online players.");
            player.ChatMessage("Type " + wipe + " in chat to check wipe schedule.");
            //player.ChatMessage("Type " + kit + " to claim one-time complementary STARTER KIT. We apologize for the sudden wipe.");
        }
    }
}