namespace MinecraftClient.Physics
{
    /// <summary>
    /// Represents movement input state, equivalent to vanilla ClientInput / KeyboardInput.
    /// </summary>
    public class MovementInput
    {
        public bool Forward;
        public bool Back;
        public bool Left;
        public bool Right;
        public bool Jump;
        public bool Sneak;
        public bool Sprint;

        /// <summary>
        /// The keys as KeyboardInput reads them (calculateImpulse): -1, 0 or 1 sideways (left positive) and forward;
        /// PlayerPhysics turns them into the walk's input as the player's version does.
        /// </summary>
        public (float Left, float Forward) GetMoveImpulse()
        {
            return (Impulse(Left, Right), Impulse(Forward, Back));
        }

        private static float Impulse(bool positive, bool negative)
        {
            return positive == negative ? 0.0f : positive ? 1.0f : -1.0f;
        }

        public void Reset()
        {
            Forward = false;
            Back = false;
            Left = false;
            Right = false;
            Jump = false;
            Sneak = false;
            Sprint = false;
        }
    }
}
