using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_8;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_9;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

/// <summary>
/// 26.3 (protocol 777) data_component_type registry: 122 entries.
/// Order generated from the 26.3 server data report (registries.json).
/// </summary>
public class StructuredComponentsRegistry263 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        : base(dataTypes, itemPalette, subComponentRegistry)
    {
        RegisterComponent<CustomDataComponent>(0, "minecraft:custom_data");
        RegisterComponent<MaxStackSizeComponent>(1, "minecraft:max_stack_size");
        RegisterComponent<MaxDamageComponent>(2, "minecraft:max_damage");
        RegisterComponent<DamageComponent>(3, "minecraft:damage");
        RegisterComponent<EmptyComponent>(4, "minecraft:unbreakable");
        RegisterComponent<UseEffectsComponent>(5, "minecraft:use_effects");
        RegisterComponent<CustomNameComponent>(6, "minecraft:custom_name");
        RegisterComponent<PotionDurationScaleComponent>(7, "minecraft:minimum_attack_charge");
        RegisterComponent<VarIntComponent>(8, "minecraft:damage_type");
        RegisterComponent<ItemNameComponent>(9, "minecraft:item_name");
        RegisterComponent<ItemModelComponent>(10, "minecraft:item_model");
        RegisterComponent<LoreNameComponent1206>(11, "minecraft:lore");
        RegisterComponent<RarityComponent>(12, "minecraft:rarity");
        RegisterComponent<EnchantmentsComponent1215>(13, "minecraft:enchantments");
        RegisterComponent<CanPlaceOnComponent1215>(14, "minecraft:can_place_on");
        RegisterComponent<CanBreakComponent1215>(15, "minecraft:can_break");
        RegisterComponent<AttributeModifiersComponent1218>(16, "minecraft:attribute_modifiers");
        RegisterComponent<CustomModelDataComponent>(17, "minecraft:custom_model_data");
        RegisterComponent<TooltipDisplayComponent>(18, "minecraft:tooltip_display");
        RegisterComponent<RepairCostComponent>(19, "minecraft:repair_cost");
        RegisterComponent<CreativeSlotLockComponent>(20, "minecraft:creative_slot_lock");
        RegisterComponent<EnchantmentGlintOverrideComponent>(21, "minecraft:enchantment_glint_override");
        RegisterComponent<IntangibleProjectileComponent>(22, "minecraft:intangible_projectile");
        RegisterComponent<FoodComponent1212>(23, "minecraft:food");
        RegisterComponent<ConsumableComponent>(24, "minecraft:consumable");
        RegisterComponent<UseRemainderComponent261>(25, "minecraft:use_remainder");
        RegisterComponent<UseCooldownComponent>(26, "minecraft:use_cooldown");
        RegisterComponent<HolderSetComponent261>(27, "minecraft:damage_resistant");
        RegisterComponent<ToolComponent1215>(28, "minecraft:tool");
        RegisterComponent<WeaponComponent>(29, "minecraft:weapon");
        RegisterComponent<AttackRangeComponent>(30, "minecraft:attack_range");
        RegisterComponent<EnchantableComponent>(31, "minecraft:enchantable");
        RegisterComponent<EquippableComponent1218>(32, "minecraft:equippable");
        RegisterComponent<RepairableComponent>(33, "minecraft:repairable");
        RegisterComponent<GliderComponent>(34, "minecraft:glider");
        RegisterComponent<TooltipStyleComponent>(35, "minecraft:tooltip_style");
        RegisterComponent<DeathProtectionComponent>(36, "minecraft:death_protection");
        RegisterComponent<BlocksAttacksComponent>(37, "minecraft:blocks_attacks");
        RegisterComponent<PiercingWeaponComponent>(38, "minecraft:piercing_weapon");
        RegisterComponent<KineticWeaponComponent>(39, "minecraft:kinetic_weapon");
        RegisterComponent<SwingAnimationComponent>(40, "minecraft:attack_animation");           // New in 26.3 (replaces swing_animation)
        RegisterComponent<SwingAnimationComponent>(41, "minecraft:interact_animation");         // New in 26.3
        RegisterComponent<VarIntComponent>(42, "minecraft:additional_trade_cost");
        RegisterComponent<VarIntComponent>(43, "minecraft:block_transformer");                  // New in 26.3 (holder id)
        RegisterComponent<VarIntComponent>(44, "minecraft:villager_food");                      // New in 26.3
        RegisterComponent<StoredEnchantmentsComponent1215>(45, "minecraft:stored_enchantments");
        RegisterComponent<VarIntComponent>(46, "minecraft:dye");
        RegisterComponent<DyeColorComponent1215>(47, "minecraft:dyed_color");
        // minecraft:map_color removed in 26.3
        RegisterComponent<MapIdComponent>(48, "minecraft:map_id");
        RegisterComponent<MapDecorationsComponent>(49, "minecraft:map_decorations");
        RegisterComponent<MapPostProcessingComponent>(50, "minecraft:map_post_processing");
        RegisterComponent<ItemStackTemplateListComponent261>(51, "minecraft:charged_projectiles");
        RegisterComponent<ItemStackTemplateListComponent261>(52, "minecraft:bundle_contents");
        RegisterComponent<PotionContentsComponent1212>(53, "minecraft:potion_contents");
        RegisterComponent<PotionDurationScaleComponent>(54, "minecraft:potion_duration_scale");
        RegisterComponent<SuspiciousStewEffectsComponent>(55, "minecraft:suspicious_stew_effects");
        RegisterComponent<WritableBookContentComponent>(56, "minecraft:writable_book_content");
        RegisterComponent<WrittenBookContentComponent>(57, "minecraft:written_book_content");
        RegisterComponent<TrimComponent263>(58, "minecraft:trim");                              // 26.3: inline material = palette_id + description
        RegisterComponent<DebugStickStateComponent>(59, "minecraft:debug_stick_state");
        RegisterComponent<TypedEntityDataComponent>(60, "minecraft:entity_data");
        RegisterComponent<BucketEntityDataComponent>(61, "minecraft:bucket_entity_data");
        RegisterComponent<TypedBlockEntityDataComponent>(62, "minecraft:block_entity_data");
        RegisterComponent<InstrumentComponent263>(63, "minecraft:instrument");                  // 26.3: + durability_damage
        RegisterComponent<ProvidesTrimMaterialComponent263>(64, "minecraft:provides_trim_material"); // 26.3: inline material = palette_id + description
        RegisterComponent<OminousBottleAmplifierComponent>(65, "minecraft:ominous_bottle_amplifier");
        RegisterComponent<JukeBoxPlayableComponent1215>(66, "minecraft:jukebox_playable");
        RegisterComponent<HolderSetComponent261>(67, "minecraft:provides_banner_patterns");
        RegisterComponent<NbtTagComponent261>(68, "minecraft:recipes");
        RegisterComponent<LodestoneTrackerComponent>(69, "minecraft:lodestone_tracker");
        RegisterComponent<FireworkExplosionComponent>(70, "minecraft:firework_explosion");
        RegisterComponent<FireworksComponent>(71, "minecraft:fireworks");
        RegisterComponent<ProfileComponent>(72, "minecraft:profile");
        RegisterComponent<NoteBlockSoundComponent>(73, "minecraft:note_block_sound");
        RegisterComponent<BannerPatternsComponent>(74, "minecraft:banner_patterns");
        RegisterComponent<BaseColorComponent>(75, "minecraft:base_color");
        RegisterComponent<PotDecorationsComponent263>(76, "minecraft:pot_decorations");         // 26.3: 4 x Optional<ItemStackTemplate>
        RegisterComponent<ContainerComponent261>(77, "minecraft:container");
        RegisterComponent<BlockStateComponent>(78, "minecraft:block_state");
        RegisterComponent<BeesComponent1219>(79, "minecraft:bees");
        RegisterComponent<SulfurCubeContentComponent262>(80, "minecraft:sulfur_cube_content");
        RegisterComponent<LockComponent>(81, "minecraft:lock");
        RegisterComponent<ContainerLootComponent>(82, "minecraft:container_loot");

        RegisterComponent<SoundEventHolderComponent>(83, "minecraft:break_sound");
        RegisterComponent<CompostableComponent263>(84, "minecraft:compostable");                // New in 26.3
        RegisterComponent<FuelComponent263>(85, "minecraft:cooking_fuel");                      // New in 26.3
        RegisterComponent<FuelComponent263>(86, "minecraft:brewing_fuel");                      // New in 26.3
        RegisterComponent<MobVisibilityComponent263>(87, "minecraft:mob_visibility");           // New in 26.3
        RegisterComponent<VarIntComponent>(88, "minecraft:villager/variant");
        RegisterComponent<VarIntComponent>(89, "minecraft:wolf/variant");
        RegisterComponent<VarIntComponent>(90, "minecraft:wolf/sound_variant");
        RegisterComponent<VarIntComponent>(91, "minecraft:wolf/collar");
        RegisterComponent<VarIntComponent>(92, "minecraft:fox/variant");
        RegisterComponent<VarIntComponent>(93, "minecraft:salmon/size");
        RegisterComponent<VarIntComponent>(94, "minecraft:parrot/variant");
        RegisterComponent<VarIntComponent>(95, "minecraft:tropical_fish/pattern");
        RegisterComponent<VarIntComponent>(96, "minecraft:tropical_fish/base_color");
        RegisterComponent<VarIntComponent>(97, "minecraft:tropical_fish/pattern_color");
        RegisterComponent<VarIntComponent>(98, "minecraft:mooshroom/variant");
        RegisterComponent<VarIntComponent>(99, "minecraft:rabbit/variant");
        RegisterComponent<VarIntComponent>(100, "minecraft:pig/variant");
        RegisterComponent<VarIntComponent>(101, "minecraft:pig/sound_variant");
        RegisterComponent<VarIntComponent>(102, "minecraft:cow/variant");
        RegisterComponent<VarIntComponent>(103, "minecraft:cow/sound_variant");
        RegisterComponent<VarIntComponent>(104, "minecraft:chicken/variant");
        RegisterComponent<VarIntComponent>(105, "minecraft:chicken/sound_variant");
        RegisterComponent<VarIntComponent>(106, "minecraft:zombie_nautilus/variant");
        RegisterComponent<VarIntComponent>(107, "minecraft:frog/variant");
        RegisterComponent<VarIntComponent>(108, "minecraft:horse/variant");
        RegisterComponent<PaintingVariantHolderComponent>(109, "minecraft:painting/variant");
        RegisterComponent<VarIntComponent>(110, "minecraft:llama/variant");
        RegisterComponent<VarIntComponent>(111, "minecraft:axolotl/variant");
        RegisterComponent<VarIntComponent>(112, "minecraft:cat/variant");
        RegisterComponent<VarIntComponent>(113, "minecraft:cat/sound_variant");
        RegisterComponent<VarIntComponent>(114, "minecraft:cat/collar");
        RegisterComponent<VarIntComponent>(115, "minecraft:sheep/color");
        RegisterComponent<VarIntComponent>(116, "minecraft:shulker/color");
        RegisterComponent<VarIntComponent>(117, "minecraft:provides_pottery_pattern");          // New in 26.3 (holder id)
        RegisterComponent<SignTextComponent263>(118, "minecraft:sign_text_front");              // New in 26.3
        RegisterComponent<SignTextComponent263>(119, "minecraft:sign_text_back");               // New in 26.3
        RegisterComponent<EmptyComponent>(120, "minecraft:waxed");                              // New in 26.3 (unit)
        RegisterComponent<VarIntComponent>(121, "minecraft:cushion/color");                     // New in 26.3 (DyeColor)
    }
}
