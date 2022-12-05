using UnityEngine;
using TMPro;

public class CameraController : MonoBehaviour
{
    public Camera camera;

    public Canvas canvas;
    public TMP_InputField querySWCIdx;
    public TMP_InputField queryEndPtIdx;

    public SWCObjManager SWCObjManager;

    [SerializeField, SetProperty("dist2Focus")]
    private float _dist2Focus = 100.0f;
    public float dist2Focus
    {
        get { return _dist2Focus; }
        set
        {
            _dist2Focus = value;
            updateCamera();
        }
    }

    private Vector3 _focusPos;
    private Vector3 focusPos
    {
        get { return _focusPos; }
        set
        {
            _focusPos = value;
            updateCamera();
        }
    }

    private void updateCamera()
    {
        camera.transform.position = focusPos
                - dist2Focus * camera.transform.forward;
    }

    private void Awake()
    {
        _focusPos = new();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (SWCObjManager.swcs.Count != 0)
            focusPos = SWCObjManager.swcs[0].somaPos;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void FocusOn()
    {
        int swcIdx, endPtIdx;
        try
        {
            swcIdx = System.Convert.ToInt32(querySWCIdx.text);
            endPtIdx = System.Convert.ToInt32(queryEndPtIdx.text);
        }
        catch
        {
            return;
        }
        if (swcIdx >= SWCObjManager.swcs.Count)
            return;
        if (endPtIdx >= SWCObjManager.swcs[swcIdx].positions.Count)
            return;
        
        focusPos = SWCObjManager.swcs[swcIdx].positions[endPtIdx];
        SWCObjManager.PlaySelectAnime(swcIdx, endPtIdx);
        canvas.enabled = false;
    }
}
