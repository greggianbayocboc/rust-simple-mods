using ConVar;
using System.Text;

namespace Oxide.Plugins
{
    [Info("CupboardCheck", "Gregg", "0.0.1"), Description("Checks and prevents extra auths on TC and Turret")]
    class CupboardCheck : RustPlugin
    {
        private int teamSize;
        private string valueColorHex = "#FFA500";


        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["teamSize"] = 3;
            SaveConfig();
        }

        void Init()
        {
            LoadConfigValues();
        }

        void LoadConfigValues()
        {
            teamSize = Config.Get<int>("teamSize");
        }

        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count == teamSize)
            {
                player.ChatMessage(ColorizeText("TC Auth full! You are not allowed to auth to this TC. Maximum of " + teamSize, valueColorHex));
                Puts("TC Auth check full! Player : "+player.displayName);
                return true;
            }
            return null;
        }
        object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
            if (turret.authorizedPlayers.Count == teamSize)
            {
                player.ChatMessage(ColorizeText("Turret Auth full! Maximum of "+teamSize, valueColorHex));
                Puts("Turret Auth check full! Player : " + player.displayName);
                return true;
            }
            return null;
        }

        object OnTurretAssign(AutoTurret turret, ulong targetId, BasePlayer initiator)
        {
            if (turret.authorizedPlayers.Count == teamSize)
            {
                
                initiator.ChatMessage(ColorizeText("Cannot add more player to AutoTurret!", valueColorHex));
                initiator.ChatMessage(ColorizeText("AutoTurret Auth full! Maximum team of " + teamSize, valueColorHex));
                Puts("AutoTurret Auth check full! Added by : " + initiator.displayName+" player : "+targetId);
                StringBuilder str = new StringBuilder();
                foreach (var player in turret.authorizedPlayers)
                {
                    str.Append(player.username + "\n");
                }
                initiator.ChatMessage("Players Auth : ");
                initiator.ChatMessage(str.ToString());
                return true;
            }
            return null;
        }

        string ColorizeText(string text, string hexColor)
        {
            return $"<color={hexColor}>{text}</color>";
        }
    }
}