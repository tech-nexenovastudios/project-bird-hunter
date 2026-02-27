// This interface is used to trigger specific death conditions. 
// For example, if you want to play a specific particle effect upon death, you can use this interface.

using UnityEngine;

public interface IDeath
{
    public void OnDeath();
}
