using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KillDeathHistory", "Gregg", "1.5.0")]
    [Description("PVP Kill/Death history with K/D, team ignore, scroll UI, Headshot %, Distance, and Weapon Icons")]
    public class KillDeathHistory : RustPlugin
    {
        #region Data Models

        class KDEntry
        {
            public string Type;        // Kill or Death
            public string OtherPlayer; // Victim or Killer
            public string Weapon;
            public string WeaponIcon;
            public float Distance;
            public bool Headshot;
            public string Time;
        }

        Dictionary<ulong, List<KDEntry>> History;

        const string UIName = "KDH_UI";
        int MaxEntries = 100;

        #endregion

        #region Hooks

        void Init()
        {
            LoadConfigValues();
            LoadData();
        }

        void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        void OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null)
                return;

            BasePlayer attacker = info.InitiatorPlayer;

            // ✅ STRICT PVP ONLY
            if (attacker == null || attacker.IsNpc || attacker.userID == victim.userID)
                return;

            // ✅ IGNORE TEAM KILLS
            if (IsSameTeam(attacker.userID, victim.userID))
                return;

            string weapon = info?.Weapon?.GetItem()?.info?.displayName?.english ?? "Unknown";
            string icon = GetWeaponIcon(weapon);
            float distance = Vector3.Distance(attacker.transform.position, victim.transform.position);
            bool headshot = info.HitBone.Contains("head");
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            AddEntry(victim.userID, new KDEntry
            {
                Type = "Death",
                OtherPlayer = attacker.displayName,
                Weapon = weapon,
                WeaponIcon = icon,
                Distance = distance,
                Headshot = headshot,
                Time = time
            });

            AddEntry(attacker.userID, new KDEntry
            {
                Type = "Kill",
                OtherPlayer = victim.displayName,
                Weapon = weapon,
                WeaponIcon = icon,
                Distance = distance,
                Headshot = headshot,
                Time = time
            });

            SaveData();
        }

        #endregion

        #region Commands

        [ChatCommand("kdh")]
        void CmdKDH(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            BasePlayer target = player;

            if (args.Length > 0)
            {
                if (!player.IsAdmin)
                {
                    player.ChatMessage("❌ You don't have permission.");
                    return;
                }

                target = FindPlayer(args[0]);
                if (target == null)
                {
                    player.ChatMessage("❌ Player not found.");
                    return;
                }
            }

            DrawUI(player, target);
        }

        #endregion

        #region UI

        void DrawUI(BasePlayer viewer, BasePlayer target)
        {
            DestroyUI(viewer);

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.88" },
                RectTransform = { AnchorMin = "0.25 0.1", AnchorMax = "0.75 0.9" },
                CursorEnabled = true
            }, "Overlay", UIName);

            // Title
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"Kill / Death History - {target.displayName}",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" }
            }, UIName);

            // Close
            container.Add(new CuiButton
            {
                Button = { Color = "0.8 0.2 0.2 1", Close = UIName },
                RectTransform = { AnchorMin = "0.93 0.94", AnchorMax = "0.98 0.99" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, UIName);

            // K/D Summary + Headshot %
            GetKD(target.userID, out int kills, out int deaths, out int headshots);
            float kd = deaths == 0 ? kills : (float)kills / deaths;
            float hsPercent = kills == 0 ? 0f : (float)headshots / kills * 100f;

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"Kills: {kills}   Deaths: {deaths}   K/D: {kd:F2}   HS%: {hsPercent:F1}%",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = { AnchorMin = "0 0.87", AnchorMax = "1 0.93" }
            }, UIName);

            // Scroll Panel
            string scrollPanel = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.86" }
            }, UIName);

            var scrollView = new CuiScrollView
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Scroll = { Vertical = true },
                ContentTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            };

            container.Add(scrollView, scrollPanel);

            if (History.ContainsKey(target.userID))
            {
                float y = 1f;
                float step = 0.08f;

                foreach (var entry in History[target.userID])
                {
                    string color = entry.Type == "Kill"
                        ? "0.3 0.8 0.3 1"
                        : "0.8 0.3 0.3 1";

                    string hs = entry.Headshot ? "HS" : "";
                    string text = $"[{entry.Time}] {entry.Type} → {entry.OtherPlayer} {entry.WeaponIcon} ({entry.Weapon} | {entry.Distance:F0}m | {hs})";

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = text,
                            FontSize = 12,
                            Align = TextAnchor.MiddleLeft,
                            Color = color
                        },
                        RectTransform =
                        {
                            AnchorMin = $"0 {y - step}",
                            AnchorMax = $"1 {y}"
                        }
                    }, scrollPanel);

                    y -= step;
                }
            }

            CuiHelper.AddUi(viewer, container);
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIName);
        }

        #endregion

        #region Helpers

        void GetKD(ulong id, out int kills, out int deaths, out int headshots)
        {
            kills = 0;
            deaths = 0;
            headshots = 0;

            if (!History.ContainsKey(id)) return;

            foreach (var e in History[id])
            {
                if (e.Type == "Kill") kills++;
                if (e.Type == "Death") deaths++;
                if (e.Type == "Kill" && e.Headshot) headshots++;
            }
        }

        bool IsSameTeam(ulong a, ulong b)
        {
            var teamA = RelationshipManager.ServerInstance.FindPlayersTeam(a);
            var teamB = RelationshipManager.ServerInstance.FindPlayersTeam(b);

            return teamA != null && teamB != null && teamA.teamID == teamB.teamID;
        }

        void AddEntry(ulong id, KDEntry entry)
        {
            if (!History.ContainsKey(id))
                History[id] = new List<KDEntry>();

            History[id].Insert(0, entry);

            if (History[id].Count > MaxEntries)
                History[id].RemoveAt(History[id].Count - 1);
        }

        BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == nameOrId)
                    return p;

                if (p.displayName.IndexOf(nameOrId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            }
            return null;
        }

        string GetWeaponIcon(string weapon)
        {
            weapon = weapon.ToLower();

            if (weapon.Contains("rifle") || weapon.Contains("ak") || weapon.Contains("m4"))
                return "🔫";
            if (weapon.Contains("pistol") || weapon.Contains("revolver"))
                return "🔫";
            if (weapon.Contains("bow") || weapon.Contains("crossbow"))
                return "🏹";
            if (weapon.Contains("sword") || weapon.Contains("hatchet") || weapon.Contains("pickaxe"))
                return "⚔️";
            if (weapon.Contains("fists") || weapon.Contains("knife"))
                return "🗡️";

            return "❓";
        }

        #endregion

        #region Config & Data

        protected override void LoadDefaultConfig()
        {
            Config["Max History Entries"] = 100;
            SaveConfig();
        }

        void LoadConfigValues()
        {
            MaxEntries = Convert.ToInt32(Config["Max History Entries"]);
        }

        void LoadData()
        {
            History = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<KDEntry>>>(Name)
                      ?? new Dictionary<ulong, List<KDEntry>>();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, History);
        }

        #endregion
    }
}
