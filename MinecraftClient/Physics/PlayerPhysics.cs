using System;
using MinecraftClient.Mapping;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Core physics tick engine for the player, faithfully replicating vanilla 1.21.11 physics.
    /// Mirrors the combined logic of Entity.move(), LivingEntity.aiStep()/travel()/travelInAir(),
    /// Player.travel(), and LocalPlayer.aiStep().
    /// </summary>
    public class PlayerPhysics
    {
        // --- State ---
        public Vec3d Position;
        public Vec3d DeltaMovement;
        public float Yaw;
        public float Pitch;
        public bool OnGround;
        public bool HorizontalCollision;
        public bool VerticalCollision;
        public bool VerticalCollisionBelow;
        public double FallDistance;
        public Vec3d StuckSpeedMultiplier = Vec3d.Zero;

        // Movement input
        public float Xxa; // strafe
        public float Zza; // forward
        public float Yya; // vertical (creative fly)
        public bool Jumping;

        // Movement mode flags
        /// <summary>Sprinting (LocalPlayer.isSprinting): the sprint key starts it when the player can sprint, as in the game.</summary>
        public bool Sprinting;
        /// <summary>The sneak key, as the game reads it this tick (isShiftKeyDown).</summary>
        public bool Sneaking;
        /// <summary>The slowdown of a crouching player (LocalPlayer.crouching), worked out before the tick reads its keys.</summary>
        public bool Crouching;
        /// <summary>The crouching pose (Pose.CROUCHING): a box 1.5 high instead of 1.8, from the tick after the sneak key.</summary>
        public bool CrouchPose;
        public bool CreativeFlying;
        public bool InWater;
        public bool InLava;
        public bool OnClimbable;
        public bool HasSlowFalling;
        public bool HasLevitation;
        public int LevitationAmplifier;
        /// <summary>The last move hit a wall at a small angle (Entity.minorHorizontalCollision): sprinting goes on.</summary>
        public bool MinorHorizontalCollision;

        /// <summary>
        /// 1.21.5+: the keys go as a normalized vector and a diagonal one is stretched to the unit square
        /// (LocalPlayer.modifyInputSpeedForSquareMovement); before, the keys go as they are.
        /// </summary>
        public bool SquareMovementInput = true;

        // What sprinting needs (LocalPlayer.isSprintingPossible): food above 6 or flight allowed, and no blindness
        public int FoodLevel = 20;
        public bool MayFly;
        public bool Blind;

        // Player dimensions
        public double PlayerWidth = PhysicsConsts.PlayerWidth;
        public double PlayerHeight = PhysicsConsts.PlayerHeight;

        // Anti-jump-spam
        private int noJumpDelay;

        // Whether the standing and the crouching box fit where the player stands, as the tick starts
        private bool canStand = true, canCrouch = true;

        // Tick counter for position packet timing
        public int TickCount;

        /// <summary>The player's movement speed (Attributes.MOVEMENT_SPEED: base 0.1F, the server's modifiers, sprinting).</summary>
        public readonly PlayerAttribute MovementSpeedAttribute = new(0.1f, 0.0, 1024.0);

        /// <summary>The crouching player's speed factor (Attributes.SNEAKING_SPEED: 0.3, more with Swift Sneak).</summary>
        public readonly PlayerAttribute SneakingSpeedAttribute = new(0.3, 0.0, 1.0);

        /// <summary>LivingEntity.SPEED_MODIFIER_SPRINTING: 30% on the total while sprinting.</summary>
        internal static readonly PlayerAttribute.Modifier SprintingModifier = new("minecraft:sprinting", 0.3f, 2);

        /// <summary>Player.getSpeed: the movement speed attribute as a float.</summary>
        public float MovementSpeed => (float)MovementSpeedAttribute.Value;

        /// <summary>
        /// Get the player's bounding box at current position
        /// </summary>
        public Aabb GetBoundingBox()
        {
            return BoxOf(CrouchPose);
        }

        private Aabb BoxOf(bool crouching)
        {
            return Aabb.OfSize(Position.X, Position.Y, Position.Z, PlayerWidth,
                crouching ? PhysicsConsts.PlayerSneakHeight : PlayerHeight);
        }

        /// <summary>
        /// Run one physics tick. Call at 20 TPS.
        /// </summary>
        public void Tick(World world)
        {
            TickCount++;

            // Velocity threshold zeroing (LivingEntity.aiStep)
            ZeroTinyVelocity();

            // Jump handling
            HandleJumping(world);

            // Build travel input
            Vec3d travelInput = new(Xxa, Yya, Zza);

            // Travel (dispatches to air/water/lava/fly)
            Travel(world, travelInput);

            if (noJumpDelay > 0)
                noJumpDelay--;

            UpdatePose(world);
        }

        /// <summary>
        /// Player.updatePlayerPose, at the end of the game's tick: crouching while the sneak key is down (not flying),
        /// standing otherwise; a pose whose box does not fit where the player stands gives way to the crouching one (the
        /// game's swimming pose, for a gap lower than 1.5, is not modelled: the pose stays as it was). Client 4.2.11:
        /// before, the box was 1.8 high in every pose, so a crouching player stopped under a ceiling the game walks under.
        /// </summary>
        private void UpdatePose(World world)
        {
            bool desired = Sneaking && !CreativeFlying;
            if (CanFit(world, desired))
                CrouchPose = desired;
            else if (CanFit(world, true))
                CrouchPose = true;
        }

        /// <summary>
        /// LocalPlayer.aiStep's sprinting: the sprint key starts it when the player could sprint (moving forward, not
        /// crouching, food above 6 or flight allowed, not blind, not in shallow water unless flying), and it stops when
        /// it no longer could, without the forward key, or at a wall hit straight on. Releasing the key alone does not
        /// stop it, as in the game. Client 4.2.11: before, the key was taken for sprinting and the walk never got
        /// faster, so a client holding the key (MovementSpeed 5) or told to sprint (MCP) went at walking speed.
        /// </summary>
        private void UpdateSprinting(bool sprintKey, bool forwardImpulse)
        {
            bool possible = !Blind && (FoodLevel > 6 || MayFly) && (CreativeFlying || !InWater);
            if (!Sprinting && forwardImpulse && possible && !Crouching && sprintKey)
                SetSprinting(true);

            if (Sprinting && (!possible || !forwardImpulse || (HorizontalCollision && !MinorHorizontalCollision)))
                SetSprinting(false);
        }

        /// <summary>LivingEntity.setSprinting: the state and the sprint modifier of the movement speed.</summary>
        public void SetSprinting(bool sprinting)
        {
            Sprinting = sprinting;
            MovementSpeedAttribute.RemoveModifier(SprintingModifier.Id);
            if (sprinting)
                MovementSpeedAttribute.SetModifier(SprintingModifier);
        }

        /// <summary>
        /// The walk's input from the movement keys (each -1, 0 or 1), as the game makes it: 1.21.5+ a normalized vector
        /// (KeyboardInput), then LocalPlayer.modifyInput (0.98F, the sneaking speed while crouching, then stretched so a
        /// diagonal reaches the unit square); before 1.21.5 the keys as they are, 0.98F and the sneaking speed.
        /// Client 4.2.11: before, a diagonal was normalized first and went 2% slower than the game's (sneaking, 30%).
        /// </summary>
        internal static (float X, float Z) ModifyInput(bool square, float left, float forward, bool movingSlowly,
            float sneakingSpeed)
        {
            if (!square)
            {
                left *= PhysicsConsts.InputFriction;
                forward *= PhysicsConsts.InputFriction;
                if (movingSlowly)
                {
                    left *= sneakingSpeed;
                    forward *= sneakingSpeed;
                }
                return (left, forward);
            }

            float distance = MathF.Sqrt(left * left + forward * forward);
            if (distance < 1.0E-4f)
                return (0f, 0f);
            float x = left / distance, z = forward / distance;

            x *= PhysicsConsts.InputFriction;
            z *= PhysicsConsts.InputFriction;
            if (movingSlowly)
            {
                x *= sneakingSpeed;
                z *= sneakingSpeed;
            }

            float length = MathF.Sqrt(x * x + z * z);
            if (length <= 0.0f)
                return (x, z);
            float scale = 1.0f / length;
            float dirX = x * scale, dirZ = z * scale;
            float absX = Math.Abs(dirX), absZ = Math.Abs(dirZ);
            float tan = absZ > absX ? absX / absZ : absZ / absX;
            float toUnitSquare = MathF.Sqrt(1.0f + tan * tan);
            float stretched = Math.Min(length * toUnitSquare, 1.0f);
            return (dirX * stretched, dirZ * stretched);
        }

        /// <summary>
        /// LocalPlayer.isHorizontalCollisionMinor: the wall stopped a move less than 8° off the way the keys push
        /// (sprinting goes on along it).
        /// </summary>
        internal static bool IsHorizontalCollisionMinor(float yaw, float xxa, float zza, Vec3d movement)
        {
            float yawRad = VanillaMath.ToRadians(yaw);
            double sin = VanillaMath.Sin(yawRad);
            double cos = VanillaMath.Cos(yawRad);
            double globalX = xxa * cos - zza * sin;
            double globalZ = zza * cos + xxa * sin;
            double inputLengthSqr = globalX * globalX + globalZ * globalZ;
            double movementLengthSqr = movement.X * movement.X + movement.Z * movement.Z;
            if (inputLengthSqr < 1.0E-5f || movementLengthSqr < 1.0E-5f)
                return false;
            double dot = globalX * movement.X + globalZ * movement.Z;
            return Math.Acos(dot / Math.Sqrt(inputLengthSqr * movementLengthSqr)) < 0.13962634f;
        }

        /// <summary>Player.canPlayerFitWithinBlocksAndEntitiesWhen: the pose's box, pulled in by 1.0E-7, hits no block.</summary>
        private bool CanFit(World world, bool crouching)
        {
            return CollisionDetector.NoCollision(world, BoxOf(crouching).Deflate(1.0E-7, 1.0E-7, 1.0E-7));
        }

        /// <summary>
        /// Apply the movement input from MovementInput to xxa/zza.
        /// Call before Tick() each frame.
        /// </summary>
        public void ApplyInput(MovementInput input)
        {
            // LocalPlayer.aiStep works out whether the player crouches before it reads this tick's keys, so the sneak
            // slowdown follows the sneak key a tick late, both ways, while the edge back-off follows it at once
            // (isStayingOnGroundSurface reads the new key); a player who cannot stand up crouches without the key.
            // Client 4.2.11: before, the slowdown came and went with the key, so a walk started in the tick the sneak
            // key went up (AntiAFK) went a tick too fast for an anticheat.
            Crouching = !CreativeFlying && canCrouch && (Sneaking || !canStand);

            var (left, forward) = input.GetMoveImpulse();
            Jumping = input.Jump;
            Sneaking = input.Sneak;
            UpdateSprinting(input.Sprint, forward > 1.0E-5f);

            (Xxa, Zza) = ModifyInput(SquareMovementInput, left, forward, Crouching, (float)SneakingSpeedAttribute.Value);
            Yya = 0;

            // Creative/spectator fly vertical
            if (CreativeFlying)
            {
                if (input.Jump)
                    Yya += (float)(PhysicsConsts.DefaultFlySpeed * PhysicsConsts.FlyVerticalBoostScale);
                if (input.Sneak)
                    Yya -= (float)(PhysicsConsts.DefaultFlySpeed * PhysicsConsts.FlyVerticalBoostScale);
            }
        }

        private void ZeroTinyVelocity()
        {
            double dx = DeltaMovement.X;
            double dy = DeltaMovement.Y;
            double dz = DeltaMovement.Z;

            // Player-specific: zero horizontal if combined length < 0.003
            if (dx * dx + dz * dz < PhysicsConsts.PlayerHorizontalVelocityThresholdSqr)
            {
                dx = 0;
                dz = 0;
            }
            if (Math.Abs(dy) < PhysicsConsts.VerticalVelocityThreshold)
                dy = 0;

            DeltaMovement = new Vec3d(dx, dy, dz);
        }

        private void HandleJumping(World world)
        {
            if (!Jumping) { noJumpDelay = 0; return; }

            if (InWater || InLava)
            {
                // Jump in fluid: add upward impulse
                DeltaMovement = DeltaMovement.Add(0, PhysicsConsts.WaterFloatImpulse, 0);
            }
            else if (OnGround && noJumpDelay == 0)
            {
                JumpFromGround();
                noJumpDelay = 10;
            }
        }

        private void JumpFromGround()
        {
            float jumpPower = PhysicsConsts.BaseJumpPower;
            if (jumpPower <= 1.0E-5f) return;

            DeltaMovement = new Vec3d(
                DeltaMovement.X,
                Math.Max(jumpPower, DeltaMovement.Y),
                DeltaMovement.Z);

            if (Sprinting)
            {
                float yawRad = VanillaMath.ToRadians(Yaw);
                DeltaMovement = DeltaMovement.Add(
                    -VanillaMath.Sin(yawRad) * PhysicsConsts.SprintJumpHorizontalBoost,
                    0,
                    VanillaMath.Cos(yawRad) * PhysicsConsts.SprintJumpHorizontalBoost);
            }
        }

        private void Travel(World world, Vec3d input)
        {
            if (InWater && !CreativeFlying)
            {
                TravelInWater(world, input);
            }
            else if (InLava && !CreativeFlying)
            {
                TravelInLava(world, input);
            }
            else
            {
                TravelInAir(world, input);
            }
        }

        /// <summary>
        /// Ground/air travel — LivingEntity.travelInAir(Vec3)
        /// </summary>
        private void TravelInAir(World world, Vec3d input)
        {
            // Get block friction at feet
            float blockFriction = OnGround ? GetBlockFriction(world) : 1.0f;
            float f = blockFriction * PhysicsConsts.FrictionMultiplier;

            // Apply input → velocity (handleRelativeFrictionAndCalculateMovement)
            float speed = GetFrictionInfluencedSpeed(blockFriction);
            MoveRelative(speed, input);

            // Handle climbable
            HandleOnClimbable();

            // Execute collision
            Move(world, DeltaMovement);

            Vec3d postMoveVel = DeltaMovement;
            double vy = postMoveVel.Y;

            // Climbing wall bump
            if ((HorizontalCollision || Jumping) && OnClimbable)
            {
                vy = PhysicsConsts.ClimbWallBump;
            }

            // Apply gravity
            if (HasLevitation)
            {
                vy += (0.05 * (LevitationAmplifier + 1) - vy) * 0.2;
            }
            else
            {
                vy -= GetEffectiveGravity();
            }

            // Apply drag/friction
            if (CreativeFlying)
            {
                // Player.travel override: creative fly preserves horizontal from parent, damps Y
                DeltaMovement = new Vec3d(postMoveVel.X * f, vy * PhysicsConsts.FlyVerticalDamping, postMoveVel.Z * f);
            }
            else
            {
                DeltaMovement = new Vec3d(postMoveVel.X * f, vy * PhysicsConsts.DragY, postMoveVel.Z * f);
            }

            // Block speed factor (soul sand, honey, etc.)
            ApplyBlockSpeedFactor(world);
        }

        /// <summary>
        /// Water travel — LivingEntity.travelInWater(Vec3, ...)
        /// </summary>
        private void TravelInWater(World world, Vec3d input)
        {
            float slowDown = Sprinting ? PhysicsConsts.WaterSprintSlowDown : PhysicsConsts.WaterSlowDown;
            float speed = PhysicsConsts.WaterBaseSpeed;

            MoveRelative(speed, input);
            Move(world, DeltaMovement);

            Vec3d vel = DeltaMovement;

            // Climbing bump in water
            if (HorizontalCollision && OnClimbable)
                vel = new Vec3d(vel.X, PhysicsConsts.ClimbWallBump, vel.Z);

            vel = vel.Multiply(slowDown, PhysicsConsts.WaterYDamping, slowDown);

            // Gravity adjustment in water
            double gravity = GetEffectiveGravity();
            if (gravity != 0.0)
            {
                double adjustedY = vel.Y;
                bool falling = vel.Y <= 0.0;
                if (falling && Math.Abs(vel.Y - 0.005) >= PhysicsConsts.VerticalVelocityThreshold)
                {
                    adjustedY -= gravity / 16.0;
                }

                if (!OnGround)
                    adjustedY -= gravity / 16.0;

                vel = new Vec3d(vel.X, adjustedY, vel.Z);
            }

            DeltaMovement = vel;
        }

        /// <summary>
        /// Lava travel — LivingEntity.travelInLava(Vec3, ...)
        /// </summary>
        private void TravelInLava(World world, Vec3d input)
        {
            MoveRelative(PhysicsConsts.LavaSpeed, input);
            Move(world, DeltaMovement);

            double gravity = GetEffectiveGravity();
            Vec3d vel = DeltaMovement;
            vel = vel.Multiply(PhysicsConsts.LavaHorizontalDamping, PhysicsConsts.LavaVerticalDamping, PhysicsConsts.LavaHorizontalDamping);

            if (gravity != 0.0)
            {
                vel = vel.Add(0, -gravity / 4.0, 0);
            }

            DeltaMovement = vel;
        }

        /// <summary>
        /// Add input vector rotated by yaw to deltaMovement.
        /// Equivalent to Entity.moveRelative(float, Vec3) + getInputVector().
        /// </summary>
        private void MoveRelative(float speed, Vec3d input)
        {
            Vec3d rotated = GetInputVector(input, speed, Yaw);
            DeltaMovement = DeltaMovement.Add(rotated);
        }

        /// <summary>
        /// Rotate input by yaw and scale by speed. Equivalent to Entity.getInputVector(), with the game's sine table
        /// (client 4.2.11: before, the exact sine, a few millionths of a block off the game's every tick).
        /// </summary>
        internal static Vec3d GetInputVector(Vec3d input, float speed, float yaw)
        {
            double lenSqr = input.LengthSqr();
            if (lenSqr < 1.0E-7)
                return Vec3d.Zero;

            Vec3d scaled = (lenSqr > 1.0 ? input.Normalize() : input).Scale(speed);
            float yawRad = VanillaMath.ToRadians(yaw);
            float sinYaw = VanillaMath.Sin(yawRad);
            float cosYaw = VanillaMath.Cos(yawRad);

            return new Vec3d(
                scaled.X * cosYaw - scaled.Z * sinYaw,
                scaled.Y,
                scaled.Z * cosYaw + scaled.X * sinYaw);
        }

        /// <summary>
        /// Execute movement with collision detection.
        /// Equivalent to Entity.move(MoverType.SELF, delta).
        /// </summary>
        private void Move(World world, Vec3d movement)
        {
            if (StuckSpeedMultiplier.LengthSqr() > 1.0E-7)
            {
                movement = movement.Multiply(StuckSpeedMultiplier);
                StuckSpeedMultiplier = Vec3d.Zero;
                DeltaMovement = Vec3d.Zero;
            }

            // Sneak edge back-off
            movement = MaybeBackOffFromEdge(world, movement);

            Aabb box = GetBoundingBox();
            Vec3d resolved = CollisionDetector.Collide(world, box, movement, OnGround, PhysicsConsts.StepHeight);

            double resolvedLenSqr = resolved.LengthSqr();
            if (resolvedLenSqr > 1.0E-7 || movement.LengthSqr() - resolvedLenSqr < 1.0E-7)
            {
                // Fall distance reset via trace (simplified: reset on hitting ground)
                if (FallDistance != 0.0 && resolvedLenSqr >= 1.0)
                {
                    // Simplified: just check vertical collision
                }

                Position = Position.Add(resolved);
            }

            // Collision flags
            bool blockedX = !MthEqual(movement.X, resolved.X);
            bool blockedZ = !MthEqual(movement.Z, resolved.Z);
            HorizontalCollision = blockedX || blockedZ;
            MinorHorizontalCollision = HorizontalCollision && IsHorizontalCollisionMinor(Yaw, Xxa, Zza, resolved);
            VerticalCollision = movement.Y != resolved.Y;
            VerticalCollisionBelow = VerticalCollision && movement.Y < 0.0;
            OnGround = VerticalCollisionBelow;

            // Fall distance tracking
            if (OnGround)
                FallDistance = 0;
            else if (resolved.Y < 0)
                FallDistance -= resolved.Y;

            // Zero velocity on blocked axes
            if (HorizontalCollision)
            {
                DeltaMovement = new Vec3d(
                    blockedX ? 0 : DeltaMovement.X,
                    DeltaMovement.Y,
                    blockedZ ? 0 : DeltaMovement.Z);
            }

            if (VerticalCollision)
            {
                // Slime block bounce would go here; for now just zero Y
                DeltaMovement = new Vec3d(DeltaMovement.X, 0, DeltaMovement.Z);
            }
        }

        /// <summary>
        /// Sneak edge detection: prevent walking off edges while sneaking.
        /// Equivalent to Player.maybeBackOffFromEdge(Vec3, MoverType) from 1.21.2: with the sneak key down, not flying,
        /// not moving up and above ground, each horizontal part of the move shrinks by 0.05 until the player could no longer
        /// fall a step height there. Client 4.2.11: before, the client only did it on the ground and tested its whole box
        /// moved a block down, which counts a block beside the player as ground; the game (and Grim, which replays it)
        /// tests the slab under the feet, so the client walked off edges the game stops at, or stopped where it walks on.
        /// </summary>
        private Vec3d MaybeBackOffFromEdge(World world, Vec3d movement)
        {
            float maxDownStep = PhysicsConsts.StepHeight;
            if (CreativeFlying || movement.Y > 0.0 || !Sneaking || !IsAboveGround(world, maxDownStep))
                return movement;

            double dx = movement.X;
            double dz = movement.Z;
            double stepX = Math.Sign(dx) * 0.05;
            double stepZ = Math.Sign(dz) * 0.05;

            while (dx != 0.0 && CanFallAtLeast(world, dx, 0.0, maxDownStep))
            {
                if (Math.Abs(dx) <= 0.05)
                {
                    dx = 0.0;
                    break;
                }
                dx -= stepX;
            }

            while (dz != 0.0 && CanFallAtLeast(world, 0.0, dz, maxDownStep))
            {
                if (Math.Abs(dz) <= 0.05)
                {
                    dz = 0.0;
                    break;
                }
                dz -= stepZ;
            }

            while (dx != 0.0 && dz != 0.0 && CanFallAtLeast(world, dx, dz, maxDownStep))
            {
                dx = Math.Abs(dx) <= 0.05 ? 0.0 : dx - stepX;
                dz = Math.Abs(dz) <= 0.05 ? 0.0 : dz - stepZ;
            }

            return new Vec3d(dx, movement.Y, dz);
        }

        /// <summary>Player.isAboveGround: on the ground, or a fall shorter than a step with ground within the rest of it.</summary>
        private bool IsAboveGround(World world, float maxDownStep)
        {
            return OnGround || (FallDistance < maxDownStep && !CanFallAtLeast(world, 0.0, 0.0, maxDownStep - FallDistance));
        }

        /// <summary>
        /// Player.canFallAtLeast: nothing to stand on in the slab under the feet, moved by (dx, dz), down to the given
        /// height; the slab is pulled in by 1.0E-7 on every side but the top, so a block only touching it does not count.
        /// </summary>
        private bool CanFallAtLeast(World world, double dx, double dz, double minHeight)
        {
            Aabb box = GetBoundingBox();
            return CollisionDetector.NoCollision(world, new Aabb(
                box.MinX + 1.0E-7 + dx, box.MinY - minHeight - 1.0E-7, box.MinZ + 1.0E-7 + dz,
                box.MaxX - 1.0E-7 + dx, box.MinY, box.MaxZ - 1.0E-7 + dz));
        }

        /// <summary>
        /// Clamp velocity for climbable blocks.
        /// Equivalent to LivingEntity.handleOnClimbable(Vec3).
        /// </summary>
        private void HandleOnClimbable()
        {
            if (!OnClimbable) return;

            FallDistance = 0;
            double vx = Math.Clamp(DeltaMovement.X, -PhysicsConsts.ClimbMaxSpeed, PhysicsConsts.ClimbMaxSpeed);
            double vz = Math.Clamp(DeltaMovement.Z, -PhysicsConsts.ClimbMaxSpeed, PhysicsConsts.ClimbMaxSpeed);
            double vy = Math.Max(DeltaMovement.Y, -PhysicsConsts.ClimbMaxSpeed);

            // Sneaking on ladder prevents sliding down
            if (vy < 0.0 && Sneaking)
                vy = 0.0;

            DeltaMovement = new Vec3d(vx, vy, vz);
        }

        /// <summary>
        /// Get effective gravity considering slow falling effect.
        /// </summary>
        private double GetEffectiveGravity()
        {
            double gravity = PhysicsConsts.DefaultGravity;
            if (HasSlowFalling && DeltaMovement.Y <= 0.0)
                return Math.Min(gravity, PhysicsConsts.SlowFallingCap);
            return gravity;
        }

        /// <summary>
        /// Get speed based on friction: ground uses attribute speed * 0.216/(f^3), air uses 0.02.
        /// Equivalent to LivingEntity.getFrictionInfluencedSpeed(float).
        /// </summary>
        private float GetFrictionInfluencedSpeed(float friction)
        {
            if (OnGround)
            {
                return MovementSpeed * (PhysicsConsts.GroundAccelerationFactor / (friction * friction * friction));
            }
            else
            {
                // Player.getFlyingSpeed: a sprinting player steers a little more in the air
                return CreativeFlying ? MovementSpeed * 0.1f : Sprinting ? 0.025999999f : PhysicsConsts.AirAcceleration;
            }
        }

        /// <summary>
        /// Get the friction of the block below the player's feet.
        /// </summary>
        private float GetBlockFriction(World world)
        {
            Location belowFeet = new(Position.X, Position.Y - 0.5000010, Position.Z);
            Material mat = world.GetBlock(belowFeet).Type;
            return GetMaterialFriction(mat);
        }

        /// <summary>
        /// Apply block speed factor (soul sand, honey, etc.)
        /// Equivalent to Entity.getBlockSpeedFactor().
        /// </summary>
        private void ApplyBlockSpeedFactor(World world)
        {
            Location atFeet = new(Position.X, Position.Y, Position.Z);
            Material mat = world.GetBlock(atFeet).Type;
            float factor = GetMaterialSpeedFactor(mat);

            if (factor == 1.0f)
            {
                Location belowFeet = new(Position.X, Position.Y - 0.5000010, Position.Z);
                mat = world.GetBlock(belowFeet).Type;
                factor = GetMaterialSpeedFactor(mat);
            }

            if (factor != 1.0f)
            {
                DeltaMovement = DeltaMovement.Multiply(factor, 1.0, factor);
            }
        }

        /// <summary>
        /// Get friction value for a material. Default 0.6, special blocks differ.
        /// </summary>
        public static float GetMaterialFriction(Material mat)
        {
            return mat switch
            {
                Material.Ice or Material.PackedIce => PhysicsConsts.IceFriction,
                Material.BlueIce => PhysicsConsts.BlueIceFriction,
                Material.SlimeBlock => PhysicsConsts.SlimeBlockFriction,
                Material.FrostedIce => PhysicsConsts.IceFriction,
                _ => PhysicsConsts.DefaultBlockFriction
            };
        }

        /// <summary>
        /// Get speed factor for a material.
        /// </summary>
        public static float GetMaterialSpeedFactor(Material mat)
        {
            return mat switch
            {
                Material.SoulSand or Material.SoulSoil => PhysicsConsts.SoulSandSpeedFactor,
                Material.HoneyBlock => PhysicsConsts.HoneySpeedFactor,
                _ => PhysicsConsts.DefaultSpeedFactor
            };
        }

        /// <summary>
        /// Update environmental state flags (in water, in lava, on climbable, etc.)
        /// Call before each Tick().
        /// </summary>
        public void UpdateEnvironment(World world)
        {
            Location feetLoc = new(Position.X, Position.Y, Position.Z);
            Location headLoc = new(Position.X, Position.Y + PlayerHeight * 0.5, Position.Z);

            Material feetBlock = world.GetBlock(feetLoc).Type;
            Material headBlock = world.GetBlock(headLoc).Type;

            InWater = feetBlock == Material.Water || headBlock == Material.Water
                      || feetBlock == Material.BubbleColumn;
            InLava = feetBlock == Material.Lava || headBlock == Material.Lava;
            OnClimbable = feetBlock.CanBeClimbedOn();

            // LocalPlayer.aiStep: whether the player could stand or crouch where it is (the crouching slowdown)
            canStand = CanFit(world, false);
            canCrouch = CanFit(world, true);
        }

        /// <summary>
        /// Set position from server teleport / initial spawn.
        /// </summary>
        public void SetPosition(double x, double y, double z)
        {
            Position = new Vec3d(x, y, z);
        }

        /// <summary>
        /// A new player (a join or a respawn, as the game makes a new LocalPlayer): at rest, standing, not sprinting, with
        /// what it keeps of the last one's attributes (the server sends only those that change afterwards).
        /// </summary>
        public void ResetPlayer(AttributesKept attributesKept)
        {
            DeltaMovement = Vec3d.Zero;
            FallDistance = 0;
            Sneaking = Crouching = CrouchPose = false;
            SetSprinting(false);
            HorizontalCollision = MinorHorizontalCollision = false;
            canStand = canCrouch = true;
            MovementSpeedAttribute.ForNewPlayer(attributesKept);
            SneakingSpeedAttribute.ForNewPlayer(attributesKept);
        }

        /// <summary>
        /// Set position and reset velocity (for teleports).
        /// </summary>
        public void Teleport(double x, double y, double z)
        {
            Position = new Vec3d(x, y, z);
            DeltaMovement = Vec3d.Zero;
            FallDistance = 0;
        }

        private static bool MthEqual(double a, double b)
        {
            return Math.Abs(a - b) < 1.0E-5;
        }
    }
}
