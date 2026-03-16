using UnityEngine;
using TMPro;

public class HeightSliderScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string prefix = "";

    public void UpdateValue(float value)
    {
        if (label != null)
        {
            label.text = prefix + value.ToString("F0");
            GameManager.Instance.settingsHeight = (int) value;
        }
    }
}