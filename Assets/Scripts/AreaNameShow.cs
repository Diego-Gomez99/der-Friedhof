using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AreaNameShow : MonoBehaviour
{
    public TextMeshProUGUI areaNameText;
    public string areaName;
    private Coroutine displayCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Update the TextMeshPro Text with the area name.
            areaNameText.text = "" + areaName;

            // If a display coroutine is running, stop it.
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }

            // Start a coroutine to clear the text after 5 seconds.
            //displayCoroutine = StartCoroutine(ClearAreaNameAfterDelay(5f));

        }
    }
    private IEnumerator ClearAreaNameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (areaNameText.text == areaName)
        {
            areaNameText.text = ""; // Clear the TextMeshPro Text.
        }
    }
}
