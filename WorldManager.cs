
using UnityEngine;
using LastSeed.Domain;

public class WorldManager : MonoBehaviour
{
    [SerializeField] private Renderer groundA1;   
    [SerializeField] private Transform plantVisual;  
    [SerializeField] private GameObject winPanel;    
    [SerializeField] private GameObject gateToA2;    

    private static readonly Color Dead = new Color(0.45f, 0.45f, 0.45f);
    private static readonly Color Fertile = new Color(0.25f, 0.65f, 0.3f);

    private void Start()
    {
        GameManager.Instance.StateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= Refresh;
    }

    private void Refresh()
    {
        var s = GameManager.Instance.State;

        if (groundA1 != null)
            groundA1.material.color = s.PS1.Soil == SoilState.Fertile ? Fertile : Dead;

        if (plantVisual != null)
        {
            float scale = s.PS1.Stage == PlantStage.None ? 0f
                        : s.PS1.Stage == PlantStage.Seed ? 0.25f
                        : s.PS1.Stage == PlantStage.Sprout ? 0.6f : 1f;
            plantVisual.localScale = Vector3.one * scale;
        }

        if (winPanel != null) winPanel.SetActive(s.Status == GameStatus.AreaRestored);
        if (gateToA2 != null) gateToA2.SetActive(s.Status != GameStatus.AreaRestored);
    }
}
