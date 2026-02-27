using UnityEngine;

namespace Gameplay.PowerUps
{
    public interface ICannonPowerUp
    {
        [Tooltip( "Unique ID, e.g. 'rapid_fire_1'")]
        string Id { get; }          // Unique ID, e.g. "rapid_fire_1"
        
        [ Tooltip( "Display name, e.g. 'Rapid Fire'" )]
        string DisplayName { get; }
        
        [ Tooltip( "Description of the ability" )]
        string Description { get; }
        
        [ Tooltip( "Ability icon" )]
        Sprite Icon { get; }

        void Apply(GameObject cannon);
    }
}