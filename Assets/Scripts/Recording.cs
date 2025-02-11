using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

// Recording storage data structure
// Does not include audio - that is loaded on-demand to avoid large memory usage
public class Recording : MonoBehaviour
{
    public AnimationClip clip = null;
    public Text text;
    public ListController listController;

    // When open button clicked, open the selected animation
    public void open()
    {
        // Load on demand, rather than when the list is loaded
        if (!clip)
        {
            AnimationClip newClip = AnimationLoader.LoadExistingClip(ListController.shareAvatar, text.text);
            newClip.name = text.text;

            // Have to update the list, as it contains the main copy
            ListController.savedList.Find(item => item.GetComponent<Recording>().text.text == text.text).GetComponent<Recording>().clip = newClip;

            this.clip = newClip;
            Debug.Log(this.clip.name);
        }

        // Shift this item to the front of the saved list so that it is the most recently accessed
        GameObject newest = ListController.savedList.Find(item => item.GetComponent<Recording>().text.text == text.text);
        ListController.savedList.Remove(newest);
        ListController.savedList.Insert(0, newest);

        MocapPlayerOurs.recordedClip = this.clip;
        MocapPlayerOurs.existingRecording = true;
        SceneManager.LoadScene("ViewingPage");
    }
    
    // When the delete button is pressed, remove all references to the selected animation
    public void delete()
    {
        // Attempt to delete the associated .anim file for this recording
        File.Delete(Path.Combine(Application.streamingAssetsPath, text.text + ".anim"));
        try
        {
            File.Delete(Path.Combine(Application.streamingAssetsPath, text.text + ".audio"));
        }
        catch
        {
            Debug.Log("No associated audio for this animation to delete.");
        }

        ListController.savedList.Remove(ListController.savedList.Find(item => item.GetComponent<Recording>().text.text == text.text));
        Destroy(this.gameObject);
    }
}
