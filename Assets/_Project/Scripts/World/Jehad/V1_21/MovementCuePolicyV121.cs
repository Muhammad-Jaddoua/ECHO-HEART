using System;

namespace Echoheart.World
{
    public enum MovementCueV121 { None, Jump, Land }

    /// <summary>Presentation heuristic for sampled CharacterController motion, not an input/jump authority.</summary>
    public sealed class MovementCuePolicyV121
    {
        public const float JumpRise = .5f;
        public const float MinimumAirTime = .12f;
        public const float MeaningfulFallSpeed = -3f;
        private bool _ready, _grounded, _jumped;
        private float _airTime, _lowestSpeed;

        public void Reset()
        {
            _ready = _grounded = _jumped = false;
            _airTime = _lowestSpeed = 0f;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public MovementCueV121 Step(bool grounded, float verticalSpeed, float seconds, bool playing, bool discontinuity)
        {
            if (discontinuity || !Finite(verticalSpeed)) { Reset(); return MovementCueV121.None; }
            // Pause preserves the phase and does not accumulate unscaled time.
            if (!playing || !Finite(seconds) || seconds <= 0f) return MovementCueV121.None;
            if (!_ready)
            {
                if (grounded) { _ready = true; _grounded = true; }
                return MovementCueV121.None;
            }
            MovementCueV121 result = MovementCueV121.None;
            if (!grounded)
            {
                if (_grounded)
                {
                    _airTime = _lowestSpeed = 0f;
                    _jumped = verticalSpeed > JumpRise;
                    if (_jumped) result = MovementCueV121.Jump;
                }
                _airTime = Math.Min(10f, _airTime + Math.Min(10f, seconds));
                _lowestSpeed = Math.Min(_lowestSpeed, verticalSpeed);
            }
            else if (!_grounded)
            {
                _airTime = Math.Min(10f, _airTime + Math.Min(10f, seconds));
                if (_airTime >= MinimumAirTime && (_jumped || _lowestSpeed <= MeaningfulFallSpeed))
                    result = MovementCueV121.Land;
                _airTime = _lowestSpeed = 0f;
                _jumped = false;
            }
            _grounded = grounded;
            return result;
        }
    }
}
