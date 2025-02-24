using UnityEngine;
using UnityEngine.UI;

public class FlagIndicator : MonoBehaviour
{
    Image image;
    Sprite blueFlag;
    Sprite redFlag;
    int localPlayerTeam;

    void Start()
    {
        image  = GetComponent<Image>();
        blueFlag = Resources.Load<Sprite>("Sprites/BlueFlag");
        redFlag = Resources.Load<Sprite>("Sprites/RedFlag");
    }

    public void SetLocalPlayerTeam(int localPlayerTeam)
    {
        this.localPlayerTeam = localPlayerTeam;
    }

    // Set flag colour based on local player team and flag team
    public void SetColour(int flagTeam)
    {
        image.sprite = localPlayerTeam == flagTeam ? blueFlag : redFlag;
    }
}
