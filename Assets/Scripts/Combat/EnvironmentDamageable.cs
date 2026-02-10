using UnityEngine;
public class EnvironmentDamageable : MonoBehaviour, IDamageable
{
    public void TakeDamage(float amount)
    {
        /*
         * Walls don't take damage!
         * 
         * Put this script on walls or any environment object that the Red Note should explode against
         */
    }
}
