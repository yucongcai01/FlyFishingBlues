using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
   public void CutFish() { 
        SceneManager.LoadScene("CuttingFish");
    }
    public void CutEel()
    {
        SceneManager.LoadScene("CuttingDianMan");
    }
    public void LoadKillFishScene()
    {
        SceneManager.LoadScene("KillFishScene");
    }
    public void DelayLoad()
    {
        Invoke("FishFree", 1.5f);
    }
    public void FishFree()
    {

        SceneManager.LoadScene("LandScene");
    }
    public void LoadSharkScene()
    {
        SceneManager.LoadScene("SharkScene");
    }
}
