using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace GolfMayhem
{
    public static class GolfMayhemNetwork
    {
        private const string PREFIX = "##GOLFMAYHEM##";

        private static MethodInfo _serverShowInfoFeedMessage;

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            GolfMayhemPlugin.Log.LogInfo("[GolfMayhemNetwork] Initialized.");
        }

        public static void Shutdown()
        {
            IsInitialized = false;
        }

        public static void SendWarn(string eventName, string warnMessage)
            => Send($"{PREFIX}0|{eventName}|{warnMessage}");

        public static void SendActivate(string eventName, string activateMessage)
            => Send($"{PREFIX}1|{eventName}|{activateMessage}");

        public static void SendDeactivate(string eventName, string deactivateMessage)
            => Send($"{PREFIX}2|{eventName}|{deactivateMessage}");

        // Phase 3: environment settings broadcast (rain|nightTime as 0/1)
        public static void SendEnvironment(bool rain, bool nightTime)
            => Send($"{PREFIX}3|env|{(rain ? 1 : 0)}|{(nightTime ? 1 : 0)}");

        private static void Send(string encoded)
        {
            if (!NetworkServer.active) return;
            TextChatManager.SendChatMessage(encoded);
        }

        public static bool TryHandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || !message.StartsWith(PREFIX)) return false;

            string payload = message.Substring(PREFIX.Length);

            if (payload.StartsWith("3|"))
            {
                var envParts = payload.Split('|');
                if (envParts.Length >= 4)
                {
                    bool rain = envParts[2] == "1";
                    bool nightTime = envParts[3] == "1";
                    EnvironmentManager.Instance?.ApplyEnvironment(rain, nightTime);
                }
                return true;
            }

            var parts = payload.Split(new[] { '|' }, 3);
            if (parts.Length < 3) return true;

            string displayMessage = parts[2];

            if (!string.IsNullOrEmpty(displayMessage))
            {
                string colorizedName = GameManager.UiSettings.ApplyColorTag("GolfMayhem", TextHighlight.Regular);
                TextChatUi.ShowMessage($"{colorizedName}: {displayMessage}");
            }

            if (!NetworkServer.active)
            {
                byte phase = byte.Parse(parts[0]);
                string eventName = parts[1];
                var mgr = ChaosEventSystem.ChaosEventManager.Instance;
                if (mgr != null)
                {
                    var evt = mgr.GetEventByName(eventName);
                    if (evt != null)
                    {
                        switch (phase)
                        {
                            case 1: mgr.ActivateEventLocally(evt); break;
                            case 2: mgr.DeactivateEventLocally(evt); break;
                        }
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(TextChatManager), "UserCode_RpcMessage__String__PlayerInfo")]
    public static class Patch_TextChatManager_RpcMessage
    {
        [HarmonyPrefix]
        public static bool Prefix(string message, PlayerInfo sender)
        {
            return !GolfMayhemNetwork.TryHandleMessage(message);
        }
    }
}