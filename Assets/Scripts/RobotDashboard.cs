using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RobotDashboard : MonoBehaviour
{
    private Transform buttonsContainer;

    [Tooltip("Stores all the button GameObjects found under the 'Buttons' child.")]
    public GameObject[] dashboardButtons;

    private void Start()
    {
        // Find the "Buttons" child object automatically
        //buttonsContainer = transform.Find("Buttons");
        buttonsContainer = this.transform;

        if (buttonsContainer == null)
        {
            Debug.LogWarning("RobotDashboard: Could not find a child named 'Buttons'. Please ensure one exists under RobotDashboard.");
            return;
        }

        // Initialize a list to hold the buttons temporarily
        List<GameObject> buttonList = new List<GameObject>();

        // Iterate through all children of the buttons container
        foreach (Transform child in buttonsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                buttonList.Add(btn.gameObject);

                // Register OnClick listener
                btn.onClick.AddListener(() => OnButtonClicked(btn.gameObject.name));

                // Dynamically add hover listeners using EventTrigger
                AddHoverListeners(btn.gameObject);
            }
        }

        // Store the buttons in the array
        dashboardButtons = buttonList.ToArray();
    }

    private void OnButtonClicked(string buttonName)
    {
        Debug.Log($"Hell World.");
        
        // NOTE: You can easily modify this script slightly to include extra functionality 
        // per button by checking the buttonName:
        /*
        if (buttonName == "StartButton") { ... }
        else if (buttonName == "StopButton") { ... }
        */
    }

    private void AddHoverListeners(GameObject targetObj)
    {
        // Add an EventTrigger component if the button doesn't already have one
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetObj.AddComponent<EventTrigger>();
        }

        // Setup PointerEnter (Hover start)
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnHoverEnter(targetObj.name); });
        trigger.triggers.Add(pointerEnterEntry);

        // Setup PointerExit (Hover end)
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnHoverExit(targetObj.name); });
        trigger.triggers.Add(pointerExitEntry);
    }

    private void OnHoverEnter(string buttonName)
    {
        Debug.Log($"Hovered over button: {buttonName}");
    }

    private void OnHoverExit(string buttonName)
    {
        Debug.Log($"Stopped hovering button: {buttonName}");
    }

    public void test()
    {
        Debug.Log("Hello");
    }
}
