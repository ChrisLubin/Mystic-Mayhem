using TMPro;
using UnityEngine;

public class CanvasDebugController : StaticInstance<CanvasDebugController>
{
    [SerializeField] private TextMeshProUGUI _debugText;
    [SerializeField] private TextMeshProUGUI _debugCounter;

    private string _text;
    private int _counter = 0;

    private void Start()
    {
        if (!Debug.isDebugBuild)
            this.gameObject.SetActive(false);
    }

    public void IncrementCounter()
    {
        this._counter += 1;
        this.UpdateCanvas();
    }

    public void DecrementCounter()
    {
        this._counter -= 1;
        this.UpdateCanvas();
    }

    public void ResetCounter()
    {
        this._counter = 0;
        this.UpdateCanvas();
    }

    public void SetText(string text)
    {
        this._text = text;
        this.UpdateCanvas();
    }

    private void UpdateCanvas()
    {
        this._debugText.text = this._text;
        this._debugCounter.text = this._counter.ToString();
    }
}
