using UnityEngine;
using UnityEngine.UI;

public class cooldownHandler : MonoBehaviour
{
    private float cooldownDuration;
    private float cooldownTimer;

    [Header("UI References")]
    [SerializeField] private Image overlayImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        overlayImage.fillAmount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            UpdateCD(); 
        }
    }

    // Start the cooldown
    public void StartCooldown(float cdDuration)
    {
        cooldownDuration = cdDuration;
        cooldownTimer = cooldownDuration;
    }

    private void UpdateCD()
    {
        if (overlayImage != null)
        {
            // Calculate the fill amount based on the cooldown timer
            float fillAmount = 1 - (cooldownTimer / cooldownDuration);
            overlayImage.fillAmount = fillAmount;
        }

        if (cooldownTimer > 0)
        {
            overlayImage.color = Color.gray;
        }
        else
        {
            overlayImage.color = Color.clear;
        }
    }
}
