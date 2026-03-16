using UnityEngine;
using UnityEngine.EventSystems;

public class FocusHandler : MonoBehaviour
{
    public GameObject objectToFocus = null;
    public void GiveFocus()
    {
        EventSystem.current.SetSelectedGameObject(objectToFocus);
    }
}
