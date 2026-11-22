using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The inventory screen: opened/closed with the inventory button (arcade button 3, I, gamepad Y).
// Shows Narcissus wearing what he has taken so far (m094305's "possessions" idea),
// the list of those possessions, and the weapon in hand.
public class InventoryView : MonoBehaviour
{
    public static bool Exists { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Image portrait;
    public TMPro.TextMeshProUGUI possessionsText;
    public TMPro.TextMeshProUGUI weaponText;

    [Header("Narcissus by possessions")]
    public Sprite nothing;
    public Sprite scarf;
    public Sprite cross;
    public Sprite rings;
    public Sprite crossScarf;
    public Sprite crossRings;
    public Sprite ringsScarf;
    public Sprite all;

    PlayerInventory inventory;
    DialogueDirector.Item shownItems = (DialogueDirector.Item)(-1);
    int shownCollected = -1;
    PlayerInventory.WeaponType shownWeapon = (PlayerInventory.WeaponType)(-1);

    void Awake()
    {
        Exists = true;
        // Use the same PlayerInventory the buttons toggle. (The Player has one on itself AND one
        // on its "Inventory" child; ArcadeButtons picks the first via GetComponentInChildren.)
        ArcadeButtons buttons = FindAnyObjectByType<ArcadeButtons>(FindObjectsInactive.Include);
        inventory = buttons != null
            ? buttons.GetComponentInChildren<PlayerInventory>(true)
            : FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        if (panel != null) panel.SetActive(false);
    }

    void OnDestroy()
    {
        Exists = false;
    }

    void Update()
    {
        bool open = inventory != null && inventory.IsOpen && !DialogueManager.AnyDialogueActive;
        if (panel != null && panel.activeSelf != open)
            panel.SetActive(open);
        if (!open) return;

        DialogueDirector.Item items = DialogueDirector.Stolen;
        if (items != shownItems || Collectible.Collected.Count != shownCollected)
        {
            shownItems = items;
            shownCollected = Collectible.Collected.Count;
            if (portrait != null) portrait.sprite = SpriteFor(items);
            if (possessionsText != null) possessionsText.text = Describe(items);
        }

        if (inventory.Equipped != shownWeapon)
        {
            shownWeapon = inventory.Equipped;
            if (weaponText != null)
                weaponText.text = "In hand: " + (shownWeapon == PlayerInventory.WeaponType.None
                    ? "nothing"
                    : shownWeapon == PlayerInventory.WeaponType.Melee ? "a knife" : "a stone");
        }
    }

    Sprite SpriteFor(DialogueDirector.Item i)
    {
        switch (i)
        {
            case DialogueDirector.Item.Scarf: return scarf;
            case DialogueDirector.Item.Cross: return cross;
            case DialogueDirector.Item.Rings: return rings;
            case DialogueDirector.Item.Cross | DialogueDirector.Item.Scarf: return crossScarf;
            case DialogueDirector.Item.Cross | DialogueDirector.Item.Rings: return crossRings;
            case DialogueDirector.Item.Rings | DialogueDirector.Item.Scarf: return ringsScarf;
            case DialogueDirector.Item.Scarf | DialogueDirector.Item.Cross | DialogueDirector.Item.Rings: return all;
            default: return nothing;
        }
    }

    static string Describe(DialogueDirector.Item i)
    {
        var lines = new List<string>();
        if ((i & DialogueDirector.Item.Scarf) != 0) lines.Add("- Purple silk, from the merchant");
        if ((i & DialogueDirector.Item.Cross) != 0) lines.Add("- Golden cross, from the knight");
        if ((i & DialogueDirector.Item.Rings) != 0) lines.Add("- Rings, from the old woman");
        lines.AddRange(Collectible.Collected);
        return lines.Count == 0
            ? "Nothing yet worthy of Me."
            : string.Join("\n", lines);
    }
}
