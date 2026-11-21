
using UnityEngine;

public class NPCPossesions : MonoBehaviour
{
    [Header("NPCs")]
    public GuardHealth baba;
    public GuardHealth vendor;
    public GuardHealth knight;

    [Header("Possession Images")]
    public GameObject scarf;
    public GameObject rings;
    public GameObject cross;
    public GameObject scarfRings;
    public GameObject scarfCross;
    public GameObject ringsCross;
    public GameObject all;

    private int lastState = -1;

    public bool HasEverything =>
        baba != null &&
        vendor != null &&
        knight != null &&
        baba.isDead &&
        vendor.isDead &&
        knight.isDead;

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        int state = 0;

        if (baba != null && baba.isDead)
            state |= 1;

        if (vendor != null && vendor.isDead)
            state |= 2;

        if (knight != null && knight.isDead)
            state |= 4;

        if (state == lastState)
            return;

        lastState = state;

        SetVisible(scarf, state == 1);
        SetVisible(rings, state == 2);
        SetVisible(cross, state == 4);

        SetVisible(scarfRings, state == 3);
        SetVisible(scarfCross, state == 5);
        SetVisible(ringsCross, state == 6);

        SetVisible(all, state == 7);
    }

    void SetVisible(GameObject obj, bool visible)
    {
        if (obj != null && obj.activeSelf != visible)
            obj.SetActive(visible);
    }
}