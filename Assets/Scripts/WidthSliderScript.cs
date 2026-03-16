using UnityEngine;
using TMPro;

public class WidthSliderScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string prefix = "";

    public void UpdateValue(float value)
    {
        if (label != null)
        {
            label.text = prefix + value.ToString("F0");
            GameManager.Instance.settingsWidth = (int) value;
        }
    }
}