using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkupManager : MonoBehaviour
{
    [Tooltip("The container which has markup, used for easing export")]
    public GameObject markupContainer;

    // Markup control
    private bool doMarkup = false;
    private bool currentlyDrawing = false;
    private Texture2D texture;
    private Sprite blankSprite;

    // Mouse positon
    private Vector3 mousePos = new Vector3(0f,0f,0f);

    public void ToggleMarkup()
    {
        doMarkup = !doMarkup;
    }

    void Start()
    {
        texture = new Texture2D(Screen.width, Screen.height);
        blankSprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
        markupContainer.GetComponent<UnityEngine.UI.Image>().sprite = blankSprite;


        Color invisible = new Color(0f,0f,0f,1f);
        // Initialize the texture as being blank
        for (int i = 0; i < Screen.width; i++)
        {
            for (int k = 0; k < Screen.height; k++)
            {
                texture.SetPixel(i, k, invisible);
            }
        }

        // "Apply" our new texture to the object
        texture.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        // If markup state is toggled and the mouse is down, draw at the mouse
        if (doMarkup && Input.GetKeyDown(KeyCode.Mouse0))
        {
            mousePos = Input.mousePosition;
            currentlyDrawing = true;
        }
        else if (doMarkup && Input.GetKeyUp(KeyCode.Mouse0))
        {
            currentlyDrawing = false;
        }

        // Draw at cursor
        if (doMarkup && currentlyDrawing)
        {
            // Only cause a change and draw if the mouse has moved; not necessarily every frame
            if (Vector3.Distance(mousePos, Input.mousePosition) >= 1)
            {
                // The mouse is likely to be moved at a greater rate than the update function executes, so
                // interpolation between previous and current points should be used when drawing
                // To do this in a hacky and poor way for now (see bezier curves as a good implementation example)
                // can just calculate the slope between two points and use that when performing steps
                // "i < granularity" determines the granularity; 100 is likely excessive but does not seem too slow
                Vector3 interPos;
                int granularity = 100;
                for (int i = 0; i < granularity; i++)
                {
                    interPos = Vector3.Lerp(mousePos, Input.mousePosition, ((float)i)/granularity);
                    texture.SetPixel((int)interPos.x, (int)interPos.y, Color.red);
                }
                mousePos = Input.mousePosition;
                texture.Apply(); // Maybe inefficient to call this every update?
            }
        }
    }
}
