using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes;

public class PacketPalette263 : PacketTypePalette
{
    private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.Bundle },                       // Bundle delimiter
            { 0x01, PacketTypesIn.SpawnEntity },                  // Add Entity
            { 0x02, PacketTypesIn.EntityAnimation },              // Animate
            { 0x03, PacketTypesIn.Statistics },                   // Award Stats
            { 0x04, PacketTypesIn.BlockChangedAck },              // Block Changed Ack
            { 0x05, PacketTypesIn.BlockBreakAnimation },          // Block Destruction
            { 0x06, PacketTypesIn.BlockEntityData },              // Block Entity Data
            { 0x07, PacketTypesIn.BlockAction },                  // Block Event
            { 0x08, PacketTypesIn.BlockChange },                  // Block Update
            { 0x09, PacketTypesIn.BossBar },                      // Boss Event
            { 0x0A, PacketTypesIn.ServerDifficulty },             // Change Difficulty
            { 0x0B, PacketTypesIn.ChunkBatchFinished },           // Chunk Batch Finished
            { 0x0C, PacketTypesIn.ChunkBatchStarted },            // Chunk Batch Start
            { 0x0D, PacketTypesIn.ChunksBiomes },                 // Chunks Biomes
            { 0x0E, PacketTypesIn.ClearTiles },                   // Clear Titles
            { 0x0F, PacketTypesIn.TabComplete },                  // Command Suggestions
            { 0x10, PacketTypesIn.DeclareCommands },              // Commands
            { 0x11, PacketTypesIn.CloseWindow },                  // Container Close
            { 0x12, PacketTypesIn.WindowItems },                  // Container Set Content
            { 0x13, PacketTypesIn.WindowProperty },               // Container Set Data
            { 0x14, PacketTypesIn.SetSlot },                      // Container Set Slot
            { 0x15, PacketTypesIn.CookieRequest },                // Cookie Request
            { 0x16, PacketTypesIn.SetCooldown },                  // Cooldown
            { 0x17, PacketTypesIn.ChatSuggestions },              // Custom Chat Completions
            { 0x18, PacketTypesIn.PluginMessage },                // Custom Payload
            { 0x19, PacketTypesIn.DamageEvent },                  // Damage Event
            { 0x1A, PacketTypesIn.DebugBlockValue },              // Debug Block Value
            { 0x1B, PacketTypesIn.DebugChunkValue },              // Debug Chunk Value
            { 0x1C, PacketTypesIn.DebugEntityValue },             // Debug Entity Value
            { 0x1D, PacketTypesIn.DebugEvent },                   // Debug Event
            { 0x1E, PacketTypesIn.DebugSample },                  // Debug Sample
            { 0x1F, PacketTypesIn.HideMessage },                  // Delete Chat
            { 0x20, PacketTypesIn.Disconnect },                   // Disconnect
            { 0x21, PacketTypesIn.ProfilelessChatMessage },       // Disguised Chat
            { 0x22, PacketTypesIn.EntityStatus },                 // Entity Event
            { 0x23, PacketTypesIn.EntityPositionSync },           // Entity Position Sync
            { 0x24, PacketTypesIn.Explosion },                    // Explode
            { 0x25, PacketTypesIn.AddTransientBlock },            // Add Transient Block (new in 26.3)
            { 0x26, PacketTypesIn.UnloadChunk },                  // Forget Level Chunk
            { 0x27, PacketTypesIn.ChangeGameState },              // Game Event
            { 0x28, PacketTypesIn.GameRuleValues },               // Game Rule Values
            { 0x29, PacketTypesIn.GameTestHighlightPos },         // Game Test Highlight Pos
            { 0x2A, PacketTypesIn.OpenHorseWindow },              // Mount Screen Open (renamed from Horse Screen Open)
            { 0x2B, PacketTypesIn.HurtAnimation },                // Hurt Animation
            { 0x2C, PacketTypesIn.InitializeWorldBorder },        // Initialize Border
            { 0x2D, PacketTypesIn.KeepAlive },                    // Keep Alive
            { 0x2E, PacketTypesIn.ChunkData },                    // Level Chunk With Light
            { 0x2F, PacketTypesIn.Effect },                       // Level Event
            { 0x30, PacketTypesIn.Particle },                     // Level Particles
            { 0x31, PacketTypesIn.UpdateLight },                  // Light Update
            { 0x32, PacketTypesIn.JoinGame },                     // Login
            { 0x33, PacketTypesIn.LowDiskSpaceWarning },          // Low Disk Space Warning
            { 0x34, PacketTypesIn.MapData },                      // Map Item Data
            { 0x35, PacketTypesIn.TradeList },                    // Merchant Offers
            { 0x36, PacketTypesIn.EntityPosition },               // Move Entity Pos
            { 0x37, PacketTypesIn.EntityPositionAndRotation },    // Move Entity Pos Rot
            { 0x38, PacketTypesIn.MoveMinecartAlongTrack },       // Move Minecart Along Track
            { 0x39, PacketTypesIn.EntityRotation },               // Move Entity Rot
            { 0x3A, PacketTypesIn.VehicleMove },                  // Move Vehicle
            { 0x3B, PacketTypesIn.OpenBook },                     // Open Book
            { 0x3C, PacketTypesIn.OpenWindow },                   // Open Screen
            { 0x3D, PacketTypesIn.OpenSignEditor },               // Open Sign Editor
            { 0x3E, PacketTypesIn.Ping },                         // Ping
            { 0x3F, PacketTypesIn.PingResponse },                 // Pong Response
            { 0x40, PacketTypesIn.CraftRecipeResponse },          // Place Ghost Recipe
            { 0x41, PacketTypesIn.PlayerAbilities },              // Player Abilities
            { 0x42, PacketTypesIn.ChatMessage },                  // Player Chat
            { 0x43, PacketTypesIn.EndCombatEvent },               // Player Combat End
            { 0x44, PacketTypesIn.EnterCombatEvent },             // Player Combat Enter
            { 0x45, PacketTypesIn.DeathCombatEvent },             // Player Combat Kill
            { 0x46, PacketTypesIn.PlayerRemove },                 // Player Info Remove
            { 0x47, PacketTypesIn.PlayerInfo },                   // Player Info Update
            { 0x48, PacketTypesIn.FacePlayer },                   // Player Look At
            { 0x49, PacketTypesIn.PlayerPositionAndLook },        // Player Position
            { 0x4A, PacketTypesIn.PlayerRotation },               // Player Rotation
            { 0x4B, PacketTypesIn.RecipeBookAdd },                // Recipe Book Add
            { 0x4C, PacketTypesIn.RecipeBookRemove },             // Recipe Book Remove
            { 0x4D, PacketTypesIn.RecipeBookSettings },           // Recipe Book Settings
            { 0x4E, PacketTypesIn.DestroyEntities },              // Remove Entities
            { 0x4F, PacketTypesIn.RemoveEntityEffect },           // Remove Mob Effect
            { 0x50, PacketTypesIn.ResetScore },                   // Reset Score
            { 0x51, PacketTypesIn.RemoveResourcePack },           // Resource Pack Pop
            { 0x52, PacketTypesIn.ResourcePackSend },             // Resource Pack Push
            { 0x53, PacketTypesIn.PostEffects },                  // Post Effects (new in 26.3)
            { 0x54, PacketTypesIn.Respawn },                      // Respawn
            { 0x55, PacketTypesIn.EntityHeadLook },               // Rotate Head
            { 0x56, PacketTypesIn.MultiBlockChange },             // Section Blocks Update
            { 0x57, PacketTypesIn.SelectAdvancementTab },         // Select Advancements Tab
            { 0x58, PacketTypesIn.ServerData },                   // Server Data
            { 0x59, PacketTypesIn.ActionBar },                    // Set Action Bar Text
            { 0x5A, PacketTypesIn.WorldBorderCenter },            // Set Border Center
            { 0x5B, PacketTypesIn.WorldBorderLerpSize },          // Set Border Lerp Size
            { 0x5C, PacketTypesIn.WorldBorderSize },              // Set Border Size
            { 0x5D, PacketTypesIn.WorldBorderWarningDelay },      // Set Border Warning Delay
            { 0x5E, PacketTypesIn.WorldBorderWarningReach },      // Set Border Warning Distance
            { 0x5F, PacketTypesIn.Camera },                       // Set Camera
            { 0x60, PacketTypesIn.UpdateViewPosition },           // Set Chunk Cache Center
            { 0x61, PacketTypesIn.UpdateViewDistance },           // Set Chunk Cache Radius
            { 0x62, PacketTypesIn.SetCursorItem },                // Set Cursor Item
            { 0x63, PacketTypesIn.SpawnPosition },                // Set Default Spawn Position
            { 0x64, PacketTypesIn.DisplayScoreboard },            // Set Display Objective
            { 0x65, PacketTypesIn.EntityMetadata },               // Set Entity Data
            { 0x66, PacketTypesIn.AttachEntity },                 // Set Entity Link
            { 0x67, PacketTypesIn.EntityVelocity },               // Set Entity Motion
            { 0x68, PacketTypesIn.EntityEquipment },              // Set Equipment
            { 0x69, PacketTypesIn.SetExperience },                // Set Experience
            { 0x6A, PacketTypesIn.UpdateHealth },                 // Set Health
            { 0x6B, PacketTypesIn.SetHeldSlot },                  // Set Held Slot
            { 0x6C, PacketTypesIn.ScoreboardObjective },          // Set Objective
            { 0x6D, PacketTypesIn.SetPassengers },                // Set Passengers
            { 0x6E, PacketTypesIn.SetPlayerInventory },           // Set Player Inventory
            { 0x6F, PacketTypesIn.Teams },                        // Set Player Team
            { 0x70, PacketTypesIn.UpdateScore },                  // Set Score
            { 0x71, PacketTypesIn.UpdateSimulationDistance },     // Set Simulation Distance
            { 0x72, PacketTypesIn.SetTitleSubTitle },             // Set Subtitle Text
            { 0x73, PacketTypesIn.TimeUpdate },                   // Set Time
            { 0x74, PacketTypesIn.SetTitleText },                 // Set Title Text
            { 0x75, PacketTypesIn.SetTitleTime },                 // Set Titles Animation
            { 0x76, PacketTypesIn.EntitySoundEffect },            // Sound Entity
            { 0x77, PacketTypesIn.SoundEffect },                  // Sound
            { 0x78, PacketTypesIn.StartConfiguration },           // Start Configuration
            { 0x79, PacketTypesIn.StopSound },                    // Stop Sound
            { 0x7A, PacketTypesIn.StoreCookie },                  // Store Cookie
            { 0x7B, PacketTypesIn.SwingAnimation },               // Swing Animation (new in 26.3)
            { 0x7C, PacketTypesIn.SystemChat },                   // System Chat
            { 0x7D, PacketTypesIn.PlayerListHeaderAndFooter },    // Tab List
            { 0x7E, PacketTypesIn.NBTQueryResponse },             // Tag Query
            { 0x7F, PacketTypesIn.CollectItem },                  // Take Item Entity
            { 0x80, PacketTypesIn.EntityTeleport },               // Teleport Entity
            { 0x81, PacketTypesIn.TestInstanceBlockStatus },      // Test Instance Block Status
            { 0x82, PacketTypesIn.SetTickingState },              // Ticking State
            { 0x83, PacketTypesIn.StepTick },                     // Ticking Step
            { 0x84, PacketTypesIn.Transfer },                     // Transfer
            { 0x85, PacketTypesIn.Advancements },                 // Update Advancements
            { 0x86, PacketTypesIn.EntityProperties },             // Update Attributes
            { 0x87, PacketTypesIn.EntityEffect },                 // Update Mob Effect
            { 0x88, PacketTypesIn.DeclareRecipes },               // Update Recipes
            { 0x89, PacketTypesIn.Tags },                         // Update Tags
            { 0x8A, PacketTypesIn.ProjectilePower },              // Projectile Power
            { 0x8B, PacketTypesIn.CustomReportDetails },          // Custom Report Details
            { 0x8C, PacketTypesIn.ServerLinks },                  // Server Links
            { 0x8D, PacketTypesIn.Waypoint },                     // Waypoint
            { 0x8E, PacketTypesIn.ClearDialog },                  // Clear Dialog
            { 0x8F, PacketTypesIn.ShowDialog }                    // Show Dialog
        };

    private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },               // Accept Teleportation
            { 0x01, PacketTypesOut.Attack },                        // Attack
            { 0x02, PacketTypesOut.QueryBlockNBT },                 // Block Entity Tag Query
            { 0x03, PacketTypesOut.BundleItemSelected },            // Bundle Item Selected
            { 0x04, PacketTypesOut.SetDifficulty },                 // Change Difficulty
            { 0x05, PacketTypesOut.ChangeGameMode },                // Change Game Mode
            { 0x06, PacketTypesOut.MessageAcknowledgment },         // Chat Ack
            { 0x07, PacketTypesOut.ChatCommand },                   // Chat Command
            { 0x08, PacketTypesOut.SignedChatCommand },             // Chat Command Signed
            { 0x09, PacketTypesOut.ChatMessage },                   // Chat
            { 0x0A, PacketTypesOut.PlayerSession },                 // Chat Session Update
            { 0x0B, PacketTypesOut.ChunkBatchReceived },            // Chunk Batch Received
            { 0x0C, PacketTypesOut.ClientStatus },                  // Client Command
            { 0x0D, PacketTypesOut.ClientTickEnd },                 // Client Tick End
            { 0x0E, PacketTypesOut.ClientSettings },                // Client Information
            { 0x0F, PacketTypesOut.TabComplete },                   // Command Suggestion
            { 0x10, PacketTypesOut.AcknowledgeConfiguration },      // Configuration Acknowledged
            { 0x11, PacketTypesOut.ClickWindowButton },             // Container Button Click
            { 0x12, PacketTypesOut.ClickWindow },                   // Container Click
            { 0x13, PacketTypesOut.CloseWindow },                   // Container Close
            { 0x14, PacketTypesOut.ChangeContainerSlotState },      // Container Slot State Changed
            { 0x15, PacketTypesOut.CookieResponse },                // Cookie Response
            { 0x16, PacketTypesOut.PluginMessage },                 // Custom Payload
            { 0x17, PacketTypesOut.DebugSampleSubscription },       // Debug Subscription Request (renamed)
            { 0x18, PacketTypesOut.EditBook },                      // Edit Book
            { 0x19, PacketTypesOut.EntityNBTRequest },              // Entity Tag Query
            { 0x1A, PacketTypesOut.InteractEntity },                // Interact
            { 0x1B, PacketTypesOut.GenerateStructure },             // Jigsaw Generate
            { 0x1C, PacketTypesOut.KeepAlive },                     // Keep Alive
            { 0x1D, PacketTypesOut.LockDifficulty },                // Lock Difficulty
            { 0x1E, PacketTypesOut.PlayerPosition },                // Move Player Pos
            { 0x1F, PacketTypesOut.PlayerPositionAndRotation },     // Move Player Pos Rot
            { 0x20, PacketTypesOut.PlayerRotation },                // Move Player Rot
            { 0x21, PacketTypesOut.PlayerMovement },                // Move Player Status Only
            { 0x22, PacketTypesOut.VehicleMove },                   // Move Vehicle
            { 0x23, PacketTypesOut.SteerBoat },                     // Paddle Boat
            { 0x24, PacketTypesOut.PickItem },                      // Pick Item From Block
            { 0x25, PacketTypesOut.PickItemFromEntity },            // Pick Item From Entity
            { 0x26, PacketTypesOut.PingRequest },                   // Ping Request
            { 0x27, PacketTypesOut.CraftRecipeRequest },            // Place Recipe
            { 0x28, PacketTypesOut.PlayerAbilities },               // Player Abilities
            { 0x29, PacketTypesOut.PlayerDigging },                 // Player Action
            { 0x2A, PacketTypesOut.EntityAction },                  // Player Command
            { 0x2B, PacketTypesOut.SteerVehicle },                  // Player Input
            { 0x2C, PacketTypesOut.PlayerLoaded },                  // Player Loaded
            { 0x2D, PacketTypesOut.Pong },                          // Pong
            { 0x2E, PacketTypesOut.Animation },                     // Punch (replaces Swing in 26.3)
            { 0x2F, PacketTypesOut.SetDisplayedRecipe },            // Recipe Book Change Settings
            { 0x30, PacketTypesOut.SetRecipeBookState },            // Recipe Book Seen Recipe
            { 0x31, PacketTypesOut.NameItem },                      // Rename Item
            { 0x32, PacketTypesOut.ResourcePackStatus },            // Resource Pack
            { 0x33, PacketTypesOut.AdvancementTab },                // Seen Advancements
            { 0x34, PacketTypesOut.SelectTrade },                   // Select Trade
            { 0x35, PacketTypesOut.SetBeaconEffect },               // Set Beacon
            { 0x36, PacketTypesOut.HeldItemChange },                // Set Carried Item
            { 0x37, PacketTypesOut.UpdateCommandBlock },            // Set Command Block
            { 0x38, PacketTypesOut.UpdateCommandBlockMinecart },    // Set Command Minecart
            { 0x39, PacketTypesOut.CreativeInventoryAction },       // Set Creative Mode Slot
            { 0x3A, PacketTypesOut.SetGameRule },                   // Set Game Rule
            { 0x3B, PacketTypesOut.UpdateJigsawBlock },             // Set Jigsaw Block
            { 0x3C, PacketTypesOut.UpdateStructureBlock },          // Set Structure Block
            { 0x3D, PacketTypesOut.SetTestBlock },                  // Set Test Block
            { 0x3E, PacketTypesOut.UpdateSign },                    // Sign Update
            { 0x40, PacketTypesOut.Spectate },                      // Teleport To Entity
            { 0x41, PacketTypesOut.TestInstanceBlockAction },       // Test Instance Block Action
            { 0x42, PacketTypesOut.PlayerBlockPlacement },          // Use Item On
            { 0x43, PacketTypesOut.UseItem },                       // Use Item
            { 0x44, PacketTypesOut.CustomClickAction }              // Custom Click Action
        };

    private readonly Dictionary<int, ConfigurationPacketTypesIn> configurationTypesIn = new()
        {
            { 0x00, ConfigurationPacketTypesIn.CookieRequest },
            { 0x01, ConfigurationPacketTypesIn.PluginMessage },
            { 0x02, ConfigurationPacketTypesIn.Disconnect },
            { 0x03, ConfigurationPacketTypesIn.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesIn.KeepAlive },
            { 0x05, ConfigurationPacketTypesIn.Ping },
            { 0x06, ConfigurationPacketTypesIn.ResetChat },
            { 0x07, ConfigurationPacketTypesIn.RegistryData },
            { 0x08, ConfigurationPacketTypesIn.RemoveResourcePack },
            { 0x09, ConfigurationPacketTypesIn.ResourcePack },
            { 0x0A, ConfigurationPacketTypesIn.PostEffects },
            { 0x0B, ConfigurationPacketTypesIn.StoreCookie },
            { 0x0C, ConfigurationPacketTypesIn.Transfer },
            { 0x0D, ConfigurationPacketTypesIn.FeatureFlags },
            { 0x0E, ConfigurationPacketTypesIn.UpdateTags },
            { 0x0F, ConfigurationPacketTypesIn.KnownDataPacks },
            { 0x10, ConfigurationPacketTypesIn.CustomReportDetails },
            { 0x11, ConfigurationPacketTypesIn.ServerLinks },
            { 0x12, ConfigurationPacketTypesIn.ClearDialog },
            { 0x13, ConfigurationPacketTypesIn.ShowDialog },
            { 0x14, ConfigurationPacketTypesIn.CodeOfConduct }
        };

    private readonly Dictionary<int, ConfigurationPacketTypesOut> configurationTypesOut = new()
        {
            { 0x00, ConfigurationPacketTypesOut.ClientInformation },
            { 0x01, ConfigurationPacketTypesOut.CookieResponse },
            { 0x02, ConfigurationPacketTypesOut.PluginMessage },
            { 0x03, ConfigurationPacketTypesOut.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesOut.KeepAlive },
            { 0x05, ConfigurationPacketTypesOut.Pong },
            { 0x06, ConfigurationPacketTypesOut.ResourcePackResponse },
            { 0x07, ConfigurationPacketTypesOut.KnownDataPacks },
            { 0x08, ConfigurationPacketTypesOut.CustomClickAction },
            { 0x09, ConfigurationPacketTypesOut.AcceptCodeOfConduct }
        };

    protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
    protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
    protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => configurationTypesIn!;
    protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => configurationTypesOut!;
}
