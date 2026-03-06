using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SpellBox : MonoBehaviour, IPointerClickHandler
{
    private SpellEditBar spellEditBar;
    private GameObject spellIcon = null;
    
    private int slotId;

    void Start() 
    {
        spellEditBar = transform.parent.gameObject.GetComponent<SpellEditBar>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        spellEditBar.SpellBoxClicked(slotId);
    }

    public void Init(int slotId)
    {
        this.slotId = slotId;
    }

    public void SetSpellIcon(GameObject spellIconPrefab)
    {
        if(spellIcon != null)
        {
            Destroy(spellIcon);
            spellIcon = null;
        }

        spellIcon = Instantiate(spellIconPrefab, transform.position, transform.rotation);
        spellIcon.transform.SetParent(transform);
    }
}
