using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KillDeathHistory", "Gregg", "1.3.0")]
    [Description("Records PVP kill/death history and displays it in a UI")]
    public class KillDeathHistory : RustPlugin
    {
        #region Data Models

        class KDEntry
        {
            public string Type;        // Kill or Death
            public string OtherPlayer; // Victim or Killer
            public string Weapon;
            public string Time;
        }

        Dictionary<ulong, List<KDEntry>> History;

        const string UIName = "KDH_UI";
        int MaxEntries = 20;

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

            // ✅ ONLY record PLAYER vs PLAYER
            if (attacker == null || attacker.userID == victim.userID)
                return;

            string weapon = info?.Weapon?.GetItem()?.info?.displayName?.english ?? "Unknown";
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Record victim death
            AddEntry(victim.userID, new KDEntry
            {
                Type = "Death",
                OtherPlayer = attacker.displayName,
                Weapon = weapon,
                Time = time
            });

            // Record attacker kill
            AddEntry(attacker.userID, new KDEntry
            {
                Type = "Kill",
                OtherPlayer = victim.displayName,
                Weapon = weapon,
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
                Image = { Color = "0 0 0 0.85" },
                RectTransform = { AnchorMin = "0.25 0.15", AnchorMax = "0.75 0.85" },
                CursorEnabled = true
            }, "Overlay", UIName);

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"Kill / Death History - {target.displayName}",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = "1 1" }
            }, UIName);

            container.Add(new CuiButton
            {
                Button = { Color = "0.8 0.2 0.2 1", Close = UIName },
                RectTransform = { AnchorMin = "0.92 0.94", AnchorMax = "0.98 0.99" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, UIName);

            float y = 0.85f;
            float step = 0.05f;

            if (History.ContainsKey(target.userID) && History[target.userID].Count > 0)
            {
                foreach (var entry in History[target.userID])
                {
                    if (y < 0.05f) break;

                    string color = entry.Type == "Kill"
                        ? "0.3 0.8 0.3 1"
                        : "0.8 0.3 0.3 1";

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = $"[{entry.Time}] {entry.Type} → {entry.OtherPlayer} ({entry.Weapon})",
                            FontSize = 12,
                            Align = TextAnchor.MiddleLeft,
                            Color = color
                        },
                        RectTransform =
                        {
                            AnchorMin = $"0.05 {y - step}",
                            AnchorMax = $"0.95 {y}"
                        }
                    }, UIName);

                    y -= step;
                }
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "No PVP kill/death history.",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform = { AnchorMin = "0 0.45", AnchorMax = "1 0.55" }
                }, UIName);
            }

            CuiHelper.AddUi(viewer, container);
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIName);
        }

        #endregion

        #region Helpers

        void AddEntry(ulong userId, KDEntry entry)
        {
            if (!History.ContainsKey(userId))
                History[userId] = new List<KDEntry>();

            History[userId].Insert(0, entry);

            if (History[userId].Count > MaxEntries)
                History[userId].RemoveAt(History[userId].Count - 1);
        }

        BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.UserIDString == nameOrId)
                    return player;

                if (player.displayName.IndexOf(nameOrId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return player;
            }
            return null;
        }

        #endregion

        #region Config & Data

        protected override void LoadDefaultConfig()
        {
            Config["Max History Entries"] = 20;
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
