using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Performs AABB collision detection against the block world.
    /// Mirrors Entity.collide(), collideBoundingBox(), collideWithShapes() from vanilla MC.
    /// </summary>
    public static class CollisionDetector
    {
        private static readonly int[] YxzAxisOrder = { 1, 0, 2 };
        private static readonly int[] YzxAxisOrder = { 1, 2, 0 };

        /// <summary>
        /// Resolve movement with full collision detection including step-up.
        /// This is the main entry point, equivalent to Entity.collide(Vec3).
        /// </summary>
        public static Vec3d Collide(World world, Aabb entityBox, Vec3d movement, bool onGround, float maxUpStep)
        {
            return Collide(searchBox => CollectBlockColliders(world, searchBox), entityBox, movement, onGround, maxUpStep);
        }

        /// <summary>
        /// Entity.collide with the block shapes coming from <paramref name="collectColliders"/> (the world's, or a test's).
        /// A move blocked sideways while on the ground, or landing, tries the step heights the shapes around offer, lowest
        /// first, and keeps the first that goes further sideways (Entity.collectCandidateStepUpHeights).
        /// </summary>
        internal static Vec3d Collide(Func<Aabb, List<Aabb>> collectColliders, Aabb entityBox, Vec3d movement,
            bool onGround, float maxUpStep)
        {
            if (movement.LengthSqr() == 0.0)
                return movement;

            Vec3d resolved = CollideWithShapes(movement, entityBox, collectColliders(entityBox.ExpandTowards(movement)));

            bool blockedX = movement.X != resolved.X;
            bool blockedZ = movement.Z != resolved.Z;
            bool blockedY = movement.Y != resolved.Y;
            bool onGroundAfterCollision = blockedY && movement.Y < 0.0;

            if (maxUpStep > 0.0f && (onGroundAfterCollision || onGround) && (blockedX || blockedZ))
            {
                Aabb groundedBox = onGroundAfterCollision ? entityBox.Move(0, resolved.Y, 0) : entityBox;
                Aabb stepUpBox = groundedBox.ExpandTowards(movement.X, maxUpStep, movement.Z);
                if (!onGroundAfterCollision)
                    stepUpBox = stepUpBox.ExpandTowards(0, -1.0E-5f, 0);

                List<Aabb> stepColliders = collectColliders(stepUpBox);
                float stepHeightToSkip = (float)resolved.Y;

                foreach (float stepHeight in CollectCandidateStepUpHeights(groundedBox, stepColliders, maxUpStep, stepHeightToSkip))
                {
                    Vec3d stepFromGround = CollideWithShapes(new Vec3d(movement.X, stepHeight, movement.Z), groundedBox, stepColliders);
                    if (stepFromGround.HorizontalDistanceSqr() > resolved.HorizontalDistanceSqr())
                        return stepFromGround.Subtract(0, entityBox.MinY - groundedBox.MinY, 0);
                }
            }

            return resolved;
        }

        /// <summary>
        /// Collide movement against a list of shapes using axis-separated resolution.
        /// Matches Entity.collideWithShapes() in the order Direction.axisStepOrder gives.
        /// </summary>
        internal static Vec3d CollideWithShapes(Vec3d movement, Aabb entityBox, List<Aabb> colliders)
        {
            if (colliders.Count == 0)
                return movement;

            Vec3d accumulated = Vec3d.Zero;

            foreach (int axis in GetAxisStepOrder(movement))
            {
                double dist = movement.Get(axis);
                if (dist == 0.0) continue;

                double resolved = CollideAxis(axis, entityBox.Move(accumulated), colliders, dist);
                accumulated = accumulated.With(axis, resolved);
            }

            return accumulated;
        }

        /// <summary>
        /// The order the axes of a move are resolved in, as Direction.axisStepOrder gives it: always Y first, then the
        /// horizontal axis the move goes further along (Z when |x| &lt; |z|, else X), then the other. Client 4.2.11:
        /// before, the client sorted the axes by distance, so a move slower sideways than it falls (sneaking, the first
        /// steps, a step-up try) went sideways first: off an edge a tick early, or not up a slab the game climbs.
        /// </summary>
        internal static int[] GetAxisStepOrder(Vec3d movement)
        {
            return Math.Abs(movement.X) < Math.Abs(movement.Z) ? YzxAxisOrder : YxzAxisOrder;
        }

        /// <summary>
        /// Collide along a single axis against all block shapes.
        /// Equivalent to Shapes.collide(axis, box, shapes, distance).
        /// </summary>
        private static double CollideAxis(int axis, Aabb entityBox, List<Aabb> colliders, double movement)
        {
            foreach (var collider in colliders)
            {
                if (Math.Abs(movement) < PhysicsConsts.CollisionEpsilon)
                    return 0.0;
                movement = entityBox.Collide(axis, collider, movement);
            }
            return movement;
        }

        /// <summary>
        /// Collect the collision boxes of every block whose shape overlaps the given search area: all of a block's boxes
        /// when any of them does, as the game takes a block's whole shape (BlockCollisions, Shapes.joinIsNotEmpty).
        /// </summary>
        public static List<Aabb> CollectBlockColliders(World world, Aabb searchBox)
        {
            var result = new List<Aabb>();

            int minBX = (int)Math.Floor(searchBox.MinX - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBX = (int)Math.Floor(searchBox.MaxX + PhysicsConsts.CollisionEpsilon) + 1;
            int minBY = (int)Math.Floor(searchBox.MinY - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBY = (int)Math.Floor(searchBox.MaxY + PhysicsConsts.CollisionEpsilon) + 1;
            int minBZ = (int)Math.Floor(searchBox.MinZ - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBZ = (int)Math.Floor(searchBox.MaxZ + PhysicsConsts.CollisionEpsilon) + 1;

            for (int bx = minBX; bx <= maxBX; bx++)
            {
                for (int bz = minBZ; bz <= maxBZ; bz++)
                {
                    for (int by = minBY; by <= maxBY; by++)
                    {
                        Block block = world.GetBlock(new Location(bx, by, bz));
                        Aabb[] shapes = BlockShapes.GetShapes(block);
                        AddShapeIfOverlapping(result, shapes, bx, by, bz, searchBox);
                    }
                }
            }

            return result;
        }

        /// <summary>A block's boxes, moved to its position, when any of them overlaps the search area.</summary>
        internal static void AddShapeIfOverlapping(List<Aabb> result, Aabb[] shapes, int bx, int by, int bz, Aabb searchBox)
        {
            foreach (var shape in shapes)
            {
                if (!shape.Move(bx, by, bz).Intersects(searchBox))
                    continue;

                foreach (var part in shapes)
                    result.Add(part.Move(bx, by, bz));
                return;
            }
        }

        /// <summary>
        /// The step heights a move may climb, as Entity.collectCandidateStepUpHeights finds them: every bottom and top
        /// of the shapes around, measured from the box's feet, from 0 up to the step height, without the height the move
        /// already ended at, lowest first; none means no step. Client 4.2.11: before, only the tops above the move's end
        /// counted, and with none the client still tried the full step height.
        /// </summary>
        internal static float[] CollectCandidateStepUpHeights(Aabb box, List<Aabb> colliders, float maxStepHeight,
            float stepHeightToSkip)
        {
            var heights = new SortedSet<float>();

            foreach (var collider in colliders)
            {
                AddCandidateStepUpHeight(heights, (float)(collider.MinY - box.MinY), maxStepHeight, stepHeightToSkip);
                AddCandidateStepUpHeight(heights, (float)(collider.MaxY - box.MinY), maxStepHeight, stepHeightToSkip);
            }

            var result = new float[heights.Count];
            heights.CopyTo(result);
            return result;
        }

        private static void AddCandidateStepUpHeight(SortedSet<float> heights, float height, float maxStepHeight,
            float stepHeightToSkip)
        {
            if (!(height < 0.0f) && height != stepHeightToSkip && !(height > maxStepHeight))
                heights.Add(height);
        }

        /// <summary>
        /// Check if a position is on ground by testing for vertical collision below.
        /// </summary>
        public static bool IsOnGround(World world, Aabb entityBox)
        {
            Aabb testBox = entityBox.ExpandTowards(0, -0.06, 0);
            return CollectBlockColliders(world, testBox).Count > 0;
        }

        /// <summary>
        /// Check if a given position has no collision (for checking if player fits somewhere).
        /// </summary>
        public static bool NoCollision(World world, Aabb entityBox)
        {
            return CollectBlockColliders(world, entityBox).Count == 0;
        }
    }
}
