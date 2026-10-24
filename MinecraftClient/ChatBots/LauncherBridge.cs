using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Bridges MCC to a GUI host (the Projects Launcher) that runs it as a child process.
    /// Session state is published as one JSON object per line on stdout, each prefixed with
    /// <see cref="Prefix"/> so the host can tell events apart from ordinary console output.
    /// Commands travel the other way as plain stdin lines, exactly like an interactive session.
    ///
    /// Activated by the <see cref="EnableEnvVar"/> environment variable rather than a TOML
    /// section: hosting is a property of how MCC was launched, not of the account's config.
    ///
    /// Text from a server cannot pass for an event: while hosted, every console write stays on one
    /// stdout line (<see cref="ConsoleIO.OneLinePerWrite"/>), and a host that sets
    /// <see cref="NonceEnvVar"/> only trusts lines that start with its nonce (docs/agent-bridge.md).
    /// </summary>
    public class LauncherBridge : ChatBot
    {
        public const string EnableEnvVar = "MCC_LAUNCHER_BRIDGE";

        /// <summary>
        /// Optional per-process secret from the host. When set, every event line starts with it, directly followed by
        /// <see cref="Prefix"/>: a line without it did not come from this bridge. Must match
        /// <c>^[A-Za-z0-9_-]{16,128}$</c>; any other value is ignored.
        /// </summary>
        public const string NonceEnvVar = "MCC_BRIDGE_NONCE";

        /// <summary>Line marker the host greps for. Anything else on stdout is human-readable log text.</summary>
        public const string Prefix = "@@MCC@@";

        /// <summary>What starts every event line of this process: the host's nonce (if any) and <see cref="Prefix"/>.</summary>
        internal static readonly string LinePrefix = BuildLinePrefix(Environment.GetEnvironmentVariable(NonceEnvVar));

        internal static string BuildLinePrefix(string? nonce) => IsValidNonce(nonce) ? nonce + Prefix : Prefix;

        internal static bool IsValidNonce(string? nonce)
        {
            if (nonce is null || nonce.Length is < 16 or > 128)
                return false;

            foreach (char c in nonce)
            {
                if (!(char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
                    return false;
            }

            return true;
        }

        /// <summary>Update() runs at 20 TPS; poll the tab list once per second instead.</summary>
        private const int StatusIntervalTicks = 20;

        private static readonly object writeLock = new();

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnableEnvVar));

        private int ticks;
        private float lastHealth = float.NaN;
        private int lastFood = -1;
        private int lastPlayerCount = -1;

        /// <summary>
        /// Writes a single event line. Safe to call from any thread and from static contexts
        /// (Program.cs uses it for failures that happen before a client exists). No-op when
        /// MCC was not started by a host.
        /// </summary>
        public static void Emit(string name, Dictionary<string, object?>? fields = null)
        {
            if (!Enabled)
                return;

            Dictionary<string, object?> payload = new(StringComparer.Ordinal) { ["e"] = name };
            if (fields is not null)
                foreach (KeyValuePair<string, object?> field in fields)
                    payload[field.Key] = field.Value;

            string line;
            try
            {
                // JSON escapes every line break, so the event is always exactly one stdout line.
                line = LinePrefix + JsonSerializer.Serialize(payload, jsonOptions);
            }
            catch (Exception)
            {
                return; // a status event is never worth taking the session down for
            }

            WriteEventLine(line);
        }

        /// <summary>
        /// Write one finished event line (<see cref="LinePrefix"/> and JSON) to stdout. Shared with the console kinds,
        /// so two events never interleave.
        /// </summary>
        internal static void WriteEventLine(string line)
        {
            lock (writeLock)
            {
                Console.Out.WriteLine(line);
                Console.Out.Flush();
            }
        }

        public override void Initialize()
        {
            Emit("connecting", new()
            {
                ["host"] = GetServerHost(),
                ["port"] = GetServerPort(),
            });
        }

        public override void AfterGameJoined()
        {
            Emit("joined", new()
            {
                ["user"] = GetUsername(),
                ["host"] = GetServerHost(),
                ["port"] = GetServerPort(),
                ["protocol"] = GetProtocolVersion(),
            });
        }

        public override void OnHealthUpdate(float health, int food)
        {
            if (health.Equals(lastHealth) && food == lastFood)
                return;

            lastHealth = health;
            lastFood = food;
            Emit("health", new()
            {
                ["hp"] = Math.Round(health, 1),
                ["food"] = food,
            });
        }

        public override void Update()
        {
            if (++ticks < StatusIntervalTicks)
                return;
            ticks = 0;

            int count = GetOnlinePlayers().Length;
            if (count == lastPlayerCount)
                return;

            lastPlayerCount = count;
            Emit("players", new() { ["n"] = count });
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            Emit("disconnected", new()
            {
                ["reason"] = reason.ToString(),
                ["msg"] = message,
            });

            return false; // reporting only: AutoRelog and MCC keep owning the reconnect decision
        }
    }
}
