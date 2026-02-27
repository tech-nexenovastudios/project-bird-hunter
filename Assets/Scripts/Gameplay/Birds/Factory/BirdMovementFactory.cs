namespace Gameplay.Birds
{
    public static class BirdMovementFactory
    {
        public static IBirdMovementStrategy Create(BirdMovementType type)
        {
            switch (type)
            {
                case BirdMovementType.LeftRightMove:
                    return new LeftRightMovement();

                case BirdMovementType.ZigZagMove:
                    return new ZigZagMovement();

                case BirdMovementType.TargetMove:
                    return new TargetMovement();

                case BirdMovementType.CurvePathMove:
                    return new CurvePathMovement();

                case BirdMovementType.DigonalMove:
                    return new DiagonalMovement();

                case BirdMovementType.NormalMove:
                default:
                    return new NormalMovement();
            }
        }
    }
}