using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Message
{
    public class ChatMessage
    {
        /// <summary>
        /// In 1.19 and above,  isSignedChat = true
        /// </summary>
        public bool isSignedChat;

        public string content;

        public bool isJson, isSenderJson;

        /// <summary>
        ///  0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar), 3: say command,
        ///  4: msg command,     5: team msg command,          6: emote command,            7: tellraw command
        /// </summary>
        public int chatTypeId;

        public Guid senderUUID;

        public bool isSystemChat;

        public string? unsignedContent;

        public string? displayName;

        public string? teamName;

        public DateTime? timestamp;

        public byte[]? signature;

        public bool? isSignatureLegal;

        /// <summary>
        /// Shown above the hotbar (action bar) rather than in the chat box.
        /// </summary>
        public bool isOverlay;

        /// <summary>
        /// 1.19.3+ chat sent without a player profile ("disguised" chat): a chat line decorated with a chat type
        /// and a sender name, but not a plain system message.
        /// </summary>
        public bool isProfileless;

        /// <summary>
        /// Click actions of the message's components, in document order. Only collected while
        /// <see cref="ChatClicks.Collecting"/> is set; null otherwise.
        /// </summary>
        public List<ChatClick>? clicks;

        internal ChatParser.ChatTypeDecoration? chatTypeDecoration;

        /// <summary>
        /// The text components as the server sent them (an NBT tree, or JSON text), for the console looks (client
        /// 4.2.4): the content of a system, profileless or action bar line, the unsigned content, the sender's name
        /// (only when <see cref="displayName"/> is its text) and the target. Kept only while
        /// <see cref="ChatBots.ConsoleKinds.Enabled"/>; null otherwise.
        /// </summary>
        internal object? rawContent, rawUnsigned, rawSender, rawTarget;

        public ChatMessage(string content, bool isJson, int chatType, Guid senderUUID, string? unsignedContent, string displayName, string? teamName, long timestamp, byte[]? signature, bool isSignatureLegal)
        {
            isSignedChat = true;
            isSystemChat = false;
            this.content = content;
            this.isJson = isJson;
            chatTypeId = chatType;
            this.senderUUID = senderUUID;
            this.unsignedContent = unsignedContent;
            this.displayName = displayName;
            this.teamName = teamName;
            this.timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            this.signature = signature;
            this.isSignatureLegal = isSignatureLegal;
        }

        public ChatMessage(string content, string? displayName, bool isJson, int chatType, Guid senderUUID, bool isSystemChat = false)
        {
            isSignedChat = isSystemChat;
            this.isSystemChat = isSystemChat;
            this.content = content;
            this.displayName = displayName;
            this.isJson = isJson;
            chatTypeId = chatType;
            this.senderUUID = senderUUID;
        }

        public LastSeenMessageList.AcknowledgedMessage? ToLastSeenMessageEntry()
        {
            return signature is not null ? new LastSeenMessageList.AcknowledgedMessage(senderUUID, signature, true) : null;
        }

        public bool LacksSender()
        {
            return senderUUID == Guid.Empty;
        }
    }
}
