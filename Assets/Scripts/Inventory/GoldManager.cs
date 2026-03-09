using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }
    public event Action<int> OnGoldChanged;

    private int currGold = 0;
    public int CurrentGold => currGold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddMoreGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        currGold += amount;
        OnGoldChanged?.Invoke(currGold);
    }

    //will not allow purchase to be made if player does not have enough gold collected; makes the purchase if player can afford
    public bool MakePurchase(int amount)
    {
        if (currGold < amount || amount <= 0)
        {
            return false;
        }

        currGold -= amount;
        OnGoldChanged?.Invoke(currGold);
        return true;
    }

    //reset the gold after run reset
    public void ResetGold()
    {
        currGold = 0;
        OnGoldChanged?.Invoke(currGold);
    }
}
