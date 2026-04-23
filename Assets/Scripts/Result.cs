using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RaceResult;

public class Result : MonoBehaviour
{

    public TextMeshPro firstPlaceName;
    public TextMeshPro firstPlaceTime;
    public TextMeshPro secondPlaceName;
    public TextMeshPro secondPlaceTime;
    public TextMeshPro thirdPlaceName;
    public TextMeshPro thirdPlaceTime;


    public Character firstPlaceCharacter;
    public Character secondPlaceCharacter;
    public Character thirdPlaceCharacter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateResults();
    }

    // Update is called once per frame
    void Update()
    {
        

    }

    public void UpdateResults()
    {
        var results = RaceResult.RaceResultData.FinalResults;
        firstPlaceName.text = results.Count > 0 ? results[0].Name : "N/A";
        firstPlaceTime.text = results.Count > 0 ? results[0].FinishTime.ToString("F2") : "N/A";
        SetupCharacter(firstPlaceCharacter, results[0]);


        secondPlaceName.text = results.Count > 1 ? results[1].Name : "N/A";
        secondPlaceTime.text = results.Count > 1 ? results[1].FinishTime.ToString("F2") : "N/A";
        SetupCharacter(secondPlaceCharacter, results[1]);


        thirdPlaceName.text = results.Count > 2 ? results[2].Name : "N/A";
        thirdPlaceTime.text = results.Count > 2 ? results[2].FinishTime.ToString("F2") : "N/A";
        SetupCharacter(thirdPlaceCharacter, results[2]);
    }

    private void SetupCharacter(Character charModel, RunnerResult data)
    {
        if (charModel == null) return;

        if (data.HasCharacter)
        {

            charModel.SetHair(data.HairIndex);
            charModel.SetFace(data.FaceIndex);
            charModel.SetCloth(data.ClothIndex);
            charModel.SetHairColor(data.HairColor);
        }
        else
        {
            SpriteRenderer sr = charModel.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = data.FallbackColor;
            }
        }
    }
    public void Restart()
    {
        SceneManager.LoadScene("Game");
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("Title");
    }
}
