using UnityEngine;

//this script goes on the gold coin prefab, which the player can pickup
[RequireComponent(typeof(Collider2D))] //have Trigger set Yes
public class GoldCoin : MonoBehaviour
{
    [Header("Gold Amount")]
    [SerializeField] private int amount = 1;

    public int Amount => amount;
}
