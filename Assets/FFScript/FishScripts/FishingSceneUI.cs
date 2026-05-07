using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingSceneUI : MonoBehaviour
{
    public GameObject LeftRight;
    public GameObject SpaceBar;
    public FishBiteHook fishBiteHook;
    private NewGameInputManager inputManager;
    // Start is called before the first frame update
    void Start()
    {
        inputManager = NewGameInputManager.EnsureInstance();
    }

    // Update is called once per frame
    void Update()
    {
       if (fishBiteHook.isFishBite)
        {
            LeftRight.SetActive(false);
            SpaceBar.SetActive(false);
        }
        else
        {
            if (IsPressingSpace())
            {
                LeftRight.SetActive(true);
                SpaceBar.SetActive(false);
            }
            else {
                SpaceBar.SetActive(true);
                LeftRight.SetActive(false);
            }
        }
    }

    private bool IsPressingSpace()
    {
        return Input.GetKey(KeyCode.Space)
            || (inputManager != null && inputManager.IsHeld(GameInputAction.PressSpace));
    }
}
