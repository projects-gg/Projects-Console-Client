using System.Collections.Generic;
using System.Threading;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// The keys the server is told about, sent as the game sends them. From 1.21.2 the movement keys of the tick's move
    /// and the sneak key go as Player Input when they change, at most once a client tick and before the tick's move
    /// (client 4.2.8); before 1.21.6 the sneak key is also an entity action (Player Command), and the sprint key is one in
    /// every version. An entity action goes only when it changes what the server knows (the game sends one on a change
    /// only; Grim BadPacketsG flags a second "start sneaking" before 1.21.2).
    /// <para>
    /// A screen (client 4.2.9): opening one, a window the server opened or the player's own inventory, releases every key
    /// (the game's KeyMapping.releaseAll), so the next Player Input has none down and sneaking and sprinting stop, and
    /// the keys stay up until it closes; then the keys the client holds go down again. Anticheats flag a window click or
    /// close made with a movement, sneak or sprint key down (Grim MultiActionsC cancels the click, MultiActionsD flags the
    /// close; production: an AFK account's trade clicks while AntiAFK held its sneak key, each one cancelled). So a
    /// click or a close waits until the server knows every key is up: at once when this tick's Player Input has not
    /// gone yet, else right after the next tick's, which has none down.
    /// </para>
    /// </summary>
    internal sealed class PlayerKeys(int protocolVersion, PlayerKeys.Send send)
    {
        /// <summary>Send one play packet now; false when it did not go (not in play, the connection closed).</summary>
        internal delegate bool Send(PacketTypesOut packet, byte[] data);

        private readonly Lock keysLock = new();
        private readonly Queue<(PacketTypesOut Packet, byte[] Data)> waiting = new(); // window packets waiting for the keys to be up
        private byte movementKeys; // the keys of this tick's move
        private bool sneakKey, sprintKey; // the keys the entity actions pressed
        private bool screenOpen;
        private byte sentInput; // the last Player Input sent; the game's player starts with no key down
        private bool inputSentThisTick;
        private bool sentSneak, sentSprint; // what the last entity actions told the server
        private int entityId;

        /// <summary>The keys of this tick's move (a Player Input byte), given right before the move.</summary>
        public void SetMovementKeys(byte keys)
        {
            lock (keysLock)
            {
                movementKeys = keys;
                SendKeys();
            }
        }

        /// <summary>
        /// An entity action: the sneak and sprint keys are kept and sent when they change what the server knows (the sneak
        /// key's Player Input goes with the tick's move, as before); any other action goes at once.
        /// </summary>
        public bool EntityAction(int playerEntityId, int actionId)
        {
            lock (keysLock)
            {
                entityId = playerEntityId;
                switch ((EntityActionType)actionId)
                {
                    case EntityActionType.StartSneaking:
                    case EntityActionType.StopSneaking:
                        sneakKey = actionId == (int)EntityActionType.StartSneaking;
                        break;
                    case EntityActionType.StartSprinting:
                    case EntityActionType.StopSprinting:
                        sprintKey = actionId == (int)EntityActionType.StartSprinting;
                        break;
                    default:
                        return send(PacketTypesOut.EntityAction, EntityActionPacket(actionId));
                }

                SendActions();
                return true;
            }
        }

        /// <summary>
        /// A screen opened or closed. Opening one releases every key at once (the Player Input too, unless this tick's
        /// went), before any click; after it closes the held keys go down again with the client's next tick, as the game
        /// reads them again (a server that closes a menu and opens the next one at once sees no key go down between).
        /// </summary>
        public void SetScreenOpen(bool open)
        {
            lock (keysLock)
            {
                if (screenOpen == open)
                    return;

                screenOpen = open;
                if (open)
                    SendKeys();
            }
        }

        /// <summary>A window click or close: it goes once the server knows every key is up (at once, or next tick).</summary>
        public bool WindowPacket(PacketTypesOut packet, byte[] data)
        {
            lock (keysLock)
            {
                waiting.Enqueue((packet, data));
                SendKeys();
                return true;
            }
        }

        /// <summary>A client tick starts: the window packets waiting since the last tick go after its released input.</summary>
        public void StartTick()
        {
            lock (keysLock)
            {
                if (waiting.Count > 0)
                    SendKeys();
            }
        }

        /// <summary>After the tick's update: the keys a bot changed after the tick's move, or a client without moves.</summary>
        public void Flush()
        {
            lock (keysLock)
                SendKeys();
        }

        /// <summary>The client tick ended (Client Tick End): the next tick may send its Player Input.</summary>
        public void EndTick()
        {
            lock (keysLock)
                inputSentThisTick = false;
        }

        /// <summary>
        /// A join or a respawn: the game's new player has sent no key yet, so the held keys go again, and no screen is open
        /// (the game shows its world loading screen); window packets still waiting are for a window that is gone.
        /// </summary>
        public void NewPlayer()
        {
            lock (keysLock)
            {
                sentInput = 0;
                sentSneak = sentSprint = false;
                screenOpen = false;
                waiting.Clear();
            }
        }

        /// <summary>Every key is up for the server: what a window click or close needs.</summary>
        private bool KeysUp => !sentSprint && !sentSneak && (protocolVersion < Protocol18Handler.MC_1_21_2_Version || sentInput == 0);

        /// <summary>Keys stay up while a screen is open, and while a window packet waits for them.</summary>
        private bool Held => screenOpen || waiting.Count > 0;

        /// <summary>
        /// The Player Input, then the entity actions, as the game sends them in its tick (1.21.2+: Player Input before
        /// the sprint and shift commands), then the window packets that waited for every key to be up.
        /// </summary>
        private void SendKeys()
        {
            SendInput();
            SendActions();
            SendWaiting();
        }

        /// <summary>The sprint and (before 1.21.6) sneak entity actions the keys need; sprinting first, as the game sends them.</summary>
        private void SendActions()
        {
            bool held = Held;
            bool sprint = sprintKey && !held;
            if (sprint != sentSprint && send(PacketTypesOut.EntityAction,
                    EntityActionPacket((int)(sprint ? EntityActionType.StartSprinting : EntityActionType.StopSprinting))))
                sentSprint = sprint;

            // From 1.21.6 sneaking is the Player Input's sneak key alone
            bool sneak = sneakKey && !held;
            if (protocolVersion < Protocol18Handler.MC_1_21_6_Version && sneak != sentSneak
                && send(PacketTypesOut.EntityAction,
                    EntityActionPacket((int)(sneak ? EntityActionType.StartSneaking : EntityActionType.StopSneaking))))
                sentSneak = sneak;
        }

        /// <summary>
        /// The Player Input (1.21.2+) when the keys changed since the last one, at most once a client tick (as the game
        /// does; Grim BadPacketsZ flags a second one before the tick's end): a change after this tick's input waits for the
        /// next tick, which sends it before its move.
        /// </summary>
        private void SendInput()
        {
            if (protocolVersion < Protocol18Handler.MC_1_21_2_Version || inputSentThisTick)
                return;

            byte keys = Held ? (byte)0 : (byte)(movementKeys | (sneakKey ? Protocol18Handler.PlayerInputSneak : 0));
            if (keys == sentInput || !send(PacketTypesOut.SteerVehicle, [keys]))
                return;

            sentInput = keys;
            inputSentThisTick = true;
        }

        /// <summary>
        /// The window packets waiting, once the server knows every key is up. Without a screen open the held keys go down
        /// again with the next tick.
        /// </summary>
        private void SendWaiting()
        {
            if (waiting.Count == 0 || !KeysUp)
                return;

            while (waiting.TryDequeue(out var packet))
                send(packet.Packet, packet.Data);
        }

        /// <summary>A Player Command for this entity action, in the form this protocol version reads.</summary>
        private byte[] EntityActionPacket(int actionId)
        {
            List<byte> fields = new();
            fields.AddRange(DataTypes.GetVarInt(entityId));
            fields.AddRange(DataTypes.GetVarInt(Protocol18Handler.PlayerCommandAction(protocolVersion, actionId, out _) ?? actionId));
            fields.AddRange(DataTypes.GetVarInt(0));
            return fields.ToArray();
        }
    }
}
