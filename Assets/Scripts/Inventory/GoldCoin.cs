using UnityEngine;

//this script goes on the gold coin prefab, which the player can pickup
[RequireComponent(typeof(Collider2D))] //have Trigger set Yes
public class GoldCoin : MonoBehaviour
{
    private const int amount = 1;
    public int Amount => amount;
}
