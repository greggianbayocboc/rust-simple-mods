using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BanTeamOnGameBan", "Gregg", "1.0.2")]
    [Description("Bans all teammates of a player if the player is game banned")]
    public class BanTeamOnGameBan : RustPlugin
    {
        private void OnUserBanned(string name, string id, string address, string reason)
        {

            if (!ulong.TryParse(id, out ulong bannedId)) return;

            BasePlayer bannedPlayer = BasePlayer.FindByID(bannedId);
            if (bannedPlayer == null) return;

            ulong teamId = bannedPlayer.currentTeam;
            if (teamId == 0) return;

            var team = RelationshipManager.ServerInstance.FindTeam(teamId);
            if (team == null) return;

            foreach (ulong memberId in team.members)
            {
                if (memberId == bannedId) continue;

                string banReason = "Banned due to teammate receiving a game ban";

                BasePlayer teammate =
                    BasePlayer.FindByID(memberId) ??
                    BasePlayer.FindSleeping(memberId);

                string username = teammate != null ? teammate.displayName : "Teammate";

                ServerUsers.Set(
                    memberId,
                    ServerUsers.UserGroup.Banned,
                    username,
                    banReason,
                    0L // permanent ban
                );

                ServerUsers.Save();

                if (teammate != null)
                    teammate.Kick(banReason);

                Puts($"[BanTeamOnGameBan] Teammate {memberId} banned due to team game ban.");
            }
        }
    }
}
